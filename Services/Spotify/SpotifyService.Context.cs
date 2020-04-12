using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        /// We have to keep a copy, because the original may get modified in lastKnownPlayback when a WebPlaybackState is applied to it. // TODO: may not be necessary
        private string? lastKnownContextUri = null;

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
            string Id = parts[parts.Length - 1];
            return (context.Type ?? parts[1]) switch
            {
                "artist"    => (await dispatcher.GetArtist(Id), null, null),
                "album"     => (null, await dispatcher.GetAlbum(Id), null),
                "playlist"  => (null, null, await dispatcher.GetPlaylist(Id)),
                _           => (null, null, null)
            };
        }

        public async Task<ArtistProfile> GetArtist(string Id) =>
            await dispatcher.GetArtist(Id);

        public async Task<CompleteAlbum> GetAlbum(string Id) =>
            await dispatcher.GetAlbum(Id);

        public async Task<CompletePlaylist> GetPlaylist(string Id) =>
            await dispatcher.GetPlaylist(Id);

        private void FireIfContextChanged(PlaybackContext? playback)
        {
            if (playback is null)
                return;

            if (lastKnownContextUri != playback?.Context?.Uri
                    || (!string.IsNullOrEmpty(lastKnownContextUri)
                        && !string.IsNullOrEmpty(playback?.Context?.Uri)
                        && !lastKnownContextUri.Equals(playback.Context.Uri)))
            {
                lastKnownContextUri = playback?.Context?.Uri;
                ContextChanged?.Invoke(playback);
            }
        }
    }
}
