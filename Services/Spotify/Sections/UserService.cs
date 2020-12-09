using Caerostris.Services.Spotify.Web;
using Caerostris.Services.Spotify.Web.Extensions;
using System.Threading.Tasks;

namespace Caerostris.Services.Spotify.Sections
{
    public sealed class UserService
    {
        private readonly WebApiManager dispatcher;

        public UserService(WebApiManager webApiManager)
        {
            dispatcher = webApiManager;
        }

        public async Task<string> GetUsername() =>
            (await dispatcher.GetPrivateProfile()).GetUsername();

        public async Task<string> GetUserId() =>
            (await dispatcher.GetPrivateProfile()).Id;
    }
}
