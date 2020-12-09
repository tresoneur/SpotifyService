using Caerostris.Services.Spotify.Web.Extensions;
using SpotifyAPI.Web;
using Microsoft.JSInterop;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Player
{
    public sealed class MediaSessionManager : IDisposable
    {
        private const string JsWrapper = "SpotifyService.MediaSessionWrapper";

        private readonly IJSRuntime jsRuntime;
        private readonly DotNetObjectReference<MediaSessionManager> selfReference;

        private Func<Task>? onPlay;
        private Func<Task>? onPause;
        private Func<Task>? onPrevious;
        private Func<Task>? onNext;

        public MediaSessionManager(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            selfReference = DotNetObjectReference.Create(this);
        }

        /// <summary>
        /// Call this method before you attempt to interact with this class in any other way.
        /// </summary>
        public async Task Initialize(
            Func<Task> play,
            Func<Task> pause,
            Func<Task> previous,
            Func<Task> next)
        {
            onPlay = play;
            onPause = pause;
            onPrevious = previous;
            onNext = next;

            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Initialize", selfReference);
        }

        public async Task SetMetadata(CurrentlyPlayingContext playbackContext)
        {
            if (playbackContext.ValidTrackItemOrNull() is FullTrack track)
                await jsRuntime.InvokeVoidAsync($"{JsWrapper}.SetMetadata",
                    playbackContext.IsPlaying,
                    track.Name,
                    string.Join(", ", track.Artists.Select(a => a.Name)),
                    track.Album.Name,
                    track.Album.Images);
        }

        public async Task OnUserStartedPlayback()
        {
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Play");
        }

        [JSInvokable]
        public async Task OnPlay()
        {
            if (onPlay is not null)
                await onPlay();
        }

        [JSInvokable]
        public async Task OnPause()
        {
            if (onPause is not null)
                await onPause();
        }

        [JSInvokable]
        public async Task OnPrevious()
        {
            if (onPrevious is not null)
                await onPrevious();
        }

        [JSInvokable]
        public async Task OnNext()
        {
            if (onNext is not null)
                await onNext();
        }

        public void Dispose()
        {
            selfReference.Dispose();
        }
    }
}
