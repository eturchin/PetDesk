param([Parameter(Mandatory)][int]$ProcessId, [ValidateRange(5,30)][int]$Seconds = 15, [string]$Label = 'background', [string]$OutputFile = '')
$ErrorActionPreference = 'Stop'
$sampleBefore = Get-Process -Id $ProcessId
if ($sampleBefore.ProcessName -ne 'PetDesk') { throw 'Choose the PetDesk process.' }
$startCpu = $sampleBefore.TotalProcessorTime.TotalSeconds
$watch = [Diagnostics.Stopwatch]::StartNew()
Start-Sleep -Seconds $Seconds
$sampleAfter = Get-Process -Id $ProcessId
$elapsed = $watch.Elapsed.TotalSeconds
$deltaCpu = $sampleAfter.TotalProcessorTime.TotalSeconds - $startCpu
$sample = [ordered]@{
    Label = $Label
    Utc = [DateTime]::UtcNow.ToString('o')
    ProcessId = $ProcessId
    DurationSeconds = [Math]::Round($elapsed, 3)
    LogicalProcessors = [Environment]::ProcessorCount
    CpuMachinePercent = [Math]::Round(100 * $deltaCpu / $elapsed / [Environment]::ProcessorCount, 3)
    CpuSingleCorePercent = [Math]::Round(100 * $deltaCpu / $elapsed, 3)
    WorkingSetMiB = [Math]::Round($sampleAfter.WorkingSet64 / 1MB, 2)
    PrivateCommitMiB = [Math]::Round($sampleAfter.PrivateMemorySize64 / 1MB, 2)
    Handles = $sampleAfter.HandleCount
    Threads = $sampleAfter.Threads.Count
}
$json = $sample | ConvertTo-Json
if ($OutputFile) { Set-Content -LiteralPath $OutputFile -Value $json -Encoding utf8 }
$json
