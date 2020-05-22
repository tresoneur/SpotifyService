using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// <summary>
        /// Subscribe to this event to get updates about the (down)loading of the audio analytics associated with the user's saved tracks.
        /// Analytics are typically downloaded in batches of 100, loaded from cache in batches of 10, and frequently number in the thousands.
        /// </summary>
        public event Action<int, int>? AnalyticsLoadingProgress;

        public async Task<IEnumerable<AudioFeatures>> GetAudioFeaturesForSavedTracks()
        {
            var savedTracks = await GetSavedTracks();
            return await dispatcher.GetAudioFeatures(
                savedTracks.Select(t => t.Track.Id),
                (c, t) => AnalyticsLoadingProgress?.Invoke(c, t));
        }
    }
}
