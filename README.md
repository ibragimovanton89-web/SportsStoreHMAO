# SportsStoreHMAO
Основа спортивного интернет-магазина для ХМАО — Югры. Реализованы Code First модель, PostgreSQL, импорт BallMarket, серверные цены и административная Blazor-панель. Добавлены закупочные «Мои заказы», частичная приёмка на собственный склад и публичная витрина опубликованных карточек. Добавлены аккаунты покупателей, личный кабинет, заявки на опт и персональная цена подтверждённого покупателя. Пятый этап добавляет корзину, оформление и резервирование собственных остатков. Оплата и отгрузка остаются следующими этапами. Публичная главная сохранена.

## Управление магазином

Вход: **https://localhost:7180/account/login**. После настройки подключения и применения миграций создайте первого Admin в обычном интерактивном PowerShell:

```powershell
. ./scripts/Use-LocalDatabase.ps1
dotnet run --project src/SportsStore.Tools -- bootstrap-admin
```

Пароль вводится скрыто, затем требуется явное подтверждение аккаунта оператором. При первом входе обязательно подключение TOTP; резервные коды показываются один раз. Повторный bootstrap не сбрасывает пароль и не повышает существующего покупателя.

Основной путь: **Мои заказы → выбор позиций и опта → Создать заказ → передача / в пути → приёмка на Мой склад → собственная карточка → цена вручную или процент от закупки → Опубликовать**. Карточку можно подготовить уже после создания закупки, до поступления. Полная инструкция: [закупки и склад](docs/purchasing.md). Вход в новый раздел: https://localhost:7180/admin/purchases; витрина: https://localhost:7180/catalog. Ассортимент прайса автоматически не публикуется.

Подробнее: [инструкция сотруднику](docs/admin-panel.md), [матрица прав](docs/admin-permissions.md), [фактические проверки](docs/verification.md). Изображения хранятся вне исходников (`Storage:RootPath`); их каталог и постоянные ключи Data Protection необходимо сохранять между перезапусками.

## Структура
Русская документация классов, методов и свойств находится непосредственно в коде и доступна в подсказках IDE. Начните с [карты кода](docs/code-guide.md). Правило документировать новый код сразу закреплено в [AGENTS.md](AGENTS.md); сборка проверяет XML-документацию публичных членов.

```text
SportsStoreHMAO.sln
src/
  SportsStore.Domain/          сущности без EF/Identity
  SportsStore.Application/     контракты импорта, расчёт цен, снимки
  SportsStore.Infrastructure/  EF, Identity, миграции, парсер, сервисы
  SportsStore.Web/             существующий Blazor Web App
  SportsStore.Tools/           локальная консоль оператора
tests/SportsStore.Tests/       unit + настоящий PostgreSQL
docker/init-app.sh
compose.yaml
scripts/
docs/database.md
docs/price-import.md
docs/SECURITY.md
```
Web/Tools → Application + Infrastructure → Application → Domain. Внедрение зависимостей сохранено. Nullable и warnings-as-errors включены. EF/Identity/dotnet-ef 10.0.11, Npgsql EF provider 10.0.3, ExcelDataReader 3.9.0 (MIT). .NET SDK 10.

## PostgreSQL в Docker
Нужны Docker Desktop с Linux containers и Docker Compose. Для первого запуска из корня решения:
```powershell
./scripts/Initialize-Local.ps1
. ./scripts/Use-LocalDatabase.ps1
```
Скрипт генерирует разные случайные пароли в исключённый из Git .env. При занятом порте выбирает следующий, начиная с 55432. Если .env уже существует, не удаляйте его:
```powershell
docker compose up -d --wait
. ./scripts/Use-LocalDatabase.ps1
```
Если PowerShell сообщает, что выполнение сценариев отключено, разрешите локальные сценарии только для текущего окна и повторите команду:
```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy RemoteSigned -Force
```
Изменение перестаёт действовать после закрытия окна.

Альтернатива: скопировать .env.example в .env и задать собственные секреты вручную. Не выводите docker compose config без --quiet: он может раскрыть environment.

