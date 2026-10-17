param(
    [Parameter(Mandatory=$true)][string]$SdkDirectory,
    [switch]$Offline,
    [switch]$Package
)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $sdkPath = (Resolve-Path -LiteralPath $SdkDirectory).Path
    $restoreArgs = @()
    if ($Offline) { $restoreArgs = @('--ignore-failed-sources', '-p:NuGetAudit=false') }
    & dotnet restore Client/OblivionTravel.Client.csproj "-p:ReadyMClientDir=$sdkPath" @restoreArgs --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Client restore failed.' }
    & dotnet build Client/OblivionTravel.Client.csproj -c Release "-p:ReadyMClientDir=$sdkPath" --no-restore --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Client build failed.' }
    & dotnet run --project Tests/OblivionTravel.Tests.csproj -c Release --nologo
    if ($LASTEXITCODE -ne 0) { throw 'Automated checks failed.' }
    & ./Audit-Source.ps1
    if ($Package) { & ./Package.ps1 }
} finally { Pop-Location }
