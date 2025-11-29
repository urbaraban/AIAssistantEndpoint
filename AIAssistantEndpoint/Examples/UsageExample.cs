namespace AIAssistantEndpoint.Examples
{
    using System;
    using System.Threading.Tasks;
    using AIAssistantEndpoint.Caching;
    using AIAssistantEndpoint.Configuration;
    using AIAssistantEndpoint.Logging;
    using AIAssistantEndpoint.Services;
    using AIAssistantEndpoint.Settings;

    /// <summary>
    /// Пример использования AI Assistant расширения
    /// </summary>
    public class RunExampleAsync
    {
        public async Task ExecuteAsync()
        {
            // Инициализация логирования
            var logger = new DebugLogger("AIAssistant");

            // Загрузка конфигурации
            var configManager = new JsonConfigurationManager();
            ServerConnectionSettings settings = configManager.SettingsExist() 
                ? configManager.LoadSettings() 
                : new ServerConnectionSettings("https://api.example.com", "your-api-key");

            // Инициализация сервиса подключения с кэшем
            var cacheProvider = new MemoryCacheProvider();
            var connectionService = new ServerConnectionService(settings, cacheProvider);

            logger.Info("=== Пример использования AI Assistant ===");

            // 1. Подключение к серверу
            logger.Info("Подключение к серверу...");
            bool connected = await connectionService.ConnectAsync();
            if (!connected)
            {
                logger.Error("Не удалось подключиться к серверу");
                return;
            }

            // 2. Обычный запрос (с кэшированием)
            try
            {
                logger.Info("Отправка обычного запроса...");
                string response = await connectionService.SendRequestAsync(
                    "Что такое искусственный интеллект?");
                logger.Info($"Ответ: {response}");
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка при отправке запроса: {ex.Message}", ex);
            }

            // 3. Потоковый запрос (для больших ответов)
            try
            {
                logger.Info("Отправка потокового запроса...");
                var streamingResponse = await connectionService.SendStreamingRequestAsync(
                    "Напиши мне стихотворение");

                // Подписываемся на события потока
                streamingResponse.OnChunkReceived += (chunk) =>
                {
                    logger.Debug($"Получен кусок: {chunk}");
                };

                streamingResponse.OnCompleted += (fullResponse) =>
                {
                    logger.Info($"Потоковый ответ завершён. Длина: {fullResponse.Length}");
                };

                streamingResponse.OnError += (error) =>
                {
                    logger.Error($"Ошибка потока: {error.Message}", error);
                };

                // Ожидание завершения потока
                while (!streamingResponse.IsCompleted && !streamingResponse.IsError)
                {
                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка при потоковом запросе: {ex.Message}", ex);
            }

            // 4. Повторный запрос (будет получен из кэша)
            try
            {
                logger.Info("Повторная отправка того же запроса (из кэша)...");
                string cachedResponse = await connectionService.SendRequestAsync(
                    "Что такое искусственный интеллект?");
                logger.Info($"Кэшированный ответ: {cachedResponse}");
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка: {ex.Message}", ex);
            }

            // 5. Очистка кэша
            logger.Info("Очистка кэша...");
            connectionService.ClearCache();

            // 6. Отключение
            logger.Info("Отключение от сервера...");
            connectionService.Disconnect();

            logger.Info("=== Пример завершён ===");
        }
    }
}
