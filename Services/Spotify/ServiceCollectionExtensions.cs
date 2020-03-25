using Blazor.Extensions.Storage;
using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web.Models;
using SpotifyService.IndexedDB;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register a SpotifyService and all its dependecies.
        /// </summary>
        public static IServiceCollection AddSpotify(this IServiceCollection services)
        {
            // LocalStorage
            services.AddStorage();

            // IndexedDB
            services.AddIndexedDB(dbStore =>
            {
                dbStore.DbName = "SpotifyService";
                dbStore.Version = 1;

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = nameof(SavedTrack),
                    PrimaryKey = new IndexSpec { Auto = true }
                    /*
                    PrimaryKey = new IndexSpec { Name = "name", KeyPath = "track.name", Auto = true , Unique = false},
                    Indexes = new List<IndexSpec>
                    {
                        new IndexSpec{Name="album", KeyPath = "track.album.name", Auto=false, Unique = false }
                    }
                    */
                });
            });


            // The dependency injection module will take care of the Dispose() call
            services.AddSingleton<SpotifyService>();

            // Injected SpotifyService dependencies
            services.AddSingleton<ImplicitGrantAuthManager>();
            services.AddSingleton<WebAPIManager>();
            services.AddSingleton<WebPlaybackSDKManager>();

            return services;
        }

        public async static Task InitializeSpotify(this IServiceProvider host)
        {
            var spotifyService = host.GetRequiredService<SpotifyService>();
            await spotifyService.Initialize();
        }
    }
}
