# Руководство по сборке CBSafe

Подробная пошаговая инструкция по сборке проекта **CBSafe** из исходного кода.

---

## 1. Системные требования

* **Операционная система:** Windows 10 / 11 (x64)
* **SDK:** [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) или более новый (.NET 8.0 SDK с поддержкой net6.0)
* **Среда разработки (на выбор):**
  * Visual Studio 2022 (с установленной рабочей нагрузкой «Разработка классических приложений .NET»)
  * JetBrains Rider 2022+
  * Visual Studio Code (с расширениями C# Dev Kit и .NET SDK)

---

## 2. Структура зависимостей

Проект разработан под **BepInEx 6 (IL2CPP)** для клиента Rust и использует сгенерированные interop-библиотеки игры.

В файле `CBSafe.csproj` пути к зависимостям по умолчанию настроены относительно папки клиента:
```text
CobaltRust/
├── BepInEx/
│   ├── core/
│   │   ├── BepInEx.Core.dll
│   │   ├── BepInEx.Unity.IL2CPP.dll
│   │   ├── Il2CppInterop.Runtime.dll
│   │   └── 0Harmony.dll
│   └── interop/
│       ├── Assembly-CSharp.dll
│       ├── UnityEngine.CoreModule.dll
│       ├── UnityEngine.UI.dll
│       ├── Unity.TextMeshPro.dll
│       └── ... (остальные модули Unity и Rust)
└── CBSafe/   <-- Папка проекта
```

### Где взять interop-библиотеки:
1. Установите **BepInEx 6 IL2CPP (x64)** в корень установленного клиента Rust.
2. Запустите игру один раз через BepInEx.
3. При первом запуске утилита `Il2CppInterop` автоматически сгенерирует все необходимые библиотеки в папку:
   `<Папка_игры>/BepInEx/interop/`

---

## 3. Настройка путей (если папка игры отличается)

Если папка с клиентом игры называется иначе или находится в другом месте:
1. Откройте файл `CBSafe.csproj` в текстовом редакторе.
2. Найдите блок `<ItemGroup>` с тегами `<Reference>` (строки 124–218).
3. Замените путь `..\CobaltRust\CobaltRust\BepInEx\` на ваш актуальный путь к BepInEx:
   ```xml
   <Reference Include="Assembly-CSharp">
     <HintPath>D:\Games\Rust\BepInEx\interop\Assembly-CSharp.dll</HintPath>
   </Reference>
   ```

---

## 4. Сборка проекта

### Вариант A. Через командную строку (CLI)

1. Откройте терминал (PowerShell или CMD) в папке проекта:
   ```powershell
   cd "C:\Users\User\Desktop\CBSafe"
   ```

2. Выполните сборку в конфигурации **Release**:
   ```powershell
   dotnet build CBSafe.csproj -c Release
   ```

3. После успешного завершения готовая библиотека будет находиться по пути:
   `bin\Release\netcoreapp6.0\CBSafe.dll`

---

### Вариант B. Через Visual Studio 2022

1. Запустите Visual Studio и выберите **«Открыть проект или решение»**.
2. Укажите файл `CBSafe.csproj`.
3. В верхней панели переключите конфигурацию сборки на **Release** и платформу **Any CPU**.
4. В верхнем меню выберите **Сборка -> Собрать решение** (или нажмите `Ctrl + Shift + B`).
5. Готовый файл появится в папке `bin\Release\netcoreapp6.0\`.

---

### Вариант C. Сборка подмодуля CBSlotColor (при необходимости)

Если вам требуется отдельно собрать вспомогательный плагин `CBSlotColor`:
```powershell
cd "C:\Users\User\Desktop\CBSafe\CBSlotColor"
dotnet build CBSlotColor.csproj -c Release
```
*(По умолчанию собранный `CBSafe.Embedded.CBSlotColor.dll` уже вшит внутрь основного проекта CBSafe).*

---

## 5. Установка плагина в игру

1. Скопируйте скомпилированный файл `CBSafe.dll`.
2. Вставьте его в папку плагинов клиента Rust:
   `<Папка_игры>\BepInEx\plugins\`
3. Запустите игру. В настройках игры появятся новые вкладки:
   * **«ОПТИМИЗАЦИЯ КЛИЕНТА»**
   * **«НАСТРОЙКИ СТЕКЛЯННОГО КАЛАША»**

---

## 6. Возможные ошибки при сборке

| Ошибка | Причина | Решение |
|---|---|---|
| `CS0246: The type or namespace '...' could not be found` | Не найдены interop-библиотеки игры | Проверьте пути `<HintPath>` в `CBSafe.csproj` к папкам `BepInEx/interop` и `BepInEx/core` |
| `NETSDK1045: The current .NET SDK does not support targeting .NET 6.0` | Не установлен .NET 6.0 SDK | Скачайте и установите [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) |
| `CS0227: Unsafe code may only appear if compiling with /unsafe` | Отключен флаг небезопасного кода | Убедитесь, что в `.csproj` указано `<AllowUnsafeBlocks>True</AllowUnsafeBlocks>` |
