# 独立复算五色灵尘递推数列（不使用工具箱的 C# 实现，避免自己验证自己）
$need = @(0, 1, 2)
for ($n = 3; $n -le 10; $n++) {
    $need += 2 * $need[$n - 1] + $need[$n - 2]
}

$cumulative = 0
for ($n = 1; $n -le 10; $n++) {
    $cumulative += $need[$n]
    Write-Output ("L{0}: need={1} cumulative={2}" -f $n, $need[$n], $cumulative)
}

Write-Output ''
Write-Output '=== 成本核对（单价 50 万/个，金价 3000万 = 30 元）==='
$unitPrice = 50
$rmbPer30M = 30
$cum = 0
for ($n = 1; $n -le 10; $n++) {
    $cum += $need[$n]
    $unitWan = $need[$n] * $unitPrice
    $totalWan = $cum * $unitPrice
    $unitRmb = $unitWan * $rmbPer30M / 3000
    $totalRmb = $totalWan * $rmbPer30M / 3000
    Write-Output ("L{0}: unitCostWan={1} totalCostWan={2} unitRmb={3} totalRmb={4}" -f $n, $unitWan, $totalWan, $unitRmb, $totalRmb)
}
