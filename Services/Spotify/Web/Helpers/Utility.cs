using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.Helpers
{
    internal static class Utility
    {
        internal static async Task<IEnumerable<T>> DownloadPagedResources<T>(
            Func<int, int, Task<Paging<T>>> aquire,
            Action<int, int>? notify = null,
            Func<IEnumerable<T>, Task>? submit = null)
        {
            const int rateLimitDelayMs = 100;
            int pageSize = 50;

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

                if (!page.HasNextPage())
                    break;

                offset += pageSize;

                await Task.Delay(rateLimitDelayMs);
            }

            return result;
        }
    }
}
