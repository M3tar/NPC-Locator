[CmdletBinding()]
param(
    [string] $GamePath,
    [ValidateSet("Debug", "Release")]
    [string] $Configuration = "Release",
    [switch] $Install,
    [switch] $UpdateExisting
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "MultiplayerNpcLocator.csproj"

function Find-GamePath {
    param([string] $RequestedPath)

    $candidates = @()
    if ($RequestedPath) {
        $candidates += $RequestedPath
    }
    if (${env:ProgramFiles(x86)}) {
        $candidates += Join-Path ${env:ProgramFiles(x86)} "Steam\steamapps\common\Stardew Valley"
    }
    if ($env:ProgramFiles) {
        $candidates += Join-Path $env:ProgramFiles "Steam\steamapps\common\Stardew Valley"
    }

    foreach ($candidate in $candidates | Select-Object -Unique) {
        $gameAssembly = Join-Path $candidate "Stardew Valley.dll"
        $smapiAssembly = Join-Path $candidate "StardewModdingAPI.dll"
        if ((Test-Path $gameAssembly) -and (Test-Path $smapiAssembly)) {
            return (Resolve-Path $candidate).Path
        }
    }

    throw "Could not find Stardew Valley.dll and StardewModdingAPI.dll. Pass the game folder explicitly with -GamePath, for example: -GamePath 'D:\SteamLibrary\steamapps\common\Stardew Valley'."
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET SDK was not found. Install the .NET 6 SDK (x64), reopen PowerShell, and try again."
}

$sdkList = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0) {
    throw "dotnet --list-sdks failed with exit code $LASTEXITCODE."
}
if (-not ($sdkList | Where-Object { $_ -match '^6\.' })) {
    throw ".NET 6 SDK was not found. Install the .NET 6 SDK (x64), reopen PowerShell, and try again."
}

$ResolvedGamePath = Find-GamePath -RequestedPath $GamePath
$GameAssembly = Join-Path $ResolvedGamePath "Stardew Valley.dll"
$SmapiAssembly = Join-Path $ResolvedGamePath "StardewModdingAPI.dll"
$GameVersion = (Get-Item $GameAssembly).VersionInfo.FileVersion
$SmapiVersion = (Get-Item $SmapiAssembly).VersionInfo.FileVersion

Write-Host "Project: $ProjectRoot"
Write-Host "Game path: $ResolvedGamePath"
Write-Host "Stardew Valley assembly version: $GameVersion"
Write-Host "SMAPI assembly version: $SmapiVersion"
Write-Host "Configuration: $Configuration"

& dotnet restore $ProjectFile "-p:GamePath=$ResolvedGamePath"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed with exit code $LASTEXITCODE."
}

& dotnet build $ProjectFile --configuration $Configuration --no-restore "-p:GamePath=$ResolvedGamePath"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$OutputPath = Join-Path $ProjectRoot "bin\$Configuration\net6.0"
$ModAssembly = Join-Path $OutputPath "MultiplayerNpcLocator.dll"
if (-not (Test-Path $ModAssembly)) {
    throw "The build reported success, but the expected mod assembly was not found at '$ModAssembly'."
}

Write-Host "Build succeeded: $ModAssembly"
$ReleaseZip = Get-ChildItem (Join-Path $ProjectRoot "bin") -Recurse -Filter "*.zip" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($ReleaseZip) {
    Write-Host "Release package: $($ReleaseZip.FullName)"
}

if ($Install) {
    $ModsPath = Join-Path $ResolvedGamePath "Mods"
    $TargetPath = Join-Path $ModsPath "MultiplayerNpcLocator"
    if (-not (Test-Path $ModsPath)) {
        throw "The SMAPI Mods folder was not found at '$ModsPath'. Run the SMAPI installer first."
    }
    if (Test-Path $TargetPath) {
        if (-not $UpdateExisting) {
            throw "Install target '$TargetPath' already exists. Use -UpdateExisting to update this specific mod after its manifest is verified."
        }

        $InstalledManifestPath = Join-Path $TargetPath "manifest.json"
        if (-not (Test-Path $InstalledManifestPath)) {
            throw "Refusing to update '$TargetPath' because it has no manifest.json."
        }
        $InstalledManifest = Get-Content $InstalledManifestPath -Raw | ConvertFrom-Json
        if ($InstalledManifest.UniqueID -ne "Mercury.MultiplayerNpcLocator") {
            throw "Refusing to update '$TargetPath' because its UniqueID is '$($InstalledManifest.UniqueID)'."
        }
    }
    else {
        New-Item -ItemType Directory -Path $TargetPath | Out-Null
    }

    Copy-Item $ModAssembly $TargetPath -Force
    $Symbols = Join-Path $OutputPath "MultiplayerNpcLocator.pdb"
    if (Test-Path $Symbols) {
        Copy-Item $Symbols $TargetPath -Force
    }
    Copy-Item (Join-Path $ProjectRoot "manifest.json") $TargetPath -Force

    $SourceI18n = Join-Path $ProjectRoot "i18n"
    if (Test-Path $SourceI18n) {
        $TargetI18n = Join-Path $TargetPath "i18n"
        if (-not (Test-Path $TargetI18n)) {
            New-Item -ItemType Directory -Path $TargetI18n | Out-Null
        }
        Get-ChildItem $SourceI18n -File | Copy-Item -Destination $TargetI18n -Force
    }
    Write-Host "Installed or updated validation build at: $TargetPath"
}

Write-Host "Build preparation is complete. See the phase validation guide in the docs folder for the in-game checks."
