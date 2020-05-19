using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Blazor.Extensions.Storage.Interfaces;
using Caerostris.Services.Spotify.Auth.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using SpotifyAPI.Web.Enums;
using SpotifyAuthServer.Controllers.Model;
using SpotifyAuthServer.Model;
using SpotifyService.Services.Spotify.Auth.Abstract;
using SpotifyService.Services.Spotify.Auth.Models;
using SpotifyService.Services.Spotify.Configuration;
using AuthToken = Caerostris.Services.Spotify.Auth.Models.AuthToken;

namespace SpotifyService.Services.Spotify.Auth
{
    /// <summary>
    /// This method of acquiring authentication involves the client secret, so it requires a supporting server. However, the authentication does not need to be renewed.
    /// </summary>
    class AuthorizationCodeAuthManager : AuthManagerBase
    {
        private readonly SpotifyServiceConfiguration configuration;

        public AuthorizationCodeAuthManager(ILocalStorage injectedLocalStorage,
            NavigationManager injectedNavigatorManager, SpotifyServiceConfiguration configuration)
            : base(injectedLocalStorage, injectedNavigatorManager)
        {
            this.configuration = configuration;
        }

        public override async Task StartProcess(string clientId, string redirectUri, Scope scope) =>
            await StartProcess(AuthType.AuthorizationCode, clientId, redirectUri, scope);

        protected override async Task<AuthToken?> GetFirstToken(string callbackUri)
        {
            var code = GetQueryParam("code");

            if (code is null)
                return null;

            // The code is used as proof of identity from here on out
            await SetWorkflow(new AuthWorkflow { State = code });

            var result = await Request(
                "register", 
                new AuthCodeAndCallbackUri { Code = code, CallbackUri = callbackUri });

            if (!string.IsNullOrEmpty(result.Error))
                return null;

            var token = new AuthToken(result.Token.ExpiresInSec, result.Token.AccessToken);
            await SetToken(token);
            
            navigationManager.NavigateTo("/", forceLoad: false);

            return token;
        }

        protected override async Task<AuthToken?> GetNewToken()
        {
            var code = await GetWorkflowState();

            if (code is null)
                return null;

            var result = await Request(
                "token",
                new AuthCode { Code = code });

            if (!string.IsNullOrEmpty(result.Error))
                return null;

            var token = new AuthToken(result.Token.ExpiresInSec, result.Token.AccessToken, result.Token.Acquired);
            await SetToken(token);

            return token;
        }

        private async Task<AuthTokenResult> Request<TBody>(string endpoint, TBody body)
        {
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await new HttpClient().PostAsync($"{configuration.AuthServerApiBase}/{endpoint}", content);
            var message = await response.Content.ReadAsStringAsync();

            AuthTokenResult result;
            try
            {
                result = JsonConvert.DeserializeObject<AuthTokenResult>(message);
            }
            catch (Exception e)
            {
                result = new AuthTokenResult { Error = e.Message };
            }

            return result;
        }
    }
}
