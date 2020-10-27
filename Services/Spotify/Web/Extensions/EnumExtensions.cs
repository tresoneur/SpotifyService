using Caerostris.Services.Spotify.Web.ViewModels;
using SpotifyAPI.Web;
using System;

namespace Caerostris.Services.Spotify.Web.Extensions
{
    public static class EnumExtensions
    {
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
    }
}
