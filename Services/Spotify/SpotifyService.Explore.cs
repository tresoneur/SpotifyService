using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.Web.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private readonly IndexedDbCache<string> listenLaterStore;
        private const string StoreName = nameof(Sections);

        public string ExploreArtistUrl { get; set; } = "/artist/";
        public string ExploreAlbumUrl { get; set; } = "/album/";
        public string ExplorePlaylistUrl { get; set; } = "/playlist/";

        /// <summary>
        /// Provides persistence for the search query.
        /// </summary>
        public string SearchQuery { get; set; }

        public async Task<Sections> Search(string query)
        {
            var result = await dispatcher.Search(query);
            return new Sections
            {
                Albums = result.Albums.Items,
                Artists = result.Artists.Items,
                Playlists = result.Playlists.Items,
                Tracks = result.Tracks.Items
            };
        }

        public async Task<Sections> GetListenLaterItems()
        {
            var items = await listenLaterStore.Load(StoreName);
            var result = new Sections()
            {
                Tracks = new List<FullTrack>(),
                Albums = new List<SimpleAlbum>(),
                Artists = new List<FullArtist>(),
                Playlists = new List<SimplePlaylist>()
            };

            IEnumerable<Task> downloads = items
                .GroupBy(s => s.Split(':').Skip(1).First())
                .Select<IGrouping<string, string>, Func<Task>>(g => g.Key switch
                {
                    "track" => async () => result.Tracks = await dispatcher.GetTracks(g.ToList()),
                    "album" => async () => result.Albums = (await dispatcher.GetAlbums(g.ToList())).Select(a => a.AsSimpleAlbum()),
                    "artist" => async () => result.Artists = await dispatcher.GetArtists(g.ToList()),
                    "playlist" => async () => result.Playlists = (await dispatcher.GetPlaylists(g.ToList())).Select(p => p.AsSimplePlaylist()),
                    string s => throw new ArgumentException($"Can't handle '{s}' context.")
                })
                .Select(t => t());

            await Task.WhenAll(downloads);
            return result;
        }

        public async Task<bool> IsAddedToListenLater(string uri)
        {
            var items = await listenLaterStore.Load(StoreName);
            return items.Any(i => i == uri);
        }

        public async Task AddListenLaterItem(string uri) =>
            await listenLaterStore.Save(StoreName, new[] { uri });

        public async Task RemoveListenLaterItem(string uri)
        {
            var items = await listenLaterStore.Load(StoreName);
            var preservedItems = items.Where(i => (i != uri));
            // TODO: itt adatvesztés lehet, ha nincsen rendes indexeddb remove fg
            await listenLaterStore.Clear(StoreName);
            await listenLaterStore.Save(StoreName, preservedItems);
        }
    }
}
