# Croak and Roll - Visual Architecture Guide

## 🎮 Current Game Architecture (Visual)

### High-Level System Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                         UNITY SCENE                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌────────────────┐         ┌─────────────────┐               │
│  │  DB_GameManager│◄───────►│DB_AlternatingRound│             │
│  │   (Singleton)  │         │    Manager        │             │
│  │                │         │                   │             │
│  │  • Game State  │         │  • Turn Tracking  │             │
│  │  • Win Logic   │         │  • Advantage      │             │
│  │  • Rule Actions│         │  • Score Totals   │             │
│  └────────┬───────┘         └────────┬──────────┘             │
│           │                          │                         │
│           │                          │                         │
│     ┌─────┴──────────────────────────┴─────┐                  │
│     │                                      │                  │
│     ▼                                      ▼                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐     │
│  │  Player  │  │  House   │  │DB_Dice   │  │DB_UI     │     │
│  │          │  │          │  │Manager   │  │Manager   │     │
│  │ • Input  │  │ • AI     │  │          │  │          │     │
│  │ • Money  │  │ • Auto   │  │ • Spawn  │  │ • Display│     │
│  │ • Perks  │  │   Roll   │  │ • Roll   │  │ • Buttons│     │
│  └──────────┘  └──────────┘  └────┬─────┘  └────┬─────┘     │
│                                    │             │            │
│                                    ▼             ▼            │
│                          ┌──────────────┐  ┌──────────────┐  │
│                          │Scored Dice   │  │UI Controllers│  │
│                          │Positioner x2 │  │    (11)      │  │
│                          │              │  │              │  │
│                          │ • Layout     │  │ • Buttons    │  │
│                          │ • Tracking   │  │ • Scores     │  │
│                          └──────────────┘  │ • Messages   │  │
│                                            └──────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

---

## 🔄 Complete Game Flow (Step by Step)

