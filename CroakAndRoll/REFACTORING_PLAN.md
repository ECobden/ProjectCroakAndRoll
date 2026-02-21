# Croak and Roll - Refactoring Action Plan

## 🎯 Goal
Simplify the game architecture while maintaining all current functionality.

---

## 📝 Pre-Refactoring Decisions Needed

Before starting, answer these questions:

### Feature Decisions ✅ DECIDED
- [x] **Perk System**: ~~Keep or~~ **REMOVED** ✅
  - Decision: REMOVE
  - Status: Code cleaned up (Phase 1.2 complete)
  
- [x] **Heat System**: ~~Keep or~~ **REMOVED** ✅
  - Decision: REMOVE
  - Status: Code cleaned up (Phase 1.2 complete)
  
- [x] **Lives System**: ~~Keep or~~ **REMOVED** ✅
  - Decision: REMOVE
  - Status: Code cleaned up (Phase 1.2 complete)
  
- [ ] **Money/Betting System**: Keep or Remove?
  - Code exists but unclear implementation
  - If KEEP: Clarify betting flow
  - If REMOVE: Clean up money references

### Architecture Decisions
- [ ] **Manager Consolidation**: Choose approach
  - Option A: Merge AlternatingRoundManager INTO GameManager
  - Option B: Merge GameManager INTO AlternatingRoundManager (rename to GameController)
  - **Recommendation**: Option A (keep GameManager as main controller)

---

## 🔧 Refactoring Phases

### Phase 1: Remove Dead Code ⏱️ ~2-3 hours

#### 1.1 Remove Legacy Turn System ✅ COMPLETED

**Status**: COMPLETED - All legacy turn code removed successfully
**Time Taken**: ~1 hour
**Files modified**:
- DB_GameManager.cs ✅
- Player.cs ✅
- House.cs ✅

**What to delete**:

**In DB_GameManager.cs**:
```csharp
// DELETE these enum values (lines ~23-26):
PlayerTurn,        // Legacy: Player rolling dice
HouseTurn          // Legacy: House rolling dice

// DELETE entire region "Turn Management (Legacy)" (lines ~230-280):
private void StartPlayerTurnInternal() { ... }
private void StartHouseTurnInternal() { ... }
public void StartPlayerTurn() { ... }
public void EndPlayerTurn() { ... }
public void StartHouseTurn() { ... }
public void EndHouseTurn() { ... }

// DELETE these case blocks from EnterState() (lines ~170-185):
case GameState.PlayerTurn:
case GameState.HouseTurn:

// DELETE these case blocks from ExitState() (lines ~150-160):
case GameState.PlayerTurn:
case GameState.HouseTurn:
```

**In Player.cs** (lines ~70-140):
```csharp
// DELETE legacy code block in OnDiceRolled() after this comment:
// Legacy single-turn mode below

// Keep only the alternating mode code:
if (gameManager != null && gameManager.GetCurrentState() == DB_GameManager.GameState.AlternatingTurns)
{
    gameManager.OnAlternatingRoll(diceAValue, diceBValue, true);
    hasRolledThisTurn = true;
    return;
}
// DELETE everything after this until end of method

// DELETE these methods:
private IEnumerator DelayedBust() { ... }
private void OnBust() { ... }
private IEnumerator DelayedWinWith21() { ... }
private void OnWinWith21() { ... }
private void UpdateTurnValueUI() { ... }
```

**In House.cs** (lines ~140-250):
```csharp
// DELETE legacy code block in OnDiceRolled() after this comment:
// Legacy single-turn mode below

// Keep only the alternating mode code

// DELETE these methods:
private IEnumerator DelayedRoll() { ... }
private IEnumerator DelayedBust() { ... }
private IEnumerator DelayedWin() { ... }
private void OnWin() { ... }
private void OnBust() { ... }
private void UpdateTurnValueUI() { ... }
```

