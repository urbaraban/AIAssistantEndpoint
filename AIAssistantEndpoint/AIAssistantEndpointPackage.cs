namespace AIAssistantEndpoint
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;
    using AIAssistantEndpoint.Logging;

    /// <summary>
    /// Пакет расширения для Visual Studio
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(UI.AIChatToolWindow), Window = "DocumentWell")]
    [ProvideToolWindowVisibility(typeof(UI.AIChatToolWindow), UIContextGuids.SolutionExists)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideBindingPath]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(OptionPages.GeneralOptionsPage), "AI Assistant", "General", 110, 111, true)]
    public sealed class AIAssistantEndpointPackage : AsyncPackage
    {
        public const string PackageGuidString = "910fb63c-0431-45e6-a526-e7ad35867bd9";
        private static readonly ILogger Logger = new DebugLogger("AIAssistantEndpointPackage");

        public AIAssistantEndpointPackage()
        {
            Logger.Info("AIAssistantEndpointPackage инициализирован");
        }

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                Logger.Info("Начало инициализации команд");

                // Регистрация команд
                try
                {
                    await Commands.ShowAIChatCommand.InitializeAsync(this);
                    Logger.Info("ShowAIChatCommand инициализирована");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка инициализации ShowAIChatCommand: {ex.Message}", ex);
                }

                try
                {
                    await Commands.ShowSettingsCommand.InitializeAsync(this);
                    Logger.Info("ShowSettingsCommand инициализирована");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка инициализации ShowSettingsCommand: {ex.Message}", ex);
                }

                try
                {
                    await Commands.ShowOptionsPageCommand.InitializeAsync(this);
                    Logger.Info("ShowOptionsPageCommand инициализирована");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Ошибка инициализации ShowOptionsPageCommand: {ex.Message}", ex);
                }

                Logger.Info("Инициализация пакета завершена");
            }
            catch (Exception ex)
            {
                Logger.Error($"Критическая ошибка при инициализации пакета: {ex.Message}", ex);
            }
        }
    }
}