Официальный postgres:17.11-bookworm, отдельный compose project sportsstorehmao-local и named volume postgres-data, привязка 127.0.0.1:<POSTGRES_PORT>. Для PostgreSQL 17 правильная точка тома — /var/lib/postgresql/data. Healthcheck pg_isready. Init создаёт sportsstore с NOSUPERUSER NOCREATEDB NOCREATEROLE и передаёт ему только БД sportsstorehmao и схему public. Приложение никогда не использует postgres. В production следует дополнительно отделить права мигратора от runtime-роли.

.env применяется к Docker Compose, но не считывается .NET автоматически. Use-LocalDatabase устанавливает ConnectionStrings__DefaultConnection только в текущей PowerShell-сессии. Для других сред используйте user-secrets или секрет-хранилище; пароль не записывайте в appsettings. Для удалённой БД — TLS VerifyFull.

MCP-инструментов управления Docker в данной сессии не обнаружено. Пользователь явно разрешил Docker CLI и успешно запустил подготовленный скрипт вне песочницы: postgres:17.11-bookworm, контейнер Healthy. Агент применил миграции, проверил импорт и интеграционные тесты через TCP с .NET CLI. Прямое управление Docker Engine из песочницы остаётся заблокированным. Точный итог проверок фиксируется в docs/verification.md.

## Сборка, миграции, Web
```powershell
dotnet tool restore
dotnet restore
dotnet build --no-restore
. ./scripts/Use-LocalDatabase.ps1
dotnet ef database update --project src/SportsStore.Infrastructure --startup-project src/SportsStore.Infrastructure
dotnet ef database update --project src/SportsStore.Infrastructure --startup-project src/SportsStore.Infrastructure
dotnet dev-certs https --trust
dotnet run --project src/SportsStore.Web --launch-profile https
```
Главная: https://localhost:7180. Повторный database update не меняет схему. Обычный запуск Web миграций не выполняет. DesignTimeDbContextFactory использует ту же переменную окружения, а для генерации модели допускает адрес без пароля, не требуя живой БД.

Development appsettings по-прежнему содержит только несекретный fallback локальной БД. Для Docker используйте переменную окружения из скрипта; она имеет приоритет. Главная страница не запрашивает БД и открывается независимо от её готовности.

## Прайс
```powershell
dotnet run --project src/SportsStore.Tools -- parse "C:\Users\ibrag\Downloads\Price-list_BallMarket.ru.xls"
dotnet run --project src/SportsStore.Tools -- preview "C:\Users\ibrag\Downloads\Price-list_BallMarket.ru.xls"
dotnet run --project src/SportsStore.Tools -- report <batchId>
dotnet run --project src/SportsStore.Tools -- apply <batchId>
dotnet run --project src/SportsStore.Tools -- inspect
```
Следующий прайс проходит тот же preview → report → apply. Идентичный SHA не создаёт новую партию. Исходные предложения не публикуются на витрине. Все команды сопоставления/наценок/пересчёта и обработка ошибок — [docs/price-import.md](docs/price-import.md). Таблицы, ER и правила — [docs/database.md](docs/database.md).

## Тесты
Полная проверка после запуска контейнера:
```powershell
./scripts/Verify-Local.ps1 -PriceFile "C:\Users\ibrag\Downloads\Price-list_BallMarket.ru.xls"
```
Фактический статус текущей сессии: [docs/verification.md](docs/verification.md).

```powershell
dotnet test --no-build --filter "Category!=PostgreSQL"
. ./scripts/Use-LocalDatabase.ps1
. ./scripts/Use-IntegrationDatabase.ps1
dotnet test --no-build --filter "Category=PostgreSQL"
```
Интеграционные тесты создают уникальные sportsstore_test_<guid> через административное соединение **только для provisioning**, затем работают через несуперпользователя sportsstore. Они применяют миграции дважды и удаляют только созданную ими тестовую БД. База разработки не очищается. Без переменных подключения интеграционные тесты явно завершаются ошибкой, а не заменяются EF InMemory или ложным PASS.

## Резервная копия и восстановление
Не выполняйте docker compose down -v: это удалит том. Обычный docker compose stop/start и restart сохраняют данные.
```powershell
New-Item -ItemType Directory -Force backups
docker compose exec -T postgres pg_dump -U sportsstore -d sportsstorehmao -Fc -f /tmp/sportsstorehmao.dump
docker compose cp postgres:/tmp/sportsstorehmao.dump ./backups/sportsstorehmao.dump
```
Для каждого backup используйте новое имя с датой, чтобы не затереть предыдущий. Dump может содержать персональные данные: ограничьте доступ, шифруйте и задайте срок хранения.

