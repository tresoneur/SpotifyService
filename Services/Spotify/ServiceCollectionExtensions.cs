using Blazor.Extensions.Storage;
using Caerostris.Services.Spotify.Auth;
using Caerostris.Services.Spotify.Auth.Abstract;
using Caerostris.Services.Spotify.Configuration;
using Caerostris.Services.Spotify.Player;
using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using Microsoft.Extensions.DependencyInjection;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.IndexedDB;
using System;
using System.Threading.Tasks;
using Caerostris.Services.Spotify.Web.ViewModels;

namespace Caerostris.Services.Spotify
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Extension method to register a SpotifyService and all its dependecies.
        /// </summary>
        public static IServiceCollection AddSpotify(this IServiceCollection services, string? authServerApiBase = null)
        {
            // LocalStorage
            services.AddStorage();

            // IndexedDB
            services.AddIndexedDB(dbStore =>
            {
                dbStore.DbName = nameof(SpotifyService);
                dbStore.Version = 3;

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = nameof(SavedTrack),
                    PrimaryKey = new IndexSpec { Auto = true }
                });

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = nameof(AudioFeatures),
                    PrimaryKey = new IndexSpec { Auto = true }
                });

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = nameof(AudioFeaturesManager),
                    PrimaryKey = new IndexSpec { Auto = true }
                });

                dbStore.Stores.Add(new StoreSchema
                {
                    Name = nameof(Sections),
                    PrimaryKey = new IndexSpec { Auto = true }
                });
            });

            // "Blazor WebAssembly apps don't currently have a concept of DI scopes. Scoped-registered services behave like Singleton services." <see ref="https://docs.microsoft.com/en-us/aspnet/core/blazor/dependency-injection?view=aspnetcore-3.1"/>

            // Injected SpotifyService dependencies
            services.AddScoped<SpotifyServiceConfiguration>(_ => new () { AuthServerApiBase = authServerApiBase });
            services.AddScoped<SpotifyWebAPI>();
            services.AddScoped<IndexedDbCache<SavedTrack>>();
            services.AddScoped<SavedTrackManager>();
            services.AddScoped<IndexedDbCache<AudioFeatures>>();
            services.AddScoped<IndexedDbCache<string>>();
            services.AddScoped<AudioFeaturesManager>();

            if (authServerApiBase is null)
                services.AddScoped<AuthManagerBase, ImplicitGrantAuthManager>();
            else
                services.AddScoped<AuthManagerBase, AuthorizationCodeAuthManager>();

            services.AddScoped<WebApiManager>();
            services.AddScoped<WebPlaybackSdkManager>();
            
            // The dependency injection module will take care of the Dispose() call
            services.AddScoped<SpotifyService>();

            return services;
        }

        /// <summary>
        /// Extension method to initialize SpotifyService. The user must call this method before running the WebAssemblyHost.
        /// </summary>
        /// <param name="playerDeviceName">The name of the device that will show up in the list of available playback devices in Spotify clients.</param>
        /// <param name="permissionScopes">
        /// Permissions that your app will need.
        /// SpotifyService will make no attempt to stop you from using functionality that you have not requested permissions for.
        /// When you add a new permission, users will have to re-authorize your app. The easiest way to force this is to call <see cref="SpotifyService.Logout"/>.
        /// </param>
        public static async Task InitializeSpotify(this IServiceProvider host, string playerDeviceName, string clientId, Scope permissionScopes)
        {
            var spotifyService = host.GetRequiredService<SpotifyService>();
            await spotifyService.Initialize(playerDeviceName, clientId, permissionScopes);
        }
    }
}
