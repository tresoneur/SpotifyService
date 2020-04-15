using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Player.Models;
using SpotifyAPI.Web.Models;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private WebPlaybackSDKManager player;

        private string localDeviceId = "";
        private bool isPlaybackLocal = false;


        private async Task InitializePlayer(WebPlaybackSDKManager injectedPlayer)
        {
            player = injectedPlayer;

            await player.Initialize(
                GetAuthToken,
                OnError,
                OnPlaybackChanged,
                OnLocalPlayerReady);

            PlaybackChanged += OnDevicePotentiallyChanged;
        }

        /// <remarks>
        /// Currently unused, updating the WebAPI context based on the WebPlaybackState turned out to be the wrong approach.
        /// </remarks>
        private void OnPlaybackChanged(WebPlaybackState? state) { }

        private async Task OnLocalPlayerReady(string deviceId)
        {
            localDeviceId = deviceId;
            await TransferPlayback(deviceId);
            isPlaybackLocal = true;
            Log("Playback automatically transferred to local device.");
        }

        private async Task OnDevicePotentiallyChanged(PlaybackContext playback)
        {
            if (!(playback?.Device?.Id is null))
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
    }
}
