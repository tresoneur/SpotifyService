# SpotifyService

## Status of this project

This project is currently under development, and breaking changes are expected to be introduced frequently.

## The goal of this project

To create a high-level Spotify API for FOSS Blazor WebAssembly projects, providing services such as Spotify playback in the browser, managing user authentication, access to the Spotify Web API, IndexedDB caching and more.

## Areas currently implemented

* __Authentication & authorization__: OAuth 2.0
    
    * __Implicit grant flow__: authenticate without any backend involvement: everything is managed in the client by _SpotifyService_. Users will have to re-authorize your app every hour.
    
    * __Authorization code flow__: configure and deploy the ASP.NET Core _SpotifyAuthServer_ in just a few minutes. Users will only have to authorize your Blazor webapp once, _SpotifyService_ and the auth server will take care of the rest.

* __Playback__: in the browser, using the Spotify Web Playback SDK.

* __Web API__: a high-level wrapper around a WebAssembly-compatible [fork](https://github.com/tresoneur/SpotifyAPI-NET) of _JohnnyCrazy_'s [_SpotifyAPI-NET_](https://github.com/JohnnyCrazy/SpotifyAPI-NET).

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
        * Extension methods for displaying _SpotifyAPI-NET.Model_ entities with ease and in a human-readable format.

## Demo

Most of _SpotifyService_'s functionality was originally implemented for use in [_Cærostris_](https://github.com/tresoneur/Caerostris), a Blazor WebAssembly Spotify client. The latest version of _Cærostris_ can be accessed [here](https://caerostris.azurewebsites.net/).

## Requirements

Your application should use Blazor WebAssembly version '3.2.0 Release Candidate' or higher.

## How to use

* (For now --) Build the [WebAssembly-compatible fork of _SpotifyAPI-NET_](https://github.com/tresoneur/SpotifyAPI-NET) and place the resulting dll where `SpotifyService.csproj` expects it to be. (TODO)

* Include [_SpotifyService_](https://github.com/tresoneur/SpotifyService) in your solution, and run a `dotnet restore`. (TODO)

* Include the lines marked with '`<--`' in your `Program.cs`:

```cs
using Caerostris.Services.Spotify;                      // <--

// ...

public static async Task Main(string[] args)
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("app");

    var services = builder.Services;

    // ... 

    // If you include the argument, the Authorization Code workflow will be used.
    // (Use https!)
    // If no URI is passed, the Implicit Grant workflow will be used instead.
    services.AddSpotify("<your auth server's uri>");    // <--

    var host = builder.Build();

    // Learn more about permissions in the example below.
    await host.Services.InitializeSpotify(              // <--
        "<your app's name>",                            // <--
        "<your app's client ID>",                       // <--
        <permissions>);                                 // <--

    await host.RunAsync();
}
```

* Include the js files needed for _SpotifyService_'s functionality in your `index.html`:

```
<script src="_content/SpotifyService/blazor.extensions.storage.js"></script>
<script src="_content/SpotifyService.IndexedDB/indexedDb.Blazor.js"></script>
<script src="_content/SpotifyService/spotifyservice-web-playback.js"></script>
<script src="https://sdk.scdn.co/spotify-player.js"></script>
```

* For tips on how to use _SpotifyService_ in your Blazor components, see the [Examples section](#examples) below.

## Design considerations

* _SpotifyService_ publishes several events, including events for the following:

    * the state of app authorization changed;
    
    * an UI update is needed or advised;
    
    * loading progress updates;

    * the playback context changed.

* _SpotifyService_ provides stateful services (caching, automatic track relinking, etc.), and uses the singleton dependency injection mode.

* All methods, properties and events are directly accessible on the injected instance.

* _SpotifyService_ includes some properties that are meant to store your application's relevant state, e.g. `SearchQuery` and `ExploreArtistUrl`.

## Examples

### Initializing your SpotifyService instance (in `Program.cs`):

```cs
public class Program
{
    // Issued by Spotify, register your app and view its ID at
    //  https://developer.spotify.com/dashboard/
    private const string clientId = "87b0c14e92bc4958b1b6fe15259d2577";

    // All permissions SpotifyService currently uses
    private const Scope permissions = Scope.UserReadPrivate
                                    | Scope.UserReadEmail
                                    | Scope.UserReadPlaybackState
                                    | Scope.UserModifyPlaybackState
                                    | Scope.UserLibraryRead
                                    | Scope.UserReadCurrentlyPlaying
                                    | Scope.PlaylistReadPrivate
                                    | Scope.PlaylistReadCollaborative
                                    | Scope.PlaylistModifyPrivate
                                    | Scope.PlaylistModifyPublic
                                    | Scope.UserLibraryModify
                                    | Scope.Streaming;

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("app");

        var services = builder.Services;

        services.AddSpotify("https://api.example.com/auth");

        var host = builder.Build();

        await host.Services.InitializeSpotify(
            "Blazor WebAssembly App", 
            clientId, 
            permissions);

        await host.RunAsync();
    }
}
```

### Fetching all of the user's saved tracks:

```razor
@inject SpotifyService Spotify

@code
{
    private IEnumerable<SavedTrack>? tracks;

    protected override async Task OnInitializedAsync()
    {
        if (await Spotify.IsAuthGranted())
            tracks = await Spotify.GetSavedTracks();
    }
}
```