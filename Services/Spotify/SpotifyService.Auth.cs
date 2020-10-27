using SpotifyAPI.Web;
using System;
using System.Threading;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Auth.Abstract;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private string clientId;

        private List<string> permissions;

        private readonly AuthManagerBase authManager;

        /// <summary>
        /// Fires when the auth state changes: either the current token expires or a new token is acquired.
        /// Also fires when a valid token is found in cache on startup. The SpotifyService instance won't be able to fetch any user-specific data before this happens, so it is best if any component in need of e.g. the username subscribes to this event.
        /// </summary>
        public event Action<bool>? AuthStateChanged;
        private Timer authPollingTimer;
        private bool authGrantedWhenLastChecked = false;

        /// <summary>
        /// Relative URL that may be used to coordinate several components.
        /// </summary>
        public readonly string RelativeCallbackUrl = "/callback";

        private async Task InitializeAuth(string clientId, List<string> permissionScopes)
        {
            this.clientId = clientId;
            permissions = permissionScopes;

            await CheckAuth();

            authPollingTimer = new(
                callback: async _ => { await CheckAuth(); },
                state: null,
                dueTime: 1000,
                period: 1000);
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
                clientId,
                callbackUri,
                permissions
            );
        }

        /// <summary>
        /// The OAuth2 Implicit Grant process involves a callback to an address provided by the caller of <see cref="StartAuth(string)"/> to redirect to after authentication success or failure.
        /// Invoke this function at the given address.
        /// </summary>
        /// <returns>Whether the process was successful</returns>
        public async Task<bool> ContinueAuthOnCallback(string callbackUri)
        {
            // Internal redirection inside (forceReload: false)
            string? token = await authManager.GetTokenOnCallback(callbackUri);

            if (token is null)
                return false;

            dispatcher.Authorize(token);

            // Fire event(s)
            await CheckAuth();

            return true;
        }

        /// <summary>
        /// Checks whether there is a valid token to use.
        /// </summary>
        public async Task<bool> IsAuthGranted()
        {
            return ((await GetAuthToken()) is not null);
        }

        public async Task Logout()
        {
            await Cleanup();
            await authManager.Logout();
        }

        private async Task CheckAuth()
        {
            string? token = await GetAuthToken();

            if (token is not null)
                dispatcher.Authorize(token);

            bool authGranted = (await IsAuthGranted());
            if (authGranted != authGrantedWhenLastChecked)
            {
                authGrantedWhenLastChecked = authGranted;
                Log($"Authorization state changed to: {authGranted}");
                AuthStateChanged?.Invoke(authGranted);
            }
        }

        private async Task<string?> GetAuthToken() =>
            await authManager.GetToken();
    }
}
