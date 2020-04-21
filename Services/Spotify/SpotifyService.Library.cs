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
        /// Gets the user's saved tracks. This can either take several minutes or return with the cached values almost immediately. 
        /// Will try its best to serve fresh data, but may fail in certain cases (e.g. when the number of saved tracks didn't change since tracks were last cached).
        /// Subscribe to <seealso cref="LibraryLoadingProgress"/> so that progress may be shown to the user.
        /// </summary>
        public async Task<IEnumerable<SavedTrack>> GetSavedTracks() =>
            await dispatcher.GetSavedTracks((c, t) => LibraryLoadingProgress?.Invoke(c, t));

        public async Task<bool> IsTrackSaved(string Id) => // TODO: track relinking, use linked_from ID when removing
            await dispatcher.GetTrackSavedStatus(Id);

        public async Task ToogleTrackSaved(string Id)
        {
            if (await IsTrackSaved(Id))
                await dispatcher.RemoveSavedTrack(Id);
            else
                await dispatcher.SaveTrack(Id);
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
