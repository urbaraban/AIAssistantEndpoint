using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Logging;

namespace AIAssistantEndpoint.UI
{
    /// <summary>
    /// Логика взаимодействия для ServerSettingsWindow.xaml
    /// </summary>
    public partial class ServerSettingsWindow : Window
    {
        private readonly ILogger _logger;
        private ServerConnectionSettings _currentSettings;

        // UI элементы
        private TextBox _serverUrlTextBox;
        private PasswordBox _apiKeyPasswordBox;
        private CheckBox _useSSLCheckBox;
        private TextBox _timeoutTextBox;
        private Button _testConnectionButton;
        private TextBlock _statusTextBlock;
        private Button _okButton;
        private Button _cancelButton;

        public ServerSettingsWindow()
        {
            InitializeComponent();
            _logger = new DebugLogger("ServerSettings");
            
            // Получаем элементы из XAML
            _serverUrlTextBox = (TextBox)FindName("ServerUrlTextBox");
            _apiKeyPasswordBox = (PasswordBox)FindName("ApiKeyPasswordBox");
            _useSSLCheckBox = (CheckBox)FindName("UseSSLCheckBox");
            _timeoutTextBox = (TextBox)FindName("TimeoutTextBox");
            _testConnectionButton = (Button)FindName("TestConnectionButton");
            _statusTextBlock = (TextBlock)FindName("StatusTextBlock");
            _okButton = (Button)FindName("OkButton");
            _cancelButton = (Button)FindName("CancelButton");

            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                var configManager = new JsonConfigurationManager();
                if (configManager.SettingsExist())
                {
                    _currentSettings = configManager.LoadSettings();
                    if (_serverUrlTextBox != null) _serverUrlTextBox.Text = _currentSettings.ServerUrl;
                    if (_apiKeyPasswordBox != null) _apiKeyPasswordBox.Password = _currentSettings.ApiKey;
                    if (_useSSLCheckBox != null) _useSSLCheckBox.IsChecked = _currentSettings.UseSSL;
                    if (_timeoutTextBox != null) _timeoutTextBox.Text = _currentSettings.TimeoutMs.ToString();
                }
                else
                {
                    _currentSettings = new ServerConnectionSettings("", "");
                    if (_timeoutTextBox != null) _timeoutTextBox.Text = "30000";
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка загрузки настроек: {ex.Message}", ex);
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = $"❌ Ошибка загрузки: {ex.Message}";
                    _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
            }
        }

        private void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            TestConnection();
        }

        private async void TestConnection()
        {
            try
            {
                if (_serverUrlTextBox == null || string.IsNullOrWhiteSpace(_serverUrlTextBox.Text))
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Укажите URL сервера";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                if (_apiKeyPasswordBox == null || string.IsNullOrWhiteSpace(_apiKeyPasswordBox.Password))
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Укажите API ключ";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                if (_timeoutTextBox == null || !int.TryParse(_timeoutTextBox.Text, out int timeout) || timeout < 1000)
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Неверный таймаут (минимум 1000 мс)";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                if (_testConnectionButton != null) _testConnectionButton.IsEnabled = false;
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = "⏳ Проверка соединения...";
                    _statusTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                }

                var settings = new ServerConnectionSettings(
                    serverUrl: _serverUrlTextBox.Text,
                    apiKey: _apiKeyPasswordBox.Password)
                {
                    UseSSL = _useSSLCheckBox.IsChecked ?? true,
                    TimeoutMs = timeout
                };

                var service = new ServerConnectionService(settings);
                bool connected = await service.ConnectAsync();

                if (connected)
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "✅ Соединение успешно!";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    }
                }
                else
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Ошибка подключения";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = $"❌ Ошибка: {ex.Message}";
                    _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                _logger.Error($"Ошибка проверки: {ex.Message}", ex);
            }
            finally
            {
                if (_testConnectionButton != null) _testConnectionButton.IsEnabled = true;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SaveSettings();
        }

        private void SaveSettings()
        {
            try
            {
                if (_serverUrlTextBox == null || string.IsNullOrWhiteSpace(_serverUrlTextBox.Text))
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Укажите URL сервера";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                if (_apiKeyPasswordBox == null || string.IsNullOrWhiteSpace(_apiKeyPasswordBox.Password))
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Укажите API ключ";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                if (_timeoutTextBox == null || !int.TryParse(_timeoutTextBox.Text, out int timeout) || timeout < 1000)
                {
                    if (_statusTextBlock != null)
                    {
                        _statusTextBlock.Text = "❌ Неверный таймаут";
                        _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    }
                    return;
                }

                var settings = new ServerConnectionSettings(
                    serverUrl: _serverUrlTextBox.Text,
                    apiKey: _apiKeyPasswordBox.Password)
                {
                    UseSSL = _useSSLCheckBox.IsChecked ?? true,
                    TimeoutMs = timeout
                };

                var configManager = new JsonConfigurationManager();
                configManager.SaveSettings(settings);

                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = "✅ Настройки сохранены";
                    _statusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }

                _logger.Info("Настройки успешно сохранены");

                Task.Delay(1500).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.DialogResult = true;
                        this.Close();
                    });
                });
            }
            catch (Exception ex)
            {
                if (_statusTextBlock != null)
                {
                    _statusTextBlock.Text = $"❌ Ошибка сохранения: {ex.Message}";
                    _statusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                _logger.Error($"Ошибка сохранения: {ex.Message}", ex);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