**Testing after 1.1**:
- [ ] ⏸️ Game starts (requires Unity testing)
- [ ] ⏸️ Can roll dice (requires Unity testing)
- [ ] ⏸️ Can stand (requires Unity testing)
- [ ] ⏸️ House auto-rolls (requires Unity testing)
- [ ] ⏸️ Round ends correctly (requires Unity testing)
- [x] ✅ Code compiles without errors

**Changes made**:
- ✅ Removed `PlayerTurn` and `HouseTurn` enum states
- ✅ Removed 6 legacy turn management methods from GameManager
- ✅ Removed ~200 lines from Player.OnDiceRolled()
- ✅ Removed ~150 lines from House.OnDiceRolled()
- ✅ Removed helper methods: DelayedBust, DelayedWin, DelayedWinWith21, OnBust, OnWin
- ✅ Removed obsolete IsPlayerTurn() and IsHouseTurn() methods
- ✅ Fixed all 5 compilation errors from removed states

---

#### 1.2 Remove Unused Features ✅ COMPLETED

**If removing Perk System**:
```bash
# Delete these files:
Assets/Scripts/Perks/Perk.cs
Assets/Scripts/Perks/PerkManager.cs
Assets/Scripts/Perks/LuckySixPerk.cs
Assets/Scripts/Perks/LuckyFourPerk.cs
Assets/Scripts/Perks/FiveAndUnderPerk.cs
Assets/Scripts/Perks/DiceFlipPerk.cs
Assets/Scripts/UI/UI_PerkShopController.cs
Assets/Scripts/UI/UI_PerkShopItem.cs
```

**In Player.cs**:
```csharp
// DELETE:
[Header("Perks")]
private List<Perk> activePerks = new List<Perk>();

// DELETE all perk-related code:
public void ClearPerks() { ... }
// DELETE perk foreach loops in OnDiceRolled()
// DELETE perk hooks in OnRoundStart()
// DELETE perk hooks in Stand()
```

**In DB_UIManager.cs**:
```csharp
// DELETE:
[Header("Shop Panel")]
[SerializeField] private UI_PerkShopController perkShopController;

// DELETE all perkShopController references
```

**If removing Heat System**:
```bash
# Delete file:
Assets/Scripts/UI/UI_HeatController.cs
```

**In DB_UIManager.cs**:
```csharp
// DELETE:
[Header("Heat Display")]
[SerializeField] private UI_HeatController heatController;

// DELETE region "Heat Display"
```

**If removing Lives System**:
```bash
# Delete file:
Assets/Scripts/UI/UI_LivesController.cs
```

**In DB_UIManager.cs**:
```csharp
// DELETE:
[Header("Lives Display")]
[SerializeField] private UI_LivesController livesController;

// DELETE region "Lives Display"
```

**Testing after 1.2**:
- [ ] Verify removed features don't cause errors
- [ ] Check Unity scene for missing script references (fix them)

---

### Phase 2: Consolidate Managers ⏱️ ~4-5 hours

#### 2.1 Merge AlternatingRoundManager into GameManager ✅ COMPLETED

**Status**: COMPLETED - AlternatingRoundManager successfully merged into GameManager
**Time Taken**: ~1 hour
**Code Changes**:
- ✅ Added RollRow class and RoundResult enum to GameManager
- ✅ Added all alternating round state fields to GameManager
- ✅ Merged all 9 methods from AlternatingRoundManager
- ✅ Updated 77+ references from alternatingRoundManager.X to direct calls
- ✅ Removed alternatingRoundManager serialized field
- ✅ Updated class documentation
- ✅ All compilation errors fixed

**Next Steps in Unity**:
1. Delete `DB_AlternatingRoundManager.cs` file
2. Remove AlternatingRoundManager component from scene objects
3. Test game to verify all functionality works

**Strategy**: Copy needed code from AlternatingRoundManager into GameManager

**Step 1**: In DB_GameManager.cs, add fields from AlternatingRoundManager:

