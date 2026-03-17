# Lite.Procedures

Легковесный пайплайн процедур с интерцепторами для .NET: регистрация в DI, синхронный/асинхронный вызов, опциональная кодогенерация пайплайнов.

## Установка окружения

- **.NET SDK 10** — [скачать](https://dotnet.microsoft.com/download).
- Опционально: **mise** (или rtx) для версий: `mise install` (см. `.mise.toml`).
- Локальная установка одной командой:
  - Windows: `.\install.ps1`
  - Linux/macOS: `./install.sh`
- Dotnet-инструменты (CSharpier и др.): `dotnet tool restore` (конфиг в `.config/dotnet-tools.json`).
- Pre-commit: `pip install pre-commit` (или `mise install`), затем `pre-commit install`.

## Сборка и тесты

```bash
dotnet restore Lite.Procedures.sln
dotnet build Lite.Procedures.sln -c Release
dotnet test Lite.Procedures.sln -c Release --no-build
```

## Линт и формат

- **Проверка формата и сборки/тестов:**  
  `python .config/run_lint.py`  
  Варианты: `run_lint.py format`, `run_lint.py build`, `run_lint.py test`.
- **Форматирование C#:**  
  `dotnet format Lite.Procedures.sln` (применить изменения) или через CSharpier: `dotnet csharpier .`
- **Pre-commit:** при `pre-commit install` перед коммитом запускаются проверки из `.pre-commit-config.yaml` (format verify, build, test).

## IDE (VS Code / Cursor)

- Рекомендуемые расширения: см. `.vscode/extensions.json` (C#, CSharpier).
- Format on save и форматтер для C#: см. `.vscode/settings.json`.

## Релиз и NuGet

Релизы делаются с ветки **release** через теги. Публикация в NuGet — только при пуше тега вида `v*` с этой ветки. Подробно: [docs/PUBLISHING.md](docs/PUBLISHING.md).

## Бенчмарки

См. [BENCHMARKS.md](BENCHMARKS.md).

## Плейграунд (реальное приложение)

Минимальное ASP.NET Core приложение с Lite.Procedures — процедуры, контроллеры, DI (по аналогии с [Lite.Validation playground](https://github.com/lite-dotnet/Lite.Validation)):

```bash
dotnet run --project playground/Lite.Procedures.Playground/Lite.Procedures.Playground.csproj
```

- **GET /echo?q=...** — вызов процедуры Echo (возвращает строку).
- **POST /order** — тело `{ "productName": "...", "quantity": 1, "price": 10.5 }`, возвращает созданный заказ.
