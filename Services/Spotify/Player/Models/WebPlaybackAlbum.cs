using Newtonsoft.Json;
using SpotifyAPI.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Player.Models
{
    public class WebPlaybackAlbum
    {
        #pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
        [JsonProperty("images")]
        public List<Image> Images { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("uri")]
        public string Uri { get; set; }
        #pragma warning restore CS8618

        public SimpleAlbum ApplyTo(SimpleAlbum album)
        {
            album.Images = Images;
            album.Name = Name;
            album.Uri = Uri;

            return album;
        }
    }
}
