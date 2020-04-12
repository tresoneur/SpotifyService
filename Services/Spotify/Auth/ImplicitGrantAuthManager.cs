using Blazor.Extensions.Storage.Interfaces;
using Caerostris.Services.Spotify.Auth.Models;
using Microsoft.AspNetCore.Components;
using SpotifyAPI.Web.Enums;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Caerostris.Services.Spotify.Auth
{
    public class ImplicitGrantAuthManager
    {
        private const string APIBase = "https://accounts.spotify.com/authorize";

        private ILocalStorage localStorage;
        private NavigationManager navigationManager;

        private ImplicitGrantToken? memoryCachedToken;

        public ImplicitGrantAuthManager(ILocalStorage injectedLocalStorage, NavigationManager injectedNavigatorManager)
        {
            localStorage = injectedLocalStorage;
            navigationManager = injectedNavigatorManager;
        }

        public async Task StartProcess(string clientId, string redirectUri, Scope scope)
        {
            // Generate a state string and save it to LocalStorage to protect the client from CSRF.
            var state = new ImplicitGrantWorkflow()
            {
                State = new Random().NextDouble().GetHashCode().ToString("X")
            };
            await localStorage.SetItem(nameof(ImplicitGrantWorkflow), state);

            var builder = new StringBuilder();
            builder.Append(APIBase);
            builder.Append("?response_type=token");
            builder.Append($"&client_id={clientId}");
            if (scope != Scope.None)
                builder.Append($"&scope={SpotifyAPI.Web.Util.GetStringAttribute(scope, separator: " ")}");
            builder.Append($"&redirect_uri={redirectUri}");
            builder.Append($"&state={state.State}");
            builder.Append("&show_dialog=true"); // Spotify won't show the dialog by default even if the request contains new scopes

            navigationManager.NavigateTo(builder.ToString(), forceLoad: true);
        }

        /// <summary>
        /// Retrieves the cached token from LocalStorage.
        /// </summary>
        /// <returns>The access token of the cached token, if there is a valid token. Null otherwise</returns>
        public async Task<string?> GetToken()
        {
            if (!(memoryCachedToken is null))
                if (!memoryCachedToken.IsExpired())
                    return memoryCachedToken.AccessToken;
                else
                    memoryCachedToken = null;

            var localStorageCachedToken = await localStorage.GetItem<ImplicitGrantToken?>(nameof(ImplicitGrantToken));

            if (localStorageCachedToken is null)
                return null;

            if (localStorageCachedToken.IsExpired())
            {
                await localStorage.RemoveItem(nameof(ImplicitGrantToken));
                return null;
            }
            else
            {
                memoryCachedToken = localStorageCachedToken;
                return localStorageCachedToken.AccessToken;
            }
        }

        /// <summary>
        /// Stores the received token in LocalStorage.
        /// </summary>
        /// <returns>The access token of the received token, if there is a valid received token. Null otherwise</returns>
        public async Task<string?> StoreTokenOnCallback()
        {
            if (!(GetQueryParam("error") is null))
                return null;

            string? state = (await localStorage.GetItem<ImplicitGrantWorkflow?>(nameof(ImplicitGrantWorkflow)))?.State;
            if (state is null || state != GetQueryParam("state"))
                return null;

            await localStorage.RemoveItem(nameof(ImplicitGrantWorkflow));


            var expiresInSec = int.Parse(GetQueryParam("expires_in"));
            var accessToken = GetQueryParam("access_token");

            if (accessToken is null)
                return null;

            var token = new ImplicitGrantToken(expiresInSec, accessToken);

            await localStorage.SetItem(nameof(ImplicitGrantToken), token);

            memoryCachedToken = token;

            navigationManager.NavigateTo("/", forceLoad: false);

            return token.AccessToken;
        }

        private string? GetQueryParam(string paramName)
        {
            var uriBuilder = new UriBuilder(navigationManager.Uri);
            var q = HttpUtility.ParseQueryString(uriBuilder.Fragment.Replace('#', '?'));
            return q[paramName];
        }
    }
}
