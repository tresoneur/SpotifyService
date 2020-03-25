using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompletePlaylist
    {
        public FullPlaylist Playlist { get; set; }

        public IEnumerable<PlaylistTrack> Tracks { get; set; } 
    }
}
