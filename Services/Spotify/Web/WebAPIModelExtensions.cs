using Caerostris.Services.Spotify.Web.ViewModels;
using Humanizer;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Caerostris.Services.Spotify.Web
{
    public static class WebAPIModelExtensions
    {
        public const string Unknown = "Unkown";
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

        public static bool HasValidItem(this PlaybackContext? playback) =>
            !(playback?.Item is null || playback.Item.DurationMs == 0);

        public static bool IsPlayingOrNull(this PlaybackContext? playback) =>
            playback?.IsPlaying ?? false;

        public static bool GetShuffleState(this PlaybackContext? playback) =>
            playback?.ShuffleState ?? false;

        public static RepeatState GetRepeatState(this PlaybackContext? playback) =>
            playback?.RepeatState ?? RepeatState.Off;

        public static int GetVolumePercent(this PlaybackContext? playback) =>
            playback?.Device?.VolumePercent ?? 75;

        public static string GetTrackTitle(this PlaybackContext? playback, bool link = false)
        {
            if (playback is null)
                return Loading;
            else if (playback.Item is null)
                return Unavailable("track");
            else
                return link
                    ? $"<a href=\"{playback.Item.ExternUrls["spotify"]}\">{playback.Item.Name}</a>"
                    : playback.Item.Name;
        }

        public static string GetArtists(this PlaybackContext? playback, bool links = false)
        {
            if (playback is null)
                return Loading;
            else if (playback.Item is null)
                return string.Empty;
            else
                return GetArtists(playback.Item.Artists, links);
        }

        #endregion


        #region Track

        public static IEnumerable<FlatSavedTrack> AsFlatSavedTracks(this IEnumerable<SavedTrack> tracks) =>
            tracks.Select(t => new FlatSavedTrack() { SavedTrack = t });

        public static string HumanReadableAddedAt(this SavedTrack track) =>
            HumanReadableAddedAt(track.AddedAt);

        public static string HumanReadableAddedAt(this PlaylistTrack track) =>
            HumanReadableAddedAt(track.AddedAt);

        public static string HumanReadableDuration(this FullTrack track) =>
            HumanReadableDuration(track.DurationMs);

        public static string HumanReadableDuration(this SimpleTrack track) =>
            HumanReadableDuration(track.DurationMs);

        #endregion


        #region CompleteAlbum

        public static string HumanReadableTotalLength(this CompleteAlbum album) =>
            HumanReadableTotalLength(album.Tracks.Sum(t => t.DurationMs));

        #endregion


        #region CompletePlaylist

        public static string HumanReadableTotalLength(this CompletePlaylist playlist) =>
            HumanReadableTotalLength(playlist.Tracks.Sum(t => t.Track.DurationMs));

        #endregion


        #region Artist

        public static string GetGenres(this FullArtist artist)
        {
            if (artist.Genres.Count == 0)
                return string.Empty;

            const string delimiter = ", ";
            var builder = new StringBuilder();
            foreach (var genre in artist.Genres)
                builder.Append($"{Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(genre)}{delimiter}");

            return builder.ToString().Substring(0, builder.Length - delimiter.Length);
        }

        #endregion


        #region Utility

        public static string GetArtists(this IEnumerable<SimpleArtist> artists, bool links = false)
        {
            if (artists is null)
                return string.Empty;

            const string delimiter = ", ";
            var builder = new StringBuilder();
            foreach (var artist in artists)
            {
                builder.Append((links && !(artist?.ExternalUrls is null))
                    ? $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>{delimiter}"
                    : $"{artist?.Name}{delimiter}");
            }
            return builder.ToString().Substring(0, builder.Length - delimiter.Length);
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

        #endregion
    }
}
