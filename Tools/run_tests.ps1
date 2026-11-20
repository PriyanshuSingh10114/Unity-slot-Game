$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$dllPath = Join-Path $scriptDir "SlotGameCore.dll"

if (-not (Test-Path $dllPath)) {
    Write-Error "Could not find SlotGameCore.dll at $dllPath"
    exit 1
}

$bytes = [System.IO.File]::ReadAllBytes($dllPath)
$assembly = [System.Reflection.Assembly]::Load($bytes)

Write-Host "=================================================="
Write-Host "   SLOT MACHINE AUTOMATED UNIT TEST SUITE         "
Write-Host "   Assembly: SlotGameCore.dll                     "
Write-Host "=================================================="

$passed = 0
$failed = 0

function Run-Test($name, [scriptblock]$block) {
    try {
        & $block
        Write-Host "[PASS] $name" -ForegroundColor Green
        $script:passed++
    } catch {
        Write-Host "[FAIL] ${name}: $($_.Exception.Message)" -ForegroundColor Red
        $script:failed++
    }
}

function Assert-Equal($expected, $actual, $msg = "") {
    if ($expected -ne $actual) {
        throw "Expected '$expected', but got '$actual'. $msg"
    }
}

function Assert-True($condition, $msg = "Condition was false") {
    if (-not $condition) {
        throw $msg
    }
}

# 1. WALLET TESTS
Run-Test "Wallet: InitialBalance" {
    $wallet = New-Object SlotGame.Core.SlotWallet(1000)
    Assert-Equal 1000 $wallet.CurrentBalance
}

Run-Test "Wallet: NegativeInitialBalanceThrows" {
    $threw = $false
    try { New-Object SlotGame.Core.SlotWallet(-50) } catch { $threw = $true }
    Assert-True $threw "Expected exception on negative balance"
}

Run-Test "Wallet: TryDeductBetValid" {
    $wallet = New-Object SlotGame.Core.SlotWallet(500)
    $ok = $wallet.TryDeductBet(50)
    Assert-True $ok
    Assert-Equal 450 $wallet.CurrentBalance
}

Run-Test "Wallet: TryDeductBetInsufficient" {
    $wallet = New-Object SlotGame.Core.SlotWallet(25)
    $ok = $wallet.TryDeductBet(50)
    Assert-True (-not $ok)
    Assert-Equal 25 $wallet.CurrentBalance
}

Run-Test "Wallet: TryDeductBetZeroOrNegative" {
    $wallet = New-Object SlotGame.Core.SlotWallet(100)
    Assert-True (-not $wallet.TryDeductBet(0))
    Assert-True (-not $wallet.TryDeductBet(-25))
    Assert-Equal 100 $wallet.CurrentBalance
}

Run-Test "Wallet: CreditWinPositive" {
    $wallet = New-Object SlotGame.Core.SlotWallet(200)
    $wallet.CreditWin(150)
    Assert-Equal 350 $wallet.CurrentBalance
}

Run-Test "Wallet: CreditWinZeroPreserves" {
    $wallet = New-Object SlotGame.Core.SlotWallet(200)
    $wallet.CreditWin(0)
    Assert-Equal 200 $wallet.CurrentBalance
}

Run-Test "Wallet: BalanceNeverNegative" {
    $wallet = New-Object SlotGame.Core.SlotWallet(100)
    Assert-True ($wallet.TryDeductBet(60))
    Assert-True (-not $wallet.TryDeductBet(60))
    Assert-True ($wallet.TryDeductBet(40))
    Assert-True (-not $wallet.TryDeductBet(1))
    Assert-Equal 0 $wallet.CurrentBalance
    Assert-True (-not $wallet.CanAfford(1))
}

# 2. WIN EVALUATOR TESTS
Run-Test "WinEvaluator: 3x Seven Wins" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    $outcome = [int[]]@(0, 0, 0)
    $isWin = $evaluator.EvaluateWin($outcome, [ref]$winningSym)
    Assert-True $isWin
    Assert-Equal 0 $winningSym
}

Run-Test "WinEvaluator: 3x Cherry Wins" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    $outcome = [int[]]@(1, 1, 1)
    $isWin = $evaluator.EvaluateWin($outcome, [ref]$winningSym)
    Assert-True $isWin
    Assert-Equal 1 $winningSym
}

Run-Test "WinEvaluator: 3x Bell Wins" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    $outcome = [int[]]@(2, 2, 2)
    $isWin = $evaluator.EvaluateWin($outcome, [ref]$winningSym)
    Assert-True $isWin
    Assert-Equal 2 $winningSym
}

Run-Test "WinEvaluator: 3x Bar Wins" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    $outcome = [int[]]@(3, 3, 3)
    $isWin = $evaluator.EvaluateWin($outcome, [ref]$winningSym)
    Assert-True $isWin
    Assert-Equal 3 $winningSym
}