```csharp
#region Round State (merged from AlternatingRoundManager)

// Advantage and turn tracking
private bool playerHasAdvantage = true;
private bool isPlayerCurrentRoller = true;
private bool playerHasStood = false;
private bool isWaitingForHouseRoll = false;
private bool waitingForEqualOpportunity = false;

// Score tracking
private int playerRoundTotal = 0;
private int houseRoundTotal = 0;

// Roll history
private List<RollRow> playerRollRows = new List<RollRow>();
private List<RollRow> houseRollRows = new List<RollRow>();

[System.Serializable]
public class RollRow
{
    public int diceA;
    public int diceB;
    public int rollTotal;
    
    public RollRow(int a, int b)
    {
        diceA = a;
        diceB = b;
        rollTotal = a + b;
    }
}

public enum RoundResult
{
    None,
    PlayerWins,
    HouseWins,
    Continue
}

#endregion
```

**Step 2**: Copy methods from AlternatingRoundManager:

```csharp
// Copy these methods:
- DetermineAdvantage()
- UpdateRoundTotals()
- SwitchTurn()
- SetPlayerStood()
- WaitForHouseRoll()
- CheckRoundResult()
- PrepareAlternatingTurnsUI()
- AddRoll()
```

**Step 3**: Update all references:

```csharp
// Find and replace in DB_GameManager.cs:
alternatingRoundManager.PlayerHasAdvantage → playerHasAdvantage
alternatingRoundManager.IsPlayerCurrentRoller → isPlayerCurrentRoller
alternatingRoundManager.PlayerRoundTotal → playerRoundTotal
alternatingRoundManager.HouseRoundTotal → houseRoundTotal
// etc...
```

**Step 4**: Delete AlternatingRoundManager:

```csharp
// In DB_GameManager.cs, DELETE:
[SerializeField] private DB_AlternatingRoundManager alternatingRoundManager;

// DELETE all alternatingRoundManager null checks
```

**Step 5**: Delete the file:
```bash
Assets/Scripts/DB_AlternatingRoundManager.cs
```

**Step 6**: Update Unity scene references:
- Remove AlternatingRoundManager component from scene
- Reassign any missing references in GameManager inspector

**Testing after 2.1**:
- [ ] Full game loop works
- [ ] Advantage system works
- [ ] Turn switching works
- [ ] Score tracking works
- [ ] Round ending works

---

#### 2.2 Fix FindFirstObjectByType() Calls ✅ COMPLETED

**Status**: COMPLETED - All FindFirstObjectByType calls replaced with serialized fields
**Time Taken**: ~15 minutes
**Code Changes**:
- ✅ Player.cs: Added [SerializeField] for 3 managers
- ✅ House.cs: Added [SerializeField] for 3 managers
- ✅ Added Awake() validation in both files
- ✅ Removed all FindFirstObjectByType calls
- ✅ Removed legacy Player lookup in House.OnRoundStart()

**Next Steps in Unity**:
1. Assign manager references in Player inspector (GameManager, DiceManager, UIManager)
2. Assign manager references in House inspector (GameManager, DiceManager, UIManager)
3. Test game to verify functionality

**Performance Improvements**:
- Eliminated 7 runtime FindFirstObjectByType searches per game session
- References now validated at Awake time with clear error messages
- Faster initialization and better debugging

**Problem**: Slow and unreliable initialization

**Solution**: Use serialized fields with proper initialization

**Files to modify**:
- Player.cs
- House.cs

**In Player.cs**:
```csharp
// REPLACE:
[Header("Manager References")]
private DB_GameManager gameManager;
private DB_DiceManager diceManager;
private DB_UIManager uiManager;

void Start()
{
    gameManager = FindFirstObjectByType<DB_GameManager>();
    diceManager = FindFirstObjectByType<DB_DiceManager>();
    uiManager = FindFirstObjectByType<DB_UIManager>();
    // ...
}

// WITH:
[Header("Manager References")]
[SerializeField] private DB_GameManager gameManager;
[SerializeField] private DB_DiceManager diceManager;
[SerializeField] private DB_UIManager uiManager;

private void Awake()
{
    // Validate references
    if (gameManager == null) Debug.LogError("GameManager not assigned to Player!");
    if (diceManager == null) Debug.LogError("DiceManager not assigned to Player!");
    if (uiManager == null) Debug.LogError("UIManager not assigned to Player!");
}

void Start()
{
    currentMoney = startingMoney;
    if (moneyController != null)
        moneyController.SetMoneyValue(currentMoney);
}
```

