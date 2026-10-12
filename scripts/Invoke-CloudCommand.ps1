param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('MigrateDatabase', 'CopyMedia')]
    [string]$Action
)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$settingsPath = Join-Path $projectRoot 'App_Data/cloud/deployment-secrets.json'
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json -AsHashtable
if ($Action -eq 'MigrateDatabase' -and ![string]::IsNullOrWhiteSpace($settings['Migration__ConnectionString'])) {
    # The web runtime account deliberately has no schema-change permission.
    $settings['ConnectionStrings__DefaultConnection'] = $settings['Migration__ConnectionString']
}
$names = if ($Action -eq 'CopyMedia') {
    # Read the current local DB. Only storage credentials are overridden for media export.
    @('Storage__Provider','Storage__Supabase__Url','Storage__Supabase__Bucket','Storage__Supabase__ServiceRoleKey')
} else {
    @('ConnectionStrings__DefaultConnection','Database__Provider','Database__ServerVersion')
}
foreach ($name in $names) {
    if ([string]::IsNullOrWhiteSpace($settings[$name])) { throw "Complete $name in the private settings file first." }
}
if ($Action -eq 'MigrateDatabase') {
    if ($settings['ConnectionStrings__DefaultConnection'] -notmatch '(?i)SslMode\s*=\s*VerifyFull') { throw 'Cloud migrations require SslMode=VerifyFull and the Aiven CA certificate.' }
    if ($settings['ConnectionStrings__DefaultConnection'] -match '(?i)(localhost|127\.0\.0\.1)') { throw 'This command targets a cloud database; localhost is not accepted.' }
}
$names += @('ASPNETCORE_ENVIRONMENT','DataProtection__PersistToDatabase')
$before = @{}
Push-Location $projectRoot
try {
    foreach ($name in $names) { $before[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
    foreach ($name in $names) {
        if ($name -eq 'ASPNETCORE_ENVIRONMENT') { $value = 'Production' }
        elseif ($name -eq 'DataProtection__PersistToDatabase') { $value = 'false' }
        else { $value = $settings[$name] }
        [Environment]::SetEnvironmentVariable($name, $value, 'Process')
    }
    $flag = if ($Action -eq 'CopyMedia') { '--copy-media-to-cloud' } else { '--migrate-only' }
    & dotnet 'bin/Release/net10.0/Digital Krushi Officer.dll' $flag
    if ($LASTEXITCODE -ne 0) { throw 'Cloud operation failed. Review the error; source data was not deleted.' }
} finally {
    foreach ($name in $before.Keys) { [Environment]::SetEnvironmentVariable($name, $before[$name], 'Process') }
    Pop-Location
}
