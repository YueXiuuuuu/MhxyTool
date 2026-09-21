Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PCap7 {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    public struct R { public int L, T, Rt, B; }
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]
$SIP = [System.Windows.Automation.SelectionItemPattern]
$IP = [System.Windows.Automation.InvokePattern]

function Capture([System.IntPtr]$handle, [string]$path) {
    $r = New-Object PCap7+R
    [PCap7]::GetWindowRect($handle, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [PCap7]::PrintWindow($handle, $hdc, 3) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path); $bmp.Dispose()
    Write-Output "CAPTURED $path"
}

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
$handle = [System.IntPtr]$win.Current.NativeWindowHandle
Write-Output ("PID=" + $win.Current.ProcessId)

function All-Elements { @($win.FindAll($TreeScope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) }

function Select-Nav([string]$label) {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::ListItem -and $el.Current.Name -eq $label) {
            $p = $null
            if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$p)) { $p.Select(); return $true }
        }
    }
    return $false
}

function Get-CardNames {
    $names = @()
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::Text) {
            $n = $el.Current.Name
            if ($n -eq '示例计算器' -or $n -eq '游戏资料查询') { $names += $n }
        }
    }
    return @($names | Sort-Object -Unique)
}

# 星星状态（★ = 已收藏）
function Get-StarState {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::Button) {
            if ($el.Current.Name -eq '★') { return 'STARRED' }
            if ($el.Current.Name -eq '☆') { return 'EMPTY' }
        }
    }
    return 'NO-STAR'
}

Select-Nav '首页' | Out-Null
Start-Sleep -Milliseconds 900
Write-Output ("HOME star-state=" + (Get-StarState))
Capture $handle 'D:\MhxyTool\.tools\p1_home_after_restart.png'

Select-Nav '我的收藏' | Out-Null
Start-Sleep -Milliseconds 900
$favCards = Get-CardNames
Write-Output ("FAVORITES after restart cards=" + ($favCards -join ','))
if ($favCards -contains '示例计算器') { Write-Output 'PERSISTENCE=PASS' } else { Write-Output 'PERSISTENCE=FAIL' }
Capture $handle 'D:\MhxyTool\.tools\p2_favorites_after_restart.png'

# 顺便验证“游戏资料查询”工具页也能打开（第二种工具类型）
Select-Nav '首页' | Out-Null
Start-Sleep -Milliseconds 600
$ok = $false
foreach ($el in All-Elements) {
    if ($el.Current.Name -eq '游戏资料查询') {
        $p = $null
        if ($el.TryGetCurrentPattern($IP::Pattern, [ref]$p)) { $p.Invoke(); $ok = $true; break }
    }
}
Write-Output ("OPEN-GAMEINFO=" + $ok)
Start-Sleep -Milliseconds 900
Capture $handle 'D:\MhxyTool\.tools\p3_gameinfo_tool.png'

Write-Output 'DONE'
