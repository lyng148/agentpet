# Build the Windows installer for AgentPet.
# Steps: publish the WPF app (self-contained) -> zip it into the installer payload -> build the installer exe.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$rid = "win-x64"
$appPublish = Join-Path $root "AgentPetApp\bin\Release\net8.0-windows\$rid\publish"
$payload    = Join-Path $root "AgentPetInstaller\Payload.zip"

Write-Host "[1/3] Publishing AgentPetApp (self-contained, $rid)..." -ForegroundColor Cyan
dotnet publish "$root\AgentPetApp\AgentPetApp.csproj" `
    -c Release -r $rid --self-contained true `
    /p:PublishSingleFile=false

Write-Host "[2/3] Packing payload -> $payload" -ForegroundColor Cyan
if (Test-Path $payload) { Remove-Item $payload -Force }
Compress-Archive -Path "$appPublish\*" -DestinationPath $payload -CompressionLevel Optimal

Write-Host "[3/3] Building installer exe..." -ForegroundColor Cyan
dotnet publish "$root\AgentPetInstaller\AgentPetInstaller.csproj" `
    -c Release -r $rid --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

$installer = Join-Path $root "AgentPetInstaller\bin\Release\net8.0\$rid\publish\AgentPetInstaller.exe"
Write-Host ""
Write-Host "Done. Installer at:" -ForegroundColor Green
Write-Host "  $installer" -ForegroundColor Green
