using Caerostris.Services.Spotify.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        public string ExploreArtistUrl { get; set; } = "/artist/";
        public string ExploreAlbumUrl { get; set; } = "/album/";
        public string ExplorePlaylistUrl { get; set; } = "/playlist/";

        /// <summary>
        /// Provides persistence for the search query.
        /// </summary>
        public string SearchQuery { get; set; }

        public async Task<SearchResults> Search(string query)
        {
            var result = await dispatcher.Search(query);
            return new SearchResults
            {
                Albums = result.Albums.Items,
                Artists = result.Artists.Items,
                Playlists = result.Playlists.Items,
                Tracks = result.Tracks.Items
            };
        }
    }
}
