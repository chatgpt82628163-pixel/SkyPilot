<div align="center">

<img src="docs/logo.png" width="96" alt="SkyPilot">

# SkyPilot

**Пилотный клиент SkyNetwork для MSFS, Prepar3D и X-Plane**

[![CI](https://github.com/chatgpt82628163-pixel/SkyPilot/actions/workflows/ci.yml/badge.svg)](https://github.com/chatgpt82628163-pixel/SkyPilot/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/chatgpt82628163-pixel/SkyPilot)](https://github.com/chatgpt82628163-pixel/SkyPilot/releases/latest)

[Сайт](https://sky.network.npzy2.us) · [Скачать](https://sky.network.npzy2.us/docs/software) · [Поддержка](https://sky.network.npzy2.us/support)

</div>

---

## Что это

SkyPilot — пилотный клиент пилотной программы SkyNetwork. Программа подключается к серверу сети и передаёт ваше положение другим участникам, показывает онлайн-диспетчеров, обеспечивает текстовый чат на частотах и голосовую связь с push-to-talk. Поддерживаются Microsoft Flight Simulator 2020/2024 и Prepar3D v4–v6 через SimConnect, а также X-Plane 11/12 через встроенный плагин.

## Возможности

### Связь
- **Текстовый чат** — сообщения отправляются на активной TX-частоте; вкладки по каналам с отметкой непрочитанных.
- **Голосовая связь** — push-to-talk, микрофон и динамики выбираются в настройках, регулируются усиление и громкость приёма; опциональный эффект радиошума.
- **COM 1 / COM 2** — переключение TX/RX для каждой радиостанции, ввод частоты вручную или командой `.com1`/`.com2`; отображается название ближайшей станции и последний слышавший абонент.

### Транспондер
- Режимы Standby / Mode C, ввод кода сквока, кнопка IDENT.

### Диспетчеры
- Список онлайн-диспетчеров с частотами; двойной клик настраивает TX-радио. ATIS запрашивается из контекстного меню или двойным кликом на ATIS-станции.

### Флайт-план
- Подача плана вручную через сайт SkyNetwork.
- Импорт последнего плана из SimBrief по имени пользователя или Pilot ID (кнопка **SIMBRIEF**).
- Кнопка **REFRESH** обновляет поданный план с сайта.

### Обновления
- При запуске программа проверяет наличие новой версии на GitHub и предлагает установить её: установщик скачивается, запускается в фоне и перезапускает SkyPilot.

### Команды в строке ввода
```
.com1 118.100 / .com2 121.500  — настроить радио
.x 7000                        — выставить код сквока
.ident                         — IDENT
.modec                         — переключить Standby / Mode C
.msg CALLSIGN текст            — личное сообщение
.atis STATION                  — запросить ATIS
.wallop текст                  — вызов супервайзера
.disconnect                    — отключиться
.help                          — список команд
```

## Скриншоты

![Главное окно](https://sky.network.npzy2.us/docs/software)

> Актуальные скриншоты — на сайте [sky.network.npzy2.us](https://sky.network.npzy2.us).

## Установка

1. Откройте [sky.network.npzy2.us/docs/software](https://sky.network.npzy2.us/docs/software) и скачайте установщик SkyPilot.
2. Запустите `SkyPilot-Setup-x.y.z.exe`. Установка доступна для текущего пользователя (без прав администратора) или для всех пользователей — по выбору.
3. Запустите SkyPilot. При первом старте откройте **Настройки** и введите CID, позывной и адрес сервера.

**Требования**
- Windows 10 или новее (x64).
- .NET 8 Desktop Runtime — устанавливается вместе с программой, если его нет.
- Один из поддерживаемых симуляторов:
  - Microsoft Flight Simulator 2020 или 2024 — SimConnect.dll берётся из MSFS SDK (`SimConnect SDK\lib`) или кладётся рядом с `SkyPilot.exe`.
  - Prepar3D v4, v5 или v6 — SimConnect.dll берётся из установки P3D или из SDK; можно указать путь вручную в настройках.
  - X-Plane 11 или 12 — плагин устанавливается отдельно (см. раздел сборки).

Обновления устанавливаются автоматически: программа сама скачивает новый установщик и перезапускается.

## Сборка и запуск

### Требования к окружению
- .NET 8 SDK
- Visual Studio 2022 (для WPF-части и плагина на Windows)
- CMake 3.16+ и компилятор C++17 (для X-Plane плагина)

### Сборка .NET (Windows)
```
dotnet build SkyPilot.sln -c Release
```

### Запуск тестов
```
dotnet test tests/SkyPilot.Core.Tests
```

Тесты собираются и запускаются на Windows и Linux.

### Сборка X-Plane плагина

**Windows (Visual Studio):**
```
cmake -S xplane-plugin -B build/xp -A x64
cmake --build build/xp --config Release
```

**Linux / macOS:**
```
cmake -S xplane-plugin -B build/xp
cmake --build build/xp -j
```

**Упаковка** (создаёт папку `SkyPilot/` для X-Plane):
```
cmake --install build/xp --config Release --prefix build/xp/dist
```

Скопируйте `build/xp/dist/SkyPilot/` в `X-Plane/Resources/plugins/`.

XPMP2 скачивается автоматически на этапе конфигурации CMake.

### Основные параметры конфигурации

Настройки хранятся в `%APPDATA%\SkyPilot\settings.json` и редактируются через окно настроек программы. Параметры без секретов:

| Поле | Описание | По умолчанию |
|---|---|---|
| `Simulator` | `"auto"`, `"msfs"`, `"p3d"`, `"xplane"` | `"auto"` |
| `Website` | URL сайта SkyNetwork | `http://127.0.0.1:8000/` |
| `SimbriefUser` | Имя пользователя или Pilot ID SimBrief | — |
| `VoicePort` | UDP-порт голосового сервера | `3782` |
| `CheckForUpdates` | Проверять обновления при запуске | `true` |
| `RadioNoise` | Эффект радиошума на приёме | `true` |

## Устройство

```
SkyPilot.sln
├── src/
│   ├── SkyPilot.App/          WPF-приложение (окна, ViewModels, Assets)
│   ├── SkyPilot.Core/         Логика без зависимости от UI
│   │   ├── Fsd/               Протокол FSD (соединение, пакеты)
│   │   ├── Matching/          Сопоставление моделей (FSLTL, SimObjects)
│   │   ├── Model/             Модели данных (FlightPlan, AircraftState…)
│   │   ├── Session/           Сетевая сессия, команды, чат, ATIS
│   │   ├── Settings/          AppSettings, сериализация
│   │   ├── Simulation/        ISimulator, SimulatorHub, X-Plane-клиент
│   │   ├── Voice/             Радио и голос (PilotVoice, PilotRadios)
│   │   └── Web/               SimBrief, UpdateChecker, WebsiteClient
│   ├── SkyPilot.SimConnect/   SimConnect-обёртка (MSFS и Prepar3D)
│   └── SkyNetwork.Voice/      Голосовой движок
├── xplane-plugin/             X-Plane 11/12 плагин (C++, XPMP2)
├── tests/
│   └── SkyPilot.Core.Tests/   Юнит-тесты
├── installer/
│   └── SkyPilot.iss           Скрипт Inno Setup
└── docs/
    └── logo.png
```

## Часть SkyNetwork

SkyPilot — один из компонентов проекта SkyNetwork:

| Репозиторий | Описание |
|---|---|
| [SkyNetwork-site](https://github.com/chatgpt82628163-pixel/SkyNetwork-site) | Основной сайт сети |
| [SkyNetwork-FSD](https://github.com/chatgpt82628163-pixel/SkyNetwork-FSD) | FSD-сервер |
| [Network-ATC](https://github.com/chatgpt82628163-pixel/Network-ATC) | Диспетчерский клиент |
| [SkyPilot](https://github.com/chatgpt82628163-pixel/SkyPilot) | Пилотный клиент (этот репозиторий) |
| [Skynetwork-voice](https://github.com/chatgpt82628163-pixel/Skynetwork-voice) | Голосовой сервер |
| [SkyRUS-site](https://github.com/chatgpt82628163-pixel/SkyRUS-site) | Российский региональный сайт |
| [Skynetwork-bot](https://github.com/chatgpt82628163-pixel/Skynetwork-bot) | Бот сети |

---

## English

**SkyPilot** is the pilot client of the SkyNetwork online flight simulation network. It connects to MSFS 2020/2024 and Prepar3D v4–v6 via SimConnect, and to X-Plane 11/12 via a bundled plugin.

**Key features**

- Text chat and push-to-talk voice on COM 1 / COM 2 frequencies.
- Transponder (Mode C, IDENT, squawk entry).
- ATC panel — online controllers with frequencies; double-click to tune, ATIS on request.
- Flight plan filing via the SkyNetwork website; SimBrief import with one click.
- Silent self-updating: a newer installer is downloaded and run automatically at startup.

**Download:** [sky.network.npzy2.us/docs/software](https://sky.network.npzy2.us/docs/software)

**Requirements:** Windows 10 x64, .NET 8 (bundled), and one of the supported simulators (MSFS 2020/2024, Prepar3D v4–v6, or X-Plane 11/12).

**Build:**
```
dotnet build SkyPilot.sln -c Release
dotnet test tests/SkyPilot.Core.Tests
```

X-Plane plugin (CMake, C++17):
```
cmake -S xplane-plugin -B build/xp -A x64   # Windows
cmake --build build/xp --config Release
cmake --install build/xp --config Release --prefix build/xp/dist
```
