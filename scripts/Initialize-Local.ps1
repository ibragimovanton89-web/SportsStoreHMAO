<#
.SYNOPSIS
Создаёт локальные секреты и запускает выделенный PostgreSQL проекта.
.DESCRIPTION
Не перезаписывает существующий .env. Генерирует два независимых пароля, выбирает свободный локальный порт и ожидает healthcheck Docker.
.EXAMPLE
./scripts/Initialize-Local.ps1
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$envFile = Join-Path $root '.env'
if (Test-Path $envFile) { throw '.env already exists; keep its secrets and run docker compose up -d --wait.' }
# Криптографически случайные 32 байта; hex безопасен для локального файла окружения.
function New-LocalSecret {
    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
    return [BitConverter]::ToString($bytes).Replace('-', '')
}
$adminSecret = New-LocalSecret
$appSecret = New-LocalSecret
$port = 55432
while ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $port) { $port++ }
[IO.File]::WriteAllText($envFile, "POSTGRES_PORT=$port`nPOSTGRES_ADMIN_PASSWORD=$adminSecret`nAPP_DB_PASSWORD=$appSecret`n")
Write-Host "Created local .env with random credentials; PostgreSQL port: $port"
& docker compose --project-directory $root up -d --wait
if ($LASTEXITCODE -ne 0) { throw 'Docker startup failed; .env retained for retry.' }
