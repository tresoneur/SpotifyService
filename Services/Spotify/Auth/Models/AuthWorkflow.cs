namespace Caerostris.Services.Spotify.Auth.Models
{
    public enum AuthWorkflowType
    {
        Unspecified,
        AntiCsrf,
        AuthCode
    }

    public class AuthWorkflow
    {
        public string State { get; set; } = "";

        public AuthWorkflowType Type { get; set; }
    }
}
