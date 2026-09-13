<#
.SYNOPSIS
Проверяет сборку, миграции, тесты, импорт и сохранность после перезапуска PostgreSQL.
.DESCRIPTION
Изменяет локальную БД разработки: применяет переданный прайс и перезапускает контейнер проекта. При неуспехе команды прекращает проверки.
.PARAMETER PriceFile
Путь к локальному закупочному XLS для проверки и применения.
.EXAMPLE
./scripts/Verify-Local.ps1 -PriceFile C:/data/price.xls
#>
param([Parameter(Mandatory=$true)][string]$PriceFile)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
Push-Location $root
try {
    # Выполняет внешнюю команду и превращает ненулевой код возврата в остановку проверки.
    function Invoke-Checked {
        param([string]$Command,[string[]]$Arguments)
        & $Command @Arguments
        if ($LASTEXITCODE -ne 0) { throw "$Command failed; verification stopped." }
    }
    # Вызывает локальную утилиту и разбирает её JSON для проверок без ручного чтения вывода.
    function Invoke-StoreJson {
        param([string[]]$Arguments)
        $result = & dotnet run --project src/SportsStore.Tools --no-build -- @Arguments
        if ($LASTEXITCODE -ne 0) { throw 'Store command failed; verification stopped.' }
        return ($result -join "`n" | ConvertFrom-Json)
    }
    Invoke-Checked docker @('compose','up','-d','--wait')
    . ./scripts/Use-LocalDatabase.ps1
    . ./scripts/Use-IntegrationDatabase.ps1
    Invoke-Checked dotnet @('tool','restore')
    Invoke-Checked dotnet @('restore')
    Invoke-Checked dotnet @('build','--no-restore')
    $migrationArgs=@('ef','database','update','--project','src/SportsStore.Infrastructure','--startup-project','src/SportsStore.Infrastructure','--no-build')
    Invoke-Checked dotnet $migrationArgs
    Invoke-Checked dotnet $migrationArgs
    Invoke-Checked dotnet @('test','--no-build')
    $preview = Invoke-StoreJson @('preview',$PriceFile)
    $preview | ConvertTo-Json -Depth 5
    if ($preview.Errors -ne 0) { throw 'Import has errors. Nothing applied.' }
    $applied = Invoke-StoreJson @('apply',$preview.Id)
    $repeat = Invoke-StoreJson @('apply',$preview.Id)
    $duplicate = Invoke-StoreJson @('preview',$PriceFile)
    if ($duplicate.Id -ne $preview.Id) { throw 'Identical file created another batch.' }
    $before = Invoke-StoreJson @('inspect')
    Invoke-Checked docker @('compose','restart','postgres')
    Invoke-Checked docker @('compose','up','-d','--wait')
    $after = Invoke-StoreJson @('inspect')
    if (($before | ConvertTo-Json -Compress) -ne ($after | ConvertTo-Json -Compress)) { throw 'Database counts changed after container restart.' }
    $after | ConvertTo-Json
    Write-Host 'PASS: migrations twice, tests, import twice, file deduplication, container restart and persistence.'
}
finally {
    $env:SPORTSSTORE_TEST_ADMIN_CONNECTION = $null
    Pop-Location
}
