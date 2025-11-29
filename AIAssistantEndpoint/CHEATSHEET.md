# ?? ШПАРГАЛКА - Копируй и запускай

## Минимальная инициализация (2 строки)
```csharp
var service = new ServerConnectionService(new ServerConnectionSettings("https://api.example.com", "key"));
await service.ConnectAsync();
```

## Базовый запрос
```csharp
var response = await service.SendRequestAsync("Твой вопрос");
Console.WriteLine(response);
```

## Потоковый запрос
```csharp
var s = await service.SendStreamingRequestAsync("Вопрос");
s.OnChunkReceived += c => Console.Write(c);
s.OnCompleted += r => Console.WriteLine("\n? Готово");
while (!s.IsCompleted) await Task.Delay(100);
```

## С логированием
```csharp
var log = new DebugLogger("AI");
var svc = new ServerConnectionService(settings);
log.Info("Подключение...");
if (await svc.ConnectAsync()) log.Info("? Подключено");
```

## Сохранить конфигурацию
```csharp
var cfg = new JsonConfigurationManager();
cfg.SaveSettings(settings);
```

## Загрузить конфигурацию
```csharp
var cfg = new JsonConfigurationManager();
var settings = cfg.LoadSettings();
```

## Проверить соединение
```csharp
var helper = new ServerConfigurationHelper();
bool ok = await helper.TestConnectionAsync(settings);
```

## Полный пример (Copy-Paste Ready)
```csharp
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Configuration;
using AIAssistantEndpoint.Logging;

public async void TestAI()
{
    // Инициализация
    var logger = new DebugLogger("Test");
    var settings = new ServerConnectionSettings(
        "https://api.openai.com",  // или ваш сервер
        "sk-..."                    // ваш API ключ
    );
    
    var service = new ServerConnectionService(settings);
    
    try
    {
        // Подключение
        logger.Info("Подключение...");
        if (!await service.ConnectAsync())
        {
            logger.Error("Не подключилось");
            return;
        }
        
        logger.Info("? Подключено");
        
        // Запрос
        logger.Info("Отправка запроса...");
        var response = await service.SendRequestAsync("Привет!");
        logger.Info($"Ответ: {response}");
        
        // Потоковый запрос
        logger.Info("Потоковый запрос...");
        var stream = await service.SendStreamingRequestAsync(
            "Напиши стихотворение");
        
        stream.OnChunkReceived += chunk => 
            Console.Write(chunk);
        stream.OnCompleted += _ => 
            logger.Info("? Завершено");
        
        while (!stream.IsCompleted) 
            await Task.Delay(100);
        
        // Очистка
        service.ClearCache();
        service.Disconnect();
        logger.Info("? Готово");
    }
    catch (Exception ex)
    {
        logger.Error($"Ошибка: {ex.Message}", ex);
    }
}
```

## Обработка ошибок (шаблон)
```csharp
try
{
    // код
}
catch (ArgumentException) 
{ 
    // неверная конфигурация
}
catch (HttpRequestException) 
{ 
    // проблема с сетью
}
catch (InvalidOperationException) 
{ 
    // не подключено
}
catch (Exception ex) 
{ 
    // другое
}
```

## Логирование (все уровни)
```csharp
var log = new DebugLogger("MyComponent");
log.Debug("Отладка");
log.Info("Информация");
log.Warning("Предупреждение");
log.Error("Ошибка", exception);
log.Critical("Критическая ошибка", exception);
```

## Кэш
```csharp
var cache = new MemoryCacheProvider();

// Первый вызов - на сервер
var r1 = await service.SendRequestAsync("Q");

// Второй вызов - из кэша  
var r2 = await service.SendRequestAsync("Q");

// Очистка кэша
service.ClearCache();
```

## Конфигурация
```csharp
// Сохранить
var cfg = new JsonConfigurationManager();
cfg.SaveSettings(settings);

// Загрузить
var s = cfg.SettingsExist() ? cfg.LoadSettings() : defaults;

// Удалить
cfg.DeleteSettings();
```

## Проверка соединения
```csharp
var h = new ServerConfigurationHelper();
var ok = await h.TestConnectionAsync(settings);
```

## Command Handler интеграция
```csharp
public class MyCommand : BaseCommand
{
    private ServerConnectionService _service;

    public MyCommand()
    {
        var s = new ServerConnectionSettings("url", "key");
        _service = new ServerConnectionService(s);
    }

    protected override async void Execute(object sender, EventArgs e)
    {
        if (await _service.ConnectAsync())
        {
            var r = await _service.SendRequestAsync("Помощь");
            System.Windows.Forms.MessageBox.Show(r);
        }
    }
}
```

## Все using'и для копирования
```csharp
using AIAssistantEndpoint.Settings;
using AIAssistantEndpoint.Services;
using AIAssistantEndpoint.Caching;
using AIAssistantEndpoint.Streaming;
using AIAssistantEndpoint.Logging;
using AIAssistantEndpoint.Configuration;
using System.Threading.Tasks;
using System;
```

## Настройка параметров
```csharp
var settings = new ServerConnectionSettings("url", "key")
{
    UseSSL = true,              // HTTPS
    TimeoutMs = 60000           // 60 секунд
};
```

## JSON конфигурация (что сохраняется)
```json
{
  "serverUrl": "https://api.example.com",
  "apiKey": "your-secret-key",
  "timeoutMs": 30000,
  "useSSL": true
}
```

---

**Все готово! Копируй, вставляй, запускай! ??**
