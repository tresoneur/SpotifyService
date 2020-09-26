using System;

namespace SpotifyService.Services.Spotify.Auth
{
    public static class SessionTokenProvider
    {
        // RNGCryptoServiceProvider not supported by the .NET 5 browser-wasm runtime
        private static readonly Random random = new Random();

        public static string GetSessionToken()
        {
            byte[] array = new byte[256]; 
            random.NextBytes(array);

            return Convert.ToBase64String(array)
                .Replace('/', '_')
                .Replace('+', '-')
                .Replace("=", "");
        }
    }
}
