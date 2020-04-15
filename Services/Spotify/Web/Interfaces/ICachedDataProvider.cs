using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web.Interfaces
{
    public interface ICachedDataProvider<TData>
    {
        /// <summary>
        /// Fetches the remote or cached data. 
        /// Caches frequently provide in-memory and LocalStorage/IndexedDB caching and have differing rules of cache invalidation, with some providing custom interfaces to allow for users to choose when up-to-date data should be downloaded.
        /// </summary>
        /// <param name="progressCallback">Called when data is being loaded from cache or downloaded from the remote server. Parameters: loaded, total.</param>
        Task<IEnumerable<TData>> GetData(Action<int, int> progressCallback, string market = "");
    }
}
