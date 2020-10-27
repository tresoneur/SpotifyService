using Caerostris.Services.Spotify.Player;
using SpotifyAPI.Web;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private string localDeviceId = "";
        private bool isPlaybackLocal = false;

        private string deviceName;

        private readonly WebPlaybackSdkManager player;
        private readonly MediaSessionManager mediaSession;


        private async Task InitializePlayer(string deviceName)
        {
            this.deviceName = deviceName;

            await mediaSession.Initialize(Play, Pause, Previous, Next);

            PlaybackChanged += OnDevicePotentiallyChanged;
            PlaybackChanged += mediaSession.SetMetadata;
            AuthStateChanged += OnReInitializationPotenitallyNeeded;
        }

        private async Task OnLocalPlayerReady(string deviceId)
        {
            localDeviceId = deviceId;
            await TransferPlayback(deviceId);
            isPlaybackLocal = true;
            Log("Playback automatically transferred to local device.");
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
                    Log("Playback transferred to remote device.");
                }
                else if (!isPlaybackLocal && playbackContextIndicatesLocalPlayback)
                {
                    isPlaybackLocal = true;
                    Log("Playback transferred back to local device.");
                }
            }

            await Task.CompletedTask;
        }

        private async void OnReInitializationPotenitallyNeeded(bool authorized)
        {
            if (authorized)
            {
                await player.Initialize(
                    GetAuthToken,
                    OnNoncriticalError,
                    OnLocalPlayerReady,
                    deviceName);
            }
        }
    }
}
