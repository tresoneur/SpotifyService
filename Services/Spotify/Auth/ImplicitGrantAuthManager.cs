using Blazor.Extensions.Storage.Interfaces;
using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Auth.Models;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;

namespace Caerostris.Services.Spotify.Auth
{
    /// <summary>
    /// This method of authentication involves a renewal every hour or so, but does not require the use of the client secret.
    /// </summary>
    public class ImplicitGrantAuthManager : AuthManagerBase
    {
        public ImplicitGrantAuthManager(ILocalStorage injectedLocalStorage, NavigationManager injectedNavigatorManager)
         : base(injectedLocalStorage, injectedNavigatorManager) 
        { }

        public override async Task StartProcess(string clientId, string redirectUri, Scope scope) =>
            await StartProcess(AuthType.ImplicitGrant, clientId, redirectUri, scope);

        /// <summary>
        /// Stores the received token in LocalStorage.
        /// </summary>
        /// <returns>The access token of the received token, if there is a valid received token. Null otherwise</returns>
        protected override async Task<AuthToken?> GetFirstToken(string _)
        {
            var expiresInSecParam = GetQueryParam("expires_in");
            if (expiresInSecParam is null)
                return null;

            var expiresInSec = int.Parse(expiresInSecParam);
            var accessToken = GetQueryParam("access_token");

            if (accessToken is null)
                return null;

            var token = new AuthToken(expiresInSec, accessToken);
            await SetToken(token);

            NavigationManager.NavigateTo("/", forceLoad: false);

            return token;
        }

        protected override Task<AuthToken?> GetNewToken() =>
            Task.FromResult<AuthToken?>(null);
    }
}
