param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $root
try {
    $files = @(& git -c core.quotepath=false ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect repository files.' }

    $forbidden = '(^|/)(artifacts|bin|obj|\.idea|\.dotnet|\.vs|TestResults)(/|$)|^assets/lycien/'
    $personalPath = '(?i)[A-Z]:[\\/]+(?:Users|Portfolio)[\\/]|/(?:home|Users)/[a-zA-Z0-9_.-]+/'
    $credential = '\b(?:gh[pousr]_[A-Za-z0-9_]{30,}|github_pat_[A-Za-z0-9_]{30,})\b|-----BEGIN (?:RSA |OPENSSH )?PRIVATE KEY-----'
    $binaryExtensions = @('.png', '.jpg', '.ico')
    $privateImage = Join-Path $root 'assets/lycien/spritesheet.png'
    $privateHash = if (Test-Path -LiteralPath $privateImage) { (Get-FileHash -LiteralPath $privateImage).Hash } else { $null }

    foreach ($file in $files) {
        if ($file -match $forbidden) { throw "Local-only path included in repository: $file" }
        if (!(Test-Path -LiteralPath $file -PathType Leaf)) { continue }
        if ([IO.Path]::GetExtension($file) -in $binaryExtensions) {
            if ($privateHash -and (Get-FileHash -LiteralPath $file).Hash -eq $privateHash) {
                throw "Private image included in repository: $file"
            }
            continue
        }

        $content = [IO.File]::ReadAllText((Join-Path $root $file))
        if ($content -match $personalPath) { throw "Personal absolute path found in $file" }
        if ($content -match $credential) { throw "Credential-shaped content found in $file" }
    }

    Write-Output "Checked $($files.Count) repository files: no local data, private artwork or personal paths."
} finally {
    Pop-Location
}
