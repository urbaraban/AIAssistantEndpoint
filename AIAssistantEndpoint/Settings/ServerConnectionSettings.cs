using System;

namespace AIAssistantEndpoint.Settings
{
    public class ServerConnectionSettings : IServerConnectionSettings
    {
        private string _serverUrl;
        private string _apiKey;

        public string ServerUrl
        {
            get => _serverUrl;
            set => _serverUrl = NormalizeUrl(value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        public int TimeoutMs { get; set; } = 30000;

        public bool UseSSL { get; set; } = true;

        // New properties with sensible defaults for Timeweb Agent
        // Endpoints should contain the agent_access_id placeholder which will be replaced at runtime
        public string CallEndpoint { get; set; } = "/api/v1/cloud-ai/agents/{agent_access_id}/call";
        public string ChatEndpoint { get; set; } = "/api/v1/cloud-ai/agents/{agent_access_id}/v1/chat/completions";
        public string StreamingEndpoint { get; set; } = "/api/v1/cloud-ai/agents/{agent_access_id}/v1/chat/completions";
        public string ModelsEndpoint { get; set; } = "/api/v1/cloud-ai/agents/{agent_access_id}/v1/models";

        public string AuthHeaderName { get; set; } = "Authorization";
        public string AuthScheme { get; set; } = "Bearer";
        public bool UseApiKeyQuery { get; set; } = false;
        public string ApiKeyQueryName { get; set; } = "api_key";

        // Timeweb-specific
        public string AgentAccessId { get; set; } = string.Empty;

        public ServerConnectionSettings() { }

        public ServerConnectionSettings(string serverUrl, string apiKey)
        {
            ServerUrl = serverUrl;
            ApiKey = apiKey;
        }

        private static string NormalizeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            url = url.Trim();

            // Add scheme if missing
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                url = "https://" + url; // prefer https by default
            }

            // Validate
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid server URL.");

            // Return base (scheme + authority) without trailing slash
            return uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }
    }
}