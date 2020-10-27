using Caerostris.Services.Spotify.Web.Extensions;
using Caerostris.Services.Spotify.Web.ViewModels;
using Caerostris.Services.Spotify.Web.Extensions;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// <summary>
        /// The authoritative PlaybackContext.
        /// </summary>
        private CurrentlyPlayingContext? lastKnownPlayback;
        private DateTime? lastKnownPlaybackTimestamp;

        /// <summary>
        /// Fires when a new PlaybackContext is received from the Spotify API. A <seealso cref="PlaybackDisplayUpdate"/> event is also fired afterwards.
        /// </summary>
        public event Func<CurrentlyPlayingContext, Task>? PlaybackChanged;
        private Timer playbackContextPollingTimer;

        /// <summary>
        /// Fires when the display of the PlaybackContext needs to be updated with the supplied arguments.
        /// This does not necessarily mean that a new PlaybackContext was acquired. 
        /// Suggested use: refresh the UI periodically.
        /// </summary>
        public event Action<int>? PlaybackDisplayUpdate;
        private Timer playbackUpdateTimer;

        private Timer playbackKeepAliveTimer;

        private async Task InitializePlayback()
        {
            playbackContextPollingTimer = new(
                callback: async _ => { if (await IsAuthGranted()) FirePlaybackContextChanged(await GetPlayback()); },
                state: null,
                dueTime: 0,
                period: 1000);

            playbackUpdateTimer = new(
                callback: async _ => { if (await IsAuthGranted()) PlaybackDisplayUpdate?.Invoke(GetProgressMs()); },
                state: null,
                dueTime: 0,
                period: 33);


            int playbackKeepAliveTimerPeriod = Convert.ToInt32(TimeSpan.FromMinutes(1).TotalMilliseconds);

            playbackKeepAliveTimer = new(
                callback: async _ => { if (await IsAuthGranted()) await KeepPlaybackAlive(); },
                state: null,
                dueTime: playbackKeepAliveTimerPeriod,
                period: playbackKeepAliveTimerPeriod);

            await EnsureActiveDeviceIsAvailable();
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

        public async Task Previous() =>
            await DoPlaybackOperation(dispatcher.SkipPlaybackToPrevious, player.Previous, startsPlayback: true);

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
            FirePlaybackContextChanged(await dispatcher.GetPlayback());
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

        private void FirePlaybackContextChanged(CurrentlyPlayingContext? playback)
        {
            var timestamp = DateTime.UtcNow;

            FireIfContextChanged(lastKnownPlayback, playback);

            if (playback is null)
                return;

            lastKnownPlayback = playback;
            lastKnownPlaybackTimestamp = timestamp; // The timestamp returned by the Spotify API is unreliable.
            PlaybackChanged?.Invoke(playback);
            PlaybackDisplayUpdate?.Invoke(GetProgressMs());
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
            if (await IsAuthGranted())
            {
                var devices = await GetDevices();
                if (devices.Any(d => d.IsActive))
                {
                    var targetDevice = devices.First();
                    await TransferPlayback(targetDevice.Id);
                    Log($"No active device found. Playback transferred to device \"{targetDevice.Name}\".");
                }
            }
        }
    }
}
