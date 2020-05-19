using System;

namespace Caerostris.Services.Spotify.Auth.Models
{
    public class AuthToken
    {
        public AuthToken() { }

        public AuthToken(int expiresInSec, string accessToken, DateTime? timestamp = null)
        {
            Timestamp = timestamp?.ToString("o") ?? DateTime.UtcNow.ToString("o");
            ExpiresInSec = expiresInSec;
            AccessToken = accessToken;
        }

        public string Timestamp { get; set; } = "";

        public int ExpiresInSec { get; set; }

        public string AccessToken { get; set; } = "";

        public bool IsAlmostExpired()
        {
            const int paddingSec = 5 * 60;

            var expiryDate = DateTime.Parse(Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
            expiryDate = expiryDate.AddSeconds(ExpiresInSec - paddingSec);
            return (expiryDate < DateTime.UtcNow);
        }
    }
}
