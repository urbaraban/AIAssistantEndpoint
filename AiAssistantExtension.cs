namespace AiAssistant
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using AiAssistant.Services;
    using AiAssistant.Settings;

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid("12345678-1234-1234-1234-123456789012")]
    public sealed class AiAssistantExtension : AsyncPackage
    {
        public const string PackageGuidString = "12345678-1234-1234-1234-123456789012";

        protected override async System.Threading.Tasks.Task InitializeAsync(
            System.Threading.CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var settings = new ServerConnectionSettings(
                serverUrl: GetConfiguredServerUrl(),
                apiKey: GetConfiguredApiKey());

            var connectionService = new ServerConnectionService(settings);
            await connectionService.ConnectAsync();

            await base.InitializeAsync(cancellationToken, progress);
        }

        private static string GetConfiguredServerUrl()
        {
            // Загрузка из параметров VS или конфигурационного файла
            return "https://api.ai-assistant.local";
        }

        private static string GetConfiguredApiKey()
        {
            // Загрузка из защищённого хранилища
            return Environment.GetEnvironmentVariable("AI_ASSISTANT_API_KEY") ?? "";
        }
    }
}