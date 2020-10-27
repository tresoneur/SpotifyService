using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using System;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using SpotifyAPI.Web;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify
{
    /// <remarks>
    /// Choose the 'singleton' instantiation mode when using Dependency Injection.
    /// </remarks>
    public sealed partial class SpotifyService : IDisposable
    {
        private readonly WebApiManager dispatcher;

        #pragma warning disable CS8618 // Partial constructors aren't a thing, so the initalizations of these attributes happen in the Initialize...() methods. TODO: szétszedni
        public SpotifyService(
            AuthManagerBase authManagerBase,
            WebApiManager webApiManager,
            WebPlaybackSdkManager webPlaybackSdkManager,
            MediaSessionManager mediaSessionManager,
            IndexedDbCache<string> indexedDbCache)
        #pragma warning restore CS8618
        {
            dispatcher = webApiManager;
            authManager = authManagerBase;
            player = webPlaybackSdkManager;
            mediaSession = mediaSessionManager;
            listenLaterStore = indexedDbCache;
        }

        public async Task Initialize(string deviceName, string clientId, List<string> permissionScopes)
        {
            await InitializePlayer(deviceName);
            await InitializeAuth(clientId, permissionScopes);
            await InitializePlayback();
        }

        private async Task OnNoncriticalError(string message)
        {
            Log($"Error: {message}");
            await Task.CompletedTask;
        }

        private static void Log(string message) =>
            Console.WriteLine($"SpotifyService: {message}");

        private async Task Cleanup()
        {
            await dispatcher.Cleanup();
            await listenLaterStore.Clear(ListenLaterStoreName);
        }

        public void Dispose()
        {
            playbackContextPollingTimer.Dispose();
            playbackUpdateTimer.Dispose();
            authPollingTimer.Dispose();
        }
    }
}
