namespace AIAssistantEndpoint.Configuration
{
    using System;
    using System.IO;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Logging;

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
                _logger.Info($"Настройки сохранены в: {_configFilePath}");
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка при сохранении настроек", ex);
                throw;
            }
        }

        public ServerConnectionSettings LoadSettings()
        {
            if (!SettingsExist())
            {
                _logger.Warning("Файл настроек не найден");
                return null;
            }

            try
            {
                var json = File.ReadAllText(_configFilePath);
                var settings = DeserializeFromJson(json);
                _logger.Info("Настройки загружены успешно");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка при загрузке настроек", ex);
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
                    _logger.Info("Настройки удалены");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка при удалении настроек", ex);
                throw;
            }
        }

        private static string SerializeToJson(ServerConnectionSettings settings)
        {
            return $"{{\"serverUrl\":\"{EscapeJson(settings.ServerUrl)}\",\"apiKey\":\"{EscapeJson(settings.ApiKey)}\",\"timeoutMs\":{settings.TimeoutMs},\"useSSL\":{(settings.UseSSL ? "true" : "false")}}}";
        }

        private static ServerConnectionSettings DeserializeFromJson(string json)
        {
            var settings = new ServerConnectionSettings();

            var urlMatch = System.Text.RegularExpressions.Regex.Match(json, @"""serverUrl"":""([^""]*)""");
            if (urlMatch.Success)
                settings.ServerUrl = UnescapeJson(urlMatch.Groups[1].Value);

            var keyMatch = System.Text.RegularExpressions.Regex.Match(json, @"""apiKey"":""([^""]*)""");
            if (keyMatch.Success)
                settings.ApiKey = UnescapeJson(keyMatch.Groups[1].Value);

            var timeoutMatch = System.Text.RegularExpressions.Regex.Match(json, @"""timeoutMs"":(\d+)");
            if (timeoutMatch.Success && int.TryParse(timeoutMatch.Groups[1].Value, out int timeout))
                settings.TimeoutMs = timeout;

            var sslMatch = System.Text.RegularExpressions.Regex.Match(json, @"""useSSL"":(true|false)");
            if (sslMatch.Success)
                settings.UseSSL = sslMatch.Groups[1].Value == "true";

            return settings;
        }

        private static string EscapeJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string UnescapeJson(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\\"", "\"").Replace("\\\\", "\\");
        }
    }
}
