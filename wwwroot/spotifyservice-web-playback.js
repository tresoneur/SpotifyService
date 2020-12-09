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
                        if (token === null || token === "")
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

class MediaSessionWrapper {

    constructor() {
        this.MockAudio = null;
        this.LastKnownMetadata = null;
    }

    Initialize = (dotNetMediaSessionManager) => {
        if ('mediaSession' in navigator) {

            this.MockAudio = document.getElementById("mediasession-mock-audio");

            const eventHandlerDictionary =
            {
                'play': 'OnPlay',
                'pause': 'OnPause',
                'previoustrack': 'OnPrevious',
                'nexttrack': 'OnNext',
            };
    
            for (const fn in eventHandlerDictionary)
                navigator.mediaSession.setActionHandler(fn,
                    () => {
                        if (fn === 'play')
                            this.Play();
                        else if (fn === 'pause')
                            this.Pause();

                        dotNetMediaSessionManager.invokeMethodAsync(eventHandlerDictionary[fn]);
                    });
    
            navigator.mediaSession.playbackState = "none";
        }
    }

    // Expects a `bool`, three `string?`s and a JSON(`List<Caerostris.Services.Spotify.Web.Models.Image>?`)
    SetMetadata = (playing, title, artist, album, images) => {
        if ('mediaSession' in navigator) {
            if (playing)
                this.Play();
            else
                this.Pause();

            this.SetAndSaveMetadata(new MediaMetadata({
                title: title,
                artist: artist,
                album: album,
                artwork: (images || []).map(img => ({
                    src: img.url,
                    sizes: `${img.width}x${img.height}`
                }))
            }));
        }
    }

    SetAndSaveMetadata = (metadata) => {
        navigator.mediaSession.metadata = metadata;
        this.LastKnownMetadata = metadata;
    }

    Play  = () => { this.PlayPause(true);  }

    Pause = () => { this.PlayPause(false); }

    PlayPause = (play) => {
        try {
            if (play)
                this.MockAudio.play();
            else
                this.MockAudio.pause();
        } catch (_) { /* The media element hasn't loaded yet. There's nothing to be done. */}

        this.SetAndSaveMetadata(this.LastKnownMetadata); 
    }
}

window.SpotifyService = {
    WebPlaybackSDKWrapper: new WebPlaybackSdkWrapper(),
    MediaSessionWrapper: new MediaSessionWrapper()
}

window.onSpotifyWebPlaybackSDKReady = () => { /* Pointless requirement of the SDK */ }