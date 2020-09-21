using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class CompleteAlbum
    {
        public FullAlbum Album { get; set; } = default!;

        public IEnumerable<SimpleTrack> Tracks { get; set; } = default!;
    }
}
