using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class SearchResults
    {
        public IEnumerable<FullArtist> Artists { get; set; }
        public IEnumerable<SimpleAlbum> Albums { get; set; }
        public IEnumerable<FullTrack> Tracks { get; set; }
        public IEnumerable<SimplePlaylist> Playlists { get; set; }

    }
}
