$ErrorActionPreference = 'Stop'
$files = Get-ChildItem -LiteralPath $PSScriptRoot -Recurse -File -Force | Where-Object {
    $_.FullName.Substring($PSScriptRoot.Length) -notmatch '\\(\.git|bin|obj|inspection|artifacts|\.vs)\\'
}
foreach ($file in $files) {
    $relative = $file.FullName.Substring($PSScriptRoot.Length + 1)
    if ($file.Name -ne '.gitignore' -and $file.Extension -notin @('.cs','.csproj','.props','.ps1','.md','.json')) {
        throw "Unapproved source file type: $relative"
    }
    if ($relative -match '(?i)(^|\\)(\.env|credentials|server\.json|settings\.json|config\.json)$') {
        throw "Private/local configuration cannot be included: $relative"
    }
    $body = Get-Content -LiteralPath $file.FullName -Raw
    if ($body -match '(gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}|AKIA[A-Z0-9]{16}|-----BEGIN [A-Z ]*PRIVATE KEY-----)') {
        throw "Potential credential detected: $relative"
    }
}
$tracked = & git -C $PSScriptRoot ls-files
if ($LASTEXITCODE -ne 0) { throw 'Git source inventory unavailable.' }
foreach ($path in $tracked) {
    if ($path -match '(^|/)(inspection|artifacts|bin|obj)/|\.(dll|pdb|esp|esm|pak|sav|ess|zip)$') { throw "Forbidden tracked payload: $path" }
}
Write-Output "Source audit passed: $($files.Count) text files; no SDK/game binaries or detected credential patterns. Manual attribution/config review is also required before publication."
