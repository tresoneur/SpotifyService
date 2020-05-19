using SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
#pragma warning disable CS8618 // Data class
    public class ArtistProfile
    {
        public FullArtist Artist { get; set; }

        public IEnumerable<SimpleAlbum> Albums { get; set; }

        public IEnumerable<FullTrack> TopTracks { get; set; }

        public IEnumerable<FullArtist> RelatedArtists { get; set; }
    }
#pragma warning restore CS8618
}
