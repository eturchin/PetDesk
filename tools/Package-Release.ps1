param([string]$PackageCache = '', [switch]$Offline, [string]$ISCCPath = '')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
[xml]$versionFile = Get-Content -LiteralPath (Join-Path $root 'Version.props') -Raw
$version = [string]$versionFile.Project.PropertyGroup.Version
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Version.props must contain a stable three-part version.' }
if (!$ISCCPath) { $ISCCPath = Join-Path $root 'artifacts/tools/InnoSetup/ISCC.exe' }
if (!(Test-Path -LiteralPath $ISCCPath -PathType Leaf)) {
    $command = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($command) { $ISCCPath = $command.Source }
    else {
        foreach ($base in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
            if (!$base) { continue }
            foreach ($directory in @('Inno Setup 7', 'Inno Setup 6')) {
                $candidate = Join-Path $base "$directory/ISCC.exe"
                if (Test-Path -LiteralPath $candidate) { $ISCCPath = $candidate; break }
            }
            if (Test-Path -LiteralPath $ISCCPath -PathType Leaf) { break }
        }
    }
}
if (!(Test-Path -LiteralPath $ISCCPath -PathType Leaf)) { throw 'Pass -ISCCPath pointing to the Inno Setup compiler.' }
$output = Join-Path $root "artifacts/Releases/$version"
$payload = Join-Path $root ('artifacts/staging/' + [Guid]::NewGuid().ToString('N') + '/PetDesk')
& (Join-Path $PSScriptRoot 'Build.ps1') -PackageCache $PackageCache -Offline:$Offline -PublishDir $payload
New-Item -ItemType Directory -Path $output -Force | Out-Null
foreach ($required in @('PetDesk.exe','coreclr.dll','PetDesk.runtimeconfig.json','Assets/BuiltIn/mochi-cat/pet.json','Assets/BuiltIn/pixel-fox/pet.json','LICENSE')) {
    if (!(Test-Path -LiteralPath (Join-Path $payload $required))) { throw "Missing payload file: $required" }
}
& $ISCCPath "/DAppVersion=$version" "/DSourceDir=$payload" "/DOutputDir=$output" (Join-Path $root 'installer/PetDesk.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }
$zip = Join-Path $output "PetDesk-Portable-$version-win-x64.zip"
Compress-Archive -LiteralPath $payload -DestinationPath $zip -CompressionLevel Optimal -Force
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [IO.Compression.ZipFile]::OpenRead($zip)
try {
    $names = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\','/') })
    foreach ($entry in @('PetDesk/PetDesk.exe','PetDesk/coreclr.dll','PetDesk/Assets/BuiltIn/mochi-cat/spritesheet.png','PetDesk/Assets/BuiltIn/pixel-fox/spritesheet.png','PetDesk/README.md','PetDesk/README.ru.md','PetDesk/NOTICE.md','PetDesk/Docs/validation.md','PetDesk/Docs/screenshots/dashboard-en.jpg','PetDesk/Docs/screenshots/dashboard-ru.jpg','PetDesk/Docs/screenshots/settings-en.jpg')) { if ($names -notcontains $entry) { throw "Missing ZIP entry: $entry" } }
    if ($names | Where-Object { $_ -match '(?i)/Lycien/|(^|/)settings\.json$|/Logs/' }) { throw 'Private artwork or user data found in ZIP.' }
} finally { $archive.Dispose() }
$files = @("PetDesk-Setup-$version-win-x64.exe", "PetDesk-Portable-$version-win-x64.zip")
$checksums = foreach ($file in $files) { $hash = (Get-FileHash -LiteralPath (Join-Path $output $file) -Algorithm SHA256).Hash.ToLowerInvariant(); "$hash  $file" }
$checksums | Set-Content -LiteralPath (Join-Path $output 'SHA256SUMS.txt') -Encoding ascii
foreach ($line in $checksums) { $parts = $line -split '  ',2; if ((Get-FileHash -LiteralPath (Join-Path $output $parts[1])).Hash.ToLowerInvariant() -ne $parts[0]) { throw 'Checksum verification failed.' } }
[ordered]@{
    Version = $version
    Payload = [IO.Path]::GetRelativePath($root, $payload).Replace('\','/')
    Output = [IO.Path]::GetRelativePath($root, $output).Replace('\','/')
    PrivateArtworkIncluded = $false
    ZipVerified = $true
    ChecksumsVerified = $true
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'build-manifest.json') -Encoding utf8
Get-ChildItem -LiteralPath $output -File | Select-Object Name,Length
