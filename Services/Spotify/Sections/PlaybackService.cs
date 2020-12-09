using Caerostris.Services.Spotify.Web.Extensions;
using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Configuration;
using Caerostris.Services.Spotify.Player;

namespace Caerostris.Services.Spotify.Sections
{
    public sealed class PlaybackService
    {
        private readonly WebApiManager dispatcher;
        private readonly WebPlaybackSdkManager player;
        private readonly MediaSessionManager mediaSession;
        private readonly AuthorizationService auth;

        private bool isPlaybackLocal = false;
        private string? localDeviceId;

        private readonly SpotifyServiceConfiguration config;

        /// <summary>
        /// The authoritative PlaybackContext.
        /// </summary>
        private CurrentlyPlayingContext? lastKnownPlayback;

        private DateTime? lastKnownPlaybackTimestamp;

        /// <summary>
        /// Fires when a new and valid PlaybackContext is received from the Spotify API. A <seealso cref="PlaybackDisplayUpdate"/> event is also fired afterwards.
        /// </summary>
        public event Func<CurrentlyPlayingContext, Task>? PlaybackChanged;

        /// <summary>
        /// Fires when a PlaybackContext is received from the Spotify API. The first parameter is the last known PlaybackContext, the second is the newly received one.
        /// </summary>
        public event Func<CurrentlyPlayingContext?, CurrentlyPlayingContext?, Task>? UnsafePlaybackChanged;

        private readonly Timer playbackContextPollingTimer;

        /// <summary>
        /// Fires when the display of the PlaybackContext needs to be updated with the supplied arguments.
        /// This does not necessarily mean that a new PlaybackContext was acquired. 
        /// Suggested use: refresh the UI periodically.
        /// </summary>
        public event Action<int>? PlaybackDisplayUpdate;

        private readonly Timer playbackUpdateTimer;

        private readonly Timer playbackKeepAliveTimer;

        public PlaybackService(
            WebApiManager webApiManager,
            WebPlaybackSdkManager webPlaybackSdkManager,
            MediaSessionManager mediaSessionManager,
            AuthorizationService spotifyServiceAuth,
            SpotifyServiceConfiguration spotifyServiceConfiguration)
        {
            dispatcher = webApiManager;
            player = webPlaybackSdkManager;
            mediaSession = mediaSessionManager;
            auth = spotifyServiceAuth;
            config = spotifyServiceConfiguration;

            PlaybackChanged += OnDevicePotentiallyChanged;
            PlaybackChanged += mediaSession.SetMetadata;
            auth.AuthStateChanged += OnReInitializationPotenitallyNeeded;

            playbackContextPollingTimer = new(
               callback: async _ => { if (await auth.IsUserLoggedIn()) await FirePlaybackContextChanged(await GetPlayback()); },
               state: null,
               dueTime: 0,
               period: 1000);

            playbackUpdateTimer = new(
                callback: async _ => { if (await auth.IsUserLoggedIn()) PlaybackDisplayUpdate?.Invoke(GetProgressMs()); },
                state: null,
                dueTime: 0,
                period: 33);

            int playbackKeepAliveTimerPeriod = Convert.ToInt32(TimeSpan.FromMinutes(1).TotalMilliseconds);

            playbackKeepAliveTimer = new(
                callback: async _ => { if (await auth.IsUserLoggedIn()) await KeepPlaybackAlive(); },
                state: null,
                dueTime: playbackKeepAliveTimerPeriod,
                period: playbackKeepAliveTimerPeriod);
        }

        internal async Task Initialize()
        {
            await EnsureActiveDeviceIsAvailable();

            await mediaSession.Initialize(Play, Pause, Previous, Next);
        }

        /// <summary>
        /// The current state of playback, as reported by the Spotify Web API.
        /// Use with care: for regular updates, subscribe to <seealso cref="PlaybackChanged"/> instead.
        /// </summary>
        public async Task<CurrentlyPlayingContext?> GetPlayback() =>
            await dispatcher.GetPlayback();

        public async Task<IEnumerable<Device>> GetDevices() =>
            await dispatcher.GetDevices();

        public async Task TransferPlayback(string deviceId) =>
            await dispatcher.TransferPlayback(deviceId, play: lastKnownPlayback?.IsPlaying ?? false);

        public async Task Play() =>
            await DoPlaybackOperation(dispatcher.ResumePlayback, player.Play, startsPlayback: true);

        public async Task PlayTrack(string? contextUri, string? trackUri) =>
            await DoPlaybackOperation(async () => await dispatcher.SetPlayback(contextUri, trackUri), startsPlayback: true);

        public async Task PlayTracks(IEnumerable<string> uris) =>
            await DoPlaybackOperation(async () => await dispatcher.SetPlayback(uris), startsPlayback: true);

        public async Task Pause() =>
            await DoPlaybackOperation(dispatcher.PausePlayback, player.Pause);

        public async Task Next() =>
            await DoPlaybackOperation(dispatcher.SkipPlaybackToNext, player.Next, startsPlayback: true);

        public async Task Previous()
        {
            if (lastKnownPlayback is not null && lastKnownPlayback.ProgressMs < 5 * 1000)
                await DoPlaybackOperation(dispatcher.SkipPlaybackToPrevious, player.Previous, startsPlayback: true);
            else
                await Seek(0);
        }

