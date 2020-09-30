using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class ArtistProfile
    {
        public FullArtist Artist { get; set; } = default!;

        public IEnumerable<SimpleAlbum> Albums { get; set; } = default!;

        public IEnumerable<FullTrack> TopTracks { get; set; } = default!;

        public IEnumerable<FullArtist> RelatedArtists { get; set; } = default!;
    }
}
