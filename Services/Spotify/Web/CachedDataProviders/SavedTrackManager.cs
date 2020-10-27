using SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Services.Spotify.Web.Api;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    /// <remarks>
    /// There is no way to get e.g. a hash of all track IDs from Spotify, so we have to improvise a way to tell if the Library has been updated since we last cached it. 
    /// The method of checking the number of saved tracks is obviously quite crude, but it should work most of the time (people don't frequently remove tracks from their libraries).
    /// </remarks>
    public class SavedTrackManager : CachedDataProviderBase<SavedTrack>
    {
        private readonly Api api;
        private readonly IndexedDbCache<SavedTrack> storageCache;

        private const string StoreName = nameof(SavedTrack);

        public SavedTrackManager(Api spotifyWebApi, IndexedDbCache<SavedTrack> indexedDbCache)
        {
            api = spotifyWebApi;
            storageCache = indexedDbCache;
        }

        protected override async Task<bool> IsMemoryCacheValid() =>
            await RealAndMemoryChachedSavedTrackCountsMatch();

        protected override async Task<bool> IsStorageCacheValid() =>
            await RealAndIndexedDbChachedSavedTrackCountsMatch();

        public override async Task ClearStorageCache() =>
            await storageCache.Clear(StoreName);

        protected override async Task<IEnumerable<SavedTrack>> LoadStorageCache(Action<int, int> progressCallback, string? market = null) =>
            await storageCache.Load(StoreName, progressCallback);

        protected override async Task<IEnumerable<SavedTrack>> LoadRemoteResource(Action<int, int> progressCallback, string? market = null)
        {
            return await Utility.SynchronizedDownloadPagedResources(
                (o, p) => api.Client.Library.GetTracks(new() { Offset = o, Limit = p, Market = market }),
                progressCallback,
                (tracks) => { _ = storageCache.Save(StoreName, tracks); });
        }

        private async Task<int> GetRealSavedTrackCount(string? market = null) =>
            (await api.Client.Library.GetTracks(new() { Offset = 0, Limit = 10, Market = market })).Total ?? 0;

        private async Task<int> GetIndexedDbTrackCount() =>
            await storageCache.GetCount(StoreName);

        private async Task<bool> RealAndMemoryChachedSavedTrackCountsMatch(string? market = null)
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = LastRetrieval?.Result.Count() ?? 0;
            return (real == cached);
        }

        private async Task<bool> RealAndIndexedDbChachedSavedTrackCountsMatch(string? market = null)
        {
            var real = await GetRealSavedTrackCount(market);
            var cached = await GetIndexedDbTrackCount();
            return (real == cached);
        }
    }
}
