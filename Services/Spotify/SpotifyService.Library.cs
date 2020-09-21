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
        /// Subscribe to this event to get updates for the progress concerning the downloading or loading of the user's saved tracks.
        /// Tracks are typically downloaded in batches of 50, loaded from cache in batches of 10, and frequently number in the thousands.
        /// </summary>
        public event Action<int, int>? LibraryLoadingProgress;

        /// <summary>
        /// Fired when the list of user playlists changes as a result of an action carried out by <see cref="SpotifyService"/>.
        /// </summary>
        public event Func<Task> UserPlaylistsChanged;

        /// <summary>
        /// Fired when a track is saved or unsaved. The parameters supplied are the ID of the track and the new state, respectively.
        /// </summary>
        public event Action<string, bool> SavedStateChanged;

        /// <summary>
        /// Gets the user's saved tracks. This can either take several minutes or return with the cached values almost immediately. 
        /// Will try its best to serve fresh data, but may fail in certain cases (e.g. when the number of saved tracks didn't change since tracks were last cached).
        /// Subscribe to <seealso cref="LibraryLoadingProgress"/> so that progress may be shown to the user.
        /// </summary>
        public async Task<IEnumerable<SavedTrack>> GetSavedTracks() =>
            await dispatcher.GetSavedTracks((c, t) => LibraryLoadingProgress?.Invoke(c, t));

        /// <summary>
        /// Fetches saved state for a track.
        /// </summary>
        /// <param name="id">The ID of the track.</param>
        /// <param name="linkedFromId">The ID of the track the given track was relinked from.</param>
        /// <returns>Whether either track has been saved by the user.</returns>
        public async Task<bool> IsTrackSaved(string id, string? linkedFromId = null) =>
            (await dispatcher.GetTrackSavedStatus(id) 
                || (!(linkedFromId is null) && await dispatcher.GetTrackSavedStatus(linkedFromId)));

        /// <summary>
        /// Fetches saved state for a list of tracks.
        /// </summary>
        /// <param name="idLinkedFromIdPairs">
        /// A list of pairs of track IDs such that the first item of a pair is the shown track's ID and the second the ID of the track the first track was relinked from.
        /// </param>
        /// <returns>
        /// For each unique pair, a key-value pair such that the key is the ID of the shown track and the value is the logical value of whether either (shown, relinked-from) track has been saved by the user.
        /// </returns>
        public async Task<IDictionary<string, bool>> AreTracksSaved(IEnumerable<(string, string?)> idLinkedFromIdPairs)
        { 
            var ids = idLinkedFromIdPairs.Select(p => p.Item1)
                ?? new List<string>();

            var relinkedFromIds = idLinkedFromIdPairs.Where(p => !(p.Item2 is null)).Select(p => p.Item2!.ToString()) 
                ?? new List<string>();

            var isSaved = dispatcher.GetTrackSavedStatus(ids);
            var isRelinkedFromSaved = dispatcher.GetTrackSavedStatus(relinkedFromIds);

            await Task.WhenAll(isSaved, isRelinkedFromSaved);

            foreach (var isRelinkedFromTrackSaved in isRelinkedFromSaved.Result.Where(kv => kv.Value))
            {
                var id = idLinkedFromIdPairs
                    .Where(p => p.Item2 == isRelinkedFromTrackSaved.Key)
                    .First()
                    .Item1;

                isSaved.Result[id] = isRelinkedFromTrackSaved.Value;
            }

            return isSaved.Result;
        }

        public async Task ToogleTrackSaved(string id, string? linkedFromId)
        {
            // Set new state via the API.
            var removed = false;

            if (await IsTrackSaved(id))
            {
                await dispatcher.RemoveSavedTrack(id);
                removed = true;
            }

            if (!(linkedFromId is null) && await IsTrackSaved(linkedFromId))
            {
                await dispatcher.RemoveSavedTrack(linkedFromId);
                removed = true;
            }

            if (!removed)
                await dispatcher.SaveTrack(id);

            // Fire the corresponding event.
            SavedStateChanged?.Invoke(id, !removed);

            if (!(linkedFromId is null))
                SavedStateChanged?.Invoke(linkedFromId, !removed);
        }

        /// <summary>
        /// Returns the list of playlists that the user either owns or follows.
        /// </summary>
        public async Task<IEnumerable<SimplePlaylist>> GetUserPlaylists() =>
            await dispatcher.GetUserPlaylists(await GetUserId());

        /// <summary>
        /// Creates a playlist with the given title, description (can contain html markup) and track URIs.
        /// </summary>
        public async Task CreatePlaylist(string title, string description, IEnumerable<string> trackUris)
        {
            var createdPlaylist = await dispatcher.CreatePlaylist(await GetUserId(), title, description);
            await dispatcher.AddPlaylistTracks(createdPlaylist.Id, trackUris);
            _ = UserPlaylistsChanged?.Invoke();
        }

        /// <summary>
        /// Creates a playlist with the given title, description (can contain html markup) and tracks.
        /// </summary>
        public async Task CreatePlaylist(string title, string description, IEnumerable<SavedTrack> tracks) =>
            await CreatePlaylist(title, description, tracks.Select(t => t.Track.Uri));
    }
}
