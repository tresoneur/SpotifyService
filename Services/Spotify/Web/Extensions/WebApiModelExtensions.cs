using Caerostris.Services.Spotify.Web.ViewModels;
using Humanizer;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using AutoMapper;

namespace Caerostris.Services.Spotify.Web.Extensions
{
    /// <summary>
    /// Extension methods defined on model classes from SpotifyAPI-NET.
    /// </summary>
    public static class WebApiModelExtensions
    {
        private static readonly IMapper mapper = 
            new MapperConfiguration(cfg => 
            { 
                cfg.CreateMap<FullAlbum, SimpleAlbum>();
                cfg.CreateMap<FullPlaylist, SimplePlaylist>()
                    .ForMember(dest => dest.Tracks, opt => opt.MapFrom(src => src.Tracks));
            })
            .CreateMapper();

        #region PrivateProfile

        public static string GetUsername(this PrivateUser profile) =>
            string.IsNullOrEmpty(profile.DisplayName)
                ? profile.Id
                : profile.DisplayName;

        #endregion


        #region PlaybackContext

        public static FullTrack? ValidTrackItemOrNull(this CurrentlyPlayingContext? playback) =>
            (playback?.Item is FullTrack item) ? item : null;

        public static bool HasValidTrackItem([NotNullWhen(true)] this CurrentlyPlayingContext? playback) =>
            (playback?.Item is FullTrack item && item.DurationMs != 0);

        public static bool IsPlaying(this CurrentlyPlayingContext? playback) =>
            playback?.IsPlaying ?? false;

        public static bool IsCurrentTrack(this CurrentlyPlayingContext? playback, string trackUri) =>
            playback.ValidTrackItemOrNull()?.Uri?.Equals(trackUri) ?? false;

        public static bool GetShuffleState(this CurrentlyPlayingContext? playback) =>
            playback?.ShuffleState ?? false;

        public static string GetRepeatState(this CurrentlyPlayingContext? playback) =>
            playback?.RepeatState ?? "off";

        public static int GetVolumePercent(this CurrentlyPlayingContext? playback) =>
            playback?.Device?.VolumePercent ?? 100;

        #endregion
        

        #region Album

        public static string HumanReadableTotalLength(this CompleteAlbum album) =>
            HumanReadableTotalLength(album.Tracks.Sum(t => t.DurationMs));

        public static SimpleAlbum AsSimpleAlbum(this FullAlbum album) =>
            mapper.Map<SimpleAlbum>(album);

        #endregion


        #region Playlist

        public static string HumanReadableTotalLength(this CompletePlaylist playlist) =>
            HumanReadableTotalLength(playlist.Tracks.Sum(t => t.Track.DurationMs));

        public static SimplePlaylist AsSimplePlaylist(this FullPlaylist playlist) =>
            mapper.Map<SimplePlaylist>(playlist);

        #endregion


        #region Artist

        /// <summary>
        /// Creates a formatted string from a collection of genres.
        /// </summary>
        /// <param name="limit">
        /// These lists can get quite lengthy with genres sometimes numbering in the dozens.
        /// Set a limit > 0 to get a list of only the first <paramref name="limit"/> genres.
        /// </param>
        /// <returns></returns>
        public static string GetGenres(this FullArtist artist, int limit = 0)
        {
            if (artist.Genres.Count == 0)
                return string.Empty;

            const string delimiter = ", ";
            return string.Join(
                delimiter,
                artist.Genres
                    .Take((limit != 0) ? limit : artist.Genres.Count)
                    .Select(genre => Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(genre)));
        }

        #endregion


        #region Paging

        public static List<TData> ItemsOrEmptyList<TData>(this Paging<TData> page) =>
            page.Items ?? new();

        #endregion


        #region RepeatState

        public static PlayerSetRepeatRequest.State AsPlayerSetRepeatRequestState(this RepeatState state) =>
            state switch
            {
                RepeatState.Track => PlayerSetRepeatRequest.State.Track,
                RepeatState.Context => PlayerSetRepeatRequest.State.Context,
                RepeatState.Off => PlayerSetRepeatRequest.State.Off,
                _ => throw new ArgumentException($"No such {nameof(RepeatState)}.")
            };

