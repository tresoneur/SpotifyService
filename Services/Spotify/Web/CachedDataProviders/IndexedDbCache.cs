using SpotifyService.IndexedDB;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.CachedDataProviders
{
    public class IndexedDbCache<TData>
    {
        private readonly IndexedDBManager indexedDb;

        public IndexedDbCache(IndexedDBManager indexedDbManager)
        {
            indexedDb = indexedDbManager;
        }

        public async Task Save(string storeName, IEnumerable<TData> entities)
        {
            foreach (var entity in entities)
                await indexedDb.AddRecord(new StoreRecord<TData> { Storename = storeName, Data = entity });
        }

        public async Task<IEnumerable<TData>> Load(string storeName, Action<int, int>? progressCallback)
        {
            var records = new List<TData>();
            const int count = 10;
            int cachedCount = await GetCount(storeName);
            int offset = 0;

            while (offset < cachedCount)
            {
                var paginatedRecords =
                    await indexedDb.GetPaginatedRecords<TData>(
                        new StoreIndexQuery<object> { Storename = storeName, IndexName = null /* = primary key */ },
                        offset,
                        count);

                records.AddRange(paginatedRecords);
                progressCallback?.Invoke(records.Count, cachedCount);

                if (paginatedRecords.Count < count)
                    break;

                offset += count;
            }

            return records;
        }

        public async Task Clear(string storeName) =>
            await indexedDb.ClearStore(storeName);

        public async Task<int> GetCount(string storeName) =>
            await indexedDb.GetCount(storeName);
    }
}
