#!/bin/bash
# Однократная инициализация нового тома официальным образом PostgreSQL.
# Создаёт несуперпользовательскую роль приложения и передаёт ей базу и схему.
# Пароль поступает из окружения через переменную psql; не выводится в журнал.
set -euo pipefail
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --set=app_password="$APP_DB_PASSWORD" <<'SQL'
CREATE ROLE sportsstore LOGIN PASSWORD :'app_password' NOSUPERUSER NOCREATEDB NOCREATEROLE;
ALTER DATABASE sportsstorehmao OWNER TO sportsstore;
ALTER SCHEMA public OWNER TO sportsstore;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
SQL

