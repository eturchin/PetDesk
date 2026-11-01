param(
    [string]$PackageCache = '',
    [switch]$Offline,
    [string]$PublishDir = ''
)

$ErrorActionPreference = 'Stop'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBuildEnableWorkloadResolver = 'false'
if ($PackageCache) { $env:NUGET_PACKAGES = $PackageCache }

$projectRoot = Split-Path -Parent $PSScriptRoot
$stagingRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot 'artifacts/staging')) + [IO.Path]::DirectorySeparatorChar
if (!$PublishDir) { $PublishDir = Join-Path $stagingRoot 'build/PetDesk' }
$PublishDir = [IO.Path]::GetFullPath($PublishDir)

# Validate the exact generated output directory before replacing it.
if (!$PublishDir.StartsWith($stagingRoot, [StringComparison]::OrdinalIgnoreCase) -or
    [IO.Path]::GetFileName($PublishDir) -ne 'PetDesk') {
    throw 'PublishDir must be a PetDesk folder below this project artifacts/staging directory.'
}
$cursor = [IO.DirectoryInfo]::new($PublishDir)
while ($cursor -and $cursor.FullName.Length -ge $stagingRoot.TrimEnd('\').Length) {
    if ($cursor.Exists -and ($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw 'Staging links and junctions are not allowed.'
    }
    $cursor = $cursor.Parent
}

Push-Location -LiteralPath $projectRoot
try {
    $restoreOptions = @('--locked-mode', '-m:1', '-nr:false')
    if ($Offline) { $restoreOptions += @('--ignore-failed-sources', '-p:NuGetAudit=false') }
    dotnet restore PetDesk.slnx @restoreOptions
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }

    dotnet format whitespace PetDesk.slnx --verify-no-changes --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw 'Formatting check failed. Run dotnet format whitespace PetDesk.slnx.' }

    dotnet build PetDesk.slnx -c Release --no-restore -m:1 -nr:false -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    dotnet test PetDesk.slnx -c Release --no-build --no-restore -m:1 -nr:false `
        --logger 'trx;LogFilePrefix=Release' --results-directory artifacts/TestResults
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }

    if (Test-Path -LiteralPath $PublishDir) { Remove-Item -LiteralPath $PublishDir -Recurse -Force }
    $publishOptions = @('-p:RestoreLockedMode=true')
    if ($Offline) { $publishOptions += @('--ignore-failed-sources', '-p:NuGetAudit=false') }
    dotnet publish src/PetDesk.App/PetDesk.App.csproj -c Release -p:PublishProfile=Portable `
        -o $PublishDir -m:1 -nr:false -p:UseSharedCompilation=false @publishOptions
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    if (Test-Path -LiteralPath (Join-Path $PublishDir 'Assets/Lycien')) {
        throw 'Private artwork found in a redistributable payload.'
    }

    Copy-Item -LiteralPath 'README.md','README.ru.md','LICENSE','NOTICE.md','THIRD-PARTY-NOTICES.md' -Destination $PublishDir -Force
    $docsTarget = Join-Path $PublishDir 'Docs'
    New-Item -ItemType Directory -Path $docsTarget -Force | Out-Null
    Copy-Item -LiteralPath 'docs/pet-format.md','docs/architecture.md','docs/release.md','docs/validation.md' -Destination $docsTarget -Force
    Copy-Item -LiteralPath 'docs/screenshots' -Destination $docsTarget -Recurse -Force
} finally {
    Pop-Location
}
