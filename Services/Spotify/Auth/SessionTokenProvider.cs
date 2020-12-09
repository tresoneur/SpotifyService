using System;

namespace Caerostris.Services.Spotify.Auth
{
    public static class SessionTokenProvider
    {
        // RNGCryptoServiceProvider not supported by the .NET 5 browser-wasm runtime
        private static readonly Random Random = new();

        public static string GetSessionToken()
        {
            byte[] array = new byte[256]; 
            Random.NextBytes(array);

            return Convert.ToBase64String(array)
                .Replace('/', '_')
                .Replace('+', '-')
                .Replace("=", "");
        }
    }
}
