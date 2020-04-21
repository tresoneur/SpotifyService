using Caerostris.Services.Spotify.Web.ViewModels;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Web
{
    /// <remarks>
    /// This class is not intented to follow an actual proxy pattern, because most of the functionality offered by SpotifyWebAPI is never used by this service.
    /// The chief goal of this class is to provide in-memory, LocalStorage and IndexedDB caching as well as to automatically supply parameters to SpotifyWebAPI to enable e.g. Track Relinking.
    /// </remarks>
    public class WebAPIManager
    {
        private readonly SpotifyWebAPI api;
        private readonly SavedTrackManager savedTracks;
        private readonly AudioFeaturesManager audioFeatures;

        public WebAPIManager(SpotifyWebAPI spotifyWebApi, SavedTrackManager savedTrackManager, AudioFeaturesManager audioFeaturesManager)
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
                return await api.ResumePlaybackAsync(deviceId: "", contextUri: "", (new string[] { trackUri }).ToList(), offset: "");
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
        
        public async Task<ArtistProfile> GetArtist(string Id)
        {
            var market = await GetMarket();

            var fullArtist = api.GetArtistAsync(Id);
            var relatedArtists = api.GetRelatedArtistsAsync(Id);
            var artistAlbums = Utility.DownloadPagedResources((o, p) => api.GetArtistsAlbumsAsync(Id, offset: o, limit: p, market: market));
            var artistTopTracks = api.GetArtistsTopTracksAsync(Id, country: market);

            await Task.WhenAll(new Task[] { fullArtist, relatedArtists, artistAlbums, artistTopTracks });

            return new ArtistProfile
            {
                Artist = fullArtist.Result,
                TopTracks = artistTopTracks.Result.Tracks,
                Albums = artistAlbums.Result,
                RelatedArtists = relatedArtists.Result.Artists
            };
        }

        public async Task<CompleteAlbum> GetAlbum(string Id)
        {
            var market = await GetMarket();

            var fullAlbum = api.GetAlbumAsync(Id, market: market);
            var albumTracks = Utility.DownloadPagedResources(
                (o, p) => api.GetAlbumTracksAsync(id: Id, offset: o, limit: p, market: market));

            await Task.WhenAll(new Task[]{fullAlbum, albumTracks});

            return new CompleteAlbum { Album = fullAlbum.Result, Tracks = albumTracks.Result };
        }

        public async Task<CompletePlaylist> GetPlaylist(string Id)
        {
            var market = await GetMarket();

            var fullPlaylist = api.GetPlaylistAsync(Id, market: market);
            var playlistTracks = Utility.DownloadPagedResources(
                (o, p) => api.GetPlaylistTracksAsync(playlistId: Id, offset: o, limit: p, market: market));

            await Task.WhenAll(new Task[]{fullPlaylist, playlistTracks});

            return new CompletePlaylist { Playlist = fullPlaylist.Result, Tracks = playlistTracks.Result };
        }

        public async Task<IEnumerable<SimplePlaylist>> GetUserPlaylists(string Id) =>
            await Utility.DownloadPagedResources((o, p) => api.GetUserPlaylistsAsync(userId: Id, offset: o, limit: p));

        // TODO: listás verzió
        public async Task<bool> GetTrackSavedStatus(string Id) =>
            (await api.CheckSavedTracksAsync(new string[] { Id }.ToList())).List.FirstOrDefault();

        public async Task<ErrorResponse> SaveTrack(string Id) =>
            await api.SaveTrackAsync(Id);

        public async Task<ErrorResponse> RemoveSavedTrack(string Id) =>
            await api.RemoveSavedTracksAsync(new string[] { Id }.ToList());

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