Run-Test "WinEvaluator: PartialMatchesLoss" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    Assert-True (-not $evaluator.EvaluateWin([int[]]@(0, 0, 1), [ref]$winningSym))
    Assert-Equal -1 $winningSym
    Assert-True (-not $evaluator.EvaluateWin([int[]]@(1, 2, 1), [ref]$winningSym))
    Assert-Equal -1 $winningSym
    Assert-True (-not $evaluator.EvaluateWin([int[]]@(3, 2, 2), [ref]$winningSym))
    Assert-Equal -1 $winningSym
}

Run-Test "WinEvaluator: AllDifferentLoss" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    Assert-True (-not $evaluator.EvaluateWin([int[]]@(0, 1, 2), [ref]$winningSym))
    Assert-Equal -1 $winningSym
}

Run-Test "WinEvaluator: NullOrEmptySafe" {
    $evaluator = New-Object SlotGame.Payouts.SlotWinEvaluator
    $winningSym = -1
    Assert-True (-not $evaluator.EvaluateWin($null, [ref]$winningSym))
    Assert-True (-not $evaluator.EvaluateWin([int[]]@(), [ref]$winningSym))
}

# 3. PAYOUT CALCULATOR TESTS
Run-Test "Payout: Seven 25x Multiplier" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    $calc.SetMultiplier(0, 25)
    Assert-Equal 250 ($calc.CalculatePayout(10, 0))
    Assert-Equal 625 ($calc.CalculatePayout(25, 0))
    Assert-Equal 2500 ($calc.CalculatePayout(100, 0))
}

Run-Test "Payout: Cherry 3x Multiplier" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    $calc.SetMultiplier(1, 3)
    Assert-Equal 30 ($calc.CalculatePayout(10, 1))
    Assert-Equal 150 ($calc.CalculatePayout(50, 1))
}

Run-Test "Payout: Bell 5x Multiplier" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    $calc.SetMultiplier(2, 5)
    Assert-Equal 50 ($calc.CalculatePayout(10, 2))
    Assert-Equal 500 ($calc.CalculatePayout(100, 2))
}

Run-Test "Payout: Bar 10x Multiplier" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    $calc.SetMultiplier(3, 10)
    Assert-Equal 100 ($calc.CalculatePayout(10, 3))
    Assert-Equal 250 ($calc.CalculatePayout(25, 3))
}

Run-Test "Payout: ZeroOrNegativeBetYieldsZero" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    $calc.SetMultiplier(1, 3)
    Assert-Equal 0 ($calc.CalculatePayout(0, 1))
    Assert-Equal 0 ($calc.CalculatePayout(-10, 1))
}

Run-Test "Payout: UnregisteredSymbolYieldsZero" {
    $calc = New-Object SlotGame.Payouts.SlotPayoutCalculator
    Assert-Equal 0 ($calc.CalculatePayout(50, 999))
    Assert-Equal 0 ($calc.CalculatePayout(50, -1))
}

# 4. RNG TESTS
Run-Test "RNG: CryptoRangeBounds" {
    $rng = New-Object SlotGame.RNG.CryptoRandomProvider
    try {
        for ($i = 0; $i -lt 1000; $i++) {
            $val = $rng.NextInt(0, 4)
            Assert-True ($val -ge 0 -and $val -lt 4) "Value $val out of range"
        }
    } finally {
        $rng.Dispose()
    }
}

Run-Test "RNG: DeterministicRepeatability" {
    $rng1 = New-Object SlotGame.RNG.DeterministicRandomProvider(12345)
    $rng2 = New-Object SlotGame.RNG.DeterministicRandomProvider(12345)
    for ($i = 0; $i -lt 50; $i++) {
        Assert-Equal ($rng1.NextInt(0, 100)) ($rng2.NextInt(0, 100))
    }
}

Run-Test "RNG: OutcomeGeneratorBounds" {
    $rng = New-Object SlotGame.RNG.DeterministicRandomProvider(42)
    $gen = New-Object SlotGame.RNG.SlotOutcomeGenerator($rng)
    for ($t = 0; $t -lt 50; $t++) {
        $outcome = $gen.GenerateOutcome(3, 4)
        Assert-Equal 3 $outcome.Length
        for ($r = 0; $r -lt 3; $r++) {
            Assert-True ($outcome[$r] -ge 0 -and $outcome[$r] -lt 4)
        }
    }
}

Write-Host "=================================================="
Write-Host "RESULTS: $passed Passed, $failed Failed. Total: $($passed + $failed)"
Write-Host "=================================================="

if ($failed -gt 0) {
    exit 1
}
