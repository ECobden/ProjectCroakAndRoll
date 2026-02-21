# Croak and Roll - Game Architecture Analysis

---

## ✅ REFACTORING STATUS (Updated: 2026-02-21)

### Phase 1.1: Remove Legacy Turn System ✅ COMPLETED
- ✅ Removed PlayerTurn and HouseTurn enum states from GameManager
- ✅ Removed entire "Turn Management (Legacy)" region from GameManager
- ✅ Removed legacy case blocks from EnterState() and ExitState()
- ✅ Removed legacy OnDiceRolled code from Player.cs (200+ lines)
- ✅ Removed legacy OnDiceRolled code from House.cs (150+ lines)
- ✅ Removed Helper Methods: DelayedBust, DelayedWin, DelayedWinWith21, OnBust, OnWin, UpdateTurnValueUI
- ✅ Removed obsolete state check methods: IsPlayerTurn(), IsHouseTurn()

### Phase 1.2: Remove Unused Features ✅ COMPLETED
- ✅ **Perk System**: REMOVED
- ✅ **Heat System**: REMOVED
- ✅ **Lives System**: REMOVED

### Phase 2.1: Consolidate Managers ✅ COMPLETED
- ✅ **DB_AlternatingRoundManager merged into DB_GameManager**
- ✅ Removed circular dependency (GameManager no longer depends on AlternatingRoundManager)
- ✅ Simplified architecture - one less manager to maintain
- ✅ All alternating round logic now in GameManager

### Phase 2.2: Fix FindFirstObjectByType() Calls ✅ COMPLETED
- ✅ **Player.cs**: Replaced FindFirstObjectByType with [SerializeField] references
- ✅ **House.cs**: Replaced FindFirstObjectByType with [SerializeField] references
- ✅ Added Awake() validation methods with error logging
- ✅ Removed legacy Player lookup in House (no longer needed)
- ✅ Performance improvement: eliminated 7 runtime searches

