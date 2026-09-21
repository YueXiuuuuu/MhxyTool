Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PCapA {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    public struct R { public int L, T, Rt, B; }
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
if (-not $win) { Write-Output 'WINDOW-NOT-FOUND'; exit 1 }
$handle = [System.IntPtr]$win.Current.NativeWindowHandle
Write-Output ("PID=" + $win.Current.ProcessId)

function All-Elements { @($win.FindAll($TreeScope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) }

function Capture([System.IntPtr]$h, [string]$path) {
    $r = New-Object PCapA+R
    [PCapA]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [PCapA]::PrintWindow($h, $hdc, 3) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path); $bmp.Dispose()
}

All-Elements | Out-Null
Start-Sleep -Milliseconds 700

Write-Output '=== SIDEBAR NAV ITEMS ==='
foreach ($el in All-Elements) {
    if ($el.Current.ControlType -eq $CT::ListItem) {
        Write-Output ("  " + $el.Current.Name)
    }
}

Write-Output ''
Write-Output '=== HOME SECTION TITLES & CARDS ==='
foreach ($el in All-Elements) {
    if ($el.Current.ControlType -eq $CT::Text) {
        $n = $el.Current.Name
        if ($n -match '\(\d+\)$' -or $n -eq '宝石合成计算器') {
            Write-Output ("  " + $n)
        }
    }
}

Capture $handle 'D:\MhxyTool\.tools\v1_new_tool_home.png'

$hasNewTool = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '宝石合成计算器') { $hasNewTool = $true; break } }
$hasNewCategory = $false
foreach ($el in All-Elements) {
    if ($el.Current.ControlType -eq $CT::ListItem -and $el.Current.Name -eq '数据') { $hasNewCategory = $true; break }
}

Write-Output ''
if ($hasNewTool) { Write-Output 'AUTO-REGISTER-NEW-TOOL=PASS' } else { Write-Output 'AUTO-REGISTER-NEW-TOOL=FAIL' }
if ($hasNewCategory) { Write-Output 'AUTO-NAV-NEW-CATEGORY=PASS' } else { Write-Output 'AUTO-NAV-NEW-CATEGORY=FAIL' }

Write-Output 'DONE'
