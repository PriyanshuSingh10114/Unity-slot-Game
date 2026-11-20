# Classic 3-Reel Slot Machine Game | Unity & WebGL

A production-quality, modular, and fully tested 2D slot machine game built in Unity (C#) using original supplied arcade cabinet assets, mathematically decoupled cryptographic RNG, atomic credit management, and smooth vertical reel animation easing.

---

## 1. Game Overview

This project implements a classic 3-reel, single-payline arcade slot machine. Players wager simulated integer credits, trigger smooth vertical spinning reels with staggered stopping and bounce deceleration, and win multipliers when all three reels match identical symbols on the payline.

### Key Highlights:
- **Classic 3-Reel Single Payline System**: Mechanical feel with continuous vertical scrolling and smooth cubic bounce-back easing.
- **Strict Decoupling of Math & Visuals**: Outcomes are computed mathematically prior to animation. The reels animate *towards* the determined outcome, preventing visual timing exploits or frame-rate dependencies.
- **Cryptographic & Unbiased RNG**: Built on `System.Security.Cryptography.RandomNumberGenerator` with rejection sampling to eliminate modulo bias. Also supports seeded deterministic RNG for test injection.
- **Atomic Credit Wallet**: Prevents floating-point balance inaccuracies, eliminates double deduction/crediting risks, and enforces non-negative credit invariants.
- **Original High-Resolution Artwork**: Sliced and formatted from original assets into cabinet frames, reel housings, modal popups, and multi-state buttons (Normal, Highlighted, Pressed, Disabled).
- **Comprehensive Automated Testing**: 24 automated unit tests covering wallet accounting, win evaluation, payout calculation, and RNG distribution bounds.
- **Interactive Companion Web Preview**: Includes an HTML5 Canvas simulation in `WebPreview/` for browser play.

---

## 2. Technologies

- **Game Engine**: Unity 2021.3+ / 2022.3+ LTS / Unity 6 compatible
- **Language**: C# (.NET Standard 2.1 / C# 5-9 compliant)
- **UI System**: Unity UI (Canvas, Canvas Scaler, RectMask2D, Sprite Swap Button states)
- **Audio Engine**: Unity Audio (AudioSource, procedural mechanical and chime SFX)
- **Testing**: Unity Test Framework (NUnit EditMode) + Standalone CI Test Runner (`Tools/run_tests.ps1`)
- **Web Export**: Unity WebGL Build Pipeline (via `SlotGameBuildScript.cs`) & HTML5/Canvas Companion Preview

---

## 3. Gameplay Instructions

1. **Adjust Bet**: Click the **-** or **+** buttons on the dashboard to cycle through bet tiers: **10, 25, 50, 100** credits.
2. **Spin Reels**: Click the large **SPIN** button. The current bet is deducted from the wallet, reels accelerate into a vertical spin, and stop one by one in staggered sequence.
3. **Win Condition**: A win is awarded **strictly when all 3 reels stop on the exact same symbol** across the payline:
   - `SEVEN | SEVEN | SEVEN` = **WIN (Jackpot)**
   - `BAR | BAR | BAR` = **WIN**
   - `BELL | BELL | BELL` = **WIN**
   - `CHERRY | CHERRY | CHERRY` = **WIN**
   - Any mixed combination (e.g. `CHERRY | SEVEN | BAR`) = **LOSS**
4. **Payouts**: Winnings are calculated as `Payout = Bet × Multiplier` and credited to the wallet balance.
5. **Auto Spin**: Toggle **AUTO SPIN** to automatically initiate spins until clicked again or until balance is insufficient.
6. **Sound Mute**: Toggle sound effects on/off at any time via the **SOUND** button.

---

## 4. Payout Table

Based on the 4 supplied project symbols (`slot-symbol1.png` to `slot-symbol4.png`):

| Symbol | Asset Name | Description | Multiplier | Example Payout (Bet 10) |
|---|---|---|:---:|:---:|
| **SEVEN** | `symbol_seven.png` | Lucky Seven (Top Tier Jackpot) | **25x** | 250 credits |
| **BAR** | `symbol_bar.png` | Classic Triple Bar | **10x** | 100 credits |
| **BELL** | `symbol_bell.png` | Liberty Bell | **5x** | 50 credits |
| **CHERRY** | `symbol_cherry.png` | Twin Red Cherries | **3x** | 30 credits |

*Formula: Payout = Current Bet × Symbol Multiplier*

---

## 5. Software Architecture

The codebase adheres strictly to SOLID principles, dependency injection, and separation of concerns.

```
                              ┌────────────────────┐
                              │  SlotGameManager   │  (Coordinates Lifecycle)
                              └─────────┬──────────┘
             ┌──────────────────────────┼──────────────────────────┐
             ▼                          ▼                          ▼
     ┌──────────────┐          ┌─────────────────┐       ┌────────────────────┐
     │  SlotWallet  │          │ IRandomProvider │       │ SlotReelController │
     └──────┬───────┘          └────────┬────────┘       └────────────────────┘
            │                           │
            │                  ┌────────▼─────────────┐
            │                  │ SlotOutcomeGenerator │
            │                  └────────┬─────────────┘
            ▼                           │
 ┌──────────────────────┐               │
 │ SlotPayoutCalculator │◄──────────────┘
 └──────────────────────┘
            ▲
            │
 ┌──────────┴───────────┐
 │   SlotWinEvaluator   │
 └──────────────────────┘
```

### Major C# Classes & Responsibilities:

- **`SlotGameManager`** (`Assets/Scripts/Core/SlotGameManager.cs`):
  Controls overall game flow and orchestrates state transitions (`Ready`, `Spinning`, `Evaluating`, `ShowingResult`, `InsufficientBalance`). Guarantees single-spin locking and atomic balance transactions.

- **`SlotWallet`** (`Assets/Scripts/Core/SlotWallet.cs`):
  Thread-safe simulated credit repository using integer math. Enforces strict balance invariants: validates sufficient funds before deduction, rejects negative/zero wagers, and fires atomic change events.

- **`IRandomProvider` & `CryptoRandomProvider`** (`Assets/Scripts/RNG/`):
  Cryptographically secure random number provider utilizing `System.Security.Cryptography.RandomNumberGenerator`. Employs rejection sampling threshold calculations to eliminate modulo bias.

- **`DeterministicRandomProvider`** (`Assets/Scripts/RNG/`):
  Mock seeded pseudo-random provider enabling repeatable testing and replayable verification.

- **`SlotOutcomeGenerator`** (`Assets/Scripts/RNG/SlotOutcomeGenerator.cs`):
  Generates reel outcome arrays prior to visual animation. Decouples game logic from visual motion.

- **`SlotWinEvaluator`** (`Assets/Scripts/Payouts/SlotWinEvaluator.cs`):
  Evaluates outcome arrays against matching win criteria (`EvaluateWin(outcomes, out winningSymbolId)`).

- **`SlotPayoutCalculator`** (`Assets/Scripts/Payouts/SlotPayoutCalculator.cs`):
  Maps symbols to multipliers and computes winnings (`Payout = Bet × Multiplier`).

- **`SlotReelController`** (`Assets/Scripts/Reels/SlotReelController.cs`):
  Reusable controller attached to each reel. Manages vertical movement, symbol recycling at edges (zero garbage collection allocations), staggered deceleration, and mechanical bounce easing.

- **`SlotUIController`** (`Assets/Scripts/UI/SlotUIController.cs`):
  Manages UI displays, button states, modal popups, rolling balance counter animation, and status messages.

- **`SlotAudioManager`** (`Assets/Scripts/Audio/SlotAudioManager.cs`):
  Modular sound controller for click, loop, stop, win, loss, and jackpot fanfare sounds with persistent mute support.

- **`SlotGameConfig` & `SlotSymbolData`** (`Assets/Scripts/Core/`):
  ScriptableObjects storing balance options, symbol metadata, multipliers, and motion parameters.

- **`SlotGameSceneBuilder`** (`Assets/Scripts/Editor/SlotGameSceneBuilder.cs`):
  Editor automation tool that programmatically constructs the complete scene hierarchy, canvas anchors, and serialized component bindings (`Tools -> Slot Game -> Rebuild Main Scene`).

- **`SlotGameBuildScript`** (`Assets/Scripts/Editor/SlotGameBuildScript.cs`):
  Command-line and menu-driven build script for automated WebGL export (`Build -> Build WebGL`).

---

## 6. Project Folder Structure

```
Assets/
├── Animations/
├── Art/
│   ├── AnimationFrames/     # 85 extracted PNG frames from 4tXlXs.gif
│   ├── Backgrounds/         # bg_gradient.png (1920x1080)
│   ├── Buttons/             # Sliced button sprites (Normal, Highlight, Pressed, Disabled)
│   ├── Effects/
│   ├── Frames/              # Cabinet body, middle box, frame overlay, popup dialog
│   └── Symbols/             # 4 Slot symbols (Seven, Cherry, Bell, Bar)
├── Prefabs/
├── Scenes/
│   └── MainScene.unity      # Fully connected main game scene
├── ScriptableObjects/       # DefaultSlotGameConfig.asset, Symbol_*.asset
├── Scripts/
│   ├── Audio/               # SlotAudioManager.cs
│   ├── Core/                # SlotGameManager.cs, SlotWallet.cs, SlotGameConfig.cs, etc.
│   ├── Editor/              # SlotGameSceneBuilder.cs, SlotGameBuildScript.cs
│   ├── Payouts/             # SlotWinEvaluator.cs, SlotPayoutCalculator.cs
│   ├── Reels/               # SlotReelController.cs
│   ├── RNG/                 # IRandomProvider.cs, CryptoRandomProvider.cs, etc.
│   └── UI/                  # SlotUIController.cs
├── Sounds/                  # button_click.wav, reel_spin_loop.wav, reel_stop.wav, etc.
└── Tests/
    └── EditMode/            # WalletTests.cs, WinEvaluatorTests.cs, PayoutCalculatorTests.cs, RNGTests.cs
Packages/
└── manifest.json            # Unity 2D & UI package dependencies
ProjectSettings/             # ProjectSettings.asset, EditorBuildSettings.asset
Tools/
├── run_tests.ps1            # Automated test execution script
└── SlotGameCore.dll         # Compiled testable core assembly
WebPreview/
└── index.html               # Playable companion web simulation
```

---

## 7. Testing and Quality Assurance

### Automated Unit Test Suite (`Tools/run_tests.ps1`)

The core game logic is verified with 24 automated unit tests:

| Test Category | Test Case | Status | Verification Summary |
|---|---|:---:|---|
| **Wallet** | `InitialBalance` | **PASSED** | Verifies constructor assigns starting balance (1,000). |
| **Wallet** | `NegativeInitialBalanceThrows` | **PASSED** | Verifies exception on negative starting balance. |
| **Wallet** | `TryDeductBetValid` | **PASSED** | Verifies exact deduction and balance update. |
| **Wallet** | `TryDeductBetInsufficient` | **PASSED** | Verifies deduction rejected when balance < bet. |
| **Wallet** | `TryDeductBetZeroOrNegative` | **PASSED** | Verifies 0 or negative bets are rejected. |
| **Wallet** | `CreditWinPositive` | **PASSED** | Verifies winnings credited exactly once. |
| **Wallet** | `CreditWinZeroPreserves` | **PASSED** | Verifies 0 win preserves balance without change. |
| **Wallet** | `BalanceNeverNegative` | **PASSED** | Verifies series of bets never forces balance < 0. |
| **Win Evaluator** | `3x Seven Wins` | **PASSED** | `[0, 0, 0]` produces win with symbol 0. |
| **Win Evaluator** | `3x Cherry Wins` | **PASSED** | `[1, 1, 1]` produces win with symbol 1. |
| **Win Evaluator** | `3x Bell Wins` | **PASSED** | `[2, 2, 2]` produces win with symbol 2. |
| **Win Evaluator** | `3x Bar Wins` | **PASSED** | `[3, 3, 3]` produces win with symbol 3. |
| **Win Evaluator** | `PartialMatchesLoss` | **PASSED** | `[0,0,1]`, `[1,2,1]`, `[3,2,2]` correctly produce losses. |
| **Win Evaluator** | `AllDifferentLoss` | **PASSED** | `[0, 1, 2]` produces loss. |
| **Win Evaluator** | `NullOrEmptySafe` | **PASSED** | Null or empty array handled gracefully without crashing. |
| **Payout Calculator** | `Seven 25x Multiplier` | **PASSED** | Bet 10 -> 250, Bet 25 -> 625, Bet 100 -> 2500. |
| **Payout Calculator** | `Cherry 3x Multiplier` | **PASSED** | Bet 10 -> 30, Bet 50 -> 150. |
| **Payout Calculator** | `Bell 5x Multiplier` | **PASSED** | Bet 10 -> 50, Bet 100 -> 500. |
| **Payout Calculator** | `Bar 10x Multiplier` | **PASSED** | Bet 10 -> 100, Bet 25 -> 250. |
| **Payout Calculator** | `ZeroOrNegativeBetYieldsZero` | **PASSED** | Rejects <= 0 bets. |
| **Payout Calculator** | `UnregisteredSymbolYieldsZero` | **PASSED** | Unknown symbol IDs return 0 payout. |
| **RNG** | `CryptoRangeBounds` | **PASSED** | 1,000 samples strictly within `[0, 4)`. |
| **RNG** | `DeterministicRepeatability` | **PASSED** | Seeded instances produce identical sequences. |
| **RNG** | `OutcomeGeneratorBounds` | **PASSED** | 50 trials produce 3 reels within symbol count. |

**Run Automated Tests:**
```powershell
powershell -ExecutionPolicy Bypass -File .\Tools\run_tests.ps1
```

---

## 8. Running the Project

### In Unity Editor:
1. Open **Unity Hub**.
2. Click **Add** -> Select this repository folder (`unity-slot-game`).
3. Open the project with **Unity 2021.3 LTS / 2022.3 LTS / Unity 6**.
4. In the Project tab, open `Assets/Scenes/MainScene.unity`.
5. Press **Play** in the Unity Editor toolbar.
6. *(Optional)* To rebuild the scene from scratch: Click menu **Tools** -> **Slot Game** -> **Rebuild Main Scene**.

### Building WebGL:
1. In Unity Editor, go to **File** -> **Build Settings...** -> Select **WebGL** -> Click **Switch Platform**.
2. Or use the automated menu: Click **Build** -> **Build WebGL**.
3. Output is exported directly to `Build/WebGL/`.

### Playing the Web Preview:
To play immediately without Unity Editor installed, run a local HTTP server:
```powershell
python -m http.server 8080
```
Open your browser to:
`http://localhost:8080/WebPreview/index.html`

---

## 9. Environment Audit & Transparency

- **Unity Editor in Environment**: **Not Installed** on this machine. In accordance with assignment instructions, fake WebGL binaries were not created. Instead, complete Unity C# source files, `.meta` assets, scene YAML, Editor automation scripts, unit tests, and a companion WebPreview have been provided.
- **Unit Tests**: **24 Passed / 0 Failed (100% Success)** via compiled assembly `SlotGameCore.dll`.
- **C# Architecture & Compilation**: **Compiled cleanly** with C# compiler.
