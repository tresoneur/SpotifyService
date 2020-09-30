/// Spotify Web Playback SDK wrapper for SpotifyService
class WebPlaybackSdkWrapper {

    constructor() {
        this.Player = null;
        this.GetOAuthToken = null;
        this.OnError = null;
        this.DeviceId = null;
        this.Name = "Uninitialized Web Player";
        this.LoggingPrefix = "SpotifyService Local Playback Device: ";
    }

    Initialize = (dotNetWebPlaybackSdkManager, name) => {
        this.Name = name;

        const dotNetMethods =
        {
            GetAuthToken: 'GetAuthToken',
            OnError: 'OnError',
            OnReady: 'OnDeviceReady'
        }

        this.OnError = (message) => {
            dotNetWebPlaybackSdkManager
                .invokeMethodAsync(dotNetMethods.OnError, message.message);
        }

        // Initialize the Playback SDK Player provided by Spotify.
        this.Player = new Spotify.Player({
            name: this.Name,
            getOAuthToken: (callback) => {
                dotNetWebPlaybackSdkManager
                    .invokeMethodAsync(dotNetMethods.GetAuthToken)
                    .then(token => {
                        if (token == null || token === "")
                            this.OnError({ message: "Empty or null token passed to Spotify Web Playback SDK wrapper" });
                        else
                            callback(token);
                    });
            },
            volume: 1.0
        });

        // Errors
        this.Player.addListener('initialization_error', this.OnError);
        this.Player.addListener('authentication_error', this.OnError);
        this.Player.addListener('account_error', this.OnError);
        this.Player.addListener('playback_error', this.OnError);

        // Controls
        this.Play = () => this.Player.resume();
        this.Pause = () => this.Player.pause();
        this.Next = () => this.Player.nextTrack();
        this.Previous = () => this.Player.previousTrack();
        this.Seek = (position) => this.Player.seek(position);
        this.SetVolume = (volume) => this.Player.setVolume(volume);

        // Info
        this.Player.addListener('ready', ({ device_id }) => {
            this.DeviceId = device_id;
            console.log(this.LoggingPrefix + 'Ready with the following ID: ' + device_id);
            dotNetWebPlaybackSdkManager
                .invokeMethodAsync(dotNetMethods.OnReady, device_id);
        });

        this.Player.addListener('not_ready', () => {
            this.OnError({ message: "Local device has gone offline. Re-run WebPlaybackSdkManager initialization." });
            console.log(this.LoggingPrefix + 'Device ID has gone offline.');
        });

        this.Player.connect();
    }
}

window.SpotifyService = new Object();
window.SpotifyService.WebPlaybackSDKWrapper = new WebPlaybackSdkWrapper();

window.onSpotifyWebPlaybackSDKReady = () => { /* Pointless requirement of the SDK */ }