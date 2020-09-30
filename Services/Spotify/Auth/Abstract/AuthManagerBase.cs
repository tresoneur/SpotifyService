using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Blazor.Extensions.Storage.Interfaces;
using Caerostris.Services.Spotify.Auth.Models;
using Microsoft.AspNetCore.Components;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;

namespace Caerostris.Services.Spotify.Auth.Abstract
{
    /// <summary>
    /// Provides token caching and auth workflow initialization functionality for descendants.
    /// </summary>
    public abstract class AuthManagerBase
    {
        private const string ApiBase = "https://accounts.spotify.com/authorize";

        protected readonly ILocalStorage LocalStorage;
        protected readonly NavigationManager NavigationManager;

        protected Task<AuthToken?>? MemoryCachedToken;

        protected AuthManagerBase(ILocalStorage injectedLocalStorage, NavigationManager injectedNavigatorManager)
        {
            LocalStorage = injectedLocalStorage;
            NavigationManager = injectedNavigatorManager;
        }

        public abstract Task StartProcess(string clientId, string redirectUri, Scope scope);

        protected async Task StartProcess(AuthType type, string clientId, string redirectUri, Scope scope)
        {
            // Generate a state string and save it to LocalStorage to protect the client from CSRF.
            var state = new AuthWorkflow
            {
                State = SessionTokenProvider.GetSessionToken(),
                Type = AuthWorkflowType.AntiCsrf
            };
            await SetWorkflow(state);

            var builder = new StringBuilder();
            builder.Append(ApiBase);
            builder.Append($"?response_type={type switch { AuthType.ImplicitGrant => "token", AuthType.AuthorizationCode => "code", _ => throw new ArgumentException($"No such {nameof(AuthType)}.") }}");
            builder.Append($"&client_id={clientId}");
            if (scope != Scope.None)
                builder.Append($"&scope={scope.GetStringAttribute(separator: " ")}");
            builder.Append($"&redirect_uri={redirectUri}");
            builder.Append($"&state={state.State}");
            builder.Append("&show_dialog=true"); // Spotify won't show the dialog by default even if the request contains new scopes

            NavigationManager.NavigateTo(builder.ToString(), forceLoad: true);
        }

        /// <summary>
        /// Retrieves either a cached or a fresh token.
        /// </summary>
        /// <returns>The access token of the cached token or renewed, if there is a valid token. Null otherwise.</returns>
        /// <remarks>Not thread-safe, but Blazor WA scheduling isn't preemptive.</remarks>
        public async Task<string?> GetToken()
        {
            if (MemoryCachedToken is not null)
            {
                if (!MemoryCachedToken.IsCompleted)
                    return (await MemoryCachedToken)?.AccessToken;

                else if (MemoryCachedToken.Result is null)
                    MemoryCachedToken = null;

                else if (!MemoryCachedToken.Result.IsAlmostExpired())
                    return MemoryCachedToken.Result.AccessToken;

                else
                    MemoryCachedToken = null;
            }
            MemoryCachedToken = LocalStorage.GetItem<AuthToken?>(nameof(AuthToken)).AsTask();
            var localStorageCachedToken = await MemoryCachedToken;

            if (localStorageCachedToken is null)
                return await GetAndCacheNewToken();

            if (!localStorageCachedToken.IsAlmostExpired())
                return localStorageCachedToken.AccessToken;

            await LocalStorage.RemoveItem(nameof(AuthToken));
            return await GetAndCacheNewToken();
        }

        private async Task<string?> GetAndCacheNewToken()
        {
            MemoryCachedToken = GetNewToken();
            return (await MemoryCachedToken)?.AccessToken;
        }

        /// <param name="redirectUri">The same URI that was passed to the <see cref="StartProcess"/> method.</param>
        public async Task<string?> GetTokenOnCallback(string redirectUri)
        {
            if (GetQueryParam("error") is not null)
                return null;

            AuthWorkflow? workflow = await GetWorkflow();
            if (workflow?.State is null 
                || !workflow.State.Equals(GetQueryParam("state"))
                || workflow.Type != AuthWorkflowType.AntiCsrf)
                return null;

            await RemoveWorkflow();

            MemoryCachedToken = GetFirstToken(redirectUri);
            return (await MemoryCachedToken)?.AccessToken;
        }

        /// <summary>
        /// Deletes all tokens and session IDs from cache.
        /// </summary>
        public async Task Logout()
        {
            await RemoveWorkflow();
            await LocalStorage.RemoveItem(nameof(AuthToken));
            MemoryCachedToken = null;
        }

        /// <remarks>
        /// Has to save the token with <see cref="SetToken"/>.
        /// </remarks>
        protected abstract Task<AuthToken?> GetFirstToken(string redirectUri);

        /// <remarks>
        /// Has to save the token with <see cref="SetToken"/>.
        /// </remarks>
        protected abstract Task<AuthToken?> GetNewToken();


        protected string? GetQueryParam(string paramName)
        {
            var uriBuilder = new UriBuilder(NavigationManager.Uri);

            // The Spotify API returns the parameters in the fragment in the implicit grant scheme, but uses the query in the auth code workflow.
            var q = HttpUtility.ParseQueryString(uriBuilder.Fragment.Replace('#', '?'));
            if (q["state"] is null)
                q = HttpUtility.ParseQueryString(uriBuilder.Query);

            return q[paramName];
        }

        protected async Task<AuthWorkflow?> GetWorkflow() =>
            await LocalStorage.GetItem<AuthWorkflow?>(nameof(AuthWorkflow));

        protected async Task SetWorkflow(AuthWorkflow workflow) =>
            await LocalStorage.SetItem(nameof(AuthWorkflow), workflow);

        protected async Task RemoveWorkflow() =>
            await LocalStorage.RemoveItem(nameof(AuthWorkflow));

        protected async Task SetToken(AuthToken token)
        {
            await LocalStorage.SetItem(nameof(AuthToken), token);
            MemoryCachedToken = Task.FromResult<AuthToken?>(token);
        }
    }
}
