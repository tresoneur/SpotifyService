# SpotifyService

## Status of this project

This project is currently under development, and breaking changes are expected to be introduced frequently.

## The goal of this project

To create a high-level Spotify API for FOSS Blazor WebAssembly projects, providing services such as Spotify playback in the browser, managing OAuth authorization, access to the Spotify Web API, IndexedDB caching and more.

## Areas currently implemented

* __Authentication & authorization__: OAuth 2.0
    
    * __Implicit grant flow__: authenticate without any backend involvement. Users will have to re-authorize your app every hour.
    
    * __Authorization code flow__: configure and deploy the ASP.NET Core [_SpotifyAuthServer_](https://github.com/tresoneur/SpotifyAuthServer). Users will only have to authorize your Blazor webapp once, _SpotifyService_ and the supporting server will take care of the rest.

* __Playback__: in the browser, using the Spotify Web Playback SDK.

* __Web API__: a high-level wrapper around _JohnnyCrazy_'s [_SpotifyAPI-NET_](https://github.com/JohnnyCrazy/SpotifyAPI-NET).

    * __Playback__ 
        
        * Read and manage the current playback context, including the currently playing track and the state of the playback (e.g. paused or playing, shuffle and repeat status, (interpolated) progression, etc.).

        * Use automatic track relinking.

    * __Context__
        
        * Get the currently playing album, artist or playlist.

    * __User__
        
        * Get information about the current user.

    * __Library__
    
        * Get the user's saved tracks and playlists.

        * See whether a song is in the user's library.

    * __Insights__ 
        
        * Get a detailed audio analysis of each of the user's saved tracks.

    * __Extensions__
        * Extension methods for displaying _SpotifyAPI-NET.Web.Model_ entities with ease and in a human-readable format.

## Demo

Most of _SpotifyService_'s functionality was originally implemented for use in [_Cærostris_](https://github.com/tresoneur/Caerostris), a Blazor WebAssembly Spotify client. The latest version of _Cærostris_ can be accessed [here](https://caerostris.azurewebsites.net/).

## Requirements

Your application should use .NET 5.0.0 or higher.

## How to use

* Include the SpotifyService project in your solution and run `dotnet restore`.

* Include the lines marked with '`<--`' in your `Program.cs`:

    ```cs
    using Caerostris.Services.Spotify;                      // <--

    // ...

    public class Program
    {
        public static async Task Main(string[] args)
            {
                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                builder.RootComponents.Add<App>("#app");

                builder.Services
                    .AddSpotify(new() // <-- 
                    {
                        // If you supply a non-null value, the Authorization Code Grant workflow will be used.
                        // (Use https!)
                        // Otherwise, the Implicit Grant workflow will be used instead.
                        AuthServerApiBase = "https://caerostrisauthserver.azurewebsites.net/auth",
                        PlayerDeviceName = "Your Spotify player device name here",
                        // Issued by Spotify, register your app and view its ID at
                        // https://developer.spotify.com/dashboard/
                        ClientId = "0123456789abcdef0123456789abcdef",
                        // All permissions SpotifyService currently uses
                        PermissionScopes = new[]
                        {
                            "user-read-private",
                            "user-read-email",
                            "user-read-playback-state",
                            "user-modify-playback-state",
                            "user-library-read",
                            "user-library-modify",
                            "user-read-currently-playing",
                            "playlist-read-private",
                            "playlist-read-collaborative",
                            "playlist-modify-private",
                            "playlist-modify-public",
                            "streaming"
                        }
                    });

                var host = builder.Build();

                await host.Services.InitializeSpotify(); // <--
            
                await host.RunAsync();
            }
        }
    }
    ```

* Include the JavaScript and mock audio files needed for _SpotifyService_'s functionality in your `index.html`:

    ```html
    <audio id="mediasession-mock-audio" src="_content/Caerostris.Services.Spotify/media/mediasession-mock-audio.mp3" autoplay loop></audio>
    <script src="_content/Caerostris.Services.Spotify/blazor.extensions.storage.js"></script>
    <script src="_content/Caerostris.Services.Spotify.IndexedDB/indexedDb.Blazor.js"></script>
    <script src="_content/Caerostris.Services.Spotify/spotifyservice-web-playback.js"></script>
    <script src="https://sdk.scdn.co/spotify-player.js"></script>
    ```

* See some examples for using _SpotifyService_ in your Blazor components in the [Examples section](#examples) below.

## Design considerations

* _SpotifyService_ publishes several events, including:

    * Spotify authorization events;
    
    * UI update suggestions;
    
    * loading progress updates;

    * playback context changes.

* _SpotifyService_ provides stateful services (caching, automatic track relinking, etc.), and uses the singleton dependency injection mode.

## Examples

### Fetching all of the user's saved tracks:

```razor
@inject SpotifyService Spotify

@code
{
    private IEnumerable<SavedTrack>? tracks;

    protected override async Task OnInitializedAsync()
    {
        if (await Spotify.Auth.IsUserLoggedIn())
            tracks = await Spotify.Library.GetSavedTracks();
    }
}
```