using SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompleteAlbum
    {
        public FullAlbum Album { get; set; } = null!;

        public IEnumerable<SimpleTrack> Tracks { get; set; } = null!;
    }
}
