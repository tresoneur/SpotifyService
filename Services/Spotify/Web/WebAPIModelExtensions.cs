using Caerostris.Services.Spotify.Web.ViewModels;
using Humanizer;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;

namespace Caerostris.Services.Spotify.Web
{
    public static class WebApiModelExtensions
    {
        public const string Unknown = "Unknown";
        public const string Loading = "Loading...";
        public static string Unavailable(string kind) => $"Current {kind} unavailable";

        #region PrivateProfile

        public static string GetUsername(this PrivateProfile? profile)
        {
            if (profile == null)
                return Unknown;
            else
                return string.IsNullOrEmpty(profile.DisplayName)
                    ? profile.Id
                    : profile.DisplayName;
        }

        #endregion


        #region PlaybackContext

        public static bool HasValidItem([NotNullWhen(true)] this PlaybackContext? playback) =>
            !(playback?.Item is null || playback.Item.DurationMs == 0);

        public static bool IsPlayingOrNull(this PlaybackContext? playback) =>
            playback?.IsPlaying ?? false;

        public static bool GetShuffleState(this PlaybackContext? playback) =>
            playback?.ShuffleState ?? false;

        public static RepeatState GetRepeatState(this PlaybackContext? playback) =>
            playback?.RepeatState ?? RepeatState.Off;

        public static int GetVolumePercent(this PlaybackContext? playback) =>
            playback?.Device?.VolumePercent ?? 75;

        public static string GetTrackTitle(this PlaybackContext? playback, bool link = false, string localUrl = "")
        {
            if (playback is null)
                return Loading;
            else if (playback.Item is null)
                return Unavailable("track");
            else
                return GetName(playback.Item.Name, playback.Item.ExternUrls, playback.Item.Id, link, localUrl);
        }

        public static string GetArtists(this PlaybackContext? playback, bool links = false, string localUrl = "")
        {
            if (playback is null)
                return Loading;
            else if (playback.Item is null)
                return string.Empty;
            else
                return GetArtists(playback.Item.Artists, links, localUrl);
        }

        #endregion


        #region Track

        public static string HumanReadableAddedAt(this SavedTrack track) =>
            HumanReadableAddedAt(track.AddedAt);

        public static string HumanReadableAddedAt(this PlaylistTrack track) =>
            HumanReadableAddedAt(track.AddedAt);

        public static string HumanReadableDuration(this FullTrack track) =>
            HumanReadableDuration(track.DurationMs);

        public static string HumanReadableDuration(this SimpleTrack track) =>
            HumanReadableDuration(track.DurationMs);

        public static string OriginalUri(this FullTrack track)
        {
            if (track.LinkedFrom is null || string.IsNullOrEmpty(track.LinkedFrom.Uri))
                return track.Uri;
            else
                return track.LinkedFrom.Uri;
        }

        /// public static string OriginalUri(this SimpleTrack track) isn't possible at this time without a network request

        #endregion


        #region Album

        public static string HumanReadableTotalLength(this CompleteAlbum album) =>
            HumanReadableTotalLength(album.Tracks.Sum(t => t.DurationMs));

        public static string GetName(this SimpleAlbum album, bool link = false, string localUrl = "") =>
            GetName(album.Name, album.ExternalUrls, album.Id, link, localUrl);

        public static string GetName(this FullAlbum album, bool link = false, string localUrl = "") =>
            GetName(album.Name, album.ExternalUrls, album.Id, link, localUrl);

        public static string GetName(this FullArtist artist, bool link = false, string localUrl = "") =>
            GetName(artist.Name, artist.ExternalUrls, artist.Id, link, localUrl);

        #endregion


        #region Playlist

        public static string HumanReadableTotalLength(this CompletePlaylist playlist) =>
            HumanReadableTotalLength(playlist.Tracks.Sum(t => t.Track.DurationMs));

        public static string GetName(this SimplePlaylist playlist, bool link = false, string localUrl = "") =>
            GetName(playlist.Name, playlist.ExternalUrls, playlist.Id, link, localUrl);

        public static string GetName(this FullPlaylist playlist, bool link = false, string localUrl = "") =>
            GetName(playlist.Name, playlist.ExternalUrls, playlist.Id, link, localUrl);

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


        #region Utility

        /// <summary>
        /// Creates a formatted string from a collection of <see cref="SimpleArtist"/>s.
        /// </summary>
        /// <param name="link">Whether html markup should be used to create links to the artists' pages.</param>
        /// <param name="localUrl">If supplied, the links will point to {localUrl}{<see cref="SimpleArtist.Id"/>}.</param>
        public static string GetArtists(this IEnumerable<SimpleArtist> artists, bool link = false, string localUrl = "")
        {
            if (artists is null)
                return string.Empty;

            const string delimiter = ", ";
            return string.Join(
                delimiter,
                artists.Select(artist => GetName(artist.Name, artist.ExternalUrls, artist.Id, link, localUrl)));
        }

        public static string WithThousandsSeparator(this int num)
        {
            return num.ToString("#,##0").Replace(',', ' ');
        }

        /// <summary>
        /// Returns the URL of the smallest image that fits the height criterion. 
        /// If none is found, it returns the tallest image in the collection.
        /// If the collection is empty, it returns null.
        /// </summary>
        public static string? AtLeastOfHeight(this IEnumerable<Image> images, int height)
        {
            return images.OrderBy(i => i.Height).FirstOrDefault(i => i.Height >= height)?.Url
                ?? images.OrderBy(i => i.Height).LastOrDefault()?.Url;
        }

        private static string HumanReadableDuration(int durationMs)
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

        private static string HumanReadableAddedAt(DateTime addedAt)
        {
            return (addedAt.AddDays(14) > DateTime.UtcNow)
                ? addedAt.Humanize()
                : addedAt.ToString("yyyy-MM-dd");
        }

        public static string HumanReadableMonth(int month) =>
            new DateTime(2020, month, 1).ToString("MMMM", CultureInfo.InvariantCulture);

        private static string GetName(string name, Dictionary<string, string> externalUrls, string id, bool link, string? localUrl)
        {
            return (link && !(externalUrls is null))
                ? $"<a href=\"{(string.IsNullOrEmpty(localUrl) ? externalUrls["spotify"] : $"{localUrl}{id}")}\">{name}</a>"
                : $"{name}";
        }

        #endregion
    }
}
