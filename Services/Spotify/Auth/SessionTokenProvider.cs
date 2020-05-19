using System;
using System.Security.Cryptography;
using System.Web;

namespace SpotifyService.Services.Spotify.Auth
{
    public static class SessionTokenProvider
    {
        private static readonly RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();

        public static string GetSessionToken()
        {
            byte[] array = new byte[256]; 
            random.GetBytes(array);

            return Convert.ToBase64String(array)
                .Replace('/', '_')
                .Replace('+', '-')
                .Replace("=", "");
        }
    }
}
