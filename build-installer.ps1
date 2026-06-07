# Build the Windows installer for AgentPet.
# Publishes the app + CLI (self-contained), merges the CLI into the app folder
# so AgentPetCLI.exe ships beside AgentPetApp.exe, then compiles the Inno Setup
# installer -> ReleasePackage\AgentPet-Setup.exe
# Usage: build-installer.ps1 [-Version 1.0.1]
param([string]$Version = "")
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$rid  = "win-x64"

$appPublish = Join-Path $root "AgentPetApp\bin\Release\net8.0-windows\$rid\publish"
$cliPublish = Join-Path $root "AgentPetCLI\bin\Release\net8.0\$rid\publish"

Write-Host "[1/4] Publishing AgentPetApp (self-contained, $rid)..." -ForegroundColor Cyan
dotnet publish "$root\AgentPetApp\AgentPetApp.csproj" `
    -c Release -r $rid --self-contained true /p:PublishSingleFile=false

Write-Host "[2/4] Publishing AgentPetCLI (self-contained single-file, $rid)..." -ForegroundColor Cyan
dotnet publish "$root\AgentPetCLI\AgentPetCLI.csproj" `
    -c Release -r $rid --self-contained true `
    /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true

Write-Host "[3/4] Copying CLI hook bridge into app folder..." -ForegroundColor Cyan
# Copy ONLY the single-file exe. Copying the CLI's runtime DLLs would clobber
# the app's WPF-compatible runtime and crash it (WindowsBase load failure).
Copy-Item (Join-Path $cliPublish "AgentPetCLI.exe") $appPublish -Force
Copy-Item (Join-Path $root "AgentPetCLI\AgentPetCodexHook.cmd") $appPublish -Force

Write-Host "[4/4] Compiling Inno Setup installer..." -ForegroundColor Cyan
$iscc = "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $iscc)) { $iscc = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe" }
if ($Version) {
    & $iscc "/DMyAppVersion=$Version" "$root\AgentPet.iss"
} else {
    & $iscc "$root\AgentPet.iss"
}

Write-Host ""
Write-Host "Done. Installer at: $root\ReleasePackage\AgentPet-Setup.exe" -ForegroundColor Green