**Same changes in House.cs**

**After changes**: Assign references in Unity Inspector

**Testing after 2.2**:
- [ ] No errors on game start
- [ ] References assigned correctly

---

### Phase 3: Extract Rule System ✅ COMPLETED

**Status**: COMPLETED - Dice rule system successfully extracted into DB_DiceRuleSystem
**Time Taken**: ~30 minutes
**Code Changes**:
- ✅ Created DB_DiceRuleSystem.cs with all rule logic
- ✅ Extracted CheckAvailableRules() method (matching + ±1 swapping)
- ✅ Extracted HighlightAvailableActions() method
- ✅ Extracted ClearHighlights() method
- ✅ Extracted DestroyDie() method
- ✅ Extracted SwapDice() method
- ✅ Extracted House AI decision logic
- ✅ Added helper methods: CanDestroyDie(), CanSwapDice(), FindSwappablePlayerDie()
- ✅ Updated GameManager to use rule system
- ✅ Reduced GameManager by ~120 lines

**Next Steps in Unity**:
1. Assign DB_DiceRuleSystem component to scene
2. Assign UIManager reference in RuleSystem inspector
3. Assign RuleSystem reference in GameManager inspector
4. Test matching dice rule (destroy)
5. Test ±1 swapping rule

**Benefits**:
- Separation of concerns: Rule logic isolated from game flow
- Easier to test rule logic independently
- Easier to add new rules in the future
- GameManager simplified by ~120 lines
- Better code organization and maintainability

#### 3.1 Create DiceRuleSystem class ✅ COMPLETED

