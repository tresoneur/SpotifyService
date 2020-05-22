using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Caerostris.Services.Spotify.Web.SpotifyAPI.Web.Enums;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackTrack
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [JsonProperty("album")]
        public WebPlaybackAlbum? Album { get; set; }

        [JsonProperty("artists")]
        public List<WebPlaybackArtist> Artists { get; set; }

        [JsonProperty("duration_ms")]
        public int DurationMs { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TrackType Type { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }

        [JsonProperty("is_playable")]
        public bool? IsPlayable { get; set; }
#pragma warning restore CS8618
    }
}
