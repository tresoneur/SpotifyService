using SpotifyAPI.Web;
using Caerostris.Services.Spotify.Web.ViewModels;
using System;

namespace Caerostris.Services.Spotify.Web.Extensions
{
    public static class TrackExtensions
    {
        // The SpotifyAPI-NET library doesn't contain interfaces or base classes for model classes.
        // Also, I've rewritten all of this with AutoMapper once, and the result was unreadable.

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
    }
}