**Create new file**: `Assets/Scripts/DB_DiceRuleSystem.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Handles special dice rules: matching and ±1 swapping
/// </summary>
public class DB_DiceRuleSystem : MonoBehaviour
{
    #region Serialized Fields
    
    [SerializeField] private DB_UIManager uiManager;
    
    #endregion
    
    #region Nested Classes
    
    public class RuleAction
    {
        public enum ActionType { DestroyDie, SwapDice, None }
        
        public ActionType type;
        public DB_DiceController targetDie;
        public DB_DiceController sourceDie;
        public int value;
        
        public RuleAction(ActionType type, DB_DiceController target, DB_DiceController source = null, int val = 0)
        {
            this.type = type;
            this.targetDie = target;
            this.sourceDie = source;
            this.value = val;
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Check what rule actions are available for current roll
    /// </summary>
    public (List<int> matchingDice, List<int> swappableDice) CheckAvailableRules(
        int diceA, 
        int diceB, 
        ScoredDicePositioner opponentPositioner)
    {
        List<int> matchingDice = new List<int>();
        List<int> swappableDice = new List<int>();
        
        if (opponentPositioner == null) 
            return (matchingDice, swappableDice);
        
        // Get opponent's dice and last row
        List<int> opponentDiceValues = opponentPositioner.GetAllDiceValues();
        var opponentLastRow = opponentPositioner.GetLastRow();
        
        // Rule 1: Matching dice
        if (opponentDiceValues.Contains(diceA))
            matchingDice.Add(diceA);
        if (opponentDiceValues.Contains(diceB) && !matchingDice.Contains(diceB))
            matchingDice.Add(diceB);
        
        // Rule 2: ±1 dice
        if (opponentLastRow != null)
        {
            int lastDiceA = opponentLastRow.diceA != null ? opponentLastRow.diceA.GetLastRollValue() : -1;
            int lastDiceB = opponentLastRow.diceB != null ? opponentLastRow.diceB.GetLastRollValue() : -1;
            
            CheckSwappable(diceA, diceB, lastDiceA, swappableDice);
            CheckSwappable(diceA, diceB, lastDiceB, swappableDice);
        }
        
        return (matchingDice, swappableDice);
    }
    
    /// <summary>
    /// Highlight available rule actions for player
    /// </summary>
    public void HighlightAvailableActions(
        List<int> matchingDice, 
        List<int> swappableDice,
        ScoredDicePositioner opponentPositioner,
        System.Action<DB_DiceController> onDieClicked)
    {
        if (opponentPositioner == null) return;
        
        // Highlight matching dice (red)
        foreach (int value in matchingDice)
        {
            opponentPositioner.HighlightDiceWithValue(value, Color.red, onDieClicked);
        }
        
        // Highlight swappable dice (blue)
        if (swappableDice.Count > 0)
        {
            var opponentLastRow = opponentPositioner.GetLastRow();
            if (opponentLastRow != null)
            {
                bool highlightA = ShouldHighlight(opponentLastRow.diceA, swappableDice);
                bool highlightB = ShouldHighlight(opponentLastRow.diceB, swappableDice);
                opponentPositioner.HighlightLastRowDice(highlightA, highlightB, Color.blue, onDieClicked);
            }
        }
    }
    
    /// <summary>
    /// Clear all rule highlights
    /// </summary>
    public void ClearHighlights(ScoredDicePositioner positioner)
    {
        if (positioner != null)
            positioner.ClearAllHighlights();
    }
    
    /// <summary>
    /// Execute destroy die action
    /// </summary>
    public void DestroyDie(DB_DiceController die, ScoredDicePositioner positioner)
    {
        if (die == null || positioner == null) return;
        
        int value = die.GetLastRollValue();
        positioner.RemoveDie(die);
        
        if (uiManager != null)
            uiManager.UpdateGoalText($"Destroyed opponent's {value}!");
    }
    
    /// <summary>
    /// Execute swap dice action
    /// </summary>
    public IEnumerator SwapDice(
        DB_DiceController playerDie,
        DB_DiceController opponentDie,
        System.Action onPlayerScoreChanged,
        System.Action onOpponentScoreChanged)
    {
        if (playerDie == null || opponentDie == null) yield break;
        
        int playerValue = playerDie.GetLastRollValue();
        int opponentValue = opponentDie.GetLastRollValue();
        
        Debug.Log($"Swapping player's {playerValue} with opponent's {opponentValue}");
        
        // Flip both dice
        playerDie.FlipToOppositeFace(opponentValue);
        opponentDie.FlipToOppositeFace(playerValue);
        
        // Wait for flip animation
        while (playerDie.IsFlipping() || opponentDie.IsFlipping())
        {
            yield return null;
        }
        
        // Update scores
        onPlayerScoreChanged?.Invoke();
        onOpponentScoreChanged?.Invoke();
        
        if (uiManager != null)
            uiManager.UpdateGoalText($"Swapped {playerValue} for {opponentValue}!");
        
        yield return new WaitForSeconds(0.3f);
    }
    
    #endregion
    
    #region Private Methods
    
    private void CheckSwappable(int diceA, int diceB, int targetValue, List<int> swappableDice)
    {
        if (targetValue <= 0) return;
        
        if (Mathf.Abs(diceA - targetValue) == 1 && !swappableDice.Contains(diceA))
            swappableDice.Add(diceA);
        if (Mathf.Abs(diceB - targetValue) == 1 && !swappableDice.Contains(diceB))
            swappableDice.Add(diceB);
    }
    
    private bool ShouldHighlight(DB_DiceController die, List<int> swappableDice)
    {
        if (die == null) return false;
        int value = die.GetLastRollValue();
        foreach (int swapValue in swappableDice)
        {
            if (Mathf.Abs(swapValue - value) == 1)
                return true;
        }
        return false;
    }
    
    #endregion
}
```

