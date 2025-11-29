# ?? Как запустить и использовать AI Assistant Extension

## Запуск расширения в режиме отладки

### 1. Сборка проекта
```bash
Visual Studio ? Build ? Build Solution (Ctrl + Shift + B)
```

### 2. Запуск отладки
```bash
Visual Studio ? Debug ? Start Debugging (F5)
```

Это откроет экспериментальный экземпляр Visual Studio с установленным расширением.

## Использование расширения

### Первый запуск

1. **Откройте меню Tools ? AI Assistant**
   - Там вы найдёте две команды:
     - `Open AI Chat` - открыть окно чата
     - `Settings` - открыть окно настроек

2. **Настройте подключение**
   - Выберите `Tools ? AI Assistant ? Settings`
   - Введите:
     - **URL сервера**: адрес вашего AI сервера (например, `http://localhost:8000`)
     - **API ключ**: ваш API ключ для доступа
     - **Таймаут**: время ожидания ответа в мс (по умолчанию 30000)
     - **SSL/TLS**: включить защищённое соединение (HTTPS)
   - Нажмите `Проверить соединение` для проверки
   - Нажмите `Сохранить`

3. **Откройте чат**
   - Выберите `Tools ? AI Assistant ? Open AI Chat`
   - Откроется окно чата внизу VS

### Работа с чатом

1. **Отправка сообщения**
   - Напишите вопрос в текстовое поле
   - Нажмите `Отправить` или `Enter`
   - Ответ AI появится в чате

2. **Просмотр истории**
   - Все сообщения сохраняются в окне чата
   - Цвет сообщений:
     - **Синий**: ваши сообщения
     - **Зелёный**: ответы AI
     - **Серый**: системные сообщения

3. **Управление**
   - ?? (Настройки) - открыть окно настроек
   - ??? (Корзина) - очистить историю чата

## Локальное хранилище настроек

Настройки автоматически сохраняются в:
```
C:\Users\<ИМЯ_ПОЛЬЗОВАТЕЛЯ>\AppData\Roaming\AIAssistant\settings.json
```

## Через Visual Studio Options

Вы также можете настроить параметры в:
```
Tools ? Options ? AI Assistant ? General
```

Там доступны все те же параметры, что и в окне Settings.

## Устранение неполадок

### Расширение не загружается

1. Проверьте логи в выходном окне (`View ? Output`)
2. Убедитесь, что решение собирается без ошибок
3. Очистите экспериментальный реестр VS:
   ```
   Tools ? Import and Export Settings ? Reset all settings ? Next ? Visual C#
   ```

### Окно чата не открывается

1. Проверьте консоль отладки на наличие ошибок
2. Убедитесь, что сервер доступен
3. Проверьте URL и API ключ в настройках

### Сервер недоступен

1. Нажмите кнопку `Проверить соединение` в окне Settings
2. Убедитесь, что сервер запущен и доступен по указанному адресу
3. Проверьте ваш API ключ

## Разработка

### Структура проекта

```
AIAssistantEndpoint/
??? UI/
?   ??? AIChatControl.xaml          # UI контрол чата
?   ??? AIChatControl.xaml.cs       # Логика чата
?   ??? AIChatToolWindow.cs         # Tool window VS
?   ??? ServerSettingsWindow.xaml   # UI окна настроек
?   ??? ServerSettingsWindow.xaml.cs # Логика настроек
??? Services/
?   ??? IServerConnectionService.cs
?   ??? ServerConnectionService.cs   # Подключение к серверу
??? Configuration/
?   ??? IConfigurationManager.cs
?   ??? JsonConfigurationManager.cs  # Управление настройками
?   ??? XmlConfigurationManager.cs
??? Settings/
?   ??? IServerConnectionSettings.cs
?   ??? ServerConnectionSettings.cs  # Модель настроек
??? Logging/
?   ??? ILogger.cs
?   ??? DebugLogger.cs              # Система логирования
??? Commands/
?   ??? Commands.cs                 # Команды меню
??? AIAssistantEndpointPackage.cs   # Пакет VS
??? Menus.ctmenu                    # Определение меню

```

### Добавление новых команд

Чтобы добавить новую команду в меню:

1. Добавьте класс команды в `Commands/Commands.cs`
2. Добавьте запись в `Menus.ctmenu`
3. Зарегистрируйте команду в `AIAssistantEndpointPackage.cs`

## Поддержка

Если у вас есть вопросы или проблемы, откройте issue на GitHub:
https://github.com/urbaraban/AIAssistantEndpoint
