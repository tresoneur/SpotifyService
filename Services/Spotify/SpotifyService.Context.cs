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
        private string? lastKnownContextURI = null;

        /// <summary>
        /// Fires when the playback context URI changes. 
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

            return (context.Type ?? context.Uri.Split(':')[1]) switch
            {
                "artist"    => (await dispatcher.GetArtist(context.Uri), null, null),
                "album"     => (null, await dispatcher.GetAlbum(context.Uri), null),
                "playlist"  => (null, null, await dispatcher.GetPlaylist(context.Uri)),
                _           => (null, null, null)
            };
        }

        private void FireIfContextChanged(PlaybackContext? playback)
        {
            if (playback is null)
                return;

            if (lastKnownContextURI != playback?.Context?.Uri
                    || (!string.IsNullOrEmpty(lastKnownContextURI)
                        && !string.IsNullOrEmpty(playback?.Context?.Uri)
                        && !lastKnownContextURI.Equals(playback.Context.Uri)))
            {
                lastKnownContextURI = playback?.Context?.Uri;
                ContextChanged?.Invoke(playback);
            }
        }
    }
}
