$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $root 'manifest.json') -Raw | ConvertFrom-Json).version
$stage = Join-Path $root ('artifacts/OblivionTravel-' + $version + '-preview-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
$mod = Join-Path $stage 'OblivionTravel'
$client = Join-Path $mod 'client'
New-Item -ItemType Directory -Path $client -Force | Out-Null
# Explicit allowlist: never copy the build directory, SDK, source tree, or machine configuration wholesale.
foreach ($name in @('OblivionTravel.Client.dll','OblivionTravel.Core.dll')) {
    $source = Join-Path $root ('Client/bin/Release/net10.0/' + $name)
    if (!(Test-Path -LiteralPath $source)) { throw "Build first: missing $name" }
    Copy-Item -LiteralPath $source -Destination (Join-Path $client $name)
}
Copy-Item -LiteralPath (Join-Path $root 'manifest.json') -Destination $mod
Copy-Item -LiteralPath (Join-Path $root 'config.example.json') -Destination (Join-Path $mod 'config.json')
foreach ($name in @('README.md','NOTICE.md','ACCEPTANCE.md','SDK-REQUIREMENTS.md','VERIFICATION.md')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $mod
}
$hashes = Get-ChildItem -LiteralPath $mod -Recurse -File | Sort-Object FullName | ForEach-Object {
    [ordered]@{ Path=$_.FullName.Substring($stage.Length+1).Replace('\','/'); SHA256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
}
$hashes | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'SHA256.json') -Encoding utf8
$zip = $stage + '.zip'
Compress-Archive -LiteralPath $mod,(Join-Path $stage 'SHA256.json') -DestinationPath $zip
Write-Output "Preview package: $zip"
Write-Output 'Release gate: native runtime acceptance and default-save discovery persistence remain unverified. No installation/deployment was performed.'
