# Краткое руководство по интеграции в Visual Studio

## Быстрый старт

### Шаг 1: Установка и инициализация

```csharp
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Logging;

// Создание логгера
var logger = new DebugLogger("MyExtension");

// Создание конфигурации
var settings = new ServerConnectionSettings(
    serverUrl: "https://your-ai-api.com",
    apiKey: "your-api-key"
);

// Инициализация сервиса
var service = new ServerConnectionService(settings);
logger.Info("Сервис инициализирован");
```

### Шаг 2: Подключение к серверу

```csharp
try
{
    bool connected = await service.ConnectAsync();
    if (connected)
    {
        logger.Info("? Соединение успешно установлено");
    }
    else
    {
        logger.Error("? Не удалось подключиться к серверу");
    }
}
catch (Exception ex)
{
    logger.Error($"? Ошибка подключения: {ex.Message}", ex);
}
```

### Шаг 3: Отправка запроса

```csharp
try
{
    string prompt = "Помоги мне написать код";
    string response = await service.SendRequestAsync(prompt);
    logger.Info($"Ответ: {response}");
}
catch (Exception ex)
{
    logger.Error($"Ошибка запроса: {ex.Message}", ex);
}
```

### Шаг 4: Потоковая обработка (опционально)

```csharp
try
{
    var streaming = await service.SendStreamingRequestAsync(
        "Напиши программу на C#");

    streaming.OnChunkReceived += chunk =>
    {
        logger.Debug($"Получен кусок: {chunk}");
    };

    streaming.OnCompleted += fullResponse =>
    {
        logger.Info("Поток завершён");
    };

    streaming.OnError += error =>
    {
        logger.Error($"Ошибка потока: {error.Message}", error);
    };
}
catch (Exception ex)
{
    logger.Error($"Ошибка потокового запроса: {ex.Message}", ex);
}
```

## Использование конфигурационного файла

```csharp
// Сохранение конфигурации
var configManager = new JsonConfigurationManager();
configManager.SaveSettings(settings);
logger.Info("Конфигурация сохранена");

// Загрузка конфигурации при следующем запуске
var configManager = new JsonConfigurationManager();
if (configManager.SettingsExist())
{
    var loadedSettings = configManager.LoadSettings();
    var service = new ServerConnectionService(loadedSettings);
}
```

## Проверка соединения перед использованием

```csharp
var helper = new ServerConfigurationHelper();

try
{
    bool isConnected = await helper.TestConnectionAsync(settings);
    if (isConnected)
    {
        logger.Info("? Соединение проверено");
    }
    else
    {
        logger.Warning("? Сервер недоступен");
    }
}
catch (Exception ex)
{
    logger.Error($"Ошибка проверки: {ex.Message}", ex);
}
```

## Обработка и отображение ошибок

```csharp
try
{
    // Ваш код здесь
}
catch (ArgumentException ex)
{
    logger.Warning($"Ошибка конфигурации: {ex.Message}");
    // Показать пользователю диалог с ошибкой
}
catch (HttpRequestException ex)
{
    logger.Error($"Ошибка сети: {ex.Message}");
    // Показать пользователю сообщение об ошибке сети
}
catch (InvalidOperationException ex)
{
    logger.Error($"Соединение не установлено: {ex.Message}");
    // Предложить пользователю переподключиться
}
catch (Exception ex)
{
    logger.Error($"Неожиданная ошибка: {ex.Message}", ex);
    // Показать общую ошибку
}
```

## Интеграция в Command Handler

```csharp
public class MyCommandHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ServerConnectionService _aiService;
    private readonly ILogger _logger;

    public MyCommandHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _logger = new DebugLogger("MyCommand");
        
        // Инициализация AI сервиса
        var settings = new ServerConnectionSettings(
            "https://api.example.com",
            "api-key"
        );
        _aiService = new ServerConnectionService(settings);
    }

    public async void Execute(object sender, EventArgs e)
    {
        try
        {
            if (!await _aiService.ConnectAsync())
            {
                _logger.Error("Не удалось подключиться к AI");
                return;
            }

            string response = await _aiService.SendRequestAsync(
                "Помоги мне с кодом");
            
            _logger.Info($"AI ответил: {response}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Ошибка команды: {ex.Message}", ex);
        }
    }
}
```

## Лучшие практики

1. **Всегда проверяйте соединение** перед отправкой запроса
```csharp
if (!service.IsConnected)
{
    if (!await service.ConnectAsync())
    {
        // Обработать ошибку
    }
}
```

2. **Используйте потоковые запросы** для больших ответов
```csharp
// Хорошо для больших ответов
var streaming = await service.SendStreamingRequestAsync(prompt);

// Хорошо для маленьких ответов
var response = await service.SendRequestAsync(prompt);
```

3. **Кэшируйте часто используемые запросы**
```csharp
// Первый вызов: ~1-5 сек
var response1 = await service.SendRequestAsync("Общий вопрос");

// Повторный вызов: ~1 мс
var response2 = await service.SendRequestAsync("Общий вопрос");
```

4. **Очищайте кэш при изменении конфигурации**
```csharp
settings.ApiKey = newKey;
service.ClearCache();  // Очистить старый кэш
```

5. **Всегда закрывайте соединение**
```csharp
try
{
    // Работа с AI
}
finally
{
    service.Disconnect();
}
```

## Тестирование

```csharp
[TestClass]
public class AIServiceTests
{
    private ServerConnectionService _service;

    [TestInitialize]
    public void Setup()
    {
        var settings = new ServerConnectionSettings(
            "https://localhost:5000",
            "test-key"
        );
        _service = new ServerConnectionService(settings);
    }

    [TestMethod]
    public async Task TestConnection()
    {
        bool result = await _service.ConnectAsync();
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task TestRequest()
    {
        await _service.ConnectAsync();
        var response = await _service.SendRequestAsync("Test");
        Assert.IsFalse(string.IsNullOrEmpty(response));
    }

    [TestCleanup]
    public void Cleanup()
    {
        _service.Disconnect();
    }
}
```

## Отладка

Для отладки включите детальное логирование:

```csharp
var logger = new DebugLogger("AIService");

// Логи будут выводиться в Debug Output окно
// Вид -> Выходные данные -> Debug
```

## Типичные проблемы и решения

### Проблема: "Не удалось найти тип или имя пространства имен"
**Решение**: Убедитесь, что все using директивы присутствуют

```csharp
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Logging;
```

### Проблема: Timeout при подключении
**Решение**: Увеличьте TimeoutMs или проверьте доступность сервера

```csharp
settings.TimeoutMs = 60000;  // 60 секунд вместо 30
```

### Проблема: Сервер возвращает 401 Unauthorized
**Решение**: Проверьте API ключ

```csharp
var settings = new ServerConnectionSettings(
    "https://api.example.com",
    "correct-api-key"  // Проверьте здесь
);
```

### Проблема: HTTPS сертификат не доверен
**Решение**: Используйте параметр UseSSL

```csharp
settings.UseSSL = false;  // Для разработки только
// или
settings.ServerUrl = "http://localhost:5000";  // HTTP вместо HTTPS
```