### Phase 3: Extract Rule System ✅ COMPLETED
- ✅ **Created DB_DiceRuleSystem.cs** - Dedicated class for dice rule logic
- ✅ Extracted matching dice rule (destroy opponent's dice)
- ✅ Extracted ±1 swapping rule (swap values with opponent)
- ✅ Extracted House AI decision logic
- ✅ GameManager simplified by ~120 lines
- ✅ Better separation of concerns and testability

### Files to Delete in Unity
- `Assets/Scripts/Perks/` (entire folder)
- `Assets/Scripts/UI/UI_HeatController.cs`
- `Assets/Scripts/UI/UI_LivesController.cs`
- `Assets/Scripts/UI/UI_PerkShopController.cs`
- `Assets/Scripts/UI/UI_PerkShopItem.cs`
- `Assets/Scripts/DB_AlternatingRoundManager.cs` ⬅️ NEW

### Code Stats After Phase 3
- **Removed from GameManager**: ~150 lines (legacy turn methods)
- **Removed from Player.cs**: ~200 lines (legacy code + perk system)
- **Removed from House.cs**: ~150 lines (legacy code)
- **Removed from UIManager**: ~100 lines (heat/lives/perk UI)
- **Merged into GameManager**: +300 lines (from AlternatingRoundManager)
- **Extracted from GameManager**: ~120 lines (to DB_DiceRuleSystem)
- **Created DB_DiceRuleSystem.cs**: ~280 lines (new rule system)
- **Eliminated**: DB_AlternatingRoundManager.cs (~400 lines) - will be deleted
- **Performance**: Eliminated 7 FindFirstObjectByType runtime searches
- **Net Result**: Cleaner architecture, better separation of concerns, ~820 lines reduced across managers

### Next Steps
1. ⏸️ Test game in Unity - assign all new references
2. ⏸️ Delete DB_AlternatingRoundManager.cs and unused UI files in Unity
3. ⏸️ Fix any missing script references in scenes
4. ✅ **Phase 1-3 Complete!** - Major refactoring achieved
5. 🔜 Optional: Phase 4 - Add Event System (see REFACTORING_PLAN.md)

---

## 📋 Game Overview

**Croak and Roll** is a dice-based gambling game similar to Blackjack (21):
- **Goal**: Roll dice to get as close to 21 as possible without going over (busting)
- **Players**: Player vs House (AI opponent)
- **Turn System**: Alternating turns where both players take turns rolling dice
- **Special Rules**: Dice-matching and swapping mechanics based on opponent's rolls

---

## 🎮 Core Game Flow

### High-Level Flow
```
Game Start
    ↓
Round Initialization
    ↓
Determine Advantage (50/50 who goes first)
    ↓
Alternating Turns Loop
    │
    ├─→ Current Player Rolls Dice
    │       ↓
    │   Rule Actions Check (matching/swapping)
    │       ↓
    │   Update Scores
    │       ↓
    │   Check Win/Bust Conditions
    │       ↓
    │   If no end → Switch Turns → Loop
    │
    ↓
Round End → Determine Winner
    ↓
Start New Round
```

### Detailed Round Flow

1. **Round Start**
   - Clear previous round's dice
   - Determine advantage (random 50/50)
   - Initialize Player and House
   - Show gameplay buttons
   - Set turn to advantaged player

2. **Turn Sequence** (Repeats until round ends)
   - **Current Roller's Turn:**
     - Roll 2 dice
     - Check for special rule actions:
       - **Rule 1**: Matching dice → Can destroy opponent's matching die
       - **Rule 2**: ±1 dice → Can swap your die with opponent's last die
     - Add dice to scoring area
     - Update round totals
     - Check for:
       - **Bust** (>21) → Opponent wins (with equal opportunity)
       - **21** → Current player wins (with equal opportunity)
       - **Stand** (player choice) → House continues solo
   - Switch to other player (if not in solo mode)

3. **Equal Opportunity Rule**
   - If a player busts or hits 21, opponent gets one more turn (if they've had fewer turns)
   - Ensures fairness

4. **Player Standing**
   - Player can choose to "Stand" (stop rolling)
   - House then rolls solo trying to beat player's score
   - House AI decides when to stand based on risk assessment

5. **Round End**
   - Determine winner based on scores
   - Wait for delay
   - Increment round counter
   - Start new round

---

## 🏗️ Script Architecture

### Manager Hierarchy (Singleton Pattern)

#### **DB_GameManager** - Core Game Controller
- **Role**: Master game coordinator and state machine
- **Responsibilities**: 
  - Game state management (AlternatingTurns, PlayerStanding, RoundOver, GameOver)
  - Coordinates all other managers
  - Handles win/loss conditions
  - Processes dice rolls and rule actions
  - Manages round transitions
- **Dependencies**: ALL other managers
- **State Machine**:
  - `AlternatingTurns` → Both players taking turns
  - `PlayerStanding` → Player stood, house rolling solo
  - `RoundOver` → Round ending
  - `GameOver` → Game completely over
  - `PlayerTurn/HouseTurn` → Legacy states (not used)

#### **DB_AlternatingRoundManager** - Turn System Manager
- **Role**: Manages alternating turn mechanics
- **Responsibilities**:
  - Track who has advantage
  - Track whose turn it is
  - Track roll history for both players
  - Calculate round totals
  - Determine equal opportunity
  - Check round results
- **Dependencies**: Player, House, DB_DiceManager, DB_UIManager
- **Key Data**:
  - `PlayerRollRows` / `HouseRollRows` - Roll history
  - `PlayerRoundTotal` / `HouseRoundTotal` - Current scores
  - `PlayerHasAdvantage` - Who goes first
  - `IsPlayerCurrentRoller` - Whose turn it is

#### **DB_RoundManager** - Round Counter
- **Role**: Tracks round number and displays it
- **Responsibilities**:
  - Count rounds
  - Animate round number display
  - Show/hide round UI
- **Dependencies**: UI_Title
- **Note**: Simple and focused

#### **DB_DiceManager** - Dice Lifecycle Manager
- **Role**: Manages physical dice objects
- **Responsibilities**:
  - Spawn dice prefabs
  - Execute dice rolls with physics
  - Move dice to scoring positions
  - Clear dice at round end
  - Provide references to scoring positioners
- **Dependencies**: DB_DiceController, ScoredDicePositioner, DB_UIManager
- **Key Components**:
  - 2 shared dice (spawned fresh each roll)
  - Player/House launch positions
  - Player/House scoring positioners

#### **DB_UIManager** - UI Coordinator
- **Role**: Central UI controller
- **Responsibilities**:
  - Manage all UI panels and elements
  - Show/hide gameplay buttons
  - Display scores and messages
  - Control turn marker
  - Animate score transfers
- **Dependencies**: Multiple UI controllers (listed below)

### Player/AI Components

#### **Player** - Player Controller
- **Role**: Handles player input and state
- **Responsibilities**:
  - Roll dice (calls GameManager)
  - Stand (calls GameManager)
  - Track player money
  - Manage perks (feature system)
- **Dependencies**: DB_GameManager, DB_DiceManager, DB_UIManager
- **Legacy Code**: Contains code for old single-turn system

#### **House** - AI Opponent Controller
- **Role**: Handles house AI behavior
- **Responsibilities**:
  - AI decision making (when to roll/stand)
  - Auto-roll timing
  - Track house money
  - Cheat system (disabled in testing)
- **Dependencies**: DB_GameManager, DB_DiceManager, DB_UIManager
- **AI Logic**:
  - Risk assessment based on current score
  - Target value comparison
  - Cautiousness parameter

### Supporting Components

#### **DB_DiceController** - Individual Die
- **Role**: Physics and rendering for one die
- **Responsibilities**:
  - Physics-based rolling
  - Detect final face value
  - Flip animations (for swapping rule)
  - Highlighting for rules
  - Click detection for rule choices
- **Physics**: Rigidbody-based rolling with settle detection

#### **ScoredDicePositioner** - Dice Layout Manager
- **Role**: Arranges scored dice in rows
- **Responsibilities**:
  - Position dice in neat rows (2 dice per row)
  - Animate dice movement
  - Track all dice and their values
  - Calculate total score
  - Support rule actions (find, highlight, remove dice)
- **Used By**: Player and House each have one

#### **DB_DiceTargetArea** - Roll Target Zone
- **Role**: Defines where dice should land
- **Not shown in code**: Likely a simple bounds/collider

### UI Controllers (Specialized)

Located in `Assets/Scripts/UI/`:

- **UI_ButtonController** - Interactive button (Stand/Roll)
- **UI_FloatingScoreController** - Animated score display
- **UI_StandValueController** - Shows stood player's score
- **UI_GoalTextController** - Displays current objective
- **UI_RoundResultController** - Win/loss messages
- **UI_MoneyController** - Player money display
- **UI_HeatController** - Heat system (unused feature?)
- **UI_LivesController** - Lives system (unused feature?)
- **UI_PerkShopController** - Perk shop (feature system)
- **UI_Title** - Title/round display
- **TurnMarker** - Visual indicator of whose turn it is

### Feature Systems (Partially Implemented)

#### **Perk System**
Located in `Assets/Scripts/Perks/`:
- **Perk** (abstract base class)
- **PerkManager** - Manages perks
- **Specific Perks**: LuckySixPerk, LuckyFourPerk, FiveAndUnderPerk, DiceFlipPerk

**Status**: Present in code but unclear integration with main game loop

#### **Heat System**
- UI exists (UI_HeatController)
- Not fully integrated into game flow

#### **Lives System**
- UI exists (UI_LivesController)
- Not fully integrated into game flow

---

## 🐛 Identified Problems

### 1. **Dual Game Mode Confusion** ⚠️ HIGH PRIORITY
**Problem**: Code contains TWO different game modes:
- **Current Mode**: Alternating turn system (actively used)
- **Legacy Mode**: Sequential turn system (PlayerTurn → HouseTurn states)

**Evidence**:
- GameManager has unused states: `PlayerTurn`, `HouseTurn`
- Player.cs and House.cs contain legacy code paths
- Comments say "Legacy - for backward compatibility"

**Impact**: 
- Code confusion and maintenance burden
- Potential bugs from mixed code paths
- Harder to debug

**Recommendation**: Remove all legacy code completely

### 2. **Over-Complicated Manager Dependencies** ⚠️ MEDIUM
**Problem**: Circular and excessive dependencies

```
DB_GameManager depends on:
  ├─ Player (which depends on DB_GameManager)
  ├─ House (which depends on DB_GameManager)
  ├─ DB_RoundManager
  ├─ DB_AlternatingRoundManager (which depends on Player, House, DB_DiceManager, DB_UIManager)
  ├─ DB_DiceManager (which depends on DB_UIManager)
  └─ DB_UIManager

DB_AlternatingRoundManager depends on:
  ├─ Player (which depends on DB_GameManager)
  ├─ House (which depends on DB_GameManager)
  ├─ DB_DiceManager
  └─ DB_UIManager
```

**Impact**: Tight coupling, hard to test, initialization order issues

**Recommendation**: Use events/observer pattern to decouple

### 3. **Responsibility Overlap** ⚠️ MEDIUM
**Problem**: GameManager and AlternatingRoundManager both handle turns

**Overlap**:
- GameManager: Processes rolls, checks win conditions, switches turns
- AlternatingRoundManager: Tracks turns, checks win conditions, manages turn state

**Impact**: Logic split across two places, harder to maintain

**Recommendation**: Consolidate turn logic into one manager

### 4. **Rule Action Complexity** ⚠️ MEDIUM
**Problem**: Rule checking logic is embedded in GameManager (200+ lines)

**Issues**:
- Hard to extend with new rules
- Complex nested logic
- UI and game logic mixed

**Recommendation**: Extract to separate RuleSystem class

### 5. **Incomplete Feature Systems** ⚠️ LOW
**Problem**: Heat, Lives, and Perk systems partially implemented

**Impact**: Dead code, unused UI, unclear game design

**Recommendation**: Either complete or remove these systems

### 6. **State Management Inconsistency** ⚠️ MEDIUM
**Problem**: Multiple bool flags instead of clear state machine

**Example in DB_AlternatingRoundManager**:
- `IsWaitingForHouseRoll`
- `WaitingForEqualOpportunity`
- `PlayerHasStood`
- `IsPlayerCurrentRoller`

**Impact**: Hard to reason about valid state combinations

**Recommendation**: Use enum-based state machine

### 7. **House AI Incomplete** ⚠️ LOW
**Problem**: House has sophisticated decision infrastructure but simple logic

**Code exists for**:
- Cheat system (disabled)
- Risk assessment
- But actual AI is basic: beat player's score

**Recommendation**: Simplify House to match actual needs or complete AI

---

## 💡 Simplification Recommendations

### Phase 1: Remove Dead Code (Quick Wins)

1. **Remove Legacy Turn System**
   - Delete `PlayerTurn` and `HouseTurn` states from GameManager
   - Remove legacy code paths in Player.cs and House.cs
   - Remove `StartPlayerTurnInternal()`, `StartHouseTurnInternal()`, etc.

2. **Remove/Complete Partial Features**
   - **Option A**: Remove Heat and Lives systems entirely
   - **Option B**: Fully integrate them into game flow
   - **Perks**: Decide if keeping or removing

3. **Clean Up Dependencies**
   - Remove circular references where possible
   - Use events instead of direct manager calls

### Phase 2: Consolidate Logic (Architectural)

4. **Merge GameManager and AlternatingRoundManager**
   - **Option A**: Move all turn logic to GameManager
   - **Option B**: Move all game logic to AlternatingRoundManager, rename to GameController
   - Keep GameManager as thin coordinator

5. **Extract Rule System**
   - Create `DiceRuleSystem` class
   - Move matching/swapping logic out of GameManager
   - Make rules data-driven and extensible

6. **Simplify State Management**
   - Use clear enum states
   - Reduce bool flags
   - One source of truth per state

### Phase 3: Improve Structure (Optional)

7. **Event-Driven Architecture**
   - Create GameEvents class with UnityEvents
   - Managers subscribe to events instead of calling each other
   - Better decoupling

8. **Service Locator Pattern**
   - Replace FindFirstObjectByType() calls
   - Create GameServices singleton
   - Faster, more reliable references

9. **Extract UI Logic**
   - Move UI calls out of game logic classes
   - Use presenter/controller pattern
   - Game logic shouldn't know about UI

---

## 📊 Recommended Simplified Architecture

```
GameController (merged GameManager + AlternatingRoundManager)
    ├─ Manages: Game state, rounds, turns, win conditions
    ├─ Events: OnRoundStart, OnTurnChange, OnRoundEnd, OnGameOver
    └─ Uses: DiceManager, RuleSystem, UIManager

DiceManager
    ├─ Manages: Spawning, rolling, positioning dice
    ├─ Events: OnDiceRolled, OnDicePositioned
    └─ Uses: DiceController, ScoredDicePositioner

RuleSystem (new)
    ├─ Manages: Special dice rules (matching, swapping)
    ├─ Methods: CheckRules(), ExecuteRule()
    └─ Uses: ScoredDicePositioner for dice access

UIManager
    ├─ Manages: All UI display
    ├─ Listens to: GameController events
    └─ No game logic, only presentation

Player / House
    ├─ Simplified to: Money, stats, AI decisions
    ├─ No direct manager references
    └─ Triggers events that GameController handles

RoundManager
    ├─ Keep as-is (simple and focused)
    └─ Just counts rounds
```

---

## 🎯 Priority Action Items

### Immediate (Do First)
1. ✓ Remove all legacy turn system code
2. ✓ Decide on Heat/Lives/Perks: keep or remove
3. ✓ Document current game design intent

### Short Term (Next)
4. ✓ Extract rule checking to separate class
5. ✓ Consolidate GameManager + AlternatingRoundManager
6. ✓ Replace FindFirstObjectByType with proper references

### Long Term (Future)
7. ✓ Implement event-driven architecture
8. ✓ Separate UI logic from game logic completely
9. ✓ Add unit tests for core game logic

---

## 📝 Questions to Answer

Before simplifying, clarify game design:

1. **Are these features wanted?**
   - Heat system (increase difficulty/stakes)
   - Lives system (limited retries)
   - Perk system (player upgrades)
   - House cheating mechanics

2. **What's the actual game progression?**
   - Is it just endless rounds?
   - Are there stakes beyond win/loss?
   - What role does money play?

3. **What's the win condition?**
   - Best of N rounds?
   - Money threshold?
   - Survival-based?

4. **Betting system?**
   - Code mentions bets but not clearly implemented
   - How does money flow between player and house?

---

## 🔍 Code Quality Notes

### Good Practices ✅
- Region organization in most scripts
- XML documentation in newer scripts
- Singleton pattern for managers
- Coroutines for async operations
- Event system for OnRoundChanged

### Areas for Improvement ⚠️
- Inconsistent documentation (some scripts have none)
- FindFirstObjectByType() in Start() (slow and fragile)
- Deep nesting in GameManager methods
- Magic numbers (delays, thresholds)
- Missing null safety in some areas

### Performance Concerns 🔥
- Creating new dice every roll (could pool)
- Multiple WaitForSeconds allocations (could cache)
- No object pooling for UI elements
- Potential GC pressure from string operations

---

## 📈 Estimated Simplification Impact

### Current State
- **Total Scripts**: ~30+
- **Core Game Scripts**: 12
- **Lines of Code**: ~3000+ (core game logic)
- **Dependencies**: High coupling
- **Complexity**: Medium-High

### After Simplification
- **Total Scripts**: ~20-25 (remove 5-10)
- **Core Game Scripts**: 8-10
- **Lines of Code**: ~2000-2500 (30% reduction)
- **Dependencies**: Low-Medium coupling
- **Complexity**: Low-Medium

### Benefits
- ✅ Easier to understand and modify
- ✅ Fewer bugs from code confusion
- ✅ Faster iteration on features
- ✅ Better testability
- ✅ Clearer architecture

---

## 🚀 Next Steps

1. **Review this document** - Confirm understanding of current system
2. **Answer design questions** - Clarify what features to keep
3. **Choose simplification approach** - Pick which recommendations to implement
4. **Create refactoring plan** - Break work into small, testable changes
5. **Execute incrementally** - One improvement at a time with testing

---

*Generated: 2026-02-21*
*Game: Croak and Roll*
*Unity Project: ProjectCroakAndRoll*
