using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Sections
{
    public sealed class ContextsService
    {
        private readonly WebApiManager dispatcher;

        public ContextsService(
            WebApiManager webApiManager,
            PlaybackService spotifyServicePlayback)
        {
            dispatcher = webApiManager;
            spotifyServicePlayback.UnsafePlaybackChanged += FireIfContextChanged;
        }

        /// <summary>
        /// Fires when the playback context Uri changes. 
        /// Note that this includes the case when the <see cref="CurrentlyPlayingContext.Context"/> changes to null, which may happen e.g. when the user starts a playback in their Library.
        /// </summary>
        public event Func<CurrentlyPlayingContext, Task>? ContextChanged;

        public async Task<ArtistProfile> GetArtist(string id) =>
            await dispatcher.GetArtist(id);

        public async Task<CompleteAlbum> GetAlbum(string id) =>
            await dispatcher.GetAlbum(id);

        public async Task<CompletePlaylist> GetPlaylist(string id) =>
            await dispatcher.GetPlaylist(id);

        private async Task FireIfContextChanged(CurrentlyPlayingContext? current, CurrentlyPlayingContext? next)
        {
            string? nextContextUri = next?.Context?.Uri;
            string? lastKnownContextUri = current?.Context?.Uri;

            if (ContextChanged is not null
                && next is not null
                && nextContextUri is not null
                && (lastKnownContextUri != nextContextUri
                    || (!string.IsNullOrEmpty(lastKnownContextUri)
                        && !string.IsNullOrEmpty(nextContextUri)
                        && !lastKnownContextUri.Equals(nextContextUri))))
            {
                    await ContextChanged(next);
            }
        }
    }
}