#### 3.2 Update GameManager to use RuleSystem

**In DB_GameManager.cs**:

```csharp
// ADD:
[SerializeField] private DB_DiceRuleSystem ruleSystem;

// REPLACE CheckAndExecuteRuleActions() with simpler version:
private IEnumerator CheckAndExecuteRuleActions(int diceA, int diceB, bool isPlayer)
{
    if (ruleSystem == null) yield break;
    
    ScoredDicePositioner currentPos = isPlayer ? 
        diceManager.GetPlayerScoringPositioner() : 
        diceManager.GetHouseScoringPositioner();
    ScoredDicePositioner opponentPos = isPlayer ? 
        diceManager.GetHouseScoringPositioner() : 
        diceManager.GetPlayerScoringPositioner();
    
    // Check rules
    var (matchingDice, swappableDice) = ruleSystem.CheckAvailableRules(diceA, diceB, opponentPos);
    
    if (matchingDice.Count == 0 && swappableDice.Count == 0)
        yield break;
    
    // Present choices
    if (isPlayer)
        yield return StartCoroutine(PresentPlayerRuleChoices(matchingDice, swappableDice, currentPos, opponentPos));
    else
        yield return StartCoroutine(ExecuteHouseRuleDecision(matchingDice, swappableDice, opponentPos));
}
```

**Testing after 3.1-3.2**:
- [ ] Rule checking still works
- [ ] Matching dice highlights correctly
- [ ] Swapping dice works
- [ ] Rule UI displays correctly

---

### Phase 4: Improve Architecture ⏱️ ~2-3 hours

#### 4.1 Add Event System

**Create new file**: `Assets/Scripts/GameEvents.cs`

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Centralized event system for game
/// </summary>
public class GameEvents : MonoBehaviour
{
    public static GameEvents Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    
    // Round events
    public UnityEvent<int> OnRoundStarted = new UnityEvent<int>();
    public UnityEvent<bool> OnRoundEnded = new UnityEvent<bool>(); // bool = player won
    
    // Turn events
    public UnityEvent<bool> OnTurnChanged = new UnityEvent<bool>(); // bool = is player turn
    
    // Dice events
    public UnityEvent<int, int, bool> OnDiceRolled = new UnityEvent<int, int, bool>(); // diceA, diceB, isPlayer
    
    // Score events
    public UnityEvent<int> OnPlayerScoreChanged = new UnityEvent<int>();
    public UnityEvent<int> OnHouseScoreChanged = new UnityEvent<int>();
    
    // Game state events
    public UnityEvent OnGameOver = new UnityEvent();
    public UnityEvent OnGameRestarted = new UnityEvent();
}
```

#### 4.2 Use Events Instead of Direct Calls

**Example in DB_GameManager.cs**:

```csharp
// BEFORE:
if (uiManager != null)
    uiManager.UpdatePlayerRoundTotal(playerRoundTotal);

// AFTER:
if (GameEvents.Instance != null)
    GameEvents.Instance.OnPlayerScoreChanged.Invoke(playerRoundTotal);
```

**In DB_UIManager.cs**:

```csharp
private void OnEnable()
{
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.OnPlayerScoreChanged.AddListener(UpdatePlayerRoundTotal);
        GameEvents.Instance.OnHouseScoreChanged.AddListener(UpdateHouseRoundTotal);
        // etc...
    }
}

