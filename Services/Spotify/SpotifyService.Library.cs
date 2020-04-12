using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// <summary>
        /// Subscribe to this event to get updates for the progress concerning the downloading or loading of the user's saved tracks.
        /// Tracks are typically downloaded in batches of 50, loaded from cache in batches of 100, and frequently number in the thousands.
        /// </summary>
        public event Action<int, int>? LibraryLoadingProgress;

        /// <summary>
        /// Gets the user's saved tracks. This can either take several minutes or return with the cached values almost immediately. 
        /// Will try its best to serve fresh data, but may fail in certain cases (e.g. when the number of saved tracks didn't change since tracks were last cached).
        /// Subscribe to <seealso cref="LibraryLoadingProgress"/> so that progress may be shown to the user.
        /// </summary>
        public async Task<IEnumerable<SavedTrack>> GetSavedTracks() =>
            await dispatcher.GetSavedTracks((c, t) => LibraryLoadingProgress?.Invoke(c, t));

        /// <summary>
        /// Returns the number of the user's saved tracks. This number is always fetched directly from Spotify and may have changed since you requested the users saved tracks through <see cref="GetSavedTracks"/>.
        /// </summary>
        public async Task<int> GetSavedTrackCount() =>
            await dispatcher.GetSavedTrackCount();

        public async Task<bool> IsTrackSaved(string Id) =>
            await dispatcher.GetTrackSavedStatus(Id);

        public async Task ToogleTrackSaved(string Id)
        {
            if (await IsTrackSaved(Id))
                await dispatcher.RemoveSavedTrack(Id);
            else
                await dispatcher.SaveTrack(Id);
        }

    }
}
