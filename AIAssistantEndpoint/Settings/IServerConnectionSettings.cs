namespace AIAssistantEndpoint.Settings
{
    public interface IServerConnectionSettings
    {
        string ServerUrl { get; set; }
        string ApiKey { get; set; }
        int TimeoutMs { get; set; }
        bool UseSSL { get; set; }
    }
}