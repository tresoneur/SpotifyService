using Caerostris.Services.Spotify.Web.ViewModels;
using Humanizer;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                return GetArtists(playback.Item, links);
        }

        public static string GetArtists(this FullTrack? item, bool links = false)
        {
            if (item is null || item.Artists is null)
            {
                return string.Empty;
            }
            else
            {
                const string delimiter = ", ";
                var builder = new StringBuilder();
                item.Artists.ForEach(artist =>
                {
                    builder.Append((links && !(artist?.ExternalUrls is null))
                        ? $"<a href=\"{artist.ExternalUrls["spotify"]}\">{artist.Name}</a>{delimiter}"
                        : $"{artist.Name}{delimiter}");
                });
                return builder.ToString().Substring(0, builder.Length - delimiter.Length);
            }
        }

        #endregion


        #region SavedTrack

        public static IEnumerable<FlatSavedTrack> AsFlatSavedTracks(this IEnumerable<SavedTrack> tracks) =>
            tracks.Select(t => new FlatSavedTrack() { SavedTrack = t });

        public static string HumanReadableAddedAt(this SavedTrack track) =>
            (track.AddedAt.AddDays(14) > DateTime.UtcNow)
                ? track.AddedAt.Humanize()
                : track.AddedAt.ToString("yyyy-MM-dd");

        public static string HumanReadableDuration(this FullTrack track)
        {
            var duration = TimeSpan.FromMilliseconds(track.DurationMs);
            return (duration > TimeSpan.FromHours(1))
                ? duration.ToString("%h':'mm':'ss")
                : duration.ToString("%m':'ss");
        }

        #endregion

    }
}
