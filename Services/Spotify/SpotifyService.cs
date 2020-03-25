using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using SpotifyAPI.Web;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    /// <remarks>
    /// Choose the 'singleton' instantiation mode when using Dependency Injection. The internal state of the class was devised with a per-session instantiation policy in mind.
    /// </remarks>
    public sealed partial class SpotifyService : IDisposable
    {
        private SpotifyWebAPI api;
        private WebAPIManager dispatcher;

        private (ImplicitGrantAuthManager, WebPlaybackSDKManager) injected;

#pragma warning disable CS8618 // Partial constructors aren't a thing, so the initalizations of these attributes happen in the Initialize...() methods.
        public SpotifyService(ImplicitGrantAuthManager injectedAuthManager, WebAPIManager injectedWebAPIManager, WebPlaybackSDKManager injectedPlayer)
#pragma warning restore CS8618
        {
            api = new SpotifyWebAPI();

            dispatcher = injectedWebAPIManager;
            dispatcher.Initialize(api);

            injected = (injectedAuthManager, injectedPlayer);
        }

        public async Task Initialize()
        {
            var (injectedAuthManager, injectedPlayer) = injected;

            await InitializeAuth(injectedAuthManager);
            await InitializePlayer(injectedPlayer);
            InitializePlayback();
        }

        public async Task<string> GetUsername() =>
            (await dispatcher.GetPrivateProfile()).GetUsername();

        public async Task<string> GetUserID() =>
            (await dispatcher.GetPrivateProfile()).Id;

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