```
╔════════════════════════════════════════════════════════════════╗
║                        GAME START                              ║
╚════════════════════════════════════════════════════════════════╝
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ Unity Scene Loads                                              │
│  • GameManager.Awake() → Initialize singleton                  │
│  • RoundManager.Awake() → Initialize singleton                 │
│  • Player/House.Start() → Find managers (FindFirstObjectByType)│
│  • GameManager.Start() → InitializeDice(), InitializeUI()      │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
╔════════════════════════════════════════════════════════════════╗
║                     NEW ROUND STARTS                           ║
╚════════════════════════════════════════════════════════════════╝
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ GameManager.StartNewRoundInternal()                            │
│  1. ClearRoundData()                                           │
│     • diceManager.ClearScoredDice()                            │
│     • Clear rule decision flags                                │
│  2. UpdateRoundUI()                                            │
│     • roundManager.InitializeRound()                           │
│  3. PrepareRoundUI()                                           │
│     • uiManager.ClearRoundTotals()                             │
│     • uiManager.HideStandValue()                               │
│  4. AlternatingRoundManager.InitializeRound()                  │
│     • DetermineAdvantage() → Random 50/50                      │
│     • Player/House.OnRoundStart()                              │
│  5. TransitionToState(AlternatingTurns)                        │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
╔════════════════════════════════════════════════════════════════╗
║                   ALTERNATING TURNS STATE                      ║
╚════════════════════════════════════════════════════════════════╝
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ InitializeAlternatingTurnsUI()                                 │
│  • uiManager.ShowGameplayButtonsDirectly()                     │
│  • Set "Stand" and "Roll" button callbacks                     │
│  • PrepareAlternatingTurnsUI()                                 │
│    - If Player has advantage:                                  │
│      → SetTurnMarkerToPlayer()                                 │
│      → Enable Roll button, disable Stand                       │
│    - If House has advantage:                                   │
│      → SetTurnMarkerToHouse()                                  │
│      → Disable buttons                                         │
│      → WaitForHouseRoll() coroutine                            │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
╔════════════════════════════════════════════════════════════════╗
║                      PLAYER'S TURN                             ║
╚════════════════════════════════════════════════════════════════╝
                             │
                    Player clicks "Roll"
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ Player.RollDice()                                              │
│  → GameManager.RollSharedDice(onComplete, isPlayer: true)      │
│    → DiceManager.RollDiceAndGetResults()                       │
│      ┌──────────────────────────────────────────────────────┐ │
│      │ DICE ROLLING PHYSICS                                 │ │
│      │  • SpawnSharedDice() → Instantiate 2 dice            │ │
│      │  • Get player launch positions                       │ │
│      │  • diceA.RollFromLaunchPosition(launchPosA)          │ │
│      │  • diceB.RollFromLaunchPosition(launchPosB)          │ │
│      │  • Wait for IsRolling() == false                     │ │
│      │  • Get final dice values (physics-based detection)   │ │
│      │  • Show floating score UI                            │ │
│      │  • Wait delay (0.5s)                                 │ │
│      │  • MoveDiceToScoringPosition()                       │ │
│      │    - Calculate row position                          │ │
│      │    - Animate dice movement                           │ │
│      │    - Add to ScoredDicePositioner                     │ │
│      └──────────────────────────────────────────────────────┘ │
│      │                                                         │
│      ▼                                                         │
│    onComplete.Invoke(diceA, diceB)                             │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ GameManager.OnAlternatingRoll(diceA, diceB, isPlayer: true)   │
│  1. AlternatingRoundManager.AddRoll(diceA, diceB, true)       │
│     • playerRollRows.Add(new RollRow(diceA, diceB))           │
│     • Enable Stand button (after first roll)                  │
│  2. CheckAndExecuteRuleActions(diceA, diceB, true)             │
│     ┌────────────────────────────────────────────────────────┐│
│     │ RULE CHECKING                                          ││
│     │  • Get opponent's dice values                          ││
│     │  • Check Rule 1: Matching Dice                         ││
│     │    - If diceA or diceB match any opponent die         ││
│     │      → Add to matchingDice list                        ││
│     │  • Check Rule 2: ±1 Swapping                          ││
│     │    - If diceA or diceB are ±1 from opponent's last row││
│     │      → Add to swappableDice list                       ││
│     │                                                         ││
│     │  IF matchingDice.Count > 0 OR swappableDice.Count > 0: ││
│     │    → PresentPlayerRuleChoices()                        ││
│     │      - Highlight matching dice (RED)                   ││
│     │      - Highlight swappable dice (BLUE)                 ││
│     │      - Change Roll button to "End Turn"                ││
│     │      - Wait for player to:                             ││
│     │        • Click a die (destroy or swap)                 ││
│     │        • Click "End Turn" (skip action)                ││
│     │      - If swap: wait for flip animation                ││
│     │      - Update scores                                   ││
│     │      - Clear highlights                                ││
│     └────────────────────────────────────────────────────────┘│
│  3. UpdateRoundTotals()                                        │
│     • playerRoundTotal = playerPositioner.GetTotalScore()     │
│     • houseRoundTotal = housePositioner.GetTotalScore()       │
│     • uiManager.UpdatePlayerRoundTotal(...)                   │
│  4. CheckRoundResult(isPlayer: true)                           │
│     ┌────────────────────────────────────────────────────────┐│
│     │ WIN CONDITION CHECKING                                 ││
│     │  • If playerRoundTotal > 21:                           ││
│     │    - Check equal opportunity                           ││
│     │    - If satisfied → HOUSE WINS                         ││
│     │  • If playerRoundTotal == 21:                          ││
│     │    - Check equal opportunity                           ││
│     │    - If satisfied → PLAYER WINS                        ││
│     │  • Otherwise → CONTINUE                                ││
│     └────────────────────────────────────────────────────────┘│
│  5a. IF result == CONTINUE:                                    │
│      • SwitchTurn() → isPlayerCurrentRoller = false           │
│      • SetTurnMarkerToHouse()                                 │
│      • DisableGameplayButtons()                               │
│      • WaitForHouseRoll() → Start house turn                  │
│  5b. IF result == PLAYER WINS or HOUSE WINS:                  │
│      • playerWonCurrentRound = (result == PLAYER WINS)        │
│      • TransitionToState(RoundOver)                           │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
╔════════════════════════════════════════════════════════════════╗
║                      HOUSE'S TURN                              ║
╚════════════════════════════════════════════════════════════════╝
                             │
       (automatic after 1 second delay)
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ House.RollDice()                                               │
│  • Check ShouldHouseStand() → AI decision                      │
│    - If current score >= player score → Stand                 │
│    - If risk too high → Stand                                 │
│  • If not standing:                                            │
│    → GameManager.RollSharedDice(onComplete, isPlayer: false)   │
│    → [Same dice rolling flow as player]                        │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ GameManager.OnAlternatingRoll(diceA, diceB, isPlayer: false)  │
│  • Same flow as player but:                                    │
│    - House AI makes rule decisions automatically               │
│    - ExecuteHouseRuleDecision()                                │
│      → Prioritize destroying high-value dice                   │
│  • After processing:                                           │
│    - If player hasn't stood → Switch to player's turn         │
│    - If player stood → House continues rolling (solo mode)    │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
        (Turns alternate until round ends)

╔════════════════════════════════════════════════════════════════╗
║                    PLAYER STANDS                               ║
╚════════════════════════════════════════════════════════════════╝
                             │
                 Player clicks "Stand"
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ Player.Stand()                                                 │
│  → GameManager.OnPlayerStandInAlternating()                    │
│    • Check if player is bust → House wins                      │
│    • Check if house is bust → Player wins                      │
│    • Otherwise:                                                │
│      - SetPlayerStood() in AlternatingRoundManager             │
│      - TransitionToState(PlayerStanding)                       │
│      - ContinueHouseSolo()                                     │
│        ┌──────────────────────────────────────────────────┐   │
│        │ HOUSE SOLO MODE                                  │   │
│        │  • House must beat player's score                │   │
│        │  • House keeps rolling automatically            │   │
│        │  • Each roll:                                    │   │
│        │    - Check bust → Player wins                    │   │
│        │    - Check if beat player → House wins          │   │
│        │    - Otherwise → Keep rolling                    │   │
│        │  • House can also choose to Stand               │   │
│        │    - Compare final scores                        │   │
│        └──────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────┘

╔════════════════════════════════════════════════════════════════╗
║                      ROUND OVER                                ║
╚════════════════════════════════════════════════════════════════╝
                             │
         One of these conditions met:
         • Someone bust
         • Someone hit 21
         • Both stood, scores compared
                             │
                             ▼
┌────────────────────────────────────────────────────────────────┐
│ GameManager.TransitionToState(RoundOver)                       │
│  → HandleRoundOver()                                           │
│    • Log winner                                                │
│    • StartNewRoundAfterDelay()                                 │
│      - Wait 1.5 seconds                                        │
│      - OnStartNewRound()                                       │
│        • roundManager.CountUpRound() → Animate round number   │
│        • StartNewRoundInternal() → Back to NEW ROUND          │
└────────────────────────────────────────────────────────────────┘
                             │
                             ▼
              (Loop back to NEW ROUND)
```

