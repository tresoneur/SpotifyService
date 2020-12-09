using SpotifyAPI.Web;
using System;

namespace Caerostris.Services.Spotify.Services.Spotify.Web.Api
{
    public class Api
    {
        /// <summary>
        /// The underlying <see cref="SpotifyClient"/>. Never cache.
        /// </summary>
        public SpotifyClient Client { get; private set; }

        public Api()
        {
            Client = BuildClient();
        }

        public void Authorize(string token)
        {
            Client = BuildClient(token);
        }

        private static SpotifyClient BuildClient(string token = "")
        {
            var config = SpotifyClientConfig
                .CreateDefault(token)
                .WithRetryHandler(new SimpleRetryHandler() 
                {
                    RetryAfter = TimeSpan.FromSeconds(1),
                    RetryTimes = 3,
                    TooManyRequestsConsumesARetry = false
                });

            return new SpotifyClient(config);
        }
    }
}
