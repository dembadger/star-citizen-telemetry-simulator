param([Parameter(Mandatory)][string]$Executable)
$ErrorActionPreference = 'Stop'
dotnet build tests/StreamProbe -c Release
if ($LASTEXITCODE -ne 0) { throw 'Client build failed' }
$sim = Start-Process -FilePath $Executable -PassThru
try {
    dotnet run --no-build --project tests/StreamProbe -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Simulator stream check failed' }
} finally {
    if (!$sim.HasExited) { Stop-Process -Id $sim.Id }
}
