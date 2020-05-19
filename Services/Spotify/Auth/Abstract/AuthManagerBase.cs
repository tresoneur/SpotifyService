using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Blazor.Extensions.Storage.Interfaces;
using Caerostris.Services.Spotify.Auth.Models;
using Microsoft.AspNetCore.Components;
using SpotifyAPI.Web.Enums;
using SpotifyService.Services.Spotify.Auth.Models;

namespace SpotifyService.Services.Spotify.Auth.Abstract
{
    /// <summary>
    /// Provides token caching and auth workflow initialization functionality for descendants.
    /// </summary>
    public abstract class AuthManagerBase
    {
        private const string ApiBase = "https://accounts.spotify.com/authorize";

        protected readonly ILocalStorage localStorage;
        protected readonly NavigationManager navigationManager;

        protected Task<AuthToken?>? memoryCachedToken;

        protected AuthManagerBase(ILocalStorage injectedLocalStorage, NavigationManager injectedNavigatorManager)
        {
            localStorage = injectedLocalStorage;
            navigationManager = injectedNavigatorManager;
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
            builder.Append($"?response_type={type switch { AuthType.ImplicitGrant => "token", AuthType.AuthorizationCode => "code", _ => throw new ArgumentOutOfRangeException() }}");
            builder.Append($"&client_id={clientId}");
            if (scope != Scope.None)
                builder.Append($"&scope={SpotifyAPI.Web.Util.GetStringAttribute(scope, separator: " ")}");
            builder.Append($"&redirect_uri={redirectUri}");
            builder.Append($"&state={state.State}");
            builder.Append("&show_dialog=true"); // Spotify won't show the dialog by default even if the request contains new scopes

            navigationManager.NavigateTo(builder.ToString(), forceLoad: true);
        }

        /// <summary>
        /// Retrieves either a cached or a fresh token.
        /// </summary>
        /// <returns>The access token of the cached token or renewed, if there is a valid token. Null otherwise.</returns>
        /// <remarks>Not thread-safe, but Blazor WA scheduling isn't preemptive.</remarks>
        public async Task<string?> GetToken()
        {
            if (!(memoryCachedToken is null))
            {
                if (!memoryCachedToken.IsCompleted)
                    return (await memoryCachedToken)?.AccessToken;

                else if (memoryCachedToken.Result is null)
                    memoryCachedToken = null;

                else if (!memoryCachedToken.Result.IsAlmostExpired())
                    return memoryCachedToken.Result.AccessToken;

                else
                    memoryCachedToken = null;
            }

            memoryCachedToken = localStorage.GetItem<AuthToken?>(nameof(AuthToken)).AsTask();
            var localStorageCachedToken = await memoryCachedToken;

            if (localStorageCachedToken is null)
                return await GetAndCacheNewToken();

            if (!localStorageCachedToken.IsAlmostExpired())
                return localStorageCachedToken.AccessToken;

            await localStorage.RemoveItem(nameof(AuthToken));
            return await GetAndCacheNewToken();
        }

        private async Task<string?> GetAndCacheNewToken()
        {
            memoryCachedToken = GetNewToken();
            return (await memoryCachedToken)?.AccessToken;
        }

        /// <remarks>
        /// Has to save the token with <see cref="SetToken"/>.
        /// </remarks>
        /// <param name="redirectUri">The same URI that was passed to the <see cref="StartProcess"/> method.</param>
        public async Task<string?> GetTokenOnCallback(string redirectUri)
        {
            if (!(GetQueryParam("error") is null))
                return null;

            AuthWorkflow? workflow = await GetWorkflow();
            if (workflow?.State is null 
                || !workflow.State.Equals(GetQueryParam("state"))
                || workflow.Type != AuthWorkflowType.AntiCsrf)
                return null;

            await RemoveWorkflow();

            return (await GetFirstToken(redirectUri))?.AccessToken;
        }

        /// <summary>
        /// Deletes all tokens and session IDs from cache.
        /// </summary>
        public async Task Logout()
        {
            await RemoveWorkflow();
            await localStorage.RemoveItem(nameof(AuthToken));
            memoryCachedToken = null;
        }

        protected abstract Task<AuthToken?> GetFirstToken(string redirectUri);

        /// <remarks>
        /// Has to save the token with <see cref="SetToken"/>.
        /// </remarks>
        protected abstract Task<AuthToken?> GetNewToken();


        protected string? GetQueryParam(string paramName)
        {
            var uriBuilder = new UriBuilder(navigationManager.Uri);

            // The Spotify API returns the parameters in the fragment in the implicit grant scheme, but uses the query in the auth code workflow.
            var q = HttpUtility.ParseQueryString(uriBuilder.Fragment.Replace('#', '?'));
            if (q["state"] is null)
                q = HttpUtility.ParseQueryString(uriBuilder.Query);

            return q[paramName];
        }

        protected async Task<AuthWorkflow?> GetWorkflow() =>
            await localStorage.GetItem<AuthWorkflow?>(nameof(AuthWorkflow));

        protected async Task SetWorkflow(AuthWorkflow workflow) =>
            await localStorage.SetItem(nameof(AuthWorkflow), workflow);

        protected async Task RemoveWorkflow() =>
            await localStorage.RemoveItem(nameof(AuthWorkflow));

        protected async Task SetToken(AuthToken token)
        {
            await localStorage.SetItem(nameof(AuthToken), token);
            memoryCachedToken = Task.FromResult<AuthToken?>(token);
        }
    }
}
