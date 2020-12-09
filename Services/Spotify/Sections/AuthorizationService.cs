using System;
using System.Threading;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Configuration;
using System.Linq;

namespace Caerostris.Services.Spotify.Sections
{
    public sealed class AuthorizationService
    {
        private readonly WebApiManager dispatcher;
        private readonly AuthManagerBase authManager;

        private readonly SpotifyServiceConfiguration config;

        /// <summary>
        /// Fires when the auth state changes: either the current token expires or a new token is acquired.
        /// Also fires when a valid token is found in cache on startup. The SpotifyService instance won't be able to fetch any user-specific data before this happens, so it is best if any component in need of e.g. the username subscribes to this event.
        /// </summary>
        public event Action<bool>? AuthStateChanged;
        private readonly Timer authPollingTimer;
        private bool authGrantedWhenLastChecked = false;

        /// <summary>
        /// Relative URL that may be used to coordinate several components.
        /// </summary>
        public readonly string RelativeCallbackUrl = "/callback";

        public AuthorizationService(
            WebApiManager webApiManager,
            AuthManagerBase authManagerBase,
            SpotifyServiceConfiguration spotifyServiceConfiguration)
        {
            dispatcher = webApiManager;
            authManager = authManagerBase;
            config = spotifyServiceConfiguration;

            authPollingTimer = new(
                callback: async _ => { await HandleAuthStatePotentiallyChanged(); },
                state: null,
                dueTime: 1000,
                period: 1000);
        }

        internal async Task Initialize()
        {
            await HandleAuthStatePotentiallyChanged();
        }

        /// <summary>
        /// Gets an access token through the OAuth2 process. Should be used before the use of any other API functionality is attempted.
        /// The token will be saved to LocalStorage.
        /// Will reload the page (!)
        /// </summary>
        /// <remarks>
        /// The callback URI has to be added to the whitelist on the Spotify Developer Dashboard.
        /// Any permissions needed will have to be passed to <see cref="AuthManagerBase.StartProcess(string, string, Scope)"/>.
        /// </remarks>
        public async Task StartAuth(string callbackUri)
        {
            await authManager.StartProcess(
                config.ClientId,
                callbackUri,
                config.PermissionScopes.ToList()
            );
        }

        /// <summary>
        /// The OAuth2 Grant process involves a callback to an address provided by the caller of <see cref="StartAuth(string)"/> to redirect to after authentication success or failure.
        /// Invoke this function at the given address.
        /// </summary>
        /// <returns>Whether the process was successful.</returns>
        public async Task<bool> ContinueAuthOnCallback(string callbackUri)
        {
            // Internal redirection inside (forceReload: false).
            string? token = await authManager.GetTokenOnCallback(callbackUri);

            if (token is null)
                return false;

            // Authorize dispatcher, fire event(s).
            await HandleAuthStatePotentiallyChanged();

            return true;
        }

        /// <summary>
        /// Checks whether a user is logged in.
        /// </summary>
        public async Task<bool> IsUserLoggedIn()
        {
            // This call is necessary to guarantee that every part of the library is in a state where it can hande auth-only requests, e.g. the WebAPI client is authorized, etc.
            await HandleAuthStatePotentiallyChanged();
            return await IsValidAuthTokenPresent();
        }

        public async Task Logout()
        {
            await authManager.Logout();
        }

        private async Task HandleAuthStatePotentiallyChanged()
        {
            bool authGranted = (await IsValidAuthTokenPresent());

            if (authGranted && (await GetAuthToken()) is string token)
                dispatcher.Authorize(token);

            if (authGranted != authGrantedWhenLastChecked)
            {
                Console.WriteLine($"Authorization state changed to: {authGranted}");
                authGrantedWhenLastChecked = authGranted;
                AuthStateChanged?.Invoke(authGranted);
            }
        }

        internal async Task<string?> GetAuthToken() =>
            await authManager.GetToken();

        public void Dispose() =>
            authPollingTimer.Dispose();

        private async Task<bool> IsValidAuthTokenPresent() =>
            ((await GetAuthToken()) is not null);
    }
}
