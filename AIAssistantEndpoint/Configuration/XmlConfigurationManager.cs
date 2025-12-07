namespace AIAssistantEndpoint.Configuration
{
    using System;
    using System.IO;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Logging;
    using Newtonsoft.Json.Linq;

    public class JsonConfigurationManager : IConfigurationManager
    {
        private readonly string _configFilePath;
        private readonly ILogger _logger;

        public JsonConfigurationManager(string configDirectory = null, ILogger logger = null)
        {
            _logger = logger ?? new DebugLogger("ConfigManager");

            if (string.IsNullOrEmpty(configDirectory))
            {
                configDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AIAssistant");
            }

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }

            _configFilePath = Path.Combine(configDirectory, "settings.json");
        }

        public void SaveSettings(ServerConnectionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                var json = SerializeToJson(settings);
                File.WriteAllText(_configFilePath, json);
                _logger.Info($"Saved settings to: {_configFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error("Error while saving settings", ex);
                throw;
            }
        }

        public ServerConnectionSettings LoadSettings()
        {
            if (!SettingsExist())
            {
                _logger.Warning("Settings file not found");
                return null;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                var settings = DeserializeFromJson(json);
                _logger.Info("Settings loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error("Error while loading settings", ex);
                throw;
            }
        }

        public bool SettingsExist()
        {
            return File.Exists(_configFilePath);
        }

        public void DeleteSettings()
        {
            try
            {
                if (SettingsExist())
                {
                    File.Delete(_configFilePath);
                    _logger.Info("Settings deleted");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error while deleting settings", ex);
                throw;
            }
        }

        private static string SerializeToJson(ServerConnectionSettings settings)
        {
            var j = new JObject
            {
                ["serverUrl"] = settings.ServerUrl ?? string.Empty,
                ["apiKey"] = settings.ApiKey ?? string.Empty,
                ["timeoutMs"] = settings.TimeoutMs,
                ["useSSL"] = settings.UseSSL,

                ["callEndpoint"] = settings.CallEndpoint ?? string.Empty,
                ["chatEndpoint"] = settings.ChatEndpoint ?? string.Empty,
                ["streamingEndpoint"] = settings.StreamingEndpoint ?? string.Empty,
                ["modelsendpoint"] = settings.ModelsEndpoint ?? string.Empty,

                ["authHeaderName"] = settings.AuthHeaderName ?? string.Empty,
                ["authScheme"] = settings.AuthScheme ?? string.Empty,
                ["useApiKeyQuery"] = settings.UseApiKeyQuery,
                ["apiKeyQueryName"] = settings.ApiKeyQueryName ?? string.Empty,

                ["agentAccessId"] = settings.AgentAccessId ?? string.Empty
            };

            return j.ToString();
        }

        private static ServerConnectionSettings DeserializeFromJson(string json)
        {
            var settings = new ServerConnectionSettings();

            try
            {
                var j = JObject.Parse(json);

                settings.ServerUrl = j.Value<string>("serverUrl") ?? settings.ServerUrl;
                settings.ApiKey = j.Value<string>("apiKey") ?? settings.ApiKey;
                settings.TimeoutMs = j.Value<int?>("timeoutMs") ?? settings.TimeoutMs;
                settings.UseSSL = j.Value<bool?>("useSSL") ?? settings.UseSSL;

                settings.CallEndpoint = j.Value<string>("callEndpoint") ?? settings.CallEndpoint;
                settings.ChatEndpoint = j.Value<string>("chatEndpoint") ?? settings.ChatEndpoint;
                settings.StreamingEndpoint = j.Value<string>("streamingEndpoint") ?? settings.StreamingEndpoint;
                settings.ModelsEndpoint = j.Value<string>("modelsendpoint") ?? settings.ModelsEndpoint;

                settings.AuthHeaderName = j.Value<string>("authHeaderName") ?? settings.AuthHeaderName;
                settings.AuthScheme = j.Value<string>("authScheme") ?? settings.AuthScheme;
                settings.UseApiKeyQuery = j.Value<bool?>("useApiKeyQuery") ?? settings.UseApiKeyQuery;
                settings.ApiKeyQueryName = j.Value<string>("apiKeyQueryName") ?? settings.ApiKeyQueryName;

                settings.AgentAccessId = j.Value<string>("agentAccessId") ?? settings.AgentAccessId;
            }
            catch (Exception ex)
            {
                // If parsing fails, log and return defaults
                var logger = new DebugLogger("ConfigManager");
                logger.Error("Failed to parse settings JSON", ex);
            }

            return settings;
        }
    }
}
