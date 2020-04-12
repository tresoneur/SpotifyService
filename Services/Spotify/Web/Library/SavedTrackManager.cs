using SpotifyAPI.Web;
using SpotifyAPI.Web.Models;
using SpotifyService.IndexedDB;
using SpotifyService.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cearostris.Services.Spotify.Web.Library
{
    public class SavedTrackManager
    {
        private readonly SpotifyWebAPI api;
        private readonly IndexedDBManager indexedDB;

        private Task<IEnumerable<SavedTrack>>? lastRetrieval; // TODO: weak reference

        public SavedTrackManager(SpotifyWebAPI spotifyWebApi, IndexedDBManager indexedDBManager)
        {
            api = spotifyWebApi;
            indexedDB = indexedDBManager;
        }

        public async Task<IEnumerable<SavedTrack>> GetSavedTracks(Action<int, int> progressCallback, string market = "") // TODO: ez nem szálbiztos
        {
            if (!(lastRetrieval is null))
                if (!lastRetrieval.IsCompleted)
                    return await lastRetrieval;
                else if (await RealAndMemoryChachedSavedTrackCountsMatch())
                    return lastRetrieval.Result;

            Console.WriteLine("getting indexeddb cache");
            var cachedTracks = await SetAsLastRetrievalAndAwait(() => GetIndexedDbCachedSavedTracks(progressCallback, market));
            if (cachedTracks.Count() > 0)
                return cachedTracks;
            Console.WriteLine("downloading");
            var remoteTracks = await SetAsLastRetrievalAndAwait(() => DownloadSavedTracks(progressCallback));
            return remoteTracks;
        }

        public async Task<IEnumerable<SavedTrack>> GetIndexedDbCachedSavedTracks(Action<int, int> progressCallback, string market = "")
        {
            /// There is no way to get e.g. a hash of all track IDs from Spotify, so we have to improvise a way to tell if the Library has been updated since we last cached it. 
            ///  This method is obviously quite crude, but should work for /most/ intents and purposes (people don't frequently remove tracks from their libraries).
            if (!await RealAndIndexedDbChachedSavedTrackCountsMatch(market))
            {
                await indexedDB.ClearStore(nameof(SavedTrack));
                return new List<SavedTrack>();
            }

            var records = new List<SavedTrack>();
            const int count = 100;
            int cachedCount = await GetIndexedDbTrackCount();
            int offset = 0;

            while (offset < cachedCount)
            {
                var paginatedRecords =
                    await indexedDB.GetPaginatedRecords<SavedTrack>(
                        new StoreIndexQuery<object> { Storename = nameof(SavedTrack), IndexName = null /* = primary key */ },
                        offset,
                        count);

                records.AddRange(paginatedRecords);
                progressCallback(records.Count, cachedCount);

                if (paginatedRecords.Count() < count)
                    break;

                offset += count;
            }

            return records;
        }

        public async Task<int> GetSavedTrackCount(string market = "") =>
            await GetRealSavedTrackCount(market);

        private async Task<IEnumerable<SavedTrack>> SetAsLastRetrievalAndAwait(Func<Task<IEnumerable<SavedTrack>>> func)
        {
            lastRetrieval = func();
            return await lastRetrieval;
        }

        private async Task<IEnumerable<SavedTrack>> DownloadSavedTracks(Action<int, int> progressCallback, string market = "")
        {
            return await Utility.DownloadPagedResources(
                (o, p) => api.GetSavedTracksAsync(offset: o, limit: p, market: market),
                progressCallback,
                SaveSavedTracks);
        }

        private async Task SaveSavedTracks(IEnumerable<SavedTrack> tracks)
        {
            foreach (var track in tracks)
                await indexedDB.AddRecord(new StoreRecord<SavedTrack>
                {
                    Storename = nameof(SavedTrack),
                    Data = track
                });
        }

        private async Task<int> GetRealSavedTrackCount(string market = "") =>
            (await api.GetSavedTracksAsync(10, 0, market)).Total;

        private async Task<int> GetIndexedDbTrackCount() =>
            await indexedDB.GetCount(nameof(SavedTrack));

        private async Task<bool> RealAndMemoryChachedSavedTrackCountsMatch(string market = "")
        {
            if (lastRetrieval is null || !lastRetrieval.IsCompleted)
                return false;

            var real = await GetRealSavedTrackCount(market);
            var cached = lastRetrieval.Result.Count();
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
