Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class PCapB {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr dc, uint flags);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out R r);
    public struct R { public int L, T, Rt, B; }
}
"@

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]
$IP = [System.Windows.Automation.InvokePattern]
$VP = [System.Windows.Automation.ValuePattern]

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
if (-not $win) { Write-Output 'WINDOW-NOT-FOUND'; exit 1 }
$handle = [System.IntPtr]$win.Current.NativeWindowHandle
Write-Output ("PID=" + $win.Current.ProcessId)

function All-Elements { @($win.FindAll($TreeScope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) }
function Text-Set { @(All-Elements | Where-Object { $_.Current.ControlType -eq $CT::Text } | ForEach-Object { $_.Current.Name }) }
function Edit-Boxes { @(All-Elements | Where-Object { $_.Current.ControlType -eq $CT::Edit }) }

function Capture([System.IntPtr]$h, [string]$path) {
    $r = New-Object PCapB+R
    [PCapB]::GetWindowRect($h, [ref]$r) | Out-Null
    $bmp = New-Object System.Drawing.Bitmap(($r.Rt - $r.L), ($r.B - $r.T))
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $hdc = $g.GetHdc()
    [PCapB]::PrintWindow($h, $hdc, 3) | Out-Null
    $g.ReleaseHdc($hdc); $g.Dispose()
    $bmp.Save($path); $bmp.Dispose()
}

All-Elements | Out-Null
Start-Sleep -Milliseconds 700

# 先回到首页。注意：工具页停留时侧边栏仍高亮“首页”，而 UIA 对已选中项调 Select()
# 不会触发导航，所以先切到“设置”再切回“首页”，保证选择一定发生变化。
$SIP = [System.Windows.Automation.SelectionItemPattern]
function Select-NavItem([string]$label) {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::ListItem -and $el.Current.Name -eq $label) {
            $np = $null
            if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$np)) { $np.Select(); return $true }
        }
    }
    return $false
}
Select-NavItem '设置' | Out-Null
Start-Sleep -Milliseconds 1000
Select-NavItem '首页' | Out-Null
Start-Sleep -Milliseconds 1500

# ---- 1. 打开“五色灵尘计算器” ----
$card = $null
foreach ($el in All-Elements) { if ($el.Current.Name -eq '五色灵尘计算器') { $card = $el; break } }
if (-not $card) { Write-Output 'CARD-NOT-FOUND'; exit 1 }
$p = $null
if ($card.TryGetCurrentPattern($IP::Pattern, [ref]$p)) { $p.Invoke() }
Start-Sleep -Milliseconds 1200
Write-Output 'STEP1 opened tool page'
Capture $handle 'D:\MhxyTool\.tools\d1_opened.png'

# ---- 2. 无输入时应已显示数量 ----
$texts = Text-Set
$checks = @{
    '2,378' = '10级所需一级灵尘'
    '4,059' = '10级累计数量'
    '1,450' = '5级单级成本(万)'   # 单价未输入时不应出现，这里先探针
}
Write-Output ("STEP2 empty-input: has 2,378 = " + ($texts -contains '2,378') + " ; has 4,059 = " + ($texts -contains '4,059'))
Write-Output ("STEP2 empty-input: 成本列占位符 '—' 数量 = " + (@($texts | Where-Object { $_ -eq '—' }).Count))

# ---- 3. 输入单价 50（万） ----
$boxes = Edit-Boxes
Write-Output ("TEXTBOX-COUNT=" + $boxes.Count)
if ($boxes.Count -lt 2) { Write-Output 'TEXTBOX-MISSING'; exit 1 }
$pp = $null
if ($boxes[0].TryGetCurrentPattern($VP::Pattern, [ref]$pp)) { $pp.SetValue('50') }
Start-Sleep -Milliseconds 900
Capture $handle 'D:\MhxyTool\.tools\d2_unit_price.png'

$texts = Text-Set
Write-Output ("STEP3 unit=50: 118,900(10级单级万)=" + ($texts -contains '118,900') + " 202,950(10级综合万)=" + ($texts -contains '202,950') + " 1,450(5级单级万)=" + ($texts -contains '1,450'))

# ---- 4. 输入金价 30（元 / 3000万） ----
if ($boxes[1].TryGetCurrentPattern($VP::Pattern, [ref]$pp)) { $pp.SetValue('30') }
Start-Sleep -Milliseconds 900
Capture $handle 'D:\MhxyTool\.tools\d3_gold_price.png'

$texts = Text-Set
Write-Output ("STEP4 gold=30: 1,189.00(10级单级元)=" + ($texts -contains '1,189.00') + " 2,029.50(10级综合元)=" + ($texts -contains '2,029.50') + " 14.50(5级单级元)=" + ($texts -contains '14.50'))

# ---- 5. 非法输入 ----
if ($boxes[0].TryGetCurrentPattern($VP::Pattern, [ref]$pp)) { $pp.SetValue('abc') }
Start-Sleep -Milliseconds 800
$texts = Text-Set
Write-Output ("STEP5 invalid: 错误提示显示=" + ($texts -contains '请输入数字，例如 50（单位：万）'))
Capture $handle 'D:\MhxyTool\.tools\d4_invalid_input.png'

Write-Output 'DONE'
