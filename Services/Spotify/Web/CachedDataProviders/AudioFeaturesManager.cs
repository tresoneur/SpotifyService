using Caerostris.Services.Spotify.Services.Spotify.Web.Api;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    /// <remarks>
    /// This DataProvider will not download any AudioFeatures by default.
    /// The list of the desired AudioFeatures has to be set manually.
    /// </remarks>
    public class AudioFeaturesManager : CachedDataProviderBase<TrackAudioFeatures>
    {
        private readonly Api api;
        private readonly IndexedDbCache<TrackAudioFeatures> storageCache;
        private readonly IndexedDbCache<string> trackIdCache;

        private const string StoreName = nameof(TrackAudioFeatures);
        private const string TrackIdsStoreName = nameof(AudioFeaturesManager);

        /// <summary>
        /// Once set to a non-empty <see cref="IEnumerable{T}"/>, the next <see cref="CachedDataProviderBase{AudioFeatures}.GetData(Action{int, int}, string)"/> call will download the <see cref="AudioFeatures"/>.
        /// </summary>
        public IEnumerable<string> TrackIds { private get; set; } = new List<string>();

        public AudioFeaturesManager(Api spotifyWebApi, IndexedDbCache<TrackAudioFeatures> dataCache, IndexedDbCache<string> administrativeCache)
        {
            api = spotifyWebApi;
            storageCache = dataCache;
            trackIdCache = administrativeCache;
        }

        protected override async Task<bool> IsMemoryCacheValid() =>
            await SavedAndSetTrackIdsMatch();

        protected override async Task<bool> IsStorageCacheValid() =>
            await SavedAndSetTrackIdsMatch();

        public override async Task ClearStorageCache() =>
            await storageCache.Clear(StoreName);

        protected override async Task<IEnumerable<TrackAudioFeatures>> LoadStorageCache(Action<int, int> progressCallback, string? market = null) =>
            await storageCache.Load(StoreName, progressCallback);

        protected override async Task<IEnumerable<TrackAudioFeatures>> LoadRemoteResource(Action<int, int> progressCallback, string? market = null)
        {
            await trackIdCache.Save(TrackIdsStoreName, new List<string>(TrackIds));

            var result = new List<TrackAudioFeatures>();

            const int rateLimitDelayMs = 100;
            const int pageSize = 100;
            int offset = 0;
            while(offset < TrackIds.Count())
            {
                var downloaded = (await api.Client.Tracks.GetSeveralAudioFeatures(new(TrackIds.Skip(offset).Take(pageSize).ToList()))).AudioFeatures;

                progressCallback(offset, TrackIds.Count());

                result.AddRange(downloaded);
                await storageCache.Save(StoreName, downloaded);

                offset += pageSize;

                await Task.Delay(rateLimitDelayMs);
            }

            return result;
        }

        private async Task<bool> SavedAndSetTrackIdsMatch()
        {
            var cachedTrackIds = await trackIdCache.Load(TrackIdsStoreName, null);

            /// On first load. Theoretically, <see cref="TrackIds"/> should be loaded when the <see cref="AudioFeaturesManager"/> is constructed, but it cannot be awaited there, so we can't guarantee a coherent state when checking cache validity.
            if (!TrackIds.Any() && cachedTrackIds.Any())
            {
                TrackIds = cachedTrackIds;
                return true;
            }

            return new HashSet<string>(TrackIds).SetEquals(cachedTrackIds);
        }
    }
}
