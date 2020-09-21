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
        internal const int RateLimitDelayMs = 500;
        internal const int DefaultBatchSize = 20;

        private static readonly SemaphoreSlim rateLimitLock = new SemaphoreSlim(1, 1);

        internal static async Task<IEnumerable<T>> DownloadPagedResources<T>(
            Func<int, int, Task<Paging<T>>> aquire,
            Action<int, int>? notify = null,
            Func<IEnumerable<T>, Task>? submit = null,
            int pageSize = 50)
        {
            return await ExecuteRateLimited(async () =>
            {
                var result = new List<T>();

                int offset = 0;
                while (true)
                {
                    var page = await aquire(offset, pageSize);

                    if (!(page.Items is null) && page.Items.Count > 0)
                    {
                        result.AddRange(page.Items);

                        notify?.Invoke(offset + page.Items.Count, page.Total);

                        if (!(submit is null))
                            await submit.Invoke(page.Items);
                    }

                    if (!page.HasNextPage()) // TODO: parallelize by batching
                        break;

                    offset += pageSize;

                    await Task.Delay(RateLimitDelayMs);
                }

                return result;
            });
        }

        internal static async Task<IEnumerable<TResult>> PaginateAndDownloadResources<TKey, TResult>(
            IEnumerable<TKey> keys,
            Func<IEnumerable<TKey>, Task<IEnumerable<TResult>>> aquire,
            int pageSize)
        {
            return await ExecuteRateLimited(async () =>
            {
                Console.WriteLine("PaginateAndDownloadResources");

                var pages = new List<IEnumerable<TKey>>();

                int keyCount = 0;
                while (keyCount < keys.Count())
                {
                    pages.Add(keys.Skip(keyCount).Take(pageSize));
                    keyCount += pageSize;
                }

                var pageDownloads = pages
                    .Select<IEnumerable<TKey>, Func<Task<IEnumerable<TResult>>>>(p =>
                        {
                            return () => aquire(p);
                        })
                    .ToList();

                return await BatchedDownload(pageDownloads);
            });
        }

        private static async Task<TResult> ExecuteRateLimited<TResult>(Func<Task<TResult>> task)
        {
            await rateLimitLock.WaitAsync();
            try
            {
                return await task();
            }
            finally
            {
                rateLimitLock.Release();
            }
        }

        private static async Task<IEnumerable<TResult>> BatchedDownload<TResult>(
            IEnumerable<Func<Task<IEnumerable<TResult>>>> pageDownloads)
        {
            var result = new List<TResult>();

            int pageCount = 0;
            while (pageCount < pageDownloads.Count())
            {
                var batch = pageDownloads.Skip(pageCount).Take(DefaultBatchSize).ToList();

                var batchResult = batch.Select(pt => pt()).ToList();
                await Task.WhenAll(batchResult);
                pageCount += DefaultBatchSize;

                batchResult.ForEach(p => result.AddRange(p.Result));

                await Task.Delay(RateLimitDelayMs);
            }

            return result;
        }
    }
}
