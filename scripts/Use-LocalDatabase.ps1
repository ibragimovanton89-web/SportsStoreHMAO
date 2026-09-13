<#
.SYNOPSIS
Загружает подключение приложения из локального .env в текущий PowerShell.
.DESCRIPTION
Подключайте через точку. Изменяет ConnectionStrings__DefaultConnection только в текущем процессе; пароль не печатается.
.EXAMPLE
. ./scripts/Use-LocalDatabase.ps1
#>
# Подключение в текущую сессию: . ./scripts/Use-LocalDatabase.ps1
$root = Split-Path $PSScriptRoot
$values = @{}
Get-Content (Join-Path $root '.env') | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object { $pair = $_ -split '=',2; $values[$pair[0]] = $pair[1] }
if (!$values['APP_DB_PASSWORD']) { throw 'APP_DB_PASSWORD is missing.' }
$env:ConnectionStrings__DefaultConnection = "Host=127.0.0.1;Port=$($values['POSTGRES_PORT']);Database=sportsstorehmao;Username=sportsstore;Password=$($values['APP_DB_PASSWORD']);Include Error Detail=false"
Write-Host 'Database connection configured for this PowerShell session (secret not printed).'

