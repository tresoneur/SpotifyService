using Blazor.Extensions.Storage;
using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Configuration;
using Caerostris.Services.Spotify.IndexedDB;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Sections;
using Caerostris.Services.Spotify.Services.Spotify.Web.Api;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Caerostris.Services.Spotify
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register a SpotifyService and all its dependecies.
        /// </summary>
        public static IServiceCollection AddSpotify(this IServiceCollection services, SpotifyServiceConfiguration configuration)
        {
            // LocalStorage
            services.AddStorage();

            // IndexedDB
            services.AddIndexedDB(dbStore =>
            {
                dbStore.DbName = nameof(SpotifyService);
                dbStore.Version = 4;

                var stores = new List<string>()
                {
                    nameof(SavedTrack),
                    nameof(TrackAudioFeatures),
                    nameof(AudioFeaturesManager),
                    nameof(Web.ViewModels.Sections)
                };
                foreach (var name in stores)
                {
                    dbStore.Stores.Add(new StoreSchema
                    {
                        Name = name,
                        PrimaryKey = new IndexSpec { Auto = true }
                    });
                }
            });

            // "Blazor WebAssembly apps don't currently have a concept of DI scopes. Scoped-registered services behave like Singleton services." <see ref="https://docs.microsoft.com/en-us/aspnet/core/blazor/dependency-injection?view=aspnetcore-3.1"/>

            services.AddScoped<SpotifyServiceConfiguration>(_ => configuration);
            services.AddScoped<Api>();
            services.AddScoped<IndexedDbCache<SavedTrack>>();
            services.AddScoped<SavedTrackManager>();
            services.AddScoped<IndexedDbCache<TrackAudioFeatures>>();
            services.AddScoped<IndexedDbCache<string>>();
            services.AddScoped<AudioFeaturesManager>();

            if (configuration.AuthServerApiBase is null)
                services.AddScoped<AuthManagerBase, ImplicitGrantAuthManager>();
            else
                services.AddScoped<AuthManagerBase, AuthorizationCodeAuthManager>();

            services.AddScoped<WebApiManager>();
            services.AddScoped<WebPlaybackSdkManager>();
            services.AddScoped<MediaSessionManager>();
            
            // The dependency injection module will take care of the Dispose() calls
            services.AddScoped<AuthorizationService>();
            services.AddScoped<PlaybackService>();
            services.AddScoped<UserService>();
            services.AddScoped<ContextsService>();
            services.AddScoped<ExploreService>();
            services.AddScoped<LibraryService>();
            services.AddScoped<SpotifyService>();

            return services;
        }

        /// <summary>
        /// Extension method to initialize SpotifyService. The user must call this method before running the WebAssemblyHost.
        /// </summary>
        /// <param name="playerDeviceName">The name of the playback device to be created (that will show up in the list of available devices in Spotify clients).</param>
        /// <param name="permissionScopes">
        /// Permissions that your app will need, as defined by the Spotify Web API documentation.
        /// SpotifyService will make no attempt to stop you from using functionality that you have not requested permissions for.
        /// When you add a new permission, users will have to re-authorize your app. The easiest way to force this is to call <see cref="SpotifyService.Logout"/> if you do not make use of the Listen Later functionality.
        /// </param>
        public static async Task InitializeSpotify(this IServiceProvider host) => 
            await host.GetRequiredService<SpotifyService>().Initialize();
    }
}
