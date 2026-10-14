# ASP.NET MVC-шаблон игры LingvoGameOs

Шаблон поддерживает актуальный контракт портала:

- вход через `/start?session=<id>&token=<hmac>`;
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
  GAME_PATH_BASE: /games/my-game
  GAME_COOKIE_NAME: lingvogameos_my_game_session
expose:
  - "8080"
```

`ports` для игры не нужен: портал проксирует контейнер по `/games/my-game`.
