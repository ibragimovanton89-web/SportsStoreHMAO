<#
.SYNOPSIS
Настраивает административное подключение для изолированных интеграционных тестов.
.DESCRIPTION
Подключайте через точку. Учётная запись используется тестами для создания и удаления уникальной тестовой БД, не веб-приложением.
.EXAMPLE
. ./scripts/Use-IntegrationDatabase.ps1
#>
# Dot-source after Use-LocalDatabase.ps1. Only the TEST harness uses this administrative connection.
$root = Split-Path $PSScriptRoot
$testValues = @{}
Get-Content (Join-Path $root '.env') | Where-Object { $_ -match '^[A-Z_]+=' } | ForEach-Object { $pair = $_ -split '=',2; $testValues[$pair[0]] = $pair[1] }
if (!$testValues['POSTGRES_ADMIN_PASSWORD']) { throw 'POSTGRES_ADMIN_PASSWORD is missing.' }
$env:SPORTSSTORE_TEST_ADMIN_CONNECTION = "Host=127.0.0.1;Port=$($testValues['POSTGRES_PORT']);Database=postgres;Username=postgres;Password=$($testValues['POSTGRES_ADMIN_PASSWORD']);Include Error Detail=false"
Write-Host 'Isolated test database provisioning configured; credentials not printed.'
