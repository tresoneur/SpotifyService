using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class SearchResults
    {
        public IEnumerable<FullArtist> Artists { get; set; } = default!;
        public IEnumerable<SimpleAlbum> Albums { get; set; } = default!;
        public IEnumerable<FullTrack> Tracks { get; set; } = default!;
        public IEnumerable<SimplePlaylist> Playlists { get; set; } = default!;
    }
}
