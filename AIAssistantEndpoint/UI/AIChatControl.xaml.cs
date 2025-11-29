using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Logging;

namespace AIAssistantEndpoint.UI
{
    /// <summary>
    /// Логика взаимодействия для AIChatControl.xaml
    /// </summary>
    public partial class AIChatControl : UserControl
    {
        private readonly IServerConnectionService _connectionService;
        private readonly ILogger _logger;
        private readonly ObservableCollection<ChatMessage> _messages;
        private bool _isConnected;

        // UI элементы
        private ListBox _messagesListBox;
        private TextBox _inputTextBox;
        private Button _sendButton;
        private Button _clearButton;
        private Button _settingsButton;

        public AIChatControl()
        {
            InitializeComponent();

            _logger = new DebugLogger("AIChatControl");
            _messages = new ObservableCollection<ChatMessage>();
            
            // Получаем элементы из XAML
            _messagesListBox = (ListBox)FindName("MessagesListBox");
            _inputTextBox = (TextBox)FindName("InputTextBox");
            _sendButton = (Button)FindName("SendButton");
            _clearButton = (Button)FindName("ClearButton");
            _settingsButton = (Button)FindName("SettingsButton");

            if (_messagesListBox != null)
            {
                _messagesListBox.ItemsSource = _messages;
            }
            
            // Попытка загрузить сохранённые настройки
            var configManager = new JsonConfigurationManager();
            if (configManager.SettingsExist())
            {
                var settings = configManager.LoadSettings();
                _connectionService = new ServerConnectionService(settings);
                TryConnectAsync();
            }
            else
            {
                _connectionService = null;
                _isConnected = false;
                AddSystemMessage("⚙️ Настройки не найдены. Откройте меню Tools → AI Assistant → Settings для конфигурации.");
            }
        }

        private async void TryConnectAsync()
        {
            try
            {
                if (_connectionService != null)
                {
                    _isConnected = await _connectionService.ConnectAsync();
                    if (_isConnected)
                    {
                        AddSystemMessage("✅ Подключено к AI серверу");
                        if (_sendButton != null) _sendButton.IsEnabled = true;
                        if (_inputTextBox != null) _inputTextBox.IsEnabled = true;
                    }
                    else
                    {
                        AddSystemMessage("❌ Ошибка подключения. Проверьте настройки.");
                        if (_sendButton != null) _sendButton.IsEnabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка подключения: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка: {ex.Message}");
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageAsync();
        }

        private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                await SendMessageAsync();
            }
        }

        private async Task SendMessageAsync()
        {
            if (_inputTextBox == null)
                return;

            string userInput = _inputTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(userInput))
                return;

            if (!_isConnected || _connectionService == null)
            {
                AddSystemMessage("❌ Не подключено к серверу. Проверьте настройки.");
                return;
            }

            try
            {
                // Добавляем сообщение пользователя
                AddUserMessage(userInput);
                _inputTextBox.Clear();
                if (_sendButton != null) _sendButton.IsEnabled = false;

                // Отправляем запрос
                AddSystemMessage("⏳ Отправляю запрос...");

                var response = await _connectionService.SendRequestAsync(userInput);

                // Удаляем сообщение "отправляю"
                if (_messages.Count > 0 && _messages[_messages.Count - 1].IsSystem && 
                    _messages[_messages.Count - 1].Text.Contains("Отправляю"))
                {
                    _messages.RemoveAt(_messages.Count - 1);
                }

                // Добавляем ответ
                AddAIMessage(response);

                _logger.Info($"Получен ответ: {response.Substring(0, Math.Min(50, response.Length))}...");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка отправки: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка: {ex.Message}");
            }
            finally
            {
                if (_sendButton != null) _sendButton.IsEnabled = true;
                if (_inputTextBox != null) _inputTextBox.Focus();
            }
        }

        private void AddUserMessage(string text)
        {
            _messages.Add(new ChatMessage
            {
                Text = text,
                IsUser = true,
                IsSystem = false,
                Timestamp = DateTime.Now
            });
            ScrollToBottom();
        }

        private void AddAIMessage(string text)
        {
            _messages.Add(new ChatMessage
            {
                Text = text,
                IsUser = false,
                IsSystem = false,
                IsAI = true,
                Timestamp = DateTime.Now
            });
            ScrollToBottom();
        }

        private void AddSystemMessage(string text)
        {
            _messages.Add(new ChatMessage
            {
                Text = text,
                IsUser = false,
                IsSystem = true,
                Timestamp = DateTime.Now
            });
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            if (_messagesListBox != null && _messagesListBox.Items.Count > 0)
            {
                _messagesListBox.ScrollIntoView(_messagesListBox.Items[_messagesListBox.Items.Count - 1]);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _messages.Clear();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new ServerSettingsWindow();
            settingsWindow.ShowDialog();
        }
    }

    /// <summary>
    /// Модель сообщения в чате
    /// </summary>
    public class ChatMessage
    {
        public string Text { get; set; }
        public bool IsUser { get; set; }
        public bool IsAI { get; set; }
        public bool IsSystem { get; set; }
        public DateTime Timestamp { get; set; }

        public string SenderName
        {
            get
            {
                if (IsSystem) return "Система";
                if (IsUser) return "Вы";
                return "AI";
            }
        }

        public string SenderColor
        {
            get
            {
                if (IsSystem) return "#999999";
                if (IsUser) return "#2196F3";
                return "#4CAF50";
            }
        }
    }
}
