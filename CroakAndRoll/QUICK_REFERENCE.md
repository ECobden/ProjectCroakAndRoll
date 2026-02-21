# Croak and Roll - Quick Reference Guide

## 🎲 What Each Script Does (Simple)

### Core Managers
| Script | What It Does | Keep? |
|--------|--------------|-------|
| **DB_GameManager** | Boss of everything - controls game flow | ✅ Yes (but simplify) |
| **DB_AlternatingRoundManager** | Tracks whose turn it is | ⚠️ Merge with GameManager |
| **DB_RoundManager** | Counts round numbers | ✅ Yes (simple & focused) |
| **DB_DiceManager** | Spawns and rolls dice | ✅ Yes |
| **DB_UIManager** | Shows/hides UI elements | ✅ Yes |

### Players
| Script | What It Does | Keep? |
|--------|--------------|-------|
| **Player** | Handles player input (Roll/Stand) | ✅ Yes (but clean up) |
| **House** | AI opponent logic | ✅ Yes (but simplify) |

### Dice
| Script | What It Does | Keep? |
|--------|--------------|-------|
| **DB_DiceController** | One die's physics and animation | ✅ Yes |
| **ScoredDicePositioner** | Arranges dice in neat rows | ✅ Yes |
| **DB_DiceTargetArea** | Where dice should land | ✅ Yes |

### UI Components (11 scripts)
All in `Assets/Scripts/UI/` - Keep most, they're simple and focused.

### Features (Unclear Status)
| Feature | Scripts | Status | Recommendation |
|---------|---------|--------|----------------|
| **Perks** | 5 scripts in Perks/ | Partially done | Decide: keep or remove |
| **Heat** | UI_HeatController | UI only | Complete or remove |
| **Lives** | UI_LivesController | UI only | Complete or remove |

---

## 🔄 Current Game Flow (Simplified)

```
START GAME
    ↓
┌─────────────────────────────────────┐
│ NEW ROUND                           │
│  • Clear old dice                   │
│  • Flip coin: who goes first?       │
│  • Show buttons                     │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ ALTERNATING TURNS                   │
│                                     │
│  ┌──────────────────────┐          │
│  │ Player/House Rolls   │◄─────┐   │
│  └──────────────────────┘      │   │
│           ↓                     │   │
│  ┌──────────────────────┐      │   │
│  │ Check Special Rules  │      │   │
│  │ • Match: destroy     │      │   │
│  │ • ±1: swap           │      │   │
│  └──────────────────────┘      │   │
│           ↓                     │   │
│  ┌──────────────────────┐      │   │
│  │ Update Score         │      │   │
│  └──────────────────────┘      │   │
│           ↓                     │   │
│  ┌──────────────────────┐      │   │
│  │ Check End:           │      │   │
│  │ • Bust (>21)?        │──────┼──► END ROUND
│  │ • Hit 21?            │      │   │
│  │ • Stood?             │──────┼──► HOUSE SOLO
│  └──────────────────────┘      │   │
│           ↓                     │   │
│  ┌──────────────────────┐      │   │
│  │ Switch Turns         │──────┘   │
│  └──────────────────────┘          │
└─────────────────────────────────────┘
    ↓
┌─────────────────────────────────────┐
│ ROUND END                           │
│  • Show winner                      │
│  • Wait 1.5 seconds                 │
│  • Increment round number           │
└─────────────────────────────────────┘
    ↓
(Back to NEW ROUND)
```

---

## 🎮 Special Rules

### Rule 1: Matching Dice
If you roll a die that **matches** any of opponent's dice:
- **You can**: Click their die to destroy it
- **Effect**: Their score goes down
- **UI**: Red highlight on matching dice

### Rule 2: ±1 Swapping
If you roll a die that's **±1** from opponent's last roll:
- **You can**: Click their die to swap values
- **Effect**: Your die becomes their value, theirs becomes yours
- **UI**: Blue highlight on swappable dice

---

## 🐛 Main Problems & Solutions

### Problem 1: Two Game Systems in One
**What's wrong**: Code has TWO turn systems (old "sequential" + new "alternating")

**Why it's bad**: Confusing, bugs, wasted code

**Fix**: Delete all "legacy" code (marked with comments)

**Files to clean**: 
- DB_GameManager.cs (remove PlayerTurn/HouseTurn states)
- Player.cs (remove legacy OnDiceRolled code)
- House.cs (remove legacy OnDiceRolled code)

### Problem 2: Too Many Managers
**What's wrong**: GameManager + AlternatingRoundManager do similar things

**Why it's bad**: Logic split, hard to understand

**Fix**: Merge into one GameController

**Current**:
```
GameManager → calls → AlternatingRoundManager → calls → GameManager
(circular dependency!)
```

**Better**:
```
GameController
  ├─ Manages rounds
  ├─ Manages turns
  └─ Manages game state
```

### Problem 3: Rule Logic is Messy
**What's wrong**: 200+ lines of rule checking in GameManager

**Fix**: Extract to RuleSystem class

**Before**:
```csharp
// In GameManager
CheckAndExecuteRuleActions() { ... 200 lines ... }
```

**After**:
```csharp
// In new RuleSystem.cs
public class RuleSystem {
    public List<RuleAction> CheckAvailableRules(dice, opponent) { }
    public void ExecuteRule(RuleAction action) { }
}
```

---

## 📋 Cleanup Checklist

### High Priority (Do First)
- [ ] Remove legacy turn system code
- [ ] Decide: Keep or remove Perks/Heat/Lives?
- [ ] Fix FindFirstObjectByType() calls (use serialized references)
- [ ] Consolidate GameManager + AlternatingRoundManager

