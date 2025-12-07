namespace AIAssistantEndpoint.Commands
{
    using System;
    using System.ComponentModel.Design;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using AIAssistantEndpoint.Logging;
    using System.Windows;
    using AIAssistantEndpoint.Configuration;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Services;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.TextManager.Interop;
    using Microsoft.VisualStudio.Editor;
    using System.Linq;
using Microsoft.VisualStudio.Text;
using System.Windows.Controls;
using System.Windows.Documents;

    /// <summary>
    /// Команда для объяснения выделенного кода
    /// </summary>
    internal sealed class ExplainCodeCommand
    {
        public const int CommandId = 0x0106;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private ExplainCodeCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("ExplainCodeCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
                _logger.Info("ExplainCodeCommand успешно зарегистрирован");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при регистрации команды: {ex.Message}", ex);
            }
        }

        private void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            try
            {
                var command = sender as OleMenuCommand;
                if (command == null) return;

                // Команда доступна только если есть выделенный текст
                command.Visible = false;
                command.Enabled = false;

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var textView = GetActiveTextView();
                    if (textView != null && !textView.Selection.IsEmpty)
                    {
                        command.Visible = true;
                        command.Enabled = true;
                    }
                });
            }
            catch { }
        }

        public static ExplainCodeCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new ExplainCodeCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("ExplainCodeCommand");
                logger.Error($"Ошибка инициализации: {ex.Message}", ex);
            }
        }

        private IWpfTextView GetActiveTextView()
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                var textManager = (IVsTextManager)Package.GetGlobalService(typeof(SVsTextManager));
                if (textManager == null) return null;

                IVsTextView textView = null;
                textManager.GetActiveView(1, null, out textView);
                if (textView == null) return null;

                var adapterFactory = (IVsEditorAdaptersFactoryService)Package.GetGlobalService(typeof(IVsEditorAdaptersFactoryService));
                if (adapterFactory == null) return null;

                return adapterFactory.GetWpfTextView(textView);
            }
            catch
            {
                return null;
            }
        }


        private async void Execute(object sender, EventArgs e)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var textView = GetActiveTextView();
                if (textView == null)
                {
                    MessageBox.Show("Не удалось получить активный редактор.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selection = textView.Selection;
                if (selection.IsEmpty)
                {
                    MessageBox.Show("Пожалуйста, выделите код для объяснения.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем выделенный текст
                var selectedSpan = selection.SelectedSpans.FirstOrDefault();
                if (selectedSpan == null) return;

                var selectedText = selectedSpan.GetText();
                if (string.IsNullOrWhiteSpace(selectedText))
                {
                    MessageBox.Show("Выделенный текст пуст.", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Загружаем настройки
                var cfg = new JsonConfigurationManager();
                if (!cfg.SettingsExist())
                {
                    MessageBox.Show("Настройки AI не настроены. Откройте Сервис → AI Assistant → Настройки.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var settings = cfg.LoadSettings();
                if (settings == null)
                {
                    MessageBox.Show("Не удалось загрузить настройки.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Показываем окно с объяснением
                await ShowExplanationWindow(selectedText, settings);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при выполнении команды: {ex.Message}", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ShowExplanationWindow(string code, ServerConnectionSettings settings)
        {
            try
            {
                // Создаем окно для отображения объяснения
                var window = new Window
                {
                    Title = "Объяснение кода - AI Assistant",
                    Width = 800,
                    Height = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false
                };

                var grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Padding = new Thickness(10)
                };

                var explanationText = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Margin = new Thickness(5)
                };

                scrollViewer.Content = explanationText;
                Grid.SetRow(scrollViewer, 0);
                grid.Children.Add(scrollViewer);

                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(10)
                };

                var closeButton = new Button
                {
                    Content = "Закрыть",
                    Width = 100,
                    Height = 30,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                closeButton.Click += (s, e) => window.Close();

                buttonPanel.Children.Add(closeButton);
                Grid.SetRow(buttonPanel, 1);
                grid.Children.Add(buttonPanel);

                window.Content = grid;

                // Показываем окно
                window.Show();

                // Запрашиваем объяснение у AI
                explanationText.Text = "Запрос к AI... Пожалуйста, подождите...";

                var service = new ServerConnectionService(settings);
                if (!await service.ConnectAsync())
                {
                    explanationText.Text = "Ошибка: Не удалось подключиться к AI серверу.";
                    return;
                }

                var prompt = $"Объясни следующий код на русском языке простым и понятным языком. Опиши, что делает этот код, как он работает, и какие основные концепции используются:\n\n```\n{code}\n```";
                
                var response = await service.SendRequestAsync(prompt);

                if (string.IsNullOrWhiteSpace(response))
                {
                    explanationText.Text = "AI вернул пустой ответ.";
                }
                else
                {
                    explanationText.Text = response;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при показе окна объяснения: {ex.Message}", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
