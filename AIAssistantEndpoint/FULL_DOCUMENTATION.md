# AI Assistant Extension - Полная документация

## Описание
Расширение для Visual Studio, предоставляющее возможность подключения к собственному AI серверу с поддержкой:
- ?? Подключения к пользовательскому серверу
- ?? Кэширования результатов (1 час)
- ?? Потоковой обработки ответов
- ?? Логирования операций
- ?? Управления конфигурацией (JSON формат)
- ??? Валидации параметров подключения

## Архитектура проекта

```
AIAssistantEndpoint/
??? Settings/                    # Конфигурационные классы
?   ??? IServerConnectionSettings.cs
?   ??? ServerConnectionSettings.cs
??? Services/                    # Основные сервисы
?   ??? IServerConnectionService.cs
?   ??? ServerConnectionService.cs
??? Caching/                     # Система кэширования
?   ??? ICacheProvider.cs
?   ??? MemoryCacheProvider.cs
??? Streaming/                   # Обработка потоков
?   ??? IStreamingResponse.cs
?   ??? StreamingResponse.cs
??? Logging/                     # Логирование
?   ??? ILogger.cs
?   ??? DebugLogger.cs
??? Configuration/               # Управление конфигурацией
?   ??? IConfigurationManager.cs
?   ??? JsonConfigurationManager.cs (переименованный XmlConfigurationManager)
?   ??? ServerConfigurationHelper.cs
??? Examples/                    # Примеры использования
?   ??? RunExampleAsync.cs
??? Properties/
    ??? AssemblyInfo.cs
```

## Основные компоненты

### 1. Параметры подключения (Settings)

```csharp
var settings = new ServerConnectionSettings(
    serverUrl: "https://your-api-server.com",
    apiKey: "your-secret-key"
);

// Опциональные параметры
settings.UseSSL = true;          // Использовать SSL
settings.TimeoutMs = 30000;      // Таймаут 30 секунд
```

### 2. Сервис подключения (Services)

```csharp
var service = new ServerConnectionService(settings);

// Подключение к серверу
bool connected = await service.ConnectAsync();

// Обычный запрос (автоматически кэшируется)
string response = await service.SendRequestAsync("Ваш запрос");

// Потоковый запрос для больших ответов
var streaming = await service.SendStreamingRequestAsync("Ваш запрос");
streaming.OnChunkReceived += chunk => Console.Write(chunk);
streaming.OnCompleted += fullResponse => Console.WriteLine("Готово");
streaming.OnError += error => Console.WriteLine($"Ошибка: {error.Message}");

// Очистка кэша
service.ClearCache();

// Отключение
service.Disconnect();
```

### 3. Кэширование (Caching)

Автоматическое кэширование результатов на 1 час:

```csharp
// Первый вызов - запрос к серверу
var response1 = await service.SendRequestAsync("Вопрос");

// Второй вызов - из кэша (молниеносно)
var response2 = await service.SendRequestAsync("Вопрос");

// Очистка кэша
service.ClearCache();

// Или использовать пользовательский кэш
var customCache = new MemoryCacheProvider();
var service2 = new ServerConnectionService(settings, customCache);
```

### 4. Управление конфигурацией

```csharp
var configManager = new JsonConfigurationManager();

// Сохранение настроек
configManager.SaveSettings(settings);

// Загрузка настроек
var loadedSettings = configManager.LoadSettings();

// Проверка существования
if (configManager.SettingsExist())
{
    // Используем загруженные настройки
}

// Удаление настроек
configManager.DeleteSettings();
```

### 5. Помощник конфигурации

```csharp
var helper = new ServerConfigurationHelper();

// Создание и валидация параметров
try
{
    var settings = helper.CreateSettings(
        serverUrl: "https://api.example.com",
        apiKey: "key123",
        timeoutMs: 60000,
        useSSL: true
    );

    // Проверка соединения
    bool isConnected = await helper.TestConnectionAsync(settings);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Ошибка валидации: {ex.Message}");
}
```

### 6. Логирование

```csharp
var logger = new DebugLogger("MyComponent");

logger.Debug("Отладочная информация");
logger.Info("Информационное сообщение");
logger.Warning("Предупреждение");
logger.Error("Ошибка", exception);
logger.Critical("Критическая ошибка", exception);

// Логи выводятся в окно Debug Output Visual Studio
```

## Обработка потоков

