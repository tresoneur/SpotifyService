using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private string localDeviceId = "";
        private bool isPlaybackLocal = false;

        private string deviceName;

        private readonly WebPlaybackSdkManager player;

        private void InitializePlayer(string deviceName)
        {
            this.deviceName = deviceName;

            PlaybackChanged += OnDevicePotentiallyChanged;
            AuthStateChanged += OnReInitializationPotenitallyNeeded;
        }

        private async Task OnLocalPlayerReady(string deviceId)
        {
            localDeviceId = deviceId;
            await TransferPlayback(deviceId);
            isPlaybackLocal = true;
            Log("Playback automatically transferred to local device.");
        }

        private async Task OnDevicePotentiallyChanged(PlaybackContext playback)
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
                    OnError,
                    OnLocalPlayerReady,
                    deviceName);
            }
        }
    }
}
