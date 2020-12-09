using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Blazor.Extensions.Storage.Interfaces;
using Caerostris.Services.Spotify.Auth.Models;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using SpotifyAuthServer.Controllers.Model;
using SpotifyAuthServer.Model;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Configuration;
using AuthToken = Caerostris.Services.Spotify.Auth.Models.AuthToken;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Auth
{
    /// <summary>
    /// This method of acquiring authentication involves the client secret, so it requires a supporting server. However, the authentication does not need to be renewed.
    /// </summary>
    class AuthorizationCodeAuthManager : AuthManagerBase
    {
        private readonly SpotifyServiceConfiguration configuration;

        public AuthorizationCodeAuthManager(ILocalStorage localStorage,
            NavigationManager navigatorManager, SpotifyServiceConfiguration configuration)
            : base(localStorage, navigatorManager)
        {
            this.configuration = configuration;
        }

        public override async Task StartProcess(string clientId, string redirectUri, List<string> scopes) =>
            await StartProcess(AuthType.AuthorizationCode, clientId, redirectUri, scopes);

        protected override async Task<AuthToken?> GetFirstToken(string callbackUri)
        {
            var code = GetQueryParam("code");

            if (code is null)
                return null;

            // The code is used as proof of identity from here on out
            await SetWorkflow(new AuthWorkflow { State = code, Type = AuthWorkflowType.AuthCode });

            var result = await Request(
                "register", 
                new AuthCodeAndCallbackUri { Code = code, CallbackUri = callbackUri });

            if (!string.IsNullOrEmpty(result.Error))
                return null;

            var token = new AuthToken(result.Token.ExpiresInSec, result.Token.AccessToken);
            await SetToken(token);
            
            NavigationManager.NavigateTo("/", forceLoad: false);

            return token;
        }

        protected override async Task<AuthToken?> GetNewToken()
        {
            var code = await GetWorkflow();

            if (code?.State is null || code.Type != AuthWorkflowType.AuthCode)
                return null;

            var result = await Request(
                "token",
                new AuthCode { Code = code.State });

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
