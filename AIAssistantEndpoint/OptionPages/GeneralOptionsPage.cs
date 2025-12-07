namespace AIAssistantEndpoint.OptionPages
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using AIAssistantEndpoint.Logging;

    /// <summary>
    /// Страница опций для AI Assistant в Tools → Options
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [CLSCompliant(false), ComVisible(true)]
    public class GeneralOptionsPage : DialogPage
    {
        private string _serverUrl = "";
        private string _apiKey = "";
        private int _timeoutMs = 30000;
        private bool _useSSL = true;
        private string _agentAccessId = "";
        private readonly ILogger _logger;

        public GeneralOptionsPage()
        {
            _logger = new DebugLogger("OptionsPage");
        }

        [Category("Connection")]
        [DisplayName("Server URL")]
        [Description("URL адрес AI сервера")]
        public string ServerUrl
        {
            get { return _serverUrl; }
            set { _serverUrl = value; }
        }

        [Category("Connection")]
        [DisplayName("API Key")]
        [Description("API ключ для подключения")]
        public string ApiKey
        {
            get { return _apiKey; }
            set { _apiKey = value; }
        }

        [Category("Connection")]
        [DisplayName("Timeout (ms)")]
        [Description("Таймаут соединения в миллисекундах")]
        public int TimeoutMs
        {
            get { return _timeoutMs; }
            set { _timeoutMs = value; }
        }

        [Category("Connection")]
        [DisplayName("Use SSL")]
        [Description("Использовать SSL/TLS для HTTPS")]
        public bool UseSSL
        {
            get { return _useSSL; }
            set { _useSSL = value; }
        }

        [Category("Connection")]
        [DisplayName("Agent Access ID")]
        [Description("Agent access ID (agent_access_id) для Timeweb Agent API")]
        public string AgentAccessId
        {
            get { return _agentAccessId; }
            set { _agentAccessId = value; }
        }

        public override void SaveSettingsToStorage()
        {
            try
            {
                base.SaveSettingsToStorage();

                // Сохраняем в JSON файл
                var settings = new Settings.ServerConnectionSettings(_serverUrl, _apiKey)
                {
                    UseSSL = _useSSL,
                    TimeoutMs = _timeoutMs,
                    AgentAccessId = _agentAccessId
                };

                var configManager = new Configuration.JsonConfigurationManager();
                configManager.SaveSettings(settings);

                _logger.Info("Настройки успешно сохранены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка сохранения настроек: {ex.Message}", ex);
            }
        }

        public override void LoadSettingsFromStorage()
        {
            try
            {
                base.LoadSettingsFromStorage();

                // Загружаем из JSON файла
                var configManager = new Configuration.JsonConfigurationManager();
                if (configManager.SettingsExist())
                {
                    var settings = configManager.LoadSettings();
                    if (settings != null)
                    {
                        _serverUrl = settings.ServerUrl ?? "";
                        _apiKey = settings.ApiKey ?? "";
                        _timeoutMs = settings.TimeoutMs;
                        _useSSL = settings.UseSSL;
                        _agentAccessId = settings.AgentAccessId ?? "";
                    }
                }

                _logger.Info("Настройки успешно загружены");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка загрузки настроек: {ex.Message}", ex);
                // Используем значения по умолчанию
                _serverUrl = "";
                _apiKey = "";
                _timeoutMs = 30000;
                _useSSL = true;
                _agentAccessId = "";
            }
        }
    }
}
