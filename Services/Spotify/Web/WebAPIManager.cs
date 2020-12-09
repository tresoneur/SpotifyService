using Caerostris.Services.Spotify.Web.Extensions;
using Caerostris.Services.Spotify.Services.Spotify.Web.Api;
using Caerostris.Services.Spotify.Web.ViewModels;
using Caerostris.Services.Spotify.Web.CachedDataProviders;
using Caerostris.Services.Spotify.Web.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace Caerostris.Services.Spotify.Web
{
    /// <remarks>
    /// The chief goal of this class is to provide in-memory, LocalStorage and IndexedDB caching as well as to automatically supply parameters to SpotifyClient to enable e.g. track relinking.
    /// </remarks>
    public class WebApiManager
    {
        private readonly Api apiWrapper;
        private readonly SavedTrackManager savedTracks;
        private readonly AudioFeaturesManager audioFeatures;

        private SpotifyClient Api => apiWrapper.Client;

        public WebApiManager(Api api, SavedTrackManager savedTrackManager, AudioFeaturesManager audioFeaturesManager)
        {
            apiWrapper = api;
            savedTracks = savedTrackManager;
            audioFeatures = audioFeaturesManager;
        }

        public void Authorize(string token) =>
            apiWrapper.Authorize(token);

        /// <summary>
        /// We consider the private profile of the user to be unchanging during the typical lifecycle of this application, so it gets cached indefinitely.
        /// </summary>
        private PrivateUser? privateProfile;

        /// <returns>The private profile of the current user.</returns>
        public async Task<PrivateUser> GetPrivateProfile()
        {
            if (privateProfile is null)
                privateProfile = await Api.UserProfile.Current();

            return privateProfile;
        }

        public async Task<CurrentlyPlayingContext> GetPlayback() =>
            await Api.Player.GetCurrentPlayback();

        public async Task ResumePlayback() =>
            await Api.Player.ResumePlayback();

        public async Task SetPlayback(string? contextUri, string? trackUri)
        {
            if (contextUri is null && trackUri is null)
                throw new ArgumentException("Context and track URIs cannot both be null.");

            else if (contextUri is null && trackUri is not null)
                await Api.Player.ResumePlayback(new() { Uris = new List<string> { trackUri } });

            else if (contextUri is not null && trackUri is null)
                await Api.Player.ResumePlayback(new() { ContextUri = contextUri });

            else if (contextUri is not null && trackUri is not null)
                await Api.Player.ResumePlayback(new() { ContextUri = contextUri, OffsetParam = new() { Uri = trackUri } });
        }

        public async Task SetPlayback(IEnumerable<string> uris) =>
            await Api.Player.ResumePlayback(new() { Uris = uris.ToList() });

        public async Task PausePlayback() =>
            await Api.Player.PausePlayback();

        public async Task SkipPlaybackToNext() =>
            await Api.Player.SkipNext();

        public async Task SkipPlaybackToPrevious() =>
            await Api.Player.SkipPrevious();

        public async Task SeekPlayback(int positionMs) =>
            await Api.Player.SeekTo(new(positionMs));

        public async Task<IEnumerable<Device>> GetDevices() =>
            (await Api.Player.GetAvailableDevices()).Devices;

        public async Task TransferPlayback(string deviceId, bool play = false) =>
            await Api.Player.TransferPlayback(new(new List<string> { deviceId }) { Play = play });

        public async Task SetShuffle(bool shuffle) =>
            await Api.Player.SetShuffle(new(shuffle));

        public async Task SetRepeatMode(RepeatState state) =>
            await Api.Player.SetRepeat(new(state.AsPlayerSetRepeatRequestState()));

        public async Task SetVolume(int volumePercent) =>
            await Api.Player.SetVolume(new(volumePercent));

        public async Task<IEnumerable<SavedTrack>> GetSavedTracks(Action<int, int> progressCallback) =>
            await savedTracks.GetData(progressCallback, await GetMarket());

        public async Task<ArtistProfile> GetArtist(string id)
        {
            var market = await GetMarket();

            var fullArtist = Api.Artists.Get(id);
            var relatedArtists = Api.Artists.GetRelatedArtists(id);
            var artistAlbums = Utility.SynchronizedDownloadPagedResources(
                (o, p) => Api.Artists.GetAlbums(id, new() { Offset = o, Limit = p, Market = market }), 
                maxPages: 2);
            var artistTopTracks = Api.Artists.GetTopTracks(id, new(market));

            await Task.WhenAll(fullArtist, relatedArtists, artistAlbums, artistTopTracks);

            return new()
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

            var fullAlbum = Api.Albums.Get(id, new() { Market = market });
            var albumTracks = Utility.SynchronizedDownloadPagedResources(
                (o, p) => Api.Albums.GetTracks(id, new() { Offset = o, Limit = p, Market = market }));

            await Task.WhenAll(fullAlbum, albumTracks);

            return new() { Album = fullAlbum.Result, Tracks = albumTracks.Result };
        }

        public async Task<CompletePlaylist> GetPlaylist(string id)
        {
            var market = await GetMarket();
            var tracksOnly1 = PlaylistGetRequest.AdditionalTypes.Track;
            var tracksOnly2 = PlaylistGetItemsRequest.AdditionalTypes.Track;

            var fullPlaylist = Api.Playlists.Get(id, new(tracksOnly1));
            var playlistTracks = Utility.SynchronizedDownloadPagedResources(
                (o, p) => Api.Playlists.GetItems(id, new(tracksOnly2) { Offset = o, Limit = p, Market = market }));

            await Task.WhenAll(fullPlaylist, playlistTracks);

            return new() 
            { 
                Playlist = fullPlaylist.Result, 
                Tracks = playlistTracks.Result.Select(t => new PlaylistTrack<FullTrack>() 
                { 
                    AddedAt = t.AddedAt, 
                    AddedBy = t.AddedBy, 
                    IsLocal = t.IsLocal, 
                    Track = (FullTrack)t.Track 
                }) // This would not have been necessary if the PlaylistTrack interface had been declared with the `out T` (covariant) template parameter instead of `T`.
            };
        }

        public async Task<IEnumerable<SimplePlaylist>> GetUserPlaylists(string id) =>
            await Utility.SynchronizedDownloadPagedResources(
                (o, p) => Api.Playlists.GetUsers(id, new() { Offset = o, Limit = p }));

        public async Task<bool> GetTrackSavedStatus(string id) =>
            (await Api.Library.CheckTracks(new(new List<string> { id }))).First();

        public async Task<IDictionary<string, bool>> GetTrackSavedStatus(IEnumerable<string> ids) =>
            new Dictionary<string, bool>(
                (await Utility.SynchronizedPaginateAndDownloadResources<string, bool>(
                    ids, async (ids) => (await Api.Library.CheckTracks(new(ids.ToList()))), 50))
                    .Zip(ids, (isSaved, id) => new KeyValuePair<string, bool>(id, isSaved)));

        public async Task SaveTrack(string id) =>
            await Api.Library.SaveTracks(new(new List<string> { id }));

        public async Task RemoveSavedTrack(string id) =>
            await Api.Library.RemoveTracks(new(new List<string> { id }));

        public async Task<SearchResponse> Search(string query)
        {
            var types = SearchRequest.Types.Album | SearchRequest.Types.Artist | SearchRequest.Types.Playlist | SearchRequest.Types.Track;
            return await Api.Search.Item(new(types, query) { Limit = 5, Market = await GetMarket() });
        }

        public async Task<IEnumerable<TrackAudioFeatures>> GetAudioFeatures(IEnumerable<string> trackIds, Action<int, int> progressCallback)
        {
            audioFeatures.TrackIds = trackIds;
            return await audioFeatures.GetData(progressCallback, await GetMarket());
        }

        public async Task<FullPlaylist> CreatePlaylist(string userId, string title, string description) =>
            await Api.Playlists.Create(userId, new(title) { Public = false, Collaborative = false, Description = description });

        public async Task AddPlaylistTracks(string playlistId, IEnumerable<string> trackUris) =>
            await Api.Playlists.AddItems(playlistId, new(trackUris.ToList()));

        public async Task<IEnumerable<FullTrack>> GetTracks(IEnumerable<string> trackUris) =>
            (await Api.Tracks.GetSeveral(new(IdsFromUris(trackUris)) { Market = await GetMarket() })).Tracks;

        public async Task<IEnumerable<FullAlbum>> GetAlbums(IEnumerable<string> albumUris) =>
            (await Api.Albums.GetSeveral(new(IdsFromUris(albumUris)) { Market = await GetMarket() })).Albums;

        public async Task<IEnumerable<FullArtist>> GetArtists(IEnumerable<string> artistUris) =>
            (await Api.Artists.GetSeveral(new(IdsFromUris(artistUris)))).Artists;

        public async Task<IEnumerable<FullPlaylist>> GetPlaylists(IEnumerable<string> playlistUris) =>
            await Utility.SynchronizedDownloadParallel(
                IdsFromUris(playlistUris).Select<string, Func<Task<IEnumerable<FullPlaylist>>>>( 
                    id => async () => new[] { await Api.Playlists.Get(id, new(PlaylistGetRequest.AdditionalTypes.Track)) }));

        public async Task Cleanup()
        {
            await savedTracks.ClearStorageCache();
            await audioFeatures.ClearStorageCache();
        }

        #region Comfort

        private async Task<string> GetMarket() =>
            (await GetPrivateProfile()).Country;

        private static List<string> IdsFromUris(IEnumerable<string> uris) =>
            uris.Select(WebApiModelExtensions.IdFromUri).ToList();

        #endregion
    }
}
