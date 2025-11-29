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
            set => _serverUrl = ValidateUrl(value);
        }

        public string ApiKey
        {
            get => _apiKey;
            set => _apiKey = value;
        }

        public int TimeoutMs { get; set; } = 30000;

        public bool UseSSL { get; set; } = true;

        public ServerConnectionSettings() { }

        public ServerConnectionSettings(string serverUrl, string apiKey)
        {
            ServerUrl = serverUrl;
            ApiKey = apiKey;
        }

        private static string ValidateUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("URL сервера не может быть пустым.");

            if (!Uri.TryCreate(url, UriKind.Absolute, out _))
                throw new ArgumentException("Некорректный формат URL сервера.");

            return url;
        }
    }
}