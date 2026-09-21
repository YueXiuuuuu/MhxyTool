Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PCap8 {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    public struct R { public int L, T, Rt, B; }
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]
$SIP = [System.Windows.Automation.SelectionItemPattern]

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
if (-not $win) { Write-Output 'WINDOW-NOT-FOUND'; exit 1 }
$handle = [System.IntPtr]$win.Current.NativeWindowHandle

function All-Elements { @($win.FindAll($TreeScope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) }

function Capture([System.IntPtr]$h, [string]$path) {
    $r = New-Object PCap8+R
    [PCap8]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [PCap8]::PrintWindow($h, $hdc, 3) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path); $bmp.Dispose()
}

function Selected-Nav {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::ListItem) {
            $p = $null
            if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$p) -and $p.Current.IsSelected) { return $el.Current.Name }
        }
    }
    return '?'
}

function Select-Nav([string]$label) {
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        foreach ($el in All-Elements) {
            if ($el.Current.ControlType -eq $CT::ListItem -and $el.Current.Name -eq $label) {
                $p = $null
                if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$p)) { $p.Select() }
                break
            }
        }
        Start-Sleep -Milliseconds 800
        if ((Selected-Nav) -eq $label) { return $true }
    }
    return $false
}

# 工具箱里所有工具的卡片名（新增工具时在这里补一行即可）
$script:ExpectedTools = @('示例计算器', '游戏资料查询', '五色灵尘计算器')

function Get-CardNames {
    $names = @()
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::Text -and $script:ExpectedTools -contains $el.Current.Name) {
            $names += $el.Current.Name
        }
    }
    return @($names | Sort-Object -Unique)
}

# 导航切换后等待界面稳定（UIA 树刷新有延迟，读太早会拿到上一屏的内容）
function Wait-Settle([int]$ms = 1500) { Start-Sleep -Milliseconds $ms }

# UIA 预热：首次枚举在新建的 UIA 客户端里可能拿到尚未就绪的树
All-Elements | Out-Null
Start-Sleep -Milliseconds 700

$ok = Select-Nav '首页'
Write-Output ("SELECT-HOME=" + $ok + " current=" + (Selected-Nav))
Wait-Settle
Capture $handle 'D:\MhxyTool\.tools\f1_home.png'
Write-Output ("HOME cards=" + ((Get-CardNames) -join ','))

# 首页应显示收藏置顶分组
$hasPinned = $false
foreach ($el in All-Elements) {
    if ($el.Current.ControlType -eq $CT::Text -and $el.Current.Name -like '*我的收藏*') { $hasPinned = $true; break }
}
Write-Output ("HOME-PINNED-FAVORITES=" + $hasPinned)

$ok = Select-Nav '设置'
Write-Output ("SELECT-SETTINGS=" + $ok + " current=" + (Selected-Nav))
Capture $handle 'D:\MhxyTool\.tools\f2_settings.png'

$ok = Select-Nav '查询'
Write-Output ("SELECT-QUERY=" + $ok + " current=" + (Selected-Nav))
Wait-Settle
Write-Output ("QUERY cards=" + ((Get-CardNames) -join ','))
Capture $handle 'D:\MhxyTool\.tools\f3_query.png'

Write-Output 'DONE'