---

## 🔍 Data Flow Diagram

### Score Calculation Flow
```
┌─────────────────────────────────────────────────────────────────┐
│                         DICE ROLL                               │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────────┐
│          DiceManager.RollDiceAndGetResults()                    │
│               • Spawn dice                                      │
│               • Apply physics                                   │
│               • Detect face values                              │
│               • Return: diceA = 4, diceB = 3                    │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────────┐
│    ScoredDicePositioner.AddDiceRowCoroutine(diceA, diceB)      │
│               • Store dice in row list                          │
│               • Position physically                             │
│               • Trigger onScoreChanged callback                 │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────────┐
│        GameManager.UpdatePlayerScoreDisplay()                   │
│               • AlternatingRoundManager.UpdateRoundTotals()     │
│               • playerRoundTotal = positioner.GetTotalScore()   │
│                 ┌──────────────────────────────────────────┐   │
│                 │ GetTotalScore() logic:                   │   │
│                 │  total = 0                               │   │
│                 │  for each row:                           │   │
│                 │    if diceA exists: total += diceA.value │   │
│                 │    if diceB exists: total += diceB.value │   │
│                 │  return total                            │   │
│                 └──────────────────────────────────────────┘   │
│               • uiManager.UpdatePlayerRoundTotal(total)         │
└────────────────┬────────────────────────────────────────────────┘
                 │
                 ▼
┌─────────────────────────────────────────────────────────────────┐
│          UI_FloatingScoreController.UpdateScore()               │
│               • Show animated score: "7"                        │
│               • Animate to round total position                 │
│               • Update TextMeshPro: "Round Total: 7"            │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎨 State Machine Diagram

```
                    ╔═══════════════╗
                    ║  GAME START   ║
                    ╚═══════╤═══════╝
                            │
                            ▼
                    ┌───────────────┐
                    │  RoundOver    │◄──────────────┐
                    └───────┬───────┘               │
                            │                       │
                            │ StartNewRound()       │
                            ▼                       │
                ┌───────────────────────┐           │
                │  AlternatingTurns     │           │
                │                       │           │
                │  • Player rolls       │           │
                │  • House rolls        │           │
                │  • Turns switch       │           │
                └───────┬───────────────┘           │
                        │                           │
                        │ Player.Stand()            │
                        ▼                           │
                ┌───────────────┐                   │
                │ PlayerStanding│                   │
                │               │                   │
                │ • House solo  │                   │
                │ • Auto-roll   │                   │
                └───────┬───────┘                   │
                        │                           │
                        │ House wins/loses          │
                        │ Player wins               │
                        │ Someone busts             │
                        │ Someone hits 21           │
                        └───────────────────────────┘

                Optional (not used):
                ┌──────────┐
                │GameOver  │ (if player runs out of money)
                └──────────┘
