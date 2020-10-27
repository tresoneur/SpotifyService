using SpotifyAPI.Web;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompletePlaylist
    {
        public FullPlaylist Playlist { get; set; } = default!;

        public IEnumerable<PlaylistTrack<FullTrack>> Tracks { get; set; } = default!;
    }
}
