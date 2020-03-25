using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompleteAlbum
    {
        public FullAlbum Album { get; set; }

        public IEnumerable<SimpleTrack> Tracks { get; set; }
    }
}
