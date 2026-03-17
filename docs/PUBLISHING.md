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
     - при наличии секрета **`NUGET_API_KEY`** в репозитории пакеты отправляются на NuGet.

## Секрет NUGET_API_KEY

- В настройках репозитория: Settings → Secrets and variables → Actions.
- Добавить секрет `NUGET_API_KEY` с API-ключом с [nuget.org](https://www.nuget.org/account/apikeys).
- Если секрет не задан, шаг «Push to NuGet» пропускается (артефакты всё равно сохраняются).

## Смена релизной ветки

В файле `.github/workflows/ci.yml` изменить значение `RELEASE_BRANCH` (в `env` для job `pack`).