```

---

## 📦 Object Relationships

### Manager Relationships (Current - Complex)
```
                    ┌──────────────────┐
                    │  DB_GameManager  │
                    │   (Singleton)    │
                    └────────┬─────────┘
                             │
        ┌────────────────────┼────────────────────┐
        │                    │                    │
        ▼                    ▼                    ▼
┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│DB_Alternating│    │ DB_Dice      │    │  DB_UI       │
│RoundManager  │    │ Manager      │    │  Manager     │
└──────┬───────┘    └──────┬───────┘    └──────────────┘
       │                   │
       ▼                   ▼
┌─────────────────┐ ┌─────────────────┐
│ Player, House   │ │ScoredDice       │
│ (circular ref!) │ │Positioner x2    │
└─────────────────┘ └─────────────────┘
```

### Recommended Simplified Relationships
```
                ┌──────────────────┐
                │ GameController   │
                │  (Singleton)     │
                │                  │
                │ • Game state     │
                │ • Rounds         │
                │ • Turns          │
                │ • Win conditions │
                └────────┬─────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
┌────────────┐   ┌────────────┐   ┌───────────┐
│ RuleSystem │   │DiceManager │   │UIManager  │
│            │   │            │   │           │
│ • Rules    │   │ • Spawning │   │ • Display │
│ • Matching │   │ • Rolling  │   │ • Buttons │
│ • Swapping │   │ • Scoring  │   │ • Messages│
└────────────┘   └────────────┘   └───────────┘
         │               │
         └───────┬───────┘
                 │
                 ▼
        ┌────────────────┐
        │  GameEvents    │
        │  (Event Hub)   │
        │                │
        │ • OnRollDice   │
        │ • OnScoreChange│
        │ • OnRoundEnd   │
        └────────────────┘
```

---

## 🔧 Component Interaction Matrix

| Component | Calls → | Called By ← | Data Flow |
|-----------|---------|-------------|-----------|
| **GameManager** | All managers | UI, Player, House | Commands out, Events in |
| **AlternatingRoundManager** | Player, House, UI | GameManager | State updates |
| **DiceManager** | DiceController, Positioner | GameManager | Dice values |
| **UIManager** | UI Controllers | GameManager, Managers | Display data |
| **Player** | GameManager | UI buttons | User input |
| **House** | GameManager | Coroutines | AI decisions |
| **ScoredDicePositioner** | DiceController | DiceManager | Position data |

---

## 🎯 Critical Code Paths

### Path 1: Player Rolls Dice
```
UI_ButtonController.OnClick("Roll")
    → Player.RollDice()
        → GameManager.RollSharedDice()
            → DiceManager.RollDiceAndGetResults()
                → DiceController.RollFromLaunchPosition() ×2
                    → Physics simulation
                    → Settle detection
                    → Face value detection
                ← Return (diceA: 4, diceB: 3)
            → ScoredDicePositioner.AddDiceRow()
        ← Callback: OnDiceRolled(4, 3)
    → GameManager.OnAlternatingRoll(4, 3, true)
        → CheckAndExecuteRuleActions()
        → UpdateRoundTotals()
        → CheckRoundResult()
        → SwitchTurn() or EndRound()
```

### Path 2: Rule Action (Destroy)
```
GameManager.CheckAndExecuteRuleActions()
    → Find matching dice: [4]
    → PresentPlayerRuleChoices()
        → ScoredDicePositioner.HighlightDiceWithValue(4, RED)
            → DiceController.Highlight(RED)
                → Show red highlight object
        → Set clickable: onDiceClicked = OnPlayerClickedOpponentDie
        → Wait for click...
    ← Player clicks die
→ OnPlayerClickedOpponentDie(clickedDie)
    → Check if value in matchingDice
    → ScoredDicePositioner.RemoveDie(clickedDie)
        → DiceController.DestroyWithEffect()
        → Spawn particle effect
        → Remove from row list
        → Trigger onScoreChanged callback
            → UpdateHouseScoreDisplay()
                → UI updates
```

### Path 3: Round End
```
GameManager.CheckRoundResult()
    → Detect: playerRoundTotal = 22 (BUST)
    → Check equal opportunity
    → If satisfied:
        → Return RoundResult.HouseWins
