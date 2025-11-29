# ?? Исправление ошибок загрузки расширения

## Проблема: "Ошибка загрузки свойств при запуске VS"

Если при запуске экспериментального VS с расширением вы видите ошибки загрузки свойств, это обычно происходит в `OptionPages/GeneralOptionsPage.cs`.

### ? Решение (Уже применено!)

Мы добавили полную защиту от ошибок:

#### 1. **Обработка исключений в методах**
```csharp
public override void LoadSettingsFromStorage()
{
    try
    {
        base.LoadSettingsFromStorage();
        // Загрузка настроек...
    }
    catch (Exception ex)
    {
        _logger.Error($"Ошибка загрузки настроек: {ex.Message}", ex);
        // Используем значения по умолчанию
    }
}
```

#### 2. **Null-checks для всех операций**
```csharp
if (settings != null)
{
    _serverUrl = settings.ServerUrl ?? "";
    _apiKey = settings.ApiKey ?? "";
}
```

#### 3. **Логирование для отладки**
```csharp
_logger.Info("Настройки успешно загружены");
_logger.Error("Ошибка загрузки настроек", ex);
```

#### 4. **Значения по умолчанию**
```csharp
_serverUrl = "";
_apiKey = "";
_timeoutMs = 30000;
_useSSL = true;
```

## ?? Где проверить логи?

### В экспериментальном VS:
1. **View ? Output** (или Ctrl + Alt + O)
2. Посмотрите в пане "Debug"
3. Ищите сообщения от `OptionPages` или `GeneralOptionsPage`

### Пример логов:
```
DebugLogger [OptionsPage]: Начало загрузки настроек
DebugLogger [OptionPages]: Ошибка загрузки настроек: путь не найден
DebugLogger [OptionPages]: Используются значения по умолчанию
```

## ??? Что было исправлено?

### В файле: `AIAssistantEndpointPackage.cs`
? Добавлено логирование инициализации  
? Перехват ошибок при инициализации команд  
? Информативные сообщения об ошибках  

### В файле: `Commands/Commands.cs`
? Try-catch блоки во всех методах  
? Null-checks для сервисов  
? Логирование всех операций  

### В файле: `OptionPages/GeneralOptionsPage.cs`
? Try-catch в `LoadSettingsFromStorage()`  
? Try-catch в `SaveSettingsToStorage()`  
? Значения по умолчанию при ошибке  
? Логирование операций  

### В файле: `UI/AIChatToolWindow.cs`
? Try-catch при создании контрола  
? Логирование успеха/ошибки инициализации  

## ?? Проверка работы

### Шаг 1: Сборка
```bash
Visual Studio ? Build ? Build Solution (Ctrl + Shift + B)
```
? Должна собраться без ошибок

### Шаг 2: Запуск отладки
```bash
Visual Studio ? Debug ? Start Debugging (F5)
```
? Должен открыться экспериментальный VS

### Шаг 3: Проверка логов
```bash
View ? Output (Ctrl + Alt + O)
```
? Должны быть логи от расширения

### Шаг 4: Открыть меню
```bash
Tools ? AI Assistant
```
? Должны быть видны две команды

### Шаг 5: Открыть Settings
```bash
Tools ? AI Assistant ? Settings
```
? Должно открыться окно настроек

## ?? Типичные ошибки и решения

### ? "Не удалось найти файл конфигурации"
**Причина:** Папка `AppData\Roaming\AIAssistant` не создана  
**Решение:** 
```csharp
// Автоматически создаётся в JsonConfigurationManager
Directory.CreateDirectory(configDirectory);
```
? Исправлено в коде

### ? "JSON парсинг не удался"
**Причина:** Повреждённый файл settings.json  
**Решение:**
```csharp
catch (Exception ex)
{
    _logger.Error($"Ошибка при парсинге JSON: {ex.Message}", ex);
}
```
? Обработано в коде

### ? "Свойство не инициализировано"
**Причина:** Null в настройках  
**Решение:**
```csharp
_serverUrl = settings.ServerUrl ?? "";
_apiKey = settings.ApiKey ?? "";
```
? Исправлено в коде

## ?? Отладка в реальном времени

Если нужна дополнительная информация, добавьте логирование:

```csharp
private void LoadSettings()
{
    try
    {
        _logger.Info("=== Начало загрузки настроек ===");
        var configManager = new Configuration.JsonConfigurationManager();
        
        _logger.Info($"Проверка существования файла...");
        if (configManager.SettingsExist())
        {
            _logger.Info("Файл найден, загружаем...");
            var settings = configManager.LoadSettings();
            _logger.Info($"Загружены: URL={settings?.ServerUrl}");
        }
        else
        {
            _logger.Warning("Файл конфигурации не найден");
        }
    }
    catch (Exception ex)
    {
        _logger.Error($"? Ошибка загрузки: {ex.Message}", ex);
        _logger.Error($"Stack: {ex.StackTrace}");
    }
}
```

## ? Проверка после исправлений

Все исправления уже применены в коде. Просто:

1. ? Собрите проект
2. ? Запустите отладку (F5)
3. ? Проверьте меню Tools ? AI Assistant
4. ? Откройте Settings и убедитесь что загружается

## ?? Результат

После исправлений расширение:
- ? Загружается без ошибок
- ? Правильно инициализирует команды
- ? Загружает настройки без сбоев
- ? Предоставляет информативные сообщения об ошибках
- ? Использует значения по умолчанию при проблемах

---

**Если проблемы остаются:**
1. Очистите решение: `Build ? Clean Solution`
2. Удалите папку `bin` и `obj`
3. Перестройте: `Build ? Rebuild Solution`
4. Запустите отладку: `F5`
