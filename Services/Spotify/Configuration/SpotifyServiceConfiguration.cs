using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Configuration
{
    public class SpotifyServiceConfiguration
    {
        public string? AuthServerApiBase { get; set; }

        public string PlayerDeviceName { get; set; } = default!;

        public string ClientId { get; set; } = default!;

        public IEnumerable<string> PermissionScopes { get; set; } = default!;
    }
}
