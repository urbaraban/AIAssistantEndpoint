namespace AIAssistantEndpoint.Settings
{
    public interface IServerConnectionSettings
    {
        string ServerUrl { get; set; }
        string ApiKey { get; set; }
        int TimeoutMs { get; set; }
        bool UseSSL { get; set; }

        // Endpoints (support Timeweb Agent templates with {agent_access_id})
        string CallEndpoint { get; set; }
        string ChatEndpoint { get; set; }
        string StreamingEndpoint { get; set; }
        string ModelsEndpoint { get; set; }

        // Auth and headers
        string AuthHeaderName { get; set; }
        string AuthScheme { get; set; }
        bool UseApiKeyQuery { get; set; }
        string ApiKeyQueryName { get; set; }

        // Timeweb-specific
        string AgentAccessId { get; set; }
    }
}