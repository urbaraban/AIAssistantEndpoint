namespace AIAssistantEndpoint.Configuration
{
    using System;
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Services;
    using AIAssistantEndpoint.Settings;

    /// <summary>
    /// Интерактивный помощник для настройки подключения к серверу
    /// </summary>
    public class ServerConfigurationHelper
    {
        private readonly IServerConnectionService _connectionService;

        public ServerConfigurationHelper()
        {
        }

        public ServerConfigurationHelper(IServerConnectionService connectionService)
        {
            _connectionService = connectionService;
        }

        /// <summary>
        /// Создаёт новые параметры подключения с валидацией
        /// </summary>
        public ServerConnectionSettings CreateSettings(string serverUrl, string apiKey, int? timeoutMs = null, bool? useSSL = null)
        {
            if (string.IsNullOrWhiteSpace(serverUrl))
                throw new ArgumentException("URL сервера не может быть пустым.");

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API ключ не может быть пустым.");

            var settings = new ServerConnectionSettings(serverUrl.Trim(), apiKey.Trim());

            if (timeoutMs.HasValue && timeoutMs.Value >= 1000)
                settings.TimeoutMs = timeoutMs.Value;

            if (useSSL.HasValue)
                settings.UseSSL = useSSL.Value;

            return settings;
        }

        /// <summary>
        /// Проверяет соединение с сервером
        /// </summary>
        public async Task<bool> TestConnectionAsync(ServerConnectionSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                var service = new ServerConnectionService(settings);
                return await service.ConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Загружает, валидирует и применяет параметры
        /// </summary>
        public async Task<bool> ApplySettingsAsync(string configPath)
        {
            try
            {
                var configManager = new JsonConfigurationManager(configPath);
                if (!configManager.SettingsExist())
                    return false;

                var settings = configManager.LoadSettings();
                if (settings == null)
                    return false;

                return await TestConnectionAsync(settings);
            }
            catch
            {
                return false;
            }
        }
    }
}
