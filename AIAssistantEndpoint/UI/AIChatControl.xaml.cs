using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Logging;
using System.Collections.Generic;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;

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
        private readonly ObservableCollection<string> _attachments;
        private bool _isConnected;

        // UI элементы
        private ListBox _messagesListBox;
        private Button _sendButton;
        private Button _clearButton;
        private Button _settingsButton;
        private ListBox _attachmentsListBox;
        private Button _attachButton;

        public AIChatControl()
        {
            InitializeComponent();

            _logger = new DebugLogger("AIChatControl");
            _messages = new ObservableCollection<ChatMessage>();
            _attachments = new ObservableCollection<string>();
            
            // Получаем элементы из XAML
            _messagesListBox = (ListBox)FindName("MessagesListBox");
            _sendButton = (Button)FindName("SendButton");
            _clearButton = (Button)FindName("ClearButton");
            _settingsButton = (Button)FindName("SettingsButton");
            _attachmentsListBox = (ListBox)FindName("AttachmentsListBox");
            _attachButton = (Button)FindName("AttachButton");

            if (_messagesListBox != null)
            {
                _messagesListBox.ItemsSource = _messages;
            }

            if (_attachmentsListBox != null)
            {
                _attachmentsListBox.ItemsSource = _attachments;
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
                        if (InputTextBox != null) InputTextBox.IsEnabled = true;
                        if (_attachButton != null) _attachButton.IsEnabled = true;
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
            // Ctrl+Enter send
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;
                await SendMessageAsync();
                return;
            }

            // Shift+Enter or plain Enter for newline
            if (e.Key == Key.Return && Keyboard.Modifiers == ModifierKeys.None)
            {
                // allow newline
                return;
            }
        }

        private async Task SendMessageAsync()
        {
            if (InputTextBox == null)
                return;

            string userInput = InputTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(userInput) && _attachments.Count == 0)
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
                InputTextBox.Clear();
                if (_sendButton != null) _sendButton.IsEnabled = false;

                // Подготовить file ids — в текущей реализации мы отправляем пути файлов как "file_ids"
                var fileIds = new List<string>(_attachments);

                // Отправляем запрос
                AddSystemMessage("⏳ Отправляю запрос...");

                var response = await _connectionService.SendRequestAsync(userInput, fileIds);

                // Удаляем сообщение "отправляю"
                if (_messages.Count > 0 && _messages[_messages.Count - 1].IsSystem && 
                    _messages[_messages.Count - 1].Text.Contains("Отправляю"))
                {
                    _messages.RemoveAt(_messages.Count - 1);
                }

                // Добавляем ответ
                AddAIMessage(response);

                _logger.Info($"Получен ответ: {response.Substring(0, Math.Min(50, response.Length))}...");

                // Очистить список вложений после успешной отправки
                _attachments.Clear();
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка отправки: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка: {ex.Message}");
            }
            finally
            {
                if (_sendButton != null) _sendButton.IsEnabled = true;
                if (InputTextBox != null) InputTextBox.Focus();
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

        private void AttachButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog();
            dlg.Multiselect = true;
            var result = dlg.ShowDialog();
            if (result == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    _attachments.Add(file);
                }
            }
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var tag = btn?.Tag as string;
            if (!string.IsNullOrEmpty(tag) && _attachments.Contains(tag))
            {
                _attachments.Remove(tag);
            }
        }

        private async void InsertSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte?.ActiveDocument == null)
                {
                    AddSystemMessage("❌ Нет активного документа");
                    return;
                }

                var textDoc = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDoc == null)
                {
                    AddSystemMessage("❌ Невозможно получить текст документа");
                    return;
                }

                var sel = textDoc.Selection;
                var selected = sel.Text;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    // fallback to full document
                    selected = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                }

                if (!string.IsNullOrEmpty(selected))
                {
                    if (InputTextBox != null)
                    {
                        var existing = InputTextBox.Text ?? string.Empty;
                        if (!string.IsNullOrEmpty(existing))
                            InputTextBox.Text = existing + Environment.NewLine + selected;
                        else
                            InputTextBox.Text = selected;
                        InputTextBox.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка вставки выделения: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка вставки: {ex.Message}");
            }
        }

        private async void InsertDocument_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte?.ActiveDocument == null)
                {
                    AddSystemMessage("❌ Нет активного документа");
                    return;
                }

                var textDoc = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDoc == null)
                {
                    AddSystemMessage("❌ Невозможно получить текст документа");
                    return;
                }

                var full = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                if (!string.IsNullOrEmpty(full) && InputTextBox != null)
                {
                    var existing = InputTextBox.Text ?? string.Empty;
                    if (!string.IsNullOrEmpty(existing))
                        InputTextBox.Text = existing + Environment.NewLine + full;
                    else
                        InputTextBox.Text = full;
                    InputTextBox.Focus();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка вставки документа: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка вставки: {ex.Message}");
            }
        }

        private async void InsertSelectionAsFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte?.ActiveDocument == null)
                {
                    AddSystemMessage("❌ Нет активного документа");
                    return;
                }

                var textDoc = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDoc == null)
                {
                    AddSystemMessage("❌ Невозможно получить текст документа");
                    return;
                }

                var sel = textDoc.Selection;
                var selected = sel.Text;
                if (string.IsNullOrWhiteSpace(selected))
                {
                    selected = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                }

                if (string.IsNullOrEmpty(selected))
                {
                    AddSystemMessage("❌ Нет текста для загрузки");
                    return;
                }

                // Wrap in markdown block
                string language = "txt";
                try
                {
                    var fileName = dte.ActiveDocument.Name;
                    var ext = Path.GetExtension(fileName)?.TrimStart('.');
                    if (!string.IsNullOrEmpty(ext)) language = ext;
                }
                catch { }

                var toUpload = "```" + language + "\n" + selected + "\n```";

                var bytes = System.Text.Encoding.UTF8.GetBytes(toUpload);
                var fileNameUpload = "selection_" + DateTime.Now.ToString("yyyyMMddHHmmss") + "." + language;

                AddSystemMessage("⏳ Загружаю выделение как файл...");

                var fileId = await _connectionService.UploadFileAsync(fileNameUpload, bytes);

                if (!string.IsNullOrEmpty(fileId))
                {
                    _attachments.Add(fileId);
                    AddSystemMessage("✅ Выделение загружено как файл и добавлено в file_ids");
                }
                else
                {
                    AddSystemMessage("⚠️ Сервер вернул пустой file_id");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка загрузки выделения как файла: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка загрузки: {ex.Message}");
            }
        }

        private async void CompleteCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var dte = (EnvDTE.DTE)Package.GetGlobalService(typeof(EnvDTE.DTE));
                if (dte?.ActiveDocument == null)
                {
                    AddSystemMessage("❌ Нет активного документа для автодополнения");
                    return;
                }

                var textDoc = dte.ActiveDocument.Object("TextDocument") as TextDocument;
                if (textDoc == null)
                {
                    AddSystemMessage("❌ Невозможно получить текст документа");
                    return;
                }

                var sel = textDoc.Selection;
                var caret = sel.ActivePoint;

                // Collect context: 1000 chars before and after caret
                var full = textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint);
                var pos = caret.AbsoluteCharOffset;
                int contextRadius = 1000;
                int start = Math.Max(0, pos - contextRadius);
                int lengthBefore = pos - start;
                var before = full.Substring(start, lengthBefore);
                int afterLength = Math.Min(contextRadius, full.Length - pos);
                var after = full.Substring(pos, afterLength);

                // Insert cursor marker
                var payloadText = before + "<<<CURSOR>>>" + after;

                // Build prompt
                var prompt = "Complete the code at <<<CURSOR>>>. Return only the code to insert (no explanations).\n\n" + payloadText;

                AddSystemMessage("⏳ Запрос на автодополнение отправлен (streaming)...");

                var streaming = await _connectionService.SendStreamingRequestAsync(prompt);

                var preview = new CompletionPreviewWindow();
                preview.Owner = System.Windows.Window.GetWindow(this);

                var currentBuilder = new System.Text.StringBuilder();
                streaming.OnChunkReceived += (chunk) =>
                {
                    currentBuilder.Append(chunk);
                    preview.UpdatePreview(currentBuilder.ToString());
                };

                streaming.OnCompleted += (fullResponse) =>
                {
                    preview.UpdatePreview(fullResponse);
                    preview.EnableAccept();
                };

                streaming.OnError += (err) =>
                {
                    preview.UpdatePreview($"Error: {err.Message}");
                };

                var accepted = preview.ShowDialog();
                if (accepted == true)
                {
                    var toInsert = preview.FinalText;
                    if (!string.IsNullOrEmpty(toInsert))
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        try
                        {
                            dte.UndoContext.Open("AI Completion");
                            sel.Insert(toInsert);
                        }
                        finally
                        {
                            if (dte.UndoContext.IsOpen) dte.UndoContext.Close();
                        }
                    }
                }
                else
                {
                    AddSystemMessage("❌ Автодополнение отменено");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка автодополнения: {ex.Message}", ex);
                AddSystemMessage($"❌ Ошибка автодополнения: {ex.Message}");
            }
        }

        // end of AIChatControl class
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
