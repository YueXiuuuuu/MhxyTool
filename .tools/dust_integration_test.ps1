Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]
$SIP = [System.Windows.Automation.SelectionItemPattern]
$VP = [System.Windows.Automation.ValuePattern]
$IP = [System.Windows.Automation.InvokePattern]

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
if (-not $win) { Write-Output 'WINDOW-NOT-FOUND'; exit 1 }

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
function Set-Search([string]$text) {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::Edit) {
            $p = $null
            if ($el.TryGetCurrentPattern($VP::Pattern, [ref]$p)) { $p.SetValue($text); return $true }
        }
    }
    return $false
}
function Has-Tool([string]$name) {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::Button -and $el.Current.Name -eq $name) { return $true }
    }
    return $false
}

All-Elements | Out-Null
Start-Sleep -Milliseconds 700

# 回到首页（点卡片前先确保在首页）
Select-Nav '首页' | Out-Null
Start-Sleep -Milliseconds 700
if ((Has-Tool '五色灵尘计算器') -eq $false) { Write-Output 'NOTE: home card not found via Button role, will rely on search test' }

# 1) 搜索“灵尘”应命中
Set-Search '灵尘' | Out-Null
Start-Sleep -Milliseconds 900
$byName = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '五色灵尘计算器') { $byName = $true; break } }
Write-Output ("SEARCH-灵尘 命中=" + $byName)

# 2) 搜索“合成”（描述中的词）应命中
Set-Search '合成' | Out-Null
Start-Sleep -Milliseconds 900
$byDesc = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '五色灵尘计算器') { $byDesc = $true; break } }
Write-Output ("SEARCH-合成 命中=" + $byDesc)

# 3) 搜索“宝石计算”（分类名）应命中
Set-Search '宝石计算' | Out-Null
Start-Sleep -Milliseconds 900
$byCat = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '五色灵尘计算器') { $byCat = $true; break } }
Write-Output ("SEARCH-宝石计算 命中=" + $byCat)

# 清空搜索，进“计算”分类看归位
Set-Search '' | Out-Null
Start-Sleep -Milliseconds 600
Select-Nav '计算' | Out-Null
Start-Sleep -Milliseconds 900
$inCategory = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '五色灵尘计算器') { $inCategory = $true; break } }
Write-Output ("计算分类内含该工具=" + $inCategory)

# 该工具的卡片应显示“计算/宝石计算”这一分类文字
$hasSubCategory = $false
foreach ($el in All-Elements) { if ($el.Current.Name -eq '计算/宝石计算') { $hasSubCategory = $true; break } }
Write-Output ("卡片显示分类 计算/宝石计算=" + $hasSubCategory)

Write-Output 'DONE'
