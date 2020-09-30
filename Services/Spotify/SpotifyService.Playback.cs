using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// <summary>
        /// The authoritative PlaybackContext.
        /// </summary>
        private PlaybackContext? lastKnownPlayback;
        private DateTime? lastKnownPlaybackTimestamp;

        /// <summary>
        /// Fires when a new PlaybackContext is received from the Spotify API. A <seealso cref="PlaybackDisplayUpdate"/> event is also fired afterwards.
        /// </summary>
        public event Func<PlaybackContext, Task>? PlaybackChanged;
        private Timer playbackContextPollingTimer;

        /// <summary>
        /// Fires when the display of the PlaybackContext needs to be updated with the supplied arguments.
        /// This does not necessarily mean that a new PlaybackContext was acquired. 
        /// Suggested use: refresh the UI periodically.
        /// </summary>
        public event Action<int>? PlaybackDisplayUpdate;
        private Timer playbackUpdateTimer;

        private void InitializePlayback()
        {
            playbackContextPollingTimer = new System.Threading.Timer(
                callback: async _ => { if (await IsAuthGranted()) FirePlaybackContextChanged(await GetPlayback()); },
                state: null,
                dueTime: 0,
                period: 1000
            );

            playbackUpdateTimer = new System.Threading.Timer(
                callback: _ => { PlaybackDisplayUpdate?.Invoke(GetProgressMs()); },
                state: null,
                dueTime: 0,
                period: 33
            );
        }

        /// <summary>
        /// The current state of playback, as reported by the Spotify Web API.
        /// Use with care: for regular updates, subscribe to <seealso cref="PlaybackChanged"/> instead.
        /// </summary>
        public async Task<PlaybackContext?> GetPlayback() =>
            await dispatcher.GetPlayback();

        public async Task<AvailabeDevices> GetDevices() =>
            await dispatcher.GetDevices();

        public async Task TransferPlayback(string deviceId) =>
            await dispatcher.TransferPlayback(deviceId, play: lastKnownPlayback?.IsPlaying ?? false);

        public async Task Play() =>
            await DoPlaybackOperation(player.Play, dispatcher.ResumePlayback);

        public async Task PlayTrack(string? contextUri, string? trackUri) =>
            await DoRemotePlaybackOperation(async () => await dispatcher.SetPlayback(contextUri, trackUri));

        public async Task PlayTracks(IEnumerable<string> uris) =>
            await DoRemotePlaybackOperation(async () => await dispatcher.SetPlayback(uris));

        public async Task Pause() =>
            await DoPlaybackOperation(player.Pause, dispatcher.PausePlayback);

        public async Task Next() =>
            await DoPlaybackOperation(player.Next, dispatcher.SkipPlaybackToNext);

        public async Task Previous() =>
            await DoPlaybackOperation(player.Previous, dispatcher.SkipPlaybackToPrevious);

        public async Task Seek(int positionMs) =>
            await DoPlaybackOperation(
                async () => await player.Seek(positionMs),
                async () => await dispatcher.SeekPlayback(positionMs));

        public async Task SetShuffle(bool shuffle)
        {
            if (lastKnownPlayback is not null)
                lastKnownPlayback.ShuffleState = shuffle;

            await DoRemotePlaybackOperation(async () => await dispatcher.SetShuffle(shuffle));
        }

        public async Task SetRepeat(RepeatState state)
        {
            if (lastKnownPlayback is not null)
                lastKnownPlayback.RepeatState = state;

            await DoRemotePlaybackOperation(async () => await dispatcher.SetRepeatMode(state));
        }

        public async Task SetVolume(int volumePercent)
        {
            if (lastKnownPlayback?.Device is not null)
                lastKnownPlayback.Device.VolumePercent = volumePercent;

            await DoPlaybackOperation(
                async () => await player.SetVolume(volumePercent),
                async () => await dispatcher.SetVolume(volumePercent));
        }

        private async Task DoPlaybackOperation(Func<Task> local, Func<Task> remote)
        {
            if (isPlaybackLocal)
                await DoLocalPlaybackOperation(local);
            else
                await DoRemotePlaybackOperation(remote);
        }

        /// Previously, the PlaybackContext was instead updated with information reported by the Spotify Web Playback SDK, which proved to be unreliable.
        private async Task DoLocalPlaybackOperation(Func<Task> action) =>
            await DoRemotePlaybackOperation(action);

        private async Task DoRemotePlaybackOperation(Func<Task> action)
        {
            await action();
            await Task.Delay(200); // The Spotify Web API sends incorrect results when queried too soon after a playback operation.
            FirePlaybackContextChanged(await dispatcher.GetPlayback());
        }

        public int GetProgressMs()
        {
            if (lastKnownPlayback?.Item is null || lastKnownPlaybackTimestamp is null)
                return 0;

            var extraProgressIfPlaying = DateTime.UtcNow - lastKnownPlaybackTimestamp.Value;
            long totalProgressIfPlaying = Convert.ToInt64(extraProgressIfPlaying.TotalMilliseconds) + lastKnownPlayback.ProgressMs;
            try
            {
                int progressIfPlayingSane = Convert.ToInt32(totalProgressIfPlaying);
                var bestGuess = ((lastKnownPlayback.IsPlaying) ? progressIfPlayingSane : lastKnownPlayback.ProgressMs);
                var totalDurationMs = lastKnownPlayback.Item.DurationMs;

                return ((bestGuess > totalDurationMs) ? totalDurationMs : bestGuess);
            }
            catch (OverflowException)
            {
                return 0;
            }
        }

        private void FirePlaybackContextChanged(PlaybackContext? playback)
        {
            FireIfContextChanged(lastKnownPlayback, playback);

            if (playback is null)
                return;

            lastKnownPlayback = playback;
            lastKnownPlaybackTimestamp = DateTime.UtcNow.AddMilliseconds(-50); // TODO: don't cheat
            PlaybackChanged?.Invoke(playback);
            PlaybackDisplayUpdate?.Invoke(GetProgressMs());
        }
    }
}
