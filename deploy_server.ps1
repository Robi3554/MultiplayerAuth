# ═══════════════════════════════════════════════════════════════
#  Deploy Dedicated Server to VPS
#  Usage:  .\deploy_server.ps1
#  Or with options:
#    .\deploy_server.ps1 -SkipBuild    # skip Unity build, just upload
#    .\deploy_server.ps1 -Restart      # just restart the server (no upload)
# ═══════════════════════════════════════════════════════════════

param(
    [switch]$SkipBuild,
    [switch]$Restart
)

# ─── Configuration ────────────────────────────────────────────
$ServerUser       = "builder"
$ServerIP         = "193.226.15.26"
$ServerDest       = "/home/builder"
$RemoteDir        = "build_server"
$ExecutableName   = "MultiplayerAuthServer"

$LocalBuildDir    = "Builds\Server_Linux"
$TarFileName      = "build_server.tar.gz"
$LocalTarPath     = "Builds\$TarFileName"

# SSH/SCP  — uses your default SSH key. Add -i "path\to\key" if needed.
$SSHTarget        = "$ServerUser@$ServerIP"
# ──────────────────────────────────────────────────────────────

function Write-Step($msg) {
    Write-Host "`n═══ $msg ═══" -ForegroundColor Cyan
}

# ─── Just restart? ────────────────────────────────────────────
if ($Restart) {
    Write-Step "Restarting server"
    ssh $SSHTarget "cd $ServerDest/$RemoteDir && pkill -f $ExecutableName; sleep 1; nohup ./$ExecutableName -logFile server.log > /dev/null 2>&1 &"
    Write-Host "Server restarted!" -ForegroundColor Green
    Write-Host "View logs:  ssh $SSHTarget 'tail -f $ServerDest/$RemoteDir/server.log'"
    exit 0
}

# ─── Step 1: Build (unless skipped) ──────────────────────────
if (-not $SkipBuild) {
    Write-Step "1/5  Building Dedicated Server (Linux) via Unity"
    
    # Find Unity Editor — check common locations
    $unityExe = $null
    $searchPaths = @(
        "G:\Unity",
        "C:\Program Files\Unity\Hub\Editor",
        "D:\Unity",
        "C:\Unity"
    )
    
    foreach ($searchPath in $searchPaths) {
        if (Test-Path $searchPath) {
            $found = Get-ChildItem $searchPath -Directory | Sort-Object Name -Descending | Select-Object -First 1
            if ($found) {
                $candidate = Join-Path $found.FullName "Editor\Unity.exe"
                if (Test-Path $candidate) {
                    $unityExe = $candidate
                    break
                }
            }
        }
    }
    
    if (-not $unityExe) {
        Write-Host "ERROR: Unity Editor not found!" -ForegroundColor Red
        Write-Host "Set the path manually in deploy_server.ps1" -ForegroundColor Yellow
        exit 1
    }
    
    Write-Host "Using Unity: $unityExe"
    $projectPath = Split-Path $PSScriptRoot -Parent
    if (-not (Test-Path "$PSScriptRoot\Assets")) {
        $projectPath = $PSScriptRoot
    }
    
    $buildLog = "Builds\build_server.log"
    & $unityExe -quit -batchmode -projectPath $PSScriptRoot `
        -executeMethod GameBuilder.BuildLinuxServer `
        -logFile $buildLog
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Unity build failed! Check $buildLog" -ForegroundColor Red
        exit 1
    }
    Write-Host "Build complete." -ForegroundColor Green
} else {
    Write-Step "1/5  Skipping build (SkipBuild flag)"
}

# ─── Step 2: Create tar.gz ───────────────────────────────────
Write-Step "2/5  Creating archive"
if (Test-Path $LocalTarPath) { Remove-Item $LocalTarPath -Force }

# Use tar (built into Windows 10+)
Push-Location "Builds"
tar -czf $TarFileName -C . "Server_Linux"
Pop-Location

$sizeMB = [math]::Round((Get-Item $LocalTarPath).Length / 1MB, 1)
Write-Host "Archive: $LocalTarPath ($sizeMB MB)" -ForegroundColor Green

# ─── Step 3: Stop remote server ──────────────────────────────
Write-Step "3/5  Stopping remote server"
ssh $SSHTarget "pkill -f $ExecutableName 2>/dev/null; echo 'Server stopped.'"

# ─── Step 4: Upload & extract ────────────────────────────────
Write-Step "4/5  Uploading to $ServerIP"
ssh $SSHTarget "rm -rf $ServerDest/$RemoteDir $ServerDest/$TarFileName"
scp $LocalTarPath "${SSHTarget}:$ServerDest/$TarFileName"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Upload failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Extracting on server..."
ssh $SSHTarget "cd $ServerDest && tar -xzf $TarFileName && mv Server_Linux $RemoteDir && chmod +x $RemoteDir/$ExecutableName && rm $TarFileName"
Write-Host "Upload complete." -ForegroundColor Green

# ─── Step 5: Start server ────────────────────────────────────
Write-Step "5/5  Starting server"
ssh $SSHTarget "cd $ServerDest/$RemoteDir && nohup ./$ExecutableName -logFile server.log > /dev/null 2>&1 &"
Write-Host "Server started!" -ForegroundColor Green

# ─── Done ─────────────────────────────────────────────────────
Write-Host "`n════════════════════════════════════════" -ForegroundColor Green
Write-Host "  Deployment complete!" -ForegroundColor Green
Write-Host "════════════════════════════════════════" -ForegroundColor Green
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor Yellow
Write-Host "  View logs:     ssh $SSHTarget 'tail -f $ServerDest/$RemoteDir/server.log'"
Write-Host "  Stop server:   ssh $SSHTarget 'pkill -f $ExecutableName'"
Write-Host "  Restart:       .\deploy_server.ps1 -Restart"
Write-Host "  Upload only:   .\deploy_server.ps1 -SkipBuild"