        public async Task Seek(int positionMs) =>
            await DoPlaybackOperation(
                async () => await dispatcher.SeekPlayback(positionMs),
                async () => await player.Seek(positionMs));

        public async Task SetShuffle(bool shuffle)
        {
            if (lastKnownPlayback is not null)
                lastKnownPlayback.ShuffleState = shuffle;

            await DoPlaybackOperation(async () => await dispatcher.SetShuffle(shuffle));
        }

        public async Task SetRepeat(RepeatState state)
        {
            if (lastKnownPlayback is not null)
                lastKnownPlayback.RepeatState = state.AsString();

            await DoPlaybackOperation(async () => await dispatcher.SetRepeatMode(state));
        }

        public async Task SetVolume(int volumePercent)
        {
            if (lastKnownPlayback?.Device is not null)
                lastKnownPlayback.Device.VolumePercent = volumePercent;

            await DoPlaybackOperation(
                async () => await dispatcher.SetVolume(volumePercent),
                async () => await player.SetVolume(volumePercent));
        }

        private async Task DoPlaybackOperation(Func<Task> remote, Func<Task>? local = null, bool startsPlayback = false)
        {
            if (startsPlayback)
                await mediaSession.OnUserStartedPlayback();

            if (local is not null && isPlaybackLocal)
                await local();
            else
                await remote();

            await Task.Delay(200); // The Spotify Web API returns incorrect results if the PlaybackContext is requested right after a playback operation.
            await FirePlaybackContextChanged(await dispatcher.GetPlayback());
        }

        public int GetProgressMs()
        {
            if (!lastKnownPlayback.HasValidTrackItem() || lastKnownPlaybackTimestamp is null)
                return 0;

            var extraProgressIfPlaying = DateTime.UtcNow - lastKnownPlaybackTimestamp.Value;
            long totalProgressIfPlaying = Convert.ToInt64(extraProgressIfPlaying.TotalMilliseconds) + lastKnownPlayback.ProgressMs;
            try
            {
                int progressIfPlayingSane = Convert.ToInt32(totalProgressIfPlaying);
                var bestGuess = ((lastKnownPlayback.IsPlaying) ? progressIfPlayingSane : lastKnownPlayback.ProgressMs);
                var totalDurationMs = lastKnownPlayback.ValidTrackItemOrNull()!.DurationMs;

                return Math.Min(bestGuess, totalDurationMs);
            }
            catch (OverflowException)
            {
                return 0;
            }
        }

        private async Task FirePlaybackContextChanged(CurrentlyPlayingContext? playback)
        {
            var timestamp = DateTime.UtcNow;

            if (UnsafePlaybackChanged is not null)
                await UnsafePlaybackChanged(lastKnownPlayback, playback);

            lastKnownPlayback = playback;
            lastKnownPlaybackTimestamp = timestamp; // The timestamp returned by the Spotify API is unreliable.

            if (playback is not null)
            {
                if (PlaybackChanged is not null)
                    await PlaybackChanged(playback);

                PlaybackDisplayUpdate?.Invoke(GetProgressMs());
            }
        }

        private async Task KeepPlaybackAlive()
        {
            if (lastKnownPlayback is not null 
                && lastKnownPlayback.HasValidTrackItem()
                && !lastKnownPlayback.IsPlaying)
            {
                await Seek(lastKnownPlayback.ProgressMs);
            }
        }

        private async Task EnsureActiveDeviceIsAvailable()
        {
            if (await auth.IsUserLoggedIn())
            {
                if ((await GetDevices()).FirstOrDefault(d => d.IsActive) is Device targetDevice)
                {
                    await TransferPlayback(targetDevice.Id);
                    Console.WriteLine($"No active device found. Playback transferred to device \"{targetDevice.Name}\".");
                }
            }
        }

        #region LocalPlayback

        private async Task OnLocalPlayerReady(string deviceId)
        {
            localDeviceId = deviceId;
            await TransferPlayback(deviceId);
            isPlaybackLocal = true;
            Console.WriteLine("Playback automatically transferred to local device.");
        }

        private async Task OnDevicePotentiallyChanged(CurrentlyPlayingContext playback)
        {
            if (playback.Device?.Id is not null)
            {
                bool playbackContextIndicatesLocalPlayback =
                    playback.Device.Id.Equals(localDeviceId, StringComparison.InvariantCulture);

                if (isPlaybackLocal && !playbackContextIndicatesLocalPlayback)
                {
                    isPlaybackLocal = false;
                    Console.WriteLine("Playback transferred to remote device.");
                }
                else if (!isPlaybackLocal && playbackContextIndicatesLocalPlayback)
                {
                    isPlaybackLocal = true;
                    Console.WriteLine("Playback transferred back to local device.");
                }
            }

            await Task.CompletedTask;
        }

        private async void OnReInitializationPotenitallyNeeded(bool authorized)
        {
            if (authorized)
            {
                await player.Initialize(
                    auth.GetAuthToken,
                    (e) => { Console.WriteLine(e); return Task.CompletedTask; },
                    OnLocalPlayerReady,
                    config.PlayerDeviceName);
            }
        }

        #endregion

        public void Dispose()
        {
            playbackContextPollingTimer.Dispose();
            playbackUpdateTimer.Dispose();
            playbackKeepAliveTimer.Dispose();
        }
    }
}
