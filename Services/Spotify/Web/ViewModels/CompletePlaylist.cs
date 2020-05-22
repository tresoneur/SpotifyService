using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompletePlaylist
    {
        public FullPlaylist Playlist { get; set; } = null!;

        public IEnumerable<PlaylistTrack> Tracks { get; set; } = null!;
    }
}