Проверка восстановления в **новую** БД (не перезаписывает рабочую):
```powershell
docker compose cp ./backups/sportsstorehmao.dump postgres:/tmp/restore.dump
docker compose exec -T postgres createdb -U postgres -O sportsstore sportsstorehmao_restore
docker compose exec -T postgres pg_restore -U sportsstore -d sportsstorehmao_restore --no-owner --exit-on-error /tmp/restore.dump
```
Внутриконтейнерные команды используют локальный Unix socket. Проверьте количество предложений/цен, migrations history и контрольную позицию в восстановленной БД перед признанием backup пригодным. Для проверки volume: inspect → docker compose restart postgres → docker compose up -d --wait → inspect; счётчики должны совпасть.

## До использования реальных цен
Заполнить подтверждённый тариф поставщика, сопоставления, подтверждённые коэффициенты единиц, источник закупочной цены каждого SKU, собственные наценки и оптовые ступени. Общий минимум оптового заказа больше не применяется. Налоговый режим и порядок отражения налога пока не заданы. Автоприменение выключено. Роли сотрудников не заменяют подтверждение оптового статуса клиента.
См. [docs/SECURITY.md](docs/SECURITY.md) для задач до сбора персональных данных и подключения оплаты.

### Подключение при запуске из Visual Studio

В Development веб-проект автоматически читает `POSTGRES_PORT` и `APP_DB_PASSWORD` из локального `.env` корня решения, если `ConnectionStrings:DefaultConnection` не задана явно. Поэтому для F5 не требуется предварительно запускать `Use-LocalDatabase.ps1` в отдельном окне. Окружение, User Secrets и аргументы с явным подключением сохраняют приоритет. Production `.env` этим механизмом не читает. Пароли не копируются в appsettings или launchSettings. Для EF CLI и Tools по-прежнему подключайте скрипт текущей PowerShell-сессии.

### Розничные и оптовые цены

Цены редактируются в «Мой склад» → карточка товара, отдельно для каждого варианта: «Розничная цена» и «Оптовая цена». Доступны ручная сумма и процент от выбранной закупки. Отдельного раздела «Цены и наценки» больше нет; прежние адреса перенаправляют на склад. Подробнее — `docs/admin-panel.md`. Миграция для этого изменения не требуется.

## Временный локальный вход администратора

Для текущего этапа тестирования в `src/SportsStore.Web/appsettings.Development.json` включено:

```json
"Development": {
  "BypassAdminAuthentication": true
}
```

После перезапуска приложения открывайте `/admin` напрямую: пароль и TOTP локально не требуются. В шапке показано «Тестовый вход · без пароля». Пользователь Identity не создаётся, существующие пароли и настройки MFA не меняются. Аудит использует отдельное имя `local-development-admin`.

Чтобы вернуть обычный вход, задайте `false` и перезапустите приложение. Также допустима переменная окружения `Development__BypassAdminAuthentication=false`. При тестировании сценариев приглашений и реального входа сотрудников отключайте этот флаг.

Режим разрешён только в `Development`, при прямом loopback-соединении и Host localhost/loopback. Не публикуйте локальный Development-хост через туннель или reverse proxy, переписывающий адрес клиента и Host в localhost. Для развёртывания используйте `Production`; если флаг ошибочно включён вне Development, приложение остановит запуск. CSRF-защита, проверки данных и транзакции остаются включёнными.


### Заказ поставщику в Excel

