param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'src\Keyvora\Keyvora.sln'

Write-Host "Building Keyvora ($Configuration)..." -ForegroundColor Cyan

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution -c $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet test (Join-Path $root 'tests\Keyvora.Desktop.Tests\Keyvora.Desktop.Tests.csproj') -c $Configuration --no-build
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Build completed successfully." -ForegroundColor Green
