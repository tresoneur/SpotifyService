using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web.Models;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// <summary>
        /// Fires when the playback context Uri changes. 
        /// Note that this includes the case when the context changes to null, which may happen 
        ///  - after a few minutes of user inactivity;
        ///  - when the user starts a playback in their Library/Collection/Saved Tracks;
        ///  etc., alike.
        /// </summary>
        public event Func<PlaybackContext, Task>? ContextChanged;

        /// <summary>
        /// Fetches information about the current playback context, be that an artist, an album or a playlist.
        /// </summary>
        /// <returns>A 3-tuple containing zero or one non-null value(s) of the possible context types.</returns>
        public async Task<(ArtistProfile?, CompleteAlbum?, CompletePlaylist?)> GetContext(Context? context)
        {
            if (context is null
                || string.IsNullOrEmpty(context.Uri))
                return (null, null, null);

            string[] parts = context.Uri.Split(':');
            string id = parts[^1];
            return (context.Type ?? parts[1]) switch
            {
                "artist"    => (await dispatcher.GetArtist(id), null, null),
                "album"     => (null, await dispatcher.GetAlbum(id), null),
                "playlist"  => (null, null, await dispatcher.GetPlaylist(id)),
                _           => (null, null, null)
            };
        }

        public async Task<ArtistProfile> GetArtist(string id) =>
            await dispatcher.GetArtist(id);

        public async Task<CompleteAlbum> GetAlbum(string id) =>
            await dispatcher.GetAlbum(id);

        public async Task<CompletePlaylist> GetPlaylist(string id) =>
            await dispatcher.GetPlaylist(id);

        private void FireIfContextChanged(PlaybackContext? current, PlaybackContext? next)
        {
            if (next?.Context?.Uri is null)
                return;

            string? lastKnownContextUri = current?.Context?.Uri;

            if (lastKnownContextUri != next?.Context?.Uri
                    || (!string.IsNullOrEmpty(lastKnownContextUri)
                        && !string.IsNullOrEmpty(next?.Context?.Uri)
                        && !lastKnownContextUri.Equals(next.Context.Uri)))
            {
                ContextChanged?.Invoke(next!);
            }
        }
    }
}
