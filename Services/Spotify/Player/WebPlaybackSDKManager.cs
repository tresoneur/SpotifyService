using Caerostris.Services.Spotify.Player.Models;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Player
{
    /// <remarks>
    /// Do not attempt to create more than one instance of this class in a single window.
    /// </remarks>
    public class WebPlaybackSDKManager : IDisposable
    {
        private const string JsWrapper = "SpotifyService.WebPlaybackSDKWrapper";

        private readonly IJSRuntime jsRuntime;
        private readonly DotNetObjectReference<WebPlaybackSDKManager> selfReference;


        private Func<Task<string?>> authTokenCallback =
            async () => await Task.FromResult<string?>(null);

        private Func<string, Task> errorCallback =
            async (_) => { await Task.CompletedTask; };

        private Action<WebPlaybackState?> playbackContextCallback =
            (_) => { };

        private Func<string, Task> onDeviceReady =
            async (_) => { await Task.CompletedTask; };

        public WebPlaybackSDKManager(IJSRuntime injectedJsRuntime)
        {
            jsRuntime = injectedJsRuntime;
            selfReference = DotNetObjectReference.Create(this);
        }

        /// <summary>
        /// Call this method before attempting to interact with this class in any way.
        /// This method may be called several times. This resets the inner state of the instance and all JS entities associated with it.
        /// </summary>
        /// <param name="authTokenCallback">Callback for when a valid OAuth token needs to be acquired for streaming</param>
        /// <param name="errorCallback">Called when the Spotify Web Playback SDK reports an error</param>
        /// <param name="playbackContextCallback">Called when the playback state changes while the local playback device is active</param>
        /// <param name="onDeviceReady">Reports the ID of the local playback device provided by the Spotify Web Playback SDK</param>
        /// <returns></returns>
        public async Task Initialize(
            Func<Task<string?>> authTokenCallback,
            Func<string, Task> errorCallback,
            Action<WebPlaybackState?> playbackContextCallback,
            Func<string, Task> onDeviceReady,
            string name)
        {
            this.authTokenCallback = authTokenCallback;
            this.errorCallback = errorCallback;
            this.playbackContextCallback = playbackContextCallback;
            this.onDeviceReady = onDeviceReady;

            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Initialize", selfReference, name);
        }

        [JSInvokable]
        public async Task<string?> GetAuthToken() =>
            await authTokenCallback();

        [JSInvokable]
        public async Task OnError(string? message) =>
            await errorCallback(message ?? string.Empty);

        [JSInvokable]
        public void OnPlaybackChanged(string? state)
        {
            if (!string.IsNullOrEmpty(state))
                playbackContextCallback(JsonConvert.DeserializeObject<WebPlaybackState?>(state));
        }

        [JSInvokable]
        public async Task OnDeviceReady(string? deviceId)
        {
            if (!string.IsNullOrEmpty(deviceId))
                await onDeviceReady(deviceId);
        }

        public async Task Play() =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Play");

        public async Task Pause() =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Pause");

        public async Task Next() =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Next");

        public async Task Previous() =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Previous");

        public async Task Seek(int positionMs) =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.Seek", positionMs);

        public async Task SetVolume(int volumePercent) =>
            await jsRuntime.InvokeVoidAsync($"{JsWrapper}.SetVolume", volumePercent / 100d);

        public void Dispose()
        {
            selfReference?.Dispose();
        }
    }
}
