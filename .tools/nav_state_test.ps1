Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes

$AE = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$TreeScope = [System.Windows.Automation.TreeScope]
$SIP = [System.Windows.Automation.SelectionItemPattern]
$VP = [System.Windows.Automation.ValuePattern]

$root = $AE::RootElement
$cond = New-Object System.Windows.Automation.PropertyCondition($AE::NameProperty, '梦幻工具箱')
$win = $root.FindFirst($TreeScope::Children, $cond)
if (-not $win) { Write-Output 'WINDOW-NOT-FOUND'; exit 1 }
Write-Output ("WINDOW-OK pid=" + $win.Current.ProcessId)

function All-Elements { @($win.FindAll($TreeScope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) }

function Nav-State {
    $result = @()
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::ListItem) {
            $p = $null
            if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$p)) {
                $result += ("{0}={1}" -f $el.Current.Name, $p.Current.IsSelected)
            }
        }
    }
    return ($result -join ' ')
}

function Title-Bar {
    # 顶栏标题：页面上第一个 Text 通常是顶栏
    return ''
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

function Select-Nav([string]$label) {
    foreach ($el in All-Elements) {
        if ($el.Current.ControlType -eq $CT::ListItem -and $el.Current.Name -eq $label) {
            $p = $null
            if ($el.TryGetCurrentPattern($SIP::Pattern, [ref]$p)) { $p.Select(); return $true }
        }
    }
    return $false
}

Write-Output ("INITIAL:      " + (Nav-State))

Select-Nav '计算' | Out-Null
Start-Sleep -Milliseconds 700
Write-Output ("AFTER-CLICK-计算: " + (Nav-State))

Set-Search '资料' | Out-Null
Start-Sleep -Milliseconds 1000
Write-Output ("AFTER-SEARCH: " + (Nav-State))

Set-Search '' | Out-Null
Start-Sleep -Milliseconds 700
Write-Output ("AFTER-CLEAR:  " + (Nav-State))

Select-Nav '我的收藏' | Out-Null
Start-Sleep -Milliseconds 700
Write-Output ("AFTER-收藏:    " + (Nav-State))

Write-Output 'DONE'
