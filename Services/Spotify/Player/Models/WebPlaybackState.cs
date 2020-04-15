using Newtonsoft.Json;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackState
    {
        [JsonProperty("repeat_mode")]
        public int RepeatMode { get; set; }

        [JsonProperty("shuffle")]
        public bool ShuffleState { get; set; }

        [JsonProperty("context")]
        public Context? Context { get; set; }

        [JsonProperty("timestamp")]
        public long Timestamp { get; set; }

        [JsonProperty("position")]
        public int ProgressMs { get; set; }

        [JsonProperty("paused")]
        public bool Paused { get; set; }

        [JsonProperty("track_window")]
        public WebPlaybackTrackWindow? TrackWindow { get; set; }
    }
}
