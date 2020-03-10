using Newtonsoft.Json;
using System.Collections.Generic;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackTrackWindow
    {
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [JsonProperty("current_track")]
        public WebPlaybackTrack? CurrentTrack { get; set; }

        [JsonProperty("previous_tracks")]
        public List<WebPlaybackTrack> PreviousTracks { get; set; }

        [JsonProperty("next_tracks")]

        public List<WebPlaybackTrack> NextTracks { get; set; }
#pragma warning restore CS8618
    }
}