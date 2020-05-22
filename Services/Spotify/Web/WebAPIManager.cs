using Caerostris.Services.Spotify.Web.ViewModels;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web
{
    /// <remarks>
    /// The chief goal of this class is to provide in-memory, LocalStorage and IndexedDB caching as well as to automatically supply parameters to SpotifyWebAPI to enable e.g. Track Relinking.
    /// </remarks>
    public class WebApiManager
    {
        private readonly SpotifyWebAPI api;
        private readonly SavedTrackManager savedTracks;
        private readonly AudioFeaturesManager audioFeatures;

        public WebApiManager(SpotifyWebAPI spotifyWebApi, SavedTrackManager savedTrackManager, AudioFeaturesManager audioFeaturesManager)
        {
            api = spotifyWebApi;
            savedTracks = savedTrackManager;
            audioFeatures = audioFeaturesManager;
        }

        /// <summary>
        /// We consider the private profile of the user to be unchanging during the typical lifecycle of this application, and therefore it gets cached in its entirety.
        /// </summary>
        private PrivateProfile? privateProfile;

        /// <returns>The private profile of the user</returns>
        public async Task<PrivateProfile> GetPrivateProfile()
        {
            if (privateProfile is null || privateProfile.HasError())
                privateProfile = await api.GetPrivateProfileAsync();

            return privateProfile;
        }

        public async Task<PlaybackContext> GetPlayback() =>
            await api.GetPlaybackAsync(await GetMarket());

        public async Task<ErrorResponse?> ResumePlayback() =>
            await api.ResumePlaybackAsync(offset: "");

        public async Task<ErrorResponse?> SetPlayback(string? contextUri, string? trackUri)
        {
            if (contextUri is null && trackUri is null)
                return new ErrorResponse();
            else if (contextUri is null && !(trackUri is null))
                return await api.ResumePlaybackAsync(deviceId: "", contextUri: "", (new [] { trackUri }).ToList(), offset: "");
            else
                return await api.ResumePlaybackAsync(deviceId: "", contextUri: contextUri, null, offset: trackUri ?? "");
        }

        public async Task<ErrorResponse?> SetPlayback(IEnumerable<string> Uris) =>
            await api.ResumePlaybackAsync(deviceId: "", contextUri: "", Uris.ToList(), offset: "");

        public async Task<ErrorResponse?> PausePlayback() =>
            await api.PausePlaybackAsync();

        public async Task<ErrorResponse?> SkipPlaybackToNext() =>
            await api.SkipPlaybackToNextAsync();

        public async Task<ErrorResponse?> SkipPlaybackToPrevious() =>
            await api.SkipPlaybackToPreviousAsync();

        public async Task<ErrorResponse?> SeekPlayback(int positionMs) =>
            await api.SeekPlaybackAsync(positionMs);

        public async Task<AvailabeDevices> GetDevices() =>
            await api.GetDevicesAsync();

        public async Task<ErrorResponse?> TransferPlayback(string deviceId, bool play = false) =>
            await api.TransferPlaybackAsync(deviceId, play);

        public async Task<ErrorResponse?> SetShuffle(bool shuffle) =>
            await api.SetShuffleAsync(shuffle);

        public async Task<ErrorResponse?> SetRepeatMode(RepeatState state) =>
            await api.SetRepeatModeAsync(state);

        public async Task<ErrorResponse?> SetVolume(int volumePercent) =>
            await api.SetVolumeAsync(volumePercent);

        public async Task<IEnumerable<SavedTrack>> GetSavedTracks(Action<int, int> progressCallback) =>
            await savedTracks.GetData(progressCallback, await GetMarket());
        
        public async Task<ArtistProfile> GetArtist(string id)
        {
            var market = await GetMarket();

            var fullArtist = api.GetArtistAsync(id);
            var relatedArtists = api.GetRelatedArtistsAsync(id);
            var artistAlbums = Utility.DownloadPagedResources((o, p) => api.GetArtistsAlbumsAsync(id, offset: o, limit: p, market: market));
            var artistTopTracks = api.GetArtistsTopTracksAsync(id, country: market);

            await Task.WhenAll(fullArtist, relatedArtists, artistAlbums, artistTopTracks);

            return new ArtistProfile
            {
                Artist = fullArtist.Result,
                TopTracks = artistTopTracks.Result.Tracks,
                Albums = artistAlbums.Result,
                RelatedArtists = relatedArtists.Result.Artists
            };
        }

        public async Task<CompleteAlbum> GetAlbum(string id)
        {
            var market = await GetMarket();

            var fullAlbum = api.GetAlbumAsync(id, market: market);
            var albumTracks = Utility.DownloadPagedResources(
                (o, p) => api.GetAlbumTracksAsync(id: id, offset: o, limit: p, market: market));

            await Task.WhenAll(fullAlbum, albumTracks);

            return new CompleteAlbum { Album = fullAlbum.Result, Tracks = albumTracks.Result };
        }

        public async Task<CompletePlaylist> GetPlaylist(string id)
        {
            var market = await GetMarket();

            var fullPlaylist = api.GetPlaylistAsync(id, market: market);
            var playlistTracks = Utility.DownloadPagedResources(
                (o, p) => api.GetPlaylistTracksAsync(playlistId: id, offset: o, limit: p, market: market));

            await Task.WhenAll(fullPlaylist, playlistTracks);

            return new CompletePlaylist { Playlist = fullPlaylist.Result, Tracks = playlistTracks.Result };
        }

        public async Task<IEnumerable<SimplePlaylist>> GetUserPlaylists(string id) =>
            await Utility.DownloadPagedResources((o, p) => api.GetUserPlaylistsAsync(userId: id, offset: o, limit: p));

        public async Task<bool> GetTrackSavedStatus(string id) =>
            (await api.CheckSavedTracksAsync(new [] { id }.ToList())).List.FirstOrDefault();

        public async Task<ErrorResponse> SaveTrack(string id) =>
            await api.SaveTrackAsync(id);

        public async Task<ErrorResponse> RemoveSavedTrack(string Id) =>
            await api.RemoveSavedTracksAsync(new [] { Id }.ToList());

        public async Task<SearchItem> Search(string query) =>
            await api.SearchItemsEscapedAsync(query, SearchType.All, 6, 0, await GetMarket());

        public async Task<IEnumerable<AudioFeatures>> GetAudioFeatures(IEnumerable<string> trackIds, Action<int, int> progressCallback)
        {
            audioFeatures.TrackIds = trackIds;
            return await audioFeatures.GetData(progressCallback, await GetMarket());
        }

        public async Task<FullPlaylist> CreatePlaylist(string userId, string title, string description) =>
            await api.CreatePlaylistAsync(userId, title, false, false, description);

        public async Task<ErrorResponse> AddPlaylistTracks(string playlistId, IEnumerable<string> trackUris) =>
            await api.AddPlaylistTracksAsync(playlistId, trackUris.ToList(), null);

        #region Comfort

        private async Task<string> GetMarket()
        {
            return (await GetPrivateProfile()).Country;
        }

        #endregion
    }
}
