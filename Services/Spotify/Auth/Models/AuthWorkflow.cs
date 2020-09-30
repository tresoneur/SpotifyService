namespace Caerostris.Services.Spotify.Auth.Models
{
    public enum AuthWorkflowType
    {
        AntiCsrf,
        AuthCode
    }

    public class AuthWorkflow
    {
        public string State { get; set; } = "";

        public AuthWorkflowType Type { get; set; }
    }
}