### Medium Priority
- [ ] Extract rule system to separate class
- [ ] Add XML comments to all public methods
- [ ] Reduce manager dependencies (use events)

### Low Priority (Nice to Have)
- [ ] Object pooling for dice
- [ ] Cache WaitForSeconds
- [ ] Unit tests for game logic
- [ ] Documentation for each script

---

## 🔧 How to Start Simplifying

### Step 1: Backup & Branch
```bash
git checkout -b simplify-architecture
git commit -am "Backup before refactoring"
```

### Step 2: Remove Legacy Code
1. Open DB_GameManager.cs
2. Search for "Legacy" comments
3. Delete those sections
4. Test game still works

### Step 3: Decide on Features
Ask yourself:
- Do I want a Perk shop? (Y/N)
- Do I want Heat system? (Y/N)
- Do I want Lives system? (Y/N)

If NO → Delete those scripts

### Step 4: Consolidate Managers
- Move methods from AlternatingRoundManager to GameManager
- Delete AlternatingRoundManager.cs
- Update references

### Step 5: Extract Rules
- Create new file: DiceRuleSystem.cs
- Move rule checking code there
- GameManager calls RuleSystem

---

## 💡 Quick Tips

### Finding References
In Visual Studio / Rider:
- Right-click class name → Find All References

### Testing After Changes
1. Play game
2. Roll a few times
3. Try to bust
4. Try to hit 21
5. Try special rules (match & swap)
6. Check new round starts correctly

### Common Issues After Refactoring
- **NullReferenceException**: Forgot to reassign a reference
- **Missing Method**: Forgot to move/rename a method
- **Buttons don't work**: Button callbacks changed

---

## 📞 Where Things Are Called From

### When Player Clicks "Roll"
```
UI_ButtonController ("Roll" button)
    ↓
Player.RollDice()
    ↓
GameManager.RollSharedDice()
    ↓
DiceManager.RollDiceAndGetResults()
    ↓
(Dice physics happens)
    ↓
GameManager.OnAlternatingRoll()
    ↓
AlternatingRoundManager.AddRoll()
    ↓
GameManager.CheckAndExecuteRuleActions()
    ↓
(Show rule UI or continue)
```

### When Player Clicks "Stand"
```
UI_ButtonController ("Stand" button)
    ↓
Player.Stand()
    ↓
GameManager.OnPlayerStandInAlternating()
    ↓
GameManager.TransitionToState(PlayerStanding)
    ↓
GameManager.ContinueHouseSolo()
    ↓
(House auto-rolls until wins/busts)
```

### When House Auto-Rolls
```
House.RollDice() [called by coroutine]
    ↓
GameManager.RollSharedDice()
    ↓
(Same flow as player but AI decides actions)
```

---

## 🎯 Files to Focus On

### Must Understand (Core Logic)
1. **DB_GameManager.cs** → Start here, it controls everything
2. **DB_AlternatingRoundManager.cs** → Understand turn system
3. **Player.cs** → How player input works
4. **House.cs** → How AI works

### Important (Supporting)
5. **DB_DiceManager.cs** → How dice spawn and move
6. **DB_UIManager.cs** → How UI updates
7. **ScoredDicePositioner.cs** → How dice are arranged

### Can Ignore Initially
- All UI/ scripts (they're straightforward)
- Perk/ scripts (if not using)
- Menu scripts (not core gameplay)

---

## 📊 Script Dependencies (Visual)

```
DB_GameManager (1043 lines)
├─ Dependencies:
│  ├─ Player
│  ├─ House
│  ├─ DB_RoundManager
│  ├─ DB_AlternatingRoundManager
│  ├─ DB_DiceManager
│  └─ DB_UIManager
└─ Used by:
   ├─ Player (circular!)
   ├─ House (circular!)
   └─ Start() at game launch

DB_AlternatingRoundManager (500 lines)
├─ Dependencies:
│  ├─ Player (circular!)
│  ├─ House (circular!)
│  ├─ DB_DiceManager
│  └─ DB_UIManager
└─ Used by:
   └─ DB_GameManager

Player (425 lines)
├─ Dependencies:
│  ├─ DB_GameManager (circular!)
│  ├─ DB_DiceManager
│  └─ DB_UIManager
└─ Used by:
   ├─ DB_GameManager
   └─ DB_AlternatingRoundManager

House (836 lines - has cheat system!)
├─ Dependencies:
│  ├─ DB_GameManager (circular!)
│  ├─ DB_DiceManager
│  └─ DB_UIManager
└─ Used by:
   ├─ DB_GameManager
   └─ DB_AlternatingRoundManager
```

**🔴 See all those (circular!)? That's the problem!**

---

## 🎓 Key Concepts to Understand

### State Machine
Game is always in ONE state:
- **AlternatingTurns** → Normal play
- **PlayerStanding** → House rolling solo
- **RoundOver** → Calculating winner
- **GameOver** → Game finished

Only ONE state at a time = easier to reason about

### Event Callbacks
Many things use callbacks:
```csharp
onComplete?.Invoke(diceA, diceB);
```
This means "call this function when done"

### Coroutines
For timing and animation:
```csharp
yield return new WaitForSeconds(1.0f);
```
This pauses execution for 1 second

### Singletons
Many managers use this pattern:
```csharp
public static DB_GameManager Instance { get; private set; }
```
Means: only one can exist, accessible everywhere

---

*Last Updated: 2026-02-21*
