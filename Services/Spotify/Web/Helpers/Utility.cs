using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.Helpers
{
    internal static class Utility
    {
        internal const int RateLimitDelayMs = 250;
        internal const int DefaultBatchSize = 10;
        internal const int DefaultPageSize = 50;

        private static readonly SemaphoreSlim RateLimitLock = new SemaphoreSlim(1, 1);

        internal static async Task<IEnumerable<TResult>> SynchronizedDownloadPagedResources<TResult>(
            Func<int, int, Task<Paging<TResult>>> aquire,
            Action<int, int>? notify = null,
            Action<IEnumerable<TResult>>? submit = null,
            int pageSize = DefaultPageSize)
        {
            return await ExecuteRateLimited(async () =>
            {
                var results = new List<TResult>();

                void addToResults(IEnumerable<TResult> newResults, int total)
                {
                    results.AddRange(newResults);

                    notify?.Invoke(results.Count, total);
                    submit?.Invoke(newResults);
                }

                // Aquire total count.
                var firstPage = await aquire(0, pageSize);
                addToResults(firstPage.Items, firstPage.Total);

                // The rest can be downloaded in batches.
                var pages = new List<Func<Task<IEnumerable<TResult>>>>();
                for (int offset = pageSize; offset < firstPage.Total; offset += pageSize)
                {
                    var o = offset;
                    pages.Add(async () => (await aquire(o, pageSize)).Items);
                }

                await DownloadBatched(
                    pages,
                    (newResults) => addToResults(newResults, firstPage.Total));

                return results;
            });
        }

        internal static async Task<IEnumerable<TResult>> SynchronizedPaginateAndDownloadResources<TKey, TResult>(
            IEnumerable<TKey> keys,
            Func<IEnumerable<TKey>, Task<IEnumerable<TResult>>> aquire,
            int pageSize = DefaultPageSize)
        {
            return await ExecuteRateLimited(async () =>
            {
                var pages = new List<IEnumerable<TKey>>();

                int keyCount = 0;
                while (keyCount < keys.Count())
                {
                    pages.Add(keys.Skip(keyCount).Take(pageSize));
                    keyCount += pageSize;
                }

                var pageDownloads = pages
                    .Select<IEnumerable<TKey>, Func<Task<IEnumerable<TResult>>>>(p => () => aquire(p))
                    .ToList();

                return await DownloadBatched(pageDownloads);
            });
        }

        internal static async Task<IEnumerable<TResult>> SynchronizedDownloadBatched<TResult>(
            IEnumerable<Func<Task<IEnumerable<TResult>>>> pageDownloads,
            Action<IEnumerable<TResult>>? interBatchCallback = null)
        {
            return await ExecuteRateLimited(() => DownloadBatched(pageDownloads, interBatchCallback));
        }

        private static async Task<TResult> ExecuteRateLimited<TResult>(Func<Task<TResult>> task)
        {
            await RateLimitLock.WaitAsync();
            try
            {
                return await task();
            }
            finally
            {
                RateLimitLock.Release();
            }
        }

        private static async Task<IEnumerable<TResult>> DownloadBatched<TResult>(
            IEnumerable<Func<Task<IEnumerable<TResult>>>> pageDownloads,
            Action<IEnumerable<TResult>>? interBatchCallback = null)
        {
            var result = new List<TResult>();

            int pageCount = 0;
            while (pageCount < pageDownloads.Count())
            {
                var batch = pageDownloads.Skip(pageCount).Take(DefaultBatchSize).ToList();

                var batchResult = batch.Select(pt => pt()).ToList();
                await Task.WhenAll(batchResult);
                pageCount += DefaultBatchSize;

                var newResults = batchResult.SelectMany(p => p.Result);

                result.AddRange(newResults);

                interBatchCallback?.Invoke(newResults);

                await Task.Delay(RateLimitDelayMs);
            }

            return result;
        }
    }
}
