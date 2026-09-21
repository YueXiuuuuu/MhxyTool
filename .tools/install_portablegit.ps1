$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$url = 'https://github.com/git-for-windows/git/releases/download/v2.55.0.windows.5/PortableGit-2.55.0.5-64-bit.7z.exe'
$toolsDir = 'D:\MhxyTool\.tools'
$installer = Join-Path $toolsDir 'PortableGit-installer.exe'
$target = Join-Path $toolsDir 'portablegit'

if (Test-Path $target) { Remove-Item -Recurse -Force $target }

Write-Output '下载中（约 56 MB）...'
$sw = [System.Diagnostics.Stopwatch]::StartNew()
Invoke-WebRequest -Uri $url -OutFile $installer -TimeoutSec 600 -UseBasicParsing
$sw.Stop()
$sizeMB = [math]::Round((Get-Item $installer).Length / 1MB, 1)
Write-Output ("下载完成: {0} MB，用时 {1} 秒" -f $sizeMB, [math]::Round($sw.Elapsed.TotalSeconds, 1))

Write-Output '解压中...'
$proc = Start-Process -FilePath $installer -ArgumentList @("-o`"$target`"", '-y') -Wait -PassThru -NoNewWindow
Write-Output ("解压器退出码: " + $proc.ExitCode)

$gitExe = Join-Path $target 'cmd\git.exe'
if (Test-Path $gitExe) {
    Write-Output ("GIT-EXE: " + $gitExe)
    & $gitExe --version
} else {
    Write-Output 'GIT-EXE-NOT-FOUND'
    Get-ChildItem $target | Select-Object -First 20 -ExpandProperty Name
}

Remove-Item -Force $installer -ErrorAction SilentlyContinue
Write-Output 'DONE'
