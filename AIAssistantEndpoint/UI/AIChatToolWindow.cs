namespace AIAssistantEndpoint.UI
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using AIAssistantEndpoint.Logging;

    /// <summary>
    /// Окно чата AI Assistant для Visual Studio
    /// </summary>
    [Guid("b3f1f1f1-b3f1-b3f1-b3f1-b3f1b3f1b3f1")]
    public class AIChatToolWindow : ToolWindowPane
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Инициализация окна чата
        /// </summary>
        public AIChatToolWindow() : base(null)
        {
            _logger = new DebugLogger("AIChatToolWindow");
            
            try
            {
                this.Caption = "AI Assistant";
                
                // Создание контента окна
                var chatControl = new AIChatControl();
                this.Content = chatControl;
                
                _logger.Info("AIChatToolWindow успешно инициализирован");
            }
            catch (Exception ex)
            {
                _logger.Error($"Ошибка инициализации AIChatToolWindow: {ex.Message}", ex);
                throw;
            }
        }
    }
}