private void OnDisable()
{
    if (GameEvents.Instance != null)
    {
        GameEvents.Instance.OnPlayerScoreChanged.RemoveListener(UpdatePlayerRoundTotal);
        GameEvents.Instance.OnHouseScoreChanged.RemoveListener(UpdateHouseRoundTotal);
    }
}
```

**Benefits**:
- Decoupled code
- Easier testing
- More flexible

**Testing after 4.1-4.2**:
- [ ] UI still updates correctly
- [ ] No null reference errors
- [ ] Events fire at correct times

---

## ✅ Final Testing Checklist

After all refactoring:

### Basic Functionality
- [ ] Game starts without errors
- [ ] Round counter increments
- [ ] Player can roll dice
- [ ] Player can stand
- [ ] House auto-rolls
- [ ] Dice physics works
- [ ] Dice positioning works

### Game Rules
- [ ] Bust detection (>21)
- [ ] 21 detection (instant win)
- [ ] Matching dice rule works
- [ ] ±1 swapping rule works
- [ ] Equal opportunity rule works
- [ ] Advantage system (random first turn)

### UI
- [ ] Buttons work correctly
- [ ] Turn marker switches
- [ ] Scores display correctly
- [ ] Goal text updates
- [ ] Win/loss messages show
- [ ] Round counter animates

### Edge Cases
- [ ] Both players bust
- [ ] Both players hit 21
- [ ] Player stands at 0
- [ ] House decision logic
- [ ] Multiple rounds work
- [ ] Game restart works

---

## 📊 Before/After Comparison

### Before Refactoring
```
Core Game Scripts: 12
Total Lines (core): ~3000+
Manager Dependencies: High (circular)
Code Duplication: High (two turn systems)
Maintenance Difficulty: High
```

### After Refactoring
```
Core Game Scripts: 8-9
Total Lines (core): ~2000-2200
Manager Dependencies: Low (event-based)
Code Duplication: None
Maintenance Difficulty: Low-Medium
```

---

## 🚨 Common Pitfalls

### 1. Missing Unity References
**Problem**: After deleting components, scene has missing script references
**Solution**: 
- Open scene in Unity
- Look for "Missing (MonoBehaviour)" in Inspector
- Delete or replace those components

### 2. Forgot to Update Prefabs
**Problem**: Prefabs still reference deleted scripts
**Solution**:
- Search project for prefabs referencing old scripts
- Update or remove references

### 3. Merge Conflicts
**Problem**: Lost track of what changed
**Solution**:
- Commit after each phase
- Use descriptive commit messages
- Keep backup branch

### 4. Breaking Existing Workflows
**Problem**: Other team members' scenes break
**Solution**:
- Communicate changes
- Update documentation
- Provide migration guide

---

## 💾 Commit Strategy

```bash
# Phase 1.1
git add -A
git commit -m "Remove legacy turn system code"

# Phase 1.2
git add -A
git commit -m "Remove unused features (Perks/Heat/Lives)"

# Phase 2.1
git add -A
git commit -m "Merge AlternatingRoundManager into GameManager"

# Phase 2.2
git add -A
git commit -m "Replace FindFirstObjectByType with serialized references"

# Phase 3
git add -A
git commit -m "Extract rule system to separate class"

# Phase 4
git add -A
git commit -m "Add event system for decoupling"

# Final
git add -A
git commit -m "Documentation and cleanup"
```

---

## 📅 Estimated Timeline

| Phase | Time | Difficulty |
|-------|------|-----------|
| Phase 1.1 (Remove Legacy) | 2-3 hours | Easy |
| Phase 1.2 (Remove Features) | 1-2 hours | Easy |
| Phase 2.1 (Merge Managers) | 4-5 hours | Medium |
| Phase 2.2 (Fix References) | 1-2 hours | Easy |
| Phase 3 (Extract Rules) | 3-4 hours | Medium |
| Phase 4 (Event System) | 2-3 hours | Medium |
| **Total** | **13-19 hours** | **Medium** |

Can be done over 2-3 days working part-time.

---

## 🎉 Success Criteria

You'll know the refactoring is successful when:

1. ✅ All tests pass
2. ✅ No code marked as "Legacy"
3. ✅ No circular dependencies
4. ✅ All public methods have XML comments
5. ✅ UIManager has no direct game logic
6. ✅ Fewer than 10 scripts in core game loop
7. ✅ New developer can understand flow in <1 hour

---

*Ready to start? Begin with Phase 1.1!*
