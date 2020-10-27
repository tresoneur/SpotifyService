using SpotifyAPI.Web;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class Sections
    {
        public IEnumerable<FullArtist> Artists { get; set; } = new List<FullArtist>();

        public IEnumerable<SimpleAlbum> Albums { get; set; } = new List<SimpleAlbum>();

        public IEnumerable<FullTrack> Tracks { get; set; } = new List<FullTrack>();

        public IEnumerable<SimplePlaylist> Playlists { get; set; } = new List<SimplePlaylist>();
    }
}