→ GameManager receives result
    → playerWonCurrentRound = false
    → TransitionToState(RoundOver)
        → HandleRoundOver()
            → Log: "Player lost"
            → StartCoroutine(StartNewRoundAfterDelay())
                → Wait 1.5 seconds
                → OnStartNewRound()
                    → roundManager.CountUpRound()
                        → Animate: "Round 1" → "Round 2"
                    → StartNewRoundInternal()
                        → [Back to round start]
```

---

## 📊 Performance Hotspots

### High Frequency Operations
```
┌─────────────────────────────────────────────┐
│ EVERY FRAME (Update/FixedUpdate)           │
├─────────────────────────────────────────────┤
│ • DiceController physics (while rolling)   │
│ • Settle detection checks                  │
│ • UI animations (DOTween)                  │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ EVERY ROLL (~every 2-3 seconds)            │
├─────────────────────────────────────────────┤
│ • Instantiate 2 dice                       │
│ • Apply physics forces                     │
│ • GetTotalScore() - iterate all dice       │
│ • UI score animations                      │
│ • Rule checking (iterate opponent dice)    │
└─────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ EVERY ROUND (~every 30-60 seconds)         │
├─────────────────────────────────────────────┤
│ • Destroy ~10-20 dice                      │
│ • Clear scoring positioners                │
│ • Reset all state variables                │
│ • Text animations                          │
└─────────────────────────────────────────────┘
```

### Optimization Opportunities
1. **Object Pooling** for dice (currently spawning fresh)
2. **Cache WaitForSeconds** (currently creating per call)
3. **Reduce GetTotalScore() calls** (currently called multiple times per roll)
4. **Batch UI updates** (currently updating individually)

---

## 🐛 Problem Areas (Visual)

### Problem 1: Circular Dependencies
```
    ┌─────────────┐
    │GameManager  │
    │             │
    └──────┬──────┘
           │ uses
           ▼
    ┌─────────────────┐
    │AlternatingRound │
    │    Manager      │
    └──────┬──────────┘
           │ uses
           ▼
    ┌─────────────┐
    │   Player    │
    │             │
    └──────┬──────┘
           │ uses
           └─────────────┐
                        │
    ┌──────────────────▼┐
    │GameManager        │ ← CIRCULAR!
    └───────────────────┘
```

### Problem 2: Responsibility Overlap
```
┌──────────────────────────────┐
│      GameManager             │
│ ┌──────────────────────────┐ │
│ │ Checks win conditions    │ │
│ │ Tracks turn state        │ │
│ │ Switches turns           │ │
│ └──────────────────────────┘ │
└──────────────────────────────┘
               │
               │ AND
               ▼
┌──────────────────────────────┐
│  AlternatingRoundManager     │
│ ┌──────────────────────────┐ │
│ │ Checks win conditions    │ │ ← DUPLICATE!
│ │ Tracks turn state        │ │ ← DUPLICATE!
│ │ Switches turns           │ │ ← DUPLICATE!
│ └──────────────────────────┘ │
└──────────────────────────────┘
```

---

## ✅ Proposed Simplified Architecture

```
┌───────────────────────────────────────────────────────┐
│              SIMPLIFIED ARCHITECTURE                  │
└───────────────────────────────────────────────────────┘

         ┌────────────────────────┐
         │   GameController       │
         │   (Singleton)          │
         │                        │
         │ • Game state machine   │
         │ • Round management     │
         │ • Turn management      │
         │ • Win conditions       │
         │ • Score tracking       │
         └───────────┬────────────┘
                     │
                     │ publishes events
                     ▼
         ┌────────────────────────┐
         │     GameEvents         │
         │     (Event Hub)        │
         └───────────┬────────────┘
                     │
         ┌───────────┼───────────┐
         │           │           │
         ▼           ▼           ▼
 ┌──────────┐ ┌──────────┐ ┌──────────┐
 │RuleSystem│ │DiceManager│ │UIManager │
 │          │ │           │ │          │
 │ Listens: │ │ Listens:  │ │ Listens: │
 │ OnRoll   │ │ OnRound   │ │ All      │
 └──────────┘ └──────────┘ └──────────┘
      │            │             │
      │            │             │
      └────────────┼─────────────┘
                   │ all listen to events
                   ▼
         ┌────────────────────────┐
         │  Player & House        │
         │  (Simplified)          │
         │                        │
         │ • Just input/AI        │
         │ • No manager refs      │
         │ • Fire events only     │
         └────────────────────────┘

KEY BENEFITS:
• No circular dependencies
• Clear single responsibility
• Easy to test
• Events allow loose coupling
```

---

*Use this visual guide alongside the detailed documentation files for complete understanding.*