        public static string AsString(this RepeatState state) =>
            Enum.GetName(typeof(RepeatState), state)?.ToLowerInvariant() ?? "off";

        #endregion


        #region Track

        // The raison d'être for these trivial converters is that the SpotifyAPI-NET library doesn't contain interfaces or base classes for model classes.
        // Also, I've rewritten all of this with AutoMapper once, but this version is just far more readable.

        public static Track AsTrack(this PlaylistTrack<FullTrack> playlistTrack, int uniqueIdSeed)
        {
            var track = playlistTrack.Track.AsTrack(uniqueIdSeed);
            track.AddedAt = playlistTrack.AddedAt ?? DateTimeOffset.UnixEpoch.DateTime;
            return track;
        }

        public static Track AsTrack(this SavedTrack savedTrack, int uniqueIdSeed)
        {
            var track = savedTrack.Track.AsTrack(uniqueIdSeed);
            track.AddedAt = savedTrack.AddedAt;
            return track;
        }

        public static Track AsTrack(this FullTrack fullTrack, int uniqueIdSeed)
        {
            return new Track()
            {
                Uri = fullTrack.Uri,
                UniqueId = $"{fullTrack.Id}{uniqueIdSeed}",
                Id = fullTrack.Id,
                ExternalUrl = fullTrack.ExternalUrls["spotify"],
                LinkedFromId = fullTrack.LinkedFrom?.Id,
                Title = fullTrack.Name,
                Explicit = fullTrack.Explicit,
                AlbumTitle = fullTrack.Album.Name,
                AlbumExternalUrls = fullTrack.Album.ExternalUrls,
                AlbumId = fullTrack.Album.Id,
                AlbumTrackNumber = fullTrack.TrackNumber,
                Artists = fullTrack.Artists,
                Popularity = fullTrack.Popularity,
                DurationMs = fullTrack.DurationMs
            };
        }

        public static Track AsTrack(this SimpleTrack simpleTrack, int uniqueIdSeed)
        {
            return new Track()
            {
                Uri = simpleTrack.Uri,
                UniqueId = $"{simpleTrack.Id}{uniqueIdSeed}",
                Id = simpleTrack.Id,
                ExternalUrl = simpleTrack.ExternalUrls["spotify"],
                Title = simpleTrack.Name,
                Explicit = simpleTrack.Explicit,
                AlbumTrackNumber = simpleTrack.TrackNumber,
                Artists = simpleTrack.Artists,
                DurationMs = simpleTrack.DurationMs
            };
        }

        #endregion


        #region Utility

        public static string WithThousandsSeparator(this int num)
        {
            return num.ToString("#,##0").Replace(',', ' ');
        }

        /// <summary>
        /// Returns the URL of the smallest image that fits the height criterion. 
        /// If none are found, it returns the tallest image in the collection.
        /// If the collection is empty, it returns null.
        /// </summary>
        public static string? AtLeastOfHeight(this IEnumerable<Image> images, int height)
        {
            return images.OrderBy(i => i.Height).FirstOrDefault(i => i.Height >= height)?.Url
                ?? images.OrderBy(i => i.Height).LastOrDefault()?.Url;
        }

        public static string AsHumanReadableDuration(this int durationMs)
        {
            var duration = TimeSpan.FromMilliseconds(durationMs);
            return (duration > TimeSpan.FromHours(1))
                ? duration.ToString("%h':'mm':'ss")
                : duration.ToString("%m':'ss");
        }

        private static string HumanReadableTotalLength(int durationMs)
        {
            return TimeSpan
                .FromMilliseconds(durationMs)
                .Humanize(
                    precision: 2,
                    maxUnit: Humanizer.Localisation.TimeUnit.Hour,
                    minUnit: Humanizer.Localisation.TimeUnit.Minute,
                    collectionSeparator: " ");
        }

        public static string AsHumanReadableAddedAt(this DateTime addedAt)
        {
            return (addedAt.AddDays(14) > DateTime.UtcNow)
                ? addedAt.Humanize()
                : addedAt.ToString("yyyy-MM-dd");
        }

        public static string HumanReadableMonth(int month) =>
            new DateTime(2020, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);

        public static string IdFromUri(string uri) =>
            uri.Split(':').Last();

        #endregion
    }
}
