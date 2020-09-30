using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    /// <remarks>
    /// There is no way to get e.g. a hash of all track IDs from Spotify, so we have to improvise a way to tell if the Library has been updated since we last cached it. 
    /// The method of checking the number of saved tracks is obviously quite crude, but should work most of the time (people don't frequently remove tracks from their libraries).
    /// </remarks>
    public class SavedTrackManager : CachedDataProviderBase<SavedTrack>
    {
        private readonly SpotifyWebAPI api;
        private readonly IndexedDbCache<SavedTrack> storageCache;

        private const string StoreName = nameof(SavedTrack);

        public SavedTrackManager(SpotifyWebAPI spotifyWebApi, IndexedDbCache<SavedTrack> indexedDbCache)
        {
            api = spotifyWebApi;
            storageCache = indexedDbCache;
        }

        protected override async Task<bool> IsMemoryCacheValid() =>
            await RealAndMemoryChachedSavedTrackCountsMatch();

        protected override async Task<bool> IsStorageCacheValid() =>
            await RealAndIndexedDbChachedSavedTrackCountsMatch();

        protected override async Task ClearStorageCache() =>
            await storageCache.Clear(StoreName);

        protected override async Task<IEnumerable<SavedTrack>> LoadStorageCache(Action<int, int> progressCallback, string market = "") =>
            await storageCache.Load(StoreName, progressCallback);

        protected override async Task<IEnumerable<SavedTrack>> LoadRemoteResource(Action<int, int> progressCallback, string market = "")
        {
            return await Utility.SynchronizedDownloadPagedResources(
                (o, p) => api.GetSavedTracksAsync(offset: o, limit: p, market: market),
                progressCallback,
                (tracks) => { _ = storageCache.Save(StoreName, tracks); });
        }

        private async Task<int> GetRealSavedTrackCount(string market = "") =>
            (await api.GetSavedTracksAsync(10, 0, market)).Total;

        private async Task<int> GetIndexedDbTrackCount() =>
            await storageCache.GetCount(StoreName);

        private async Task<bool> RealAndMemoryChachedSavedTrackCountsMatch(string market = "")
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = LastRetrieval?.Result.Count() ?? 0;
            return (real == cached);
        }

        private async Task<bool> RealAndIndexedDbChachedSavedTrackCountsMatch(string market = "")
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = await GetIndexedDbTrackCount();
            return (real == cached);
        }
    }
}
