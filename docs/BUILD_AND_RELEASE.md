# Сборки и релизы SimpleSummon

Workflow `.github/workflows/unity-windows.yml` отвечает за проверку и выпуск Windows-сборок.

## Что происходит автоматически

- Pull request в `main`: запускаются Unity EditMode tests.
- Push в `main`: после тестов создаётся production-сборка Windows x64 и обновляется prerelease `latest-main`.
- Push тега `vX.Y.Z`: создаётся стабильный GitHub Release с таким же тегом.
- Каждый build содержит `BUILD_INFO.json`, `VERSION.txt`, `CHANGELOG.md` и инструкцию запуска.
- Рядом с ZIP публикуется файл SHA-256.
- Actions artifacts хранятся 30 дней, результаты тестов — 14 дней. GitHub Releases хранят постоянные сборки.

## Первичная настройка GitHub

В `Settings -> Secrets and variables -> Actions` нужно добавить repository secrets:

- `UNITY_LICENSE` — содержимое Unity license file.
- `UNITY_EMAIL` — email Unity ID.
- `UNITY_PASSWORD` — пароль Unity ID.

Для Unity Personal требуется один раз получить license file по инструкции GameCI:
https://game.ci/docs/github/activation

В `Settings -> Actions -> General` должны быть разрешены workflow actions. Workflow самостоятельно запрашивает `contents: write` для Releases и `checks: write` для результатов тестов.

Секреты нельзя добавлять в Git, `.env`, issue или сообщения workflow.

## Версии

Базовая версия хранится в `ProjectSettings/ProjectSettings.asset` в поле `bundleVersion`.

- Push в `main`: `0.1.0-main.<GitHub run number>`.
- Стабильный релиз: версия берётся из тега, например `v0.2.0` превращается в `0.2.0` внутри игры.

Пример стабильного релиза:

```powershell
git tag v0.2.0
git push origin v0.2.0
```

## Где скачать

Последняя успешная сборка `main`:

https://github.com/Thisman/SimpleSummon/releases/tag/latest-main

Если репозиторий закрытый, GitHub потребует авторизацию и доступ к репозиторию.

Чтобы запустить игру, нужно скачать ZIP, полностью распаковать его и запустить `SimpleSummon.exe`. Нельзя вынимать EXE отдельно из папки сборки.

## Ручной запуск

Workflow можно запустить из `Actions -> Unity Windows Build -> Run workflow`.

Локальная сборка остаётся доступна в Unity через `Build -> Production -> Windows x64`.
