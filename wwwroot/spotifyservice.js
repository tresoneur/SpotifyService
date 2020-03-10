(() => {
    window.SpotifyService = new Object();
    window.SpotifyService.GetWidth = (selector) => window.document.querySelector(selector).clientWidth;
    window.SpotifyService.GetHeight = (selector) => window.document.querySelector(selector).clientHeight;
    window.SpotifyService.GetClientPositionX = (selector) => document.querySelector(selector).getBoundingClientRect().left;
})();