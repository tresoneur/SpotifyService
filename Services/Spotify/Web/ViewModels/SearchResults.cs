using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class SearchResults
    {
        public IEnumerable<FullArtist> Artists { get; set; } = null!;
        public IEnumerable<SimpleAlbum> Albums { get; set; } = null!;
        public IEnumerable<FullTrack> Tracks { get; set; } = null!;
        public IEnumerable<SimplePlaylist> Playlists { get; set; } = null!;
    }
}
