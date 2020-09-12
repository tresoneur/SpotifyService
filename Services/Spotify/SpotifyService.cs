using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using System;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using SpotifyService.Services.Spotify.Auth.Abstract;

namespace Caerostris.Services.Spotify
{
    /// <remarks>
    /// Choose the 'singleton' instantiation mode when using Dependency Injection. The internal state of the class was devised with a per-session instantiation policy in mind.
    /// </remarks>
    public sealed partial class SpotifyService : IDisposable
    {
        private readonly SpotifyWebAPI api;
        private readonly WebApiManager dispatcher;

        private (AuthManagerBase, WebPlaybackSDKManager) injected;

#pragma warning disable CS8618 // Partial constructors aren't a thing, so the initalizations of these attributes happen in the Initialize...() methods.
        public SpotifyService(SpotifyWebAPI spotifyWebApi, AuthManagerBase injectedAuthManager, WebApiManager injectedWebApiManager, WebPlaybackSDKManager injectedPlayer)
#pragma warning restore CS8618
        {
            api = spotifyWebApi;

            dispatcher = injectedWebApiManager;

            injected = (injectedAuthManager, injectedPlayer);
        }

        public async Task Initialize(string deviceName, string clientId, Scope permissionScopes)
        {
            var (injectedAuthManager, injectedPlayer) = injected;

            InitializePlayer(injectedPlayer, deviceName);
            await InitializeAuth(injectedAuthManager, clientId, permissionScopes);
            InitializePlayback();
        }

        private async Task OnError(string message)
        {
            Log($"Temporary error handler: Received error: {message}");
            await Task.CompletedTask;
        }

        private void Log(string message) =>
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
