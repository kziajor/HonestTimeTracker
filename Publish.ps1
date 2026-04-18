param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$OutputDir = Join-Path $PSScriptRoot "Publish"
$ProjectPath = Join-Path $PSScriptRoot "src\HonestTimeTracker.Desktop\HonestTimeTracker.Desktop.csproj"

Write-Host "Czyszczenie katalogu Publish..." -ForegroundColor Cyan
if (Test-Path $OutputDir) {
    Remove-Item -Recurse -Force $OutputDir
}

Write-Host "Publikowanie aplikacji ($Configuration / $Runtime)..." -ForegroundColor Cyan

dotnet publish $ProjectPath `
    --configuration $Configuration `
    --runtime $Runtime `
    --self-contained false `
    --output $OutputDir `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Host "Błąd publikacji!" -ForegroundColor Red
    exit 1
}

Write-Host "Opublikowano do: $OutputDir" -ForegroundColor Green
