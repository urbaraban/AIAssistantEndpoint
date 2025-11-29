# AI Assistant Endpoint - Расширение для Visual Studio

Расширение для подключения к собственному AI серверу с поддержкой:
- ?? Подключения к пользовательскому серверу
- ?? Кэширования результатов
- ?? Потоковой обработки ответов
- ?? Логирования операций
- ?? Управления конфигурацией

## Структура проекта

```
AIAssistantEndpoint/
??? Settings/              # Конфигурационные классы
?   ??? IServerConnectionSettings.cs
?   ??? ServerConnectionSettings.cs
??? Services/              # Основные сервисы
?   ??? IServerConnectionService.cs
?   ??? ServerConnectionService.cs
??? Caching/               # Система кэширования
?   ??? ICacheProvider.cs
?   ??? MemoryCacheProvider.cs
??? Streaming/             # Обработка потоков
?   ??? IStreamingResponse.cs
?   ??? StreamingResponse.cs
??? Logging/               # Логирование
?   ??? ILogger.cs
?   ??? DebugLogger.cs
??? Configuration/         # Управление конфигурацией
?   ??? IConfigurationManager.cs
?   ??? XmlConfigurationManager.cs
??? UI/                    # Интерфейс пользователя
?   ??? ServerSettingsDialog.xaml
?   ??? ServerSettingsDialog.xaml.cs
??? Examples/              # Примеры использования
    ??? UsageExample.cs
```

## Быстрый старт

### 1. Инициализация сервиса

```csharp
var settings = new ServerConnectionSettings(
    serverUrl: "https://your-ai-server.com",
    apiKey: "your-api-key"
);

var connectionService = new ServerConnectionService(settings);
await connectionService.ConnectAsync();
```

### 2. Отправка обычного запроса

```csharp
string response = await connectionService.SendRequestAsync(
    "Какая температура на улице?");
Console.WriteLine(response);
```

### 3. Потоковый запрос

```csharp
var streaming = await connectionService.SendStreamingRequestAsync(
    "Напиши программу на C#");

streaming.OnChunkReceived += (chunk) => 
{
    Console.Write(chunk);
};

streaming.OnCompleted += (fullResponse) =>
{
    Console.WriteLine("Завершено!");
};
```

### 4. Кэширование

Результаты автоматически кэшируются на 1 час:

```csharp
// Первый вызов - запрос к серверу
var response1 = await connectionService.SendRequestAsync("Вопрос");

// Второй вызов - получение из кэша
var response2 = await connectionService.SendRequestAsync("Вопрос");

// Очистка кэша
connectionService.ClearCache();
```

## Конфигурация

### Сохранение и загрузка настроек

```csharp
var configManager = new XmlConfigurationManager();

// Сохранение
configManager.SaveSettings(settings);

// Загрузка
var loadedSettings = configManager.LoadSettings();

// Проверка существования
if (configManager.SettingsExist())
{
    configManager.DeleteSettings();
}
```

### Диалог конфигурации

```csharp
var dialog = new ServerSettingsDialog();
if (dialog.ShowDialog() == true)
{
    var connectionService = new ServerConnectionService(dialog.Settings);
    await connectionService.ConnectAsync();
}
```

## API Сервера

Расширение ожидает следующих endpoint'ов:

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

Response:
{
  "response": "Ответ AI",
  "model": "model-name",
  "tokens_used": 100
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

Response: 200 OK (Stream)
Содержимое: текстовый поток в реальном времени
```

## Логирование

Все операции логируются через `ILogger`:

```csharp
var logger = new DebugLogger("MyApp");
logger.Info("Информационное сообщение");
logger.Warning("Предупреждение");
logger.Error("Ошибка", exception);
logger.Critical("Критическая ошибка", exception);
```

Логи выводятся в Debug Output окно Visual Studio.

## Обработка ошибок

```csharp
try
{
    await connectionService.ConnectAsync();
    var response = await connectionService.SendRequestAsync("Вопрос");
}
catch (ArgumentException ex)
{
    // Ошибка валидации настроек
    Console.WriteLine($"Ошибка валидации: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Ошибка сети
    Console.WriteLine($"Ошибка сети: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    // Соединение не установлено
    Console.WriteLine($"Соединение не установлено: {ex.Message}");
}
```

## Требования

- .NET Framework 4.7.2+
- Visual Studio 2019 или новее

## Лицензия

MIT License
