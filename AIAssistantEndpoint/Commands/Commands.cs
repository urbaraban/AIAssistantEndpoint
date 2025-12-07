namespace AIAssistantEndpoint.Commands
{
    using System;
    using System.ComponentModel.Design;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using AIAssistantEndpoint.Logging;
    using System.Windows;
    using AIAssistantEndpoint.Configuration;
    using AIAssistantEndpoint.Settings;
    using AIAssistantEndpoint.Services;

    /// <summary>
    /// Команда для открытия окна чата
    /// </summary>
    internal sealed class ShowAIChatCommand
    {
        public const int CommandId = 0x0100;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private ShowAIChatCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("ShowAIChatCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("ShowAIChatCommand успешно зарегистрирована");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при регистрации команды: {ex.Message}", ex);
            }
        }

        public static ShowAIChatCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new ShowAIChatCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("ShowAIChatCommand");
                logger.Error($"Ошибка инициализации: {ex.Message}", ex);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var shell = (IVsUIShell)await _package.GetServiceAsync(typeof(SVsUIShell));
                        if (shell == null)
                        {
                            _logger.Warning("IVsUIShell не найден");
                            return;
                        }

                        var toolWindowType = typeof(UI.AIChatToolWindow);
                        var window = FindToolWindow(toolWindowType, 0, true);

                        if (window?.Frame is IVsWindowFrame windowFrame)
                        {
                            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
                            _logger.Info("Окно чата открыто");
                        }
                        else
                        {
                            _logger.Warning("Не удалось получить окно инструмента");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при выполнении команды: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка в Execute: {ex.Message}", ex);
            }
        }

        private ToolWindowPane FindToolWindow(Type toolWindowType, int id, bool create)
        {
            try
            {
                var window = _package.FindToolWindow(toolWindowType, id, create);
                if (window is ToolWindowPane toolWindow)
                {
                    return toolWindow;
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при поиске окна: {ex.Message}", ex);
            }
            return null;
        }
    }

    /// <summary>
    /// Команда для открытия окна настроек
    /// </summary>
    internal sealed class ShowSettingsCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private ShowSettingsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("ShowSettingsCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("ShowSettingsCommand успешно зарегистрирована");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при регистрации команды: {ex.Message}", ex);
            }
        }

        public static ShowSettingsCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new ShowSettingsCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("ShowSettingsCommand");
                logger.Error($"Ошибка инициализации: {ex.Message}", ex);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                var settingsWindow = new UI.ServerSettingsWindow();
                settingsWindow.ShowDialog();
                _logger.Info("Окно настроек открыто");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при открытии окна настроек: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Команда для открытия страницы Options
    /// </summary>
    internal sealed class ShowOptionsPageCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private ShowOptionsPageCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("ShowOptionsPageCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("ShowOptionsPageCommand успешно зарегистрирована");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при регистрации команды: {ex.Message}", ex);
            }
        }

        public static ShowOptionsPageCommand Instance { get; private set; }

        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => _package;

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new ShowOptionsPageCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("ShowOptionsPageCommand");
                logger.Error($"Ошибка инициализации: {ex.Message}", ex);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var shell = (IVsUIShell)await _package.GetServiceAsync(typeof(SVsUIShell));
                        if (shell == null)
                        {
                            _logger.Warning("IVsUIShell не найден");
                            return;
                        }

                        // Открываем Tools ? Options ? AI Assistant ? General
                        Guid cmdSetGuid = new Guid("1A9F7F56-E7FB-4BFE-A5D7-2DA36A9C3AC7");
                        shell.PostExecCommand(
                            ref cmdSetGuid,
                            7713, // cmdidToolsOptions
                            0,
                            null);

                        _logger.Info("Страница Tools ? Options открыта");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при выполнении команды: {ex.Message}", ex);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка в Execute: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Команда для быстрого теста подключения (без открытия окна настроек)
    /// </summary>
    internal sealed class ShowTestConnectionCommand
    {
        public const int CommandId = 0x0103;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private ShowTestConnectionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("ShowTestConnectionCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("ShowTestConnectionCommand успешно зарегистрирована");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка при регистрации команды: {ex.Message}", ex);
            }
        }

        public static ShowTestConnectionCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            try
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                if (commandService != null)
                {
                    Instance = new ShowTestConnectionCommand(package, commandService);
                }
            }
            catch (Exception ex)
            {
                var logger = new DebugLogger("ShowTestConnectionCommand");
                logger.Error($"Ошибка инициализации: {ex.Message}", ex);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    try
                    {
                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                        var configManager = new JsonConfigurationManager();
                        if (!configManager.SettingsExist())
                        {
                            MessageBox.Show("Настройки не найдены. Откройте Settings и заполните поля.", "Test Connection", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var settings = configManager.LoadSettings();
                        var service = new ServerConnectionService(settings);
                        bool connected = await service.ConnectAsync();

                        if (connected)
                        {
                            MessageBox.Show("? Соединение успешно.", "Test Connection", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("? Ошибка подключения.", "Test Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Ошибка при выполнении теста подключения: {ex.Message}", ex);
                        MessageBox.Show($"Ошибка теста подключения: {ex.Message}", "Test Connection", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка в Execute: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Команда Accept AI Suggestion
    /// </summary>
    internal sealed class AcceptSuggestionCommand
    {
        public const int CommandId = 0x0104;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private AcceptSuggestionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("AcceptSuggestionCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("AcceptSuggestionCommand registered");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error registering AcceptSuggestionCommand: {ex.Message}", ex);
            }
        }

        public static AcceptSuggestionCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                Instance = new AcceptSuggestionCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                // Accept suggestion via SuggestionManager (inline ghost text)
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        AIAssistantEndpoint.Editor.SuggestionManager.AcceptSuggestion();
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing AcceptSuggestionCommand: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Команда Reject AI Suggestion
    /// </summary>
    internal sealed class RejectSuggestionCommand
    {
        public const int CommandId = 0x0105;
        public static readonly Guid CommandSet = new Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1");

        private readonly AsyncPackage _package;
        private readonly ILogger _logger;

        private RejectSuggestionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = new DebugLogger("RejectSuggestionCommand");

            try
            {
                var menuCommandID = new CommandID(CommandSet, CommandId);
                var menuItem = new MenuCommand(this.Execute, menuCommandID);
                commandService.AddCommand(menuItem);
                _logger.Info("RejectSuggestionCommand registered");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error registering RejectSuggestionCommand: {ex.Message}", ex);
            }
        }

        public static RejectSuggestionCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                Instance = new RejectSuggestionCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            try
            {
                // Reject suggestion via SuggestionManager
                ThreadHelper.JoinableTaskFactory.RunAsync(async delegate
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        AIAssistantEndpoint.Editor.SuggestionManager.ClearSuggestion();
                    }
                    catch { }
                });
            }
            catch (Exception ex)
            {
                _logger.Error($"Error executing RejectSuggestionCommand: {ex.Message}", ex);
            }
        }
    }
}
