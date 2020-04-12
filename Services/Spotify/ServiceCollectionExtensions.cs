using Blazor.Extensions.Storage;
using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using Cearostris.Services.Spotify.Web.Library;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
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

            /// <remarks>
            /// "Blazor WebAssembly apps don't currently have a concept of DI scopes. Scoped-registered services behave like Singleton services." <see ref="https://docs.microsoft.com/en-us/aspnet/core/blazor/dependency-injection?view=aspnetcore-3.1"/>
            /// </remarks>

            // The dependency injection module will take care of the Dispose() call
            services.AddScoped<SpotifyService>();

            // Injected SpotifyService dependencies
            services.AddScoped<SpotifyWebAPI>();
            services.AddScoped<SavedTrackManager>();
            services.AddScoped<ImplicitGrantAuthManager>();
            services.AddScoped<WebAPIManager>();
            services.AddScoped<WebPlaybackSDKManager>();

            return services;
        }

        public async static Task InitializeSpotify(this IServiceProvider host)
        {
            var spotifyService = host.GetRequiredService<SpotifyService>();
            await spotifyService.Initialize();
        }
    }
}
