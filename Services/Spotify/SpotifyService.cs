using Caerostris.Services.Spotify.Sections;
using Caerostris.Services.Spotify.Web;
using System.Threading.Tasks;
using SpotifyAPI.Web;

namespace Caerostris.Services.Spotify
{
    public sealed partial class SpotifyService
    {
        private readonly WebApiManager dispatcher;

        public AuthorizationService Auth { get; private set; }
        public ContextsService Context { get; private set; }
        public ExploreService Explore { get; private set; }
        public LibraryService Library { get; private set; }
        public PlaybackService Playback { get; private set; }
        public UserService User { get; private set; }

        public SpotifyService(
            WebApiManager webApiManager,
            AuthorizationService spotifyServiceAuth,
            ContextsService spotifyServiceContext,
            ExploreService spotifyServiceExplore,
            LibraryService spotifyServiceLibrary,
            PlaybackService spotifyServicePlayback,
            UserService spotifyServiceUser)
        {
            dispatcher = webApiManager;
            Auth = spotifyServiceAuth;
            Context = spotifyServiceContext;
            Explore = spotifyServiceExplore;
            Library = spotifyServiceLibrary;
            Playback = spotifyServicePlayback;
            User = spotifyServiceUser;
        }

        public async Task Initialize()
        {
            await Auth.Initialize();
            await Playback.Initialize();
        }

        public async Task Logout()
        {
            await dispatcher.Cleanup();
            await Explore.Cleanup();
            await Auth.Logout();
        }
    }
}