```csharp
var streaming = await service.SendStreamingRequestAsync("Напиши стихотворение");

// Подписываемся на события
streaming.OnChunkReceived += chunk =>
{
    Console.Write(chunk);  // Выводим каждый кусок по мере получения
};

streaming.OnCompleted += fullResponse =>
{
    Console.WriteLine($"\nИтого получено {fullResponse.Length} символов");
};

streaming.OnError += error =>
{
    Console.WriteLine($"Ошибка: {error.Message}");
};

// Проверка состояния
while (!streaming.IsCompleted && !streaming.IsError)
{
    await Task.Delay(100);
}

// Получение полного ответа
string fullText = streaming.FullResponse;
```

## Требования к API сервера

Расширение ожидает следующие endpoint'ы:

### Проверка здоровья
```
GET /health
Response: 200 OK
```

### Обычный запрос
```
POST /api/assistant
Content-Type: application/json

{
  "prompt": "Ваш вопрос"
}

Response: 200 OK
Content-Type: application/json
{
  "response": "Ответ от AI",
  "model": "model-name",
  "tokens_used": 150
}
```

### Потоковый запрос
```
POST /api/assistant/stream
Content-Type: application/json

{
  "prompt": "Ваш вопрос",
  "stream": true
}

Response: 200 OK
Content-Type: text/event-stream

Текстовый поток в реальном времени...
```

## Обработка ошибок

```csharp
try
{
    await service.ConnectAsync();
    var response = await service.SendRequestAsync("Вопрос");
}
catch (ArgumentException ex)
{
    // Ошибка валидации (неверный URL, пустой API ключ)
    Console.WriteLine($"Ошибка валидации: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Ошибка сети или сервер недоступен
    Console.WriteLine($"Ошибка сети: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Соединение не установлено
    Console.WriteLine($"Соединение не установлено");
}
catch (Exception ex)
{
    // Другие ошибки
    Console.WriteLine($"Неожиданная ошибка: {ex.Message}");
}
```

## Пример полного использования

```csharp
public class AIAssistantDemo
{
    public async Task RunDemoAsync()
    {
        var logger = new DebugLogger("Demo");
        logger.Info("Инициализация AI Assistant...");

        // 1. Загрузка или создание конфигурации
        var configManager = new JsonConfigurationManager();
        var settings = configManager.SettingsExist() 
            ? configManager.LoadSettings() 
            : new ServerConnectionSettings("https://api.example.com", "your-key");

        // 2. Инициализация сервиса
        var cacheProvider = new MemoryCacheProvider();
        var service = new ServerConnectionService(settings, cacheProvider);

        // 3. Подключение
        if (!await service.ConnectAsync())
        {
            logger.Error("Не удалось подключиться");
            return;
        }

        logger.Info("Подключено успешно");

        // 4. Запрос с кэшированием
        string response = await service.SendRequestAsync(
            "Какие возможности имеет это расширение?");
        logger.Info($"Ответ: {response}");

        // 5. Потоковый запрос
        var stream = await service.SendStreamingRequestAsync(
            "Опиши подробно процесс работы расширения");
        
        stream.OnChunkReceived += chunk => Console.Write(chunk);
        stream.OnCompleted += _ => logger.Info("Поток завершён");
        
        while (!stream.IsCompleted && !stream.IsError)
        {
            await Task.Delay(100);
        }

        // 6. Сохранение конфигурации
        configManager.SaveSettings(settings);

        // 7. Очистка
        service.ClearCache();
        service.Disconnect();

        logger.Info("Работа завершена");
    }
}
```

## Форматы хранения конфигурации

### JSON файл (по умолчанию)
```json
{
  "serverUrl": "https://api.example.com",
  "apiKey": "your-secret-key",
  "timeoutMs": 30000,
  "useSSL": true
}
```

## Требования и зависимости

- **.NET Framework 4.7.2** или выше
- Visual Studio 2019 или новее
- HttpClient (встроен в .NET Framework 4.7.2)
- Встроенный Threading (System.Threading.Tasks)

## Лицензия

MIT License

## Changelog

### v1.0.0
- Первый стабильный релиз
- Поддержка обычных и потоковых запросов
- Кэширование результатов (1 час)
- JSON конфигурация
- Логирование операций
- Валидация параметров

## Контакты и поддержка

Для вопросов и предложений используйте Issues в репозитории проекта.
