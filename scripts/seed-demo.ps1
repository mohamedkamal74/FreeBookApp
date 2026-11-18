# Loads TestData.sql into the SQL Server container started by docker compose.
# Use after `docker compose up -d` to populate the library with demo data.

$ErrorActionPreference = 'Stop'
Set-Location (Split-Path -Parent $PSScriptRoot)

$password = 'Bookify!Passw0rd'
$envFile = Join-Path (Get-Location) '.env'
if (Test-Path $envFile) {
    foreach ($line in Get-Content $envFile) {
        if ($line -match '^\s*MSSQL_SA_PASSWORD\s*=\s*(.+?)\s*$') {
            $password = $Matches[1].Trim('"', "'")
        }
    }
}

$dbContainer = 'db'
$status = docker compose ps -q $dbContainer 2>$null
if (-not $status) {
    Write-Error 'SQL Server container is not running. Start the stack with: docker compose up -d'
}

$remotePath = '/tmp/BookifyTestData.sql'
$sqlcmd = '/opt/mssql-tools18/bin/sqlcmd'

try {
    docker compose cp TestData.sql "${dbContainer}:$remotePath" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "docker compose cp failed with exit code $LASTEXITCODE"
    }
    docker compose exec $dbContainer $sqlcmd `
        -S localhost -U sa -P $password -d BookDb -C -f 65001 -i $remotePath | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "sqlcmd failed with exit code $LASTEXITCODE"
    }
    Write-Host 'Demo data loaded successfully.'
}
finally {
    docker compose exec -u root $dbContainer rm -f $remotePath 2>&1 | Out-Null
}

exit 0
