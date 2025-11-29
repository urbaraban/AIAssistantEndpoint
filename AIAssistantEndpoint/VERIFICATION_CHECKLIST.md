# ? Финальный чек-лист

## ?? Проверка перед запуском

### Шаг 1: Сборка проекта
- [ ] Откройте `AIAssistantEndpoint.sln`
- [ ] Нажмите `Ctrl + Shift + B` (Build Solution)
- [ ] Дождитесь "Build succeeded" в Output
- [ ] Нет ошибок: `Error CS...`

### Шаг 2: Состояние файлов
Проверьте что все файлы на месте:
- [ ] `AIAssistantEndpointPackage.cs` - содержит try-catch блоки
- [ ] `Commands/Commands.cs` - содержит логирование
- [ ] `OptionPages/GeneralOptionsPage.cs` - содержит обработку ошибок
- [ ] `UI/AIChatToolWindow.cs` - содержит try-catch
- [ ] `Menus.ctmenu` - упрощенный файл

### Шаг 3: Документация
Проверьте что созданы файлы:
- [ ] `QUICK_START_EXTENSION.md`
- [ ] `EXTENSION_GUIDE.md`
- [ ] `VISUAL_GUIDE.md`
- [ ] `TROUBLESHOOTING.md`
- [ ] `CHANGES_SUMMARY.md`

## ?? Запуск расширения

### Шаг 4: Отладка
- [ ] Нажмите `F5` или `Debug ? Start Debugging`
- [ ] Откроется экспериментальный Visual Studio
- [ ] Дождитесь полной загрузки (может быть медленнее обычного VS)

### Шаг 5: Проверка меню
- [ ] Нажмите на меню `Tools`
- [ ] Найдите `AI Assistant`
- [ ] Посмотрите на подменю:
  - [ ] `Open AI Chat` - должна быть
  - [ ] `Settings` - должна быть

### Шаг 6: Открыть Settings
- [ ] Нажмите `Tools ? AI Assistant ? Settings`
- [ ] Откроется окно конфигурации
- [ ] Проверьте поля:
  - [ ] `URL сервера` - текстовое поле
  - [ ] `API ключ` - защищённое поле (PasswordBox)
  - [ ] `SSL/TLS` - чекбокс (должен быть отмечен)
  - [ ] `Таймаут` - текстовое поле (по умолчанию 30000)
  - [ ] `Проверить соединение` - кнопка
  - [ ] `Сохранить` и `Отмена` - кнопки

### Шаг 7: Логирование
- [ ] Нажмите `View ? Output` (Ctrl + Alt + O)
- [ ] Выберите пану "Debug"
- [ ] Ищите логи от расширения:
  - [ ] "AIAssistantEndpointPackage инициализирован"
  - [ ] "Начало инициализации команд"
  - [ ] "ShowAIChatCommand инициализирована"
  - [ ] "ShowSettingsCommand инициализирована"
  - [ ] "Инициализация пакета завершена"

### Шаг 8: Тестирование Settings
- [ ] В окне Settings введите:
  - [ ] URL: `http://localhost:8000`
  - [ ] API Key: `test-key`
- [ ] Нажмите `Проверить соединение`
- [ ] Результат (может быть ошибка, это нормально):
  - [ ] Если сервер доступен: "? Соединение успешно!"
  - [ ] Если сервер недоступен: "? Ошибка подключения"
  - [ ] Это нормально - сервер может быть выключен

### Шаг 9: Сохранение настроек
- [ ] Нажмите кнопку `Сохранить`
- [ ] Должно показать "? Настройки сохранены"
- [ ] Окно закроется через 1.5 секунды
- [ ] Проверьте файл:
  ```
  C:\Users\<YourUsername>\AppData\Roaming\AIAssistant\settings.json
  ```
  - [ ] Файл должен существовать
  - [ ] Должен содержать ваши настройки

### Шаг 10: Открыть чат
- [ ] Нажмите `Tools ? AI Assistant ? Open AI Chat`
- [ ] Внизу VS должно появиться окно с заголовком "?? AI Assistant"
- [ ] Проверьте элементы:
  - [ ] Текстовое поле ввода
  - [ ] Кнопка "Отправить"
  - [ ] Кнопки ?? и ???
  - [ ] История сообщений (пустая изначально)
  - [ ] Статус бар внизу

## ?? Проверка логирования

### В Output ? Debug должны быть:
```
[?] DebugLogger [AIAssistantEndpointPackage]: AIAssistantEndpointPackage инициализирован
[?] DebugLogger [AIAssistantEndpointPackage]: Начало инициализации команд
[?] DebugLogger [ShowAIChatCommand]: ShowAIChatCommand инициализирована
[?] DebugLogger [ShowSettingsCommand]: ShowSettingsCommand инициализирована
[?] DebugLogger [AIAssistantEndpointPackage]: Инициализация пакета завершена
[?] DebugLogger [OptionsPage]: Настройки успешно загружены
```

## ?? Если что-то не работает

### Расширение не загружается
- [ ] Проверьте `Output ? Debug` на ошибки
- [ ] Выполните `Build ? Clean Solution`
- [ ] Выполните `Build ? Rebuild Solution`
- [ ] Запустите `F5` заново

### Меню не видно
- [ ] Проверьте что `Menus.ctmenu` имеет правильный синтаксис
- [ ] Проверьте `AIAssistantEndpointPackage.cs`:
  ```csharp
  [ProvideMenuResource("Menus.ctmenu", 1)]
  ```
  должно быть добавлено

### Options Page не загружается
- [ ] Проверьте `OptionPages/GeneralOptionsPage.cs`
- [ ] Должны быть try-catch блоки в методах
- [ ] Проверьте логирование в Output

### Окно Settings не открывается
- [ ] Убедитесь что `ServerSettingsWindow` - это `Window`, а не `UserControl`
- [ ] Проверьте в XAML:
  ```xaml
  <Window x:Class="AIAssistantEndpoint.UI.ServerSettingsWindow"
  ```

## ?? Финальная проверка

### Функциональность:
- [ ] ? Расширение загружается без ошибок
- [ ] ? Меню `Tools ? AI Assistant` видно
- [ ] ? Команды регистрируются корректно
- [ ] ? Options Page загружается
- [ ] ? Settings окно открывается
- [ ] ? Чат окно открывается
- [ ] ? Логирование работает

### Код:
- [ ] ? Нет компилятивных ошибок
- [ ] ? Все try-catch блоки на месте
- [ ] ? Логирование подключено
- [ ] ? Null-checks везде где нужно

### Документация:
- [ ] ? `QUICK_START_EXTENSION.md` создан
- [ ] ? `EXTENSION_GUIDE.md` создан
- [ ] ? `VISUAL_GUIDE.md` создан
- [ ] ? `TROUBLESHOOTING.md` создан
- [ ] ? `CHANGES_SUMMARY.md` создан

## ?? Готово!

Если все пункты отмечены ?, расширение готово к использованию!

---

## ?? Что делать дальше?

1. **Подготовьте AI сервер**
   - Убедитесь что ваш AI сервер запущен
   - Используйте правильный URL и API ключ

2. **Настройте расширение**
   - Откройте `Tools ? AI Assistant ? Settings`
   - Введите параметры вашего сервера

3. **Начните использовать**
   - Откройте `Tools ? AI Assistant ? Open AI Chat`
   - Задавайте вопросы своему AI ассистенту!

---

**Спасибо за использование AI Assistant Extension!** ??
