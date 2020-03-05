using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        public async Task<IEnumerable<SavedTrack>> GetSavedTracks()
            => await dispatcher.GetSavedTracks();

        public async Task<IEnumerable<FlatSavedTrack>> GetFlatSavedTracks()
            => (await dispatcher.GetSavedTracks()).AsFlatSavedTracks();
    }
}
