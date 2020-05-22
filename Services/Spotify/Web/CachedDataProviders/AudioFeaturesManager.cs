using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
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
    public class AudioFeaturesManager : CachedDataProviderBase<AudioFeatures>
    {
        private readonly SpotifyWebAPI api;
        private readonly IndexedDbCache<AudioFeatures> storageCache;
        private readonly IndexedDbCache<string> trackIdCache;

        private const string storeName = nameof(AudioFeatures);
        private const string trackIdsStoreName = nameof(AudioFeaturesManager);

        /// <summary>
        /// Once set to a non-empty <see cref="IEnumerable{}"/>, the next <see cref="CachedDataProviderBase{AudioFeatures}.GetData(Action{int, int}, string)"/> call will download the <see cref="AudioFeatures"/>.
        /// </summary>
        public IEnumerable<string> TrackIds { private get; set; } = new List<string>();

        public AudioFeaturesManager(SpotifyWebAPI spotifyWebApi, IndexedDbCache<AudioFeatures> dataCache, IndexedDbCache<string> administrativeCache)
        {
            api = spotifyWebApi;
            storageCache = dataCache;
            trackIdCache = administrativeCache;
        }

        protected override async Task<bool> IsMemoryCacheValid() =>
            await SavedAndSetTrackIdsMatch();

        protected override async Task<bool> IsStorageCacheValid() =>
            await SavedAndSetTrackIdsMatch();

        protected override async Task ClearStorageCache() =>
            await storageCache.Clear(storeName);

        protected override async Task<IEnumerable<AudioFeatures>> LoadStorageCache(Action<int, int> progressCallback, string market = "") =>
            await storageCache.Load(storeName, progressCallback);

        protected override async Task<IEnumerable<AudioFeatures>> LoadRemoteResource(Action<int, int> progressCallback, string market = "")
        {
            await trackIdCache.Save(trackIdsStoreName, new List<string>(TrackIds));

            var result = new List<AudioFeatures>();

            const int rateLimitDelayMs = 100;
            const int pageSize = 100;
            int offset = 0;
            while(offset < TrackIds.Count())
            {
                var downloaded = (await api.GetSeveralAudioFeaturesAsync(TrackIds.Skip(offset).Take(pageSize).ToList())).AudioFeatures;

                progressCallback(offset, TrackIds.Count());

                result.AddRange(downloaded);
                await storageCache.Save(storeName, downloaded);

                offset += pageSize;

                await Task.Delay(rateLimitDelayMs);
            }

            return result;
        }

        private async Task<bool> SavedAndSetTrackIdsMatch()
        {
            var cachedTrackIds = await trackIdCache.Load(trackIdsStoreName, null);

            /// On first load. Theoretically, <see cref="TrackIds"/> should be loaded when the <see cref="AudioFeaturesManager"/> is constructed, but there it could not be awaited, so we couldn't guarantee a coherent state when checking cache validity.
            if (!TrackIds.Any() && cachedTrackIds.Any())
            {
                TrackIds = cachedTrackIds;
                return true;
            }

            return new HashSet<string>(TrackIds).SetEquals(cachedTrackIds);
        }
    }
}
