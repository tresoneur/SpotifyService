using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using System;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Web.CachedDataProviders;

namespace Caerostris.Services.Spotify
{
    /// <remarks>
    /// Choose the 'singleton' instantiation mode when using Dependency Injection.
    /// </remarks>
    public sealed partial class SpotifyService : IDisposable
    {
        private readonly SpotifyWebAPI api;
        private readonly WebApiManager dispatcher;

        #pragma warning disable CS8618 // Partial constructors aren't a thing, so the initalizations of these attributes happen in the Initialize...() methods. TODO: szétszedni
        public SpotifyService(
            SpotifyWebAPI spotifyWebApi, 
            AuthManagerBase authManagerBase, 
            WebApiManager webApiManager, 
            WebPlaybackSdkManager webPlaybackSdkManager, 
            IndexedDbCache<string> indexedDbCache)
        #pragma warning restore CS8618
        {
            api = spotifyWebApi;
            dispatcher = webApiManager;

            authManager = authManagerBase;
            player = webPlaybackSdkManager;
            listenLaterStore = indexedDbCache;
        }

        public async Task Initialize(string deviceName, string clientId, Scope permissionScopes)
        {
            InitializePlayer(deviceName);
            await InitializeAuth(clientId, permissionScopes);
            InitializePlayback();
        }

        private async Task OnError(string message)
        {
            Log($"Error: {message}"); // TODO: raise error
            await Task.CompletedTask;
        }

        private static void Log(string message) =>
            Console.WriteLine($"SpotifyService: {message}");

        public void Dispose()
        {
            playbackContextPollingTimer.Dispose();
            playbackUpdateTimer.Dispose();
            authPollingTimer.Dispose();
            api.Dispose();
        }
    }
}
