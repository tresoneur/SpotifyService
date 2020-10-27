using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.Helpers
{
    internal static class Utility
    {
        internal const int RateLimitDelayMs = 25;
        internal const int BatchRateLimitDelayMs = 250;
        internal const int DefaultBatchSize = 10;
        internal const int DefaultPageSize = 50;

        private static readonly SemaphoreSlim RateLimitLock = new SemaphoreSlim(1, 1);

        internal static async Task<IEnumerable<TResult>> SynchronizedDownloadPagedResources<TResult>(
            Func<int, int, Task<Paging<TResult>>> aquire,
            Action<int, int>? notify = null,
            Action<IEnumerable<TResult>>? submit = null,
            int pageSize = DefaultPageSize,
            int? maxPages = null)
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
                addToResults(firstPage.Items, firstPage.Total ?? 0);

                // The rest can be downloaded in parallel.
                if (firstPage.Items.Count < firstPage.Total)
                {
                    var pages = new List<Func<Task<IEnumerable<TResult>>>>();
                    for (int offset = pageSize; offset < ((maxPages * pageSize) ?? firstPage.Total); offset += pageSize)
                    {
                        var o = offset;
                        pages.Add(async () => (await aquire(o, pageSize)).Items);
                    }

                    await DownloadParallel(
                        pages,
                        (newResults) => addToResults(newResults, firstPage.Total ?? 0));
                }

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

                return await DownloadParallel(pageDownloads);
            });
        }

        internal static async Task<IEnumerable<TResult>> SynchronizedDownloadParallel<TResult>(
            IEnumerable<Func<Task<IEnumerable<TResult>>>> pageDownloads,
            Action<IEnumerable<TResult>>? submit = null)
        {
            return await ExecuteRateLimited(() => DownloadParallel(pageDownloads, submit));
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

        private static async Task<IEnumerable<TResult>> DownloadParallel<TResult>(
            IEnumerable<Func<Task<IEnumerable<TResult>>>> pageDownloads,
            Action<IEnumerable<TResult>>? submit = null)
        {
            List<TResult> result = new();
            List<(IEnumerable<TResult>, int)> queuedResults = new();
            var orderPreservingLastSubmittedIndex = -1;
            List<Task> downloads = new();

            void submitResults(IEnumerable<TResult> newResults)
            {
                result.AddRange(newResults);
                submit?.Invoke(newResults);
            }

            async Task requestPage(Func<Task<IEnumerable<TResult>>> downloadPage, int pageIndex)
            {
                var newResults = await downloadPage();

                queuedResults.Add((newResults, pageIndex));

                while (queuedResults
                        .Where(r => ((r.Item2 - 1) == orderPreservingLastSubmittedIndex))
                        .FirstOrDefault()
                        is (IEnumerable<TResult>, int) nextToBeSubmitted)
                {
                    submitResults(nextToBeSubmitted.Item1);
                    queuedResults.Remove(nextToBeSubmitted);
                    orderPreservingLastSubmittedIndex = nextToBeSubmitted.Item2;
                }
            }

            foreach (var page in pageDownloads.Select((p, i) => new { Page = p, Index = i }))
            {
                downloads.Add(requestPage(page.Page, page.Index));
                await Task.Delay(RateLimitDelayMs);
            }

            await Task.WhenAll(downloads);

            return result;
        }
    }
}
