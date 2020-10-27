using SpotifyAPI.Web;
using System;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Web.ViewModels
{
    public class Track
    {
        public string Id { get; set; } = default!;

        public string UniqueId { get; set; } = default!; // Playlists can contain several copies of the same track.
        
        public string Uri { get; set; } = default!;

        public string ExternalUrl { get; set; } = default!;

        public string? LinkedFromId { get; set; }

        public string Title { get; set; } = default!;

        public bool Explicit { get; set; } = default!;

        public string AlbumTitle { get; set; } = default!;

        public string AlbumId { get; set; } = default!;

        public Dictionary<string, string> AlbumExternalUrls { get; set; } = default!;

        public int AlbumTrackNumber { get; set; }

        public List<SimpleArtist> Artists { get; set; } = default!;

        public int Popularity { get; set; }

        public int DurationMs { get; set; }

        public DateTime AddedAt { get; set; }

        // Some ways in which the TrackGrid could be extended: 
        // - show the (PublicProfile) AddedBy in the case of collaborative playlists
        // - etc.
    }
}
