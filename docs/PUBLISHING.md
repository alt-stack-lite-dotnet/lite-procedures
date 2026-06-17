# Публикация релизов и NuGet

## Ветка и теги

- Релизная ветка задаётся в CI: **`release`** (переменная `RELEASE_BRANCH` в `.github/workflows/ci.yml`).
- Пакеты собираются и публикуются в NuGet **только если тег поставлен с ветки `release`**: в job `pack` проверяется, что коммит тега является предком `origin/release`.

## Как выпустить релиз

1. Убедиться, что все изменения в `release` закоммичены и запушены.
2. Поставить тег с версией (семантический версионинг), например:
   ```bash
   git checkout release
   git pull origin release
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. В GitHub Actions:
   - Запустится workflow по событию `push` тега `v*`.
   - Job `build-and-test` выполнит сборку и тесты.
   - Job `pack` выполнится только если коммит тега принадлежит ветке `origin/release`:
     - версия пакетов берётся из имени тега (например, `v1.0.0` → `1.0.0`);
     - выполняется `dotnet pack`;
     - артефакты загружаются в workflow;
     - через **Trusted Publishing (OIDC)** пакеты отправляются на NuGet (без долгоживущего API-ключа).

## Trusted Publishing (OIDC)

Публикация в NuGet идёт через [Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) — без долгоживущего API-ключа. GitHub Actions получает короткоживущий OIDC-токен и обменивает его на временный ключ nuget.org (живёт 1 час).

Настройка (один раз):

1. На [nuget.org](https://www.nuget.org) → имя пользователя → **Trusted Publishing** → создать политику:
   - **Repository Owner:** `alt-stack-lite-dotnet`
   - **Repository:** `lite-procedures`
   - **Workflow File:** `ci.yml` (только имя файла, без пути)
   - **Environment:** оставить пустым (GitHub environments не используются)
   - **Policy owner:** организация `alt-stack-lite-dotnet` (или личный аккаунт)
2. В репозитории добавить секрет **`NUGET_USER`** (Settings → Secrets and variables → Actions) = **profile name** на nuget.org (НЕ email).
3. В workflow `pack` уже задано `permissions: id-token: write`, шаг `NuGet/login@v1` получает временный ключ, `dotnet nuget push` публикует.

Примечания:
- Для приватного репозитория политика стартует как «temporary active 7 дней»: если за это время не было успешной публикации — деактивируется (окно можно перезапустить). После первой успешной публикации становится постоянной.
- Если секрет `NUGET_USER` или политика не настроены, шаг login упадёт — артефакты `out/*.nupkg` всё равно загружаются в workflow.

## Смена релизной ветки

В файле `.github/workflows/ci.yml` изменить значение `RELEASE_BRANCH` (в `env` для job `pack`).