В «Моих заказах» кнопка «Отметить передачу поставщику» сохраняет статус и скачивает Excel со всем выбранным составом, тремя закупочными тарифами, количеством и итогом. Повторная выгрузка — «Скачать заказ в Excel». Отправка письма остаётся за сотрудником. Подробнее — [инструкция по закупкам](docs/purchasing.md#excel-для-поставщика). Дополнительные пакеты и миграции не требуются.

## Покупательский каталог — этап 3

`/catalog` теперь поддерживает поиск по названию/SKU/артикулу, собственные категории с потомками, несколько брендов, размеры, цвета, цены и наличие. Есть четыре сортировки, страницы по 24 товара, ссылки с сохранением фильтров. Страница товара содержит галерею, выбор варианта, его розничную цену и собственную доступность. Мобильные фильтры и меню работают без изменения главной страницы.

После обычного запуска Web откройте [каталог](https://localhost:7180/catalog). При запуске из Visual Studio используйте свой порт и путь `/catalog`. Новые миграции не требуются. Рабочие товары появляются только после публикации в «Мой склад». Демонстрационные тесты не создают данные в рабочей БД.

Публичный origin задаётся `Store__PublicBaseUrl` своим HTTPS-адресом при размещении; локально canonical использует проверенный localhost. Для гостевой проверки отдельного процесса задайте `$env:Development__BypassAdminAuthentication='false'`, не меняя общую настройку проекта.

Подробности, URL, правила видимости, наличие и подготовка аккаунтов: [docs/storefront.md](docs/storefront.md). Фактические проверки: [docs/verification.md](docs/verification.md). Список каталога остаётся розничным; аккаунты и персональный опт в карточке добавлены четвёртым этапом ниже. Корзина и оформление описаны в разделе пятого этапа ниже.


## Аккаунты покупателей — четвёртый этап

После настройки подключения примените новую миграцию штатным EF CLI (обычный старт Web миграции не выполняет):

```powershell
dotnet tool restore
dotnet ef database update --project src/SportsStore.Infrastructure --startup-project src/SportsStore.Infrastructure
```

Запуск по HTTPS с локальным перехватом писем:

```powershell
$env:Email__Mode = 'Capture'
$env:Email__PublicOrigin = 'https://localhost:7180'
$env:Email__CapturePath = Join-Path $env:LOCALAPPDATA 'SportsStoreHMAO\development-mail'
dotnet run --project src/SportsStore.Web --launch-profile https
```

Используйте фактический HTTPS-порт своего профиля запуска. В Visual Studio задайте эти значения через user-secrets/локальное окружение проекта; настоящие SMTP-пароли не добавляйте в launchSettings.json или Git.

1. Откройте `/customer/register` и создайте тестового покупателя.
2. Найдите письмо в локальном CapturePath, откройте ссылку и подтвердите почту.
3. Войдите через `/customer/login`, заполните профиль и подайте заявку в `/customer/wholesale`.
4. Сотрудник входит отдельно через `/account/login` с MFA. Admin рассматривает заявку в **Покупатели → Заявки и история**.
5. На карточке товара подтверждённому покупателю показываются **Ваша оптовая цена** и ступени количества одного SKU. Список каталога остаётся розничным.

Для достоверной проверки покупательской сессии запускайте отдельный процесс с `Development__BypassAdminAuthentication=false`. Локальный обход администратора не создаёт покупателя и не даёт ему скидку. Не меняйте свои рабочие роли и оптовые условия ради тестирования.

Подробности: [кабинет и сессии](docs/customer-accounts.md), [заявки и решения](docs/wholesale-applications.md), [SMTP и локальные письма](docs/email.md). Перед публичным запуском нужны собственный домен, SMTP, защита постоянных ключей Data Protection и утверждённые юридические тексты. Это техническая реализация кабинета, а не готовность магазина принимать заказы.

## Этап 5: корзина и заказы покупателей

Добавлены `/cart`, `/checkout`, `/customer/orders` и `/admin/customer-orders`. Перед запуском восстановите инструменты/пакеты и примените миграции штатной командой проекта (см. раздел БД выше); обычный старт Web миграции не запускает. Новая миграция `20260913231544_CustomerCartOrdersReservations` добавляет корзины, снимки подтверждения, распределённые резервы и очередь событий заказов, сохраняя старые заказы как Historical.

Настройте `Orders:ReservationHours` (локально демонстрационные 24 часа), действующие розничные/оптовые цены карточек и собственные остатки. Для оформления нужен подтверждённый покупатель, телефон и адрес. Добавьте вариант в корзину, проверьте условия и отправьте заказ; сотрудник обработает его в «Заказы покупателей». Это не онлайн-оплата и не отгрузка.

Подробности: [корзина](docs/cart-checkout.md), [заказы](docs/customer-orders.md), [резервы](docs/reservations.md), [проверки](docs/verification.md).
