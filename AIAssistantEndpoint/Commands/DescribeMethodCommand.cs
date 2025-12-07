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
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    /// <summary>
    /// Команда для описания метода с помощью AI
    /// </summary>
    internal sealed class DescribeMethodCommand
    {
        public const int CommandId = 0x0107;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private DescribeMethodCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("DescribeMethodCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new OleMenuCommand(this.Execute, menuCommandID);
                menuItem.BeforeQueryStatus += OnBeforeQueryStatus;
                commandService.AddCommand(menuItem);
                _logger.Info("DescribeMethodCommand успешно зарегистрирован");
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

                command.Visible = false;
                command.Enabled = false;

                ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var textView = GetActiveTextView();
                    if (textView != null)
                    {
                        // Проверяем, что курсор находится внутри метода
                        var methodNode = GetMethodAtCaret(textView);
                        if (methodNode != null)
                        {
                            command.Visible = true;
                            command.Enabled = true;
                        }
                    }
                });
            }
            catch { }
        }

        public static DescribeMethodCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new DescribeMethodCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("DescribeMethodCommand");
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

                var wpfTextView = adapterFactory.GetWpfTextView(textView);
                
                // Проверяем, что это C# файл
                if (wpfTextView != null && !wpfTextView.TextBuffer.ContentType.IsOfType("CSharp"))
                    return null;

                return wpfTextView;
            }
            catch
            {
                return null;
            }
        }

        private SyntaxNode GetMethodAtCaret(IWpfTextView textView)
        {
            try
            {
                var caretPosition = textView.Caret.Position.BufferPosition;
                var snapshot = caretPosition.Snapshot;
                var fullText = snapshot.GetText();
                var position = caretPosition.Position;

                // Проверяем, что это C# файл
                if (!textView.TextBuffer.ContentType.IsOfType("CSharp"))
                    return null;

                // Парсим код с помощью Roslyn
                var tree = CSharpSyntaxTree.ParseText(fullText);
                var root = tree.GetRoot();

                // Находим метод, конструктор или локальную функцию, содержащую позицию
                var methodNode = root.DescendantNodes()
                    .Where(n => n is MethodDeclarationSyntax || 
                                n is ConstructorDeclarationSyntax || 
                                n is LocalFunctionStatementSyntax)
                    .Cast<SyntaxNode>()
                    .OrderBy(n => n.Span.Length)
                    .FirstOrDefault(n => n.Span.Start <= position && n.Span.End >= position);

                return methodNode;
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
                    MessageBox.Show("Не удалось получить активный редактор или файл не является C# файлом.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var methodNode = GetMethodAtCaret(textView);
                if (methodNode == null)
                {
                    MessageBox.Show("Курсор должен находиться внутри метода, конструктора или локальной функции.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Получаем текст метода
                var snapshot = textView.TextBuffer.CurrentSnapshot;
                var span = methodNode.Span;
                var methodText = snapshot.GetText(span.Start, span.Length);

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

                // Генерируем комментарий
                await GenerateAndInsertCommentAsync(textView, span.Start, methodText, settings);
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при выполнении команды: {ex.Message}", ex);
                MessageBox.Show($"Ошибка: {ex.Message}", "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task GenerateAndInsertCommentAsync(
            IWpfTextView textView,
            int insertPosition,
            string methodText,
            ServerConnectionSettings settings)
        {
            try
            {
                var prompt = "Напиши краткий XML комментарий документации (в стиле трех слэшей C#) для следующего C# метода. Верни только строки комментария, начинающиеся с '///':\n\n" + methodText;

                var service = new ServerConnectionService(settings);
                if (!await service.ConnectAsync())
                {
                    MessageBox.Show("Не удалось подключиться к AI серверу.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var response = await service.SendRequestAsync(prompt);

                if (string.IsNullOrWhiteSpace(response))
                {
                    MessageBox.Show("AI вернул пустой ответ.", 
                        "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Нормализуем комментарий: убеждаемся, что каждая строка начинается с '///'
                var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(l => l.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .ToArray();

                for (int i = 0; i < lines.Length; i++)
                {
                    if (!lines[i].StartsWith("///"))
                        lines[i] = "/// " + lines[i];
                }

                // Вычисляем отступ целевой строки
                var snapshot = textView.TextBuffer.CurrentSnapshot;
                var line = snapshot.GetLineFromPosition(insertPosition);
                var lineText = line.GetText();
                var indent = new string(lineText.TakeWhile(Char.IsWhiteSpace).ToArray());

                // Применяем отступ ко всем строкам комментария
                var indentedComment = string.Join(Environment.NewLine, lines.Select(l => indent + l)) + Environment.NewLine;

                // Вставляем комментарий перед строкой начала метода
                using (var edit = textView.TextBuffer.CreateEdit())
                {
                    edit.Insert(line.Start.Position, indentedComment);
                    edit.Apply();
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при генерации комментария: {ex.Message}", ex);
                MessageBox.Show($"Ошибка при генерации комментария: {ex.Message}", 
                    "AI Assistant", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
