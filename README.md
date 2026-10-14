# ASP.NET MVC-шаблон игры LingvoGameOs

Шаблон поддерживает актуальный контракт портала:

- вход через `/start?session=<id>&token=<hmac>`;
- проверку сессии через `POST /api/game/session/resolve` и привязку к `LINGVO_GAME_ID`;
- профиль пользователя портала без отдельной формы логина внутри игры;
- сохранение игровой сессии между запросами;
- отправку результата в `POST /api/score`;
- публикацию под `/games/<slug>` без отдельного внешнего порта;
- отдельный cookie name и cookie path для каждой игры;
- reverse proxy headers и ссылки, учитывающие `PathBase`.

TLS завершается на портале, поэтому сама игра слушает HTTP только во внутренней
Docker-сети и не выполняет лишний HTTPS-redirect.

## Локальный запуск

`.env.example` перечисляет необходимые настройки, но приложение читает рабочие
значения из переменных окружения процесса. Общий `stack.ps1` загружает их из
корневого `.env` проекта.

```powershell
$env:LINGVO_API_URL = "http://localhost:5001/api"
$env:LINGVO_GAME_ID = "123" # реальный ID игры в БД портала
$env:GAME_PATH_BASE = "/games/my-game"
$env:GAME_COOKIE_NAME = "lingvogameos_my_game_session"
$env:ASPNETCORE_URLS = "http://localhost:8080"
dotnet run --project .\GameWithApiASPTemplate
```

Для отдельной локальной проверки без reverse proxy временно не задавайте
`GAME_PATH_BASE` и откройте
`http://localhost:8080/start?session=1&token=test-token`.

## Подключение к Docker-стеку

```yaml
environment:
  ASPNETCORE_URLS: http://+:8080
  LINGVO_API_URL: http://api:8080/api
  LINGVO_GAME_ID: "123"
  GAME_PATH_BASE: /games/my-game
  GAME_COOKIE_NAME: lingvogameos_my_game_session
expose:
  - "8080"
```

`ports` для игры не нужен: портал проксирует контейнер по `/games/my-game`.

## Docker

Для автономного локального запуска создайте рабочий `.env` и поднимите игру
одной командой:

```powershell
Copy-Item .env.example .env
docker compose up --build
```

Игра будет доступна на `http://localhost:8080`. Порт можно изменить через
`GAME_HOST_PORT` в `.env`. `compose.yaml` намеренно очищает `GAME_PATH_BASE`,
поскольку при автономном запуске перед игрой нет reverse proxy портала.

В составе LingvoGameOs используйте этот же `Dockerfile`, но не публикуйте порт
на хост: передайте контейнеру `GAME_PATH_BASE=/games/<slug>`, уникальный
`GAME_COOKIE_NAME`, реальный `LINGVO_GAME_ID` и внутренний URL API. Именованный
том в `compose.yaml` сохраняет Data Protection keys и не сбрасывает сессии при
пересоздании контейнера.
