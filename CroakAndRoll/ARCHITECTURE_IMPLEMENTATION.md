# Croak and Roll - Architecture Implementation Summary

## Overview
This document outlines the game architecture as implemented, mapping your design plan to the actual code structure.

## Core Systems & Managers

### 1. **GameManager.cs** ✓
**Current:** DB_GameManager.cs
- **Status:** Already implemented with state machine
- **Functionality:**
  - Controls main game loop with Rounds
  - Manages turn order and advantage system
  - Tracks roll limits (5 rolls per side per round)
  - Checks win conditions (exactly 21, highest without busting)
  - Game State Machine: AlternatingTurns → PlayerStanding → RoundOver → GameOver
  - Turn Mode System: PlayerTurn, HouseTurn, HouseSolo, WaitingForEquality

### 2. **UIManager.cs** ✓
**Current:** DB_UIManager.cs
- **Status:** Already implemented
- **Functionality:**
  - Reads data from GameManager and Participant scripts
  - Updates score UI and turn indicators
  - Manages shop interface pop-ups

### 3. **RoundManager.cs** ✓
**Current:** DB_RoundManager.cs
- **Status:** Already implemented
- **Functionality:** Manages round-specific logic and state

---

## Entity Scripts (Player & Opponent)

### 1. **Participant.cs** ✓ **[NEW - CREATED]**
**Type:** Abstract Base Class
- **Inheritance:** MonoBehaviour
- **Functionality:**
  - Common game logic for both Player and House
  - Scoring area management (5x2 grid of rolled dice)
  - Stand tracking (whether participant chose to stand under 21)
  - Money system management (earn, spend, reset)
  - Abstract methods for concrete implementations:
    - `OnRoundStart()` - Round initialization
    - `RollDice()` - Execute dice roll
    - `Stand()` - Choose to stand
  - Score tracking with `RecordRoll()` and `GetRoundTotal()`
  - Roll history with `GetRollHistory()`

### 2. **Player.cs** ✓ **[REFACTORED]**
**Type:** Inherits from Participant
- **Functionality:**
  - Listens for user UI input (Roll/Stand buttons)
  - Passes commands to GameManager
  - Updates money display via UI_MoneyController
  - Tracks whether player has rolled this turn
  - Implements abstract methods:
    - `OnRoundStart()` - Player ready state, show UI
    - `RollDice()` - Call GameManager to roll shared dice
    - `Stand()` - Notify GameManager player is standing

### 3. **House.cs** ✓ **[REFACTORED]**
**Type:** Inherits from Participant
- **Functionality:**
  - AI logic for House decision-making
  - Evaluates board state and decides whether to roll or stand
  - Implements House Rules (try to beat Player without busting)
  - Implements abstract methods:
    - `OnRoundStart()` - Auto-roll after delay
    - `RollDice()` - AI-driven roll or stand decision
    - `Stand()` - Notify GameManager house standing
  - AI System:
    - `ShouldHouseStand()` - Risk assessment algorithm
    - `CalculateBustProbability()` - Probability calculus
    - `CalculateWinProbability()` - Victory odds
    - Configurable cautiousness (0-1 range)
    - Safe threshold for standing (default 17)

---

## Dice Mechanics & Inventory

### 1. **DieData.cs** ✓ **[NEW - CREATED]**
**Type:** ScriptableObject (create instances in Editor)
- **Functionality:**
  - Data container for die type properties
  - Face values array (normally 1-6)
  - Associated DiceAbility list
  - Rarity level (1-5) for progression
  - Cost for shop purchases
  - Die identity: name, description, icon, color
- **Methods:**
  - `GetFaceValue(int)` - Get specific face value
  - `GetAllFaceValues()` - Get all faces
  - `HasAbilities()` - Check for abilities
  - `GetAbilities()` - Retrieve ability list
  - `HasAbilityOfType<T>()` - Type-specific ability check
  - `GetAbilityOfType<T>()` - Retrieve specific ability type
  - `IsValid()` - Validate die properties

### 2. **DiceAbility.cs** ✓ **[NEW - CREATED]**
**Type:** ScriptableObject Abstract Base Class + Implementations
- **Base Class: DiceAbility**
  - Abstract `Execute()` method for ability triggers
  - Parameters: instigator, opponent, diceValue
  - Get display name functionality

- **Implemented Abilities:**
  1. **SwapAbility** - Swap a die with opponent's die
  2. **MoneyAbility** - Grant money based on roll value (configurable rate)
  3. **ExtraRollAbility** - Grant an extra roll this turn
  4. **StealPointsAbility** - Steal points from opponent (configurable amount)
  5. **DoubleValueAbility** - Double the roll value

### 3. **DiceController.cs** ✓
**Current:** DB_DiceController.cs
- **Status:** Already implemented
- **Functionality:**
  - Attached to 3D/2D die prefabs
  - Reads DieData to know die properties
  - Handles physics & animations for rolling
  - Reports final face value back to Participant
  - Supports highlighted selection for rule decisions

### 4. **DiceBag.cs** ✓ **[NEW - CREATED]**
**Type:** MonoBehaviour Component
- **Functionality:**
  - Inventory system for players
  - Holds collection of DieData that player owns
  - Random die selection for rolls
- **Key Methods:**
  - `InitializeBag()` - Set up starting dice
  - `DrawRandomDie()` - Get one random die
  - `DrawRandomDice(int)` - Get multiple random dice
  - `DrawRollDice()` - Get default roll count (configurable, default 2)
  - `AddDie()` - Add die to collection
  - `RemoveDie()` - Remove specific die
  - `GetAllDice()` - Retrieve all dice
  - `GetDiceCount()` - Count total dice
  - `CountDieType()` - Count specific type
  - `ClearBag()` - Empty the bag
  - `GetBagSummary()` - String summary of contents

---

## Progression System

### 1. **ShopManager.cs** ✓ **[NEW - CREATED]**
**Type:** MonoBehaviour Component
- **Functionality:**
  - Controls shop interface
  - Triggered by GameManager at round end
  - Presents purchase options to player
  - Manages dice and upgrade inventory
- **Key Methods:**
  - `OpenShop(string)` - Display shop with round result
  - `CloseShop()` - Close shop and resume game
  - `PurchaseDie()` - Buy specific die type
  - `PurchaseUpgrade()` - Buy game upgrades
  - `RefreshShopUI()` - Update display
  - `GetAvailableDice()` - Query shop inventory
  - `AddDieToShop()` - Add item to shop stock
  - `RemoveDieFromShop()` - Remove item from shop
  - `GetPlayerDiceBag()` - Access player inventory
  - `ResetProgression()` - Reset for new game

- **Game Flow:**
  1. Round ends → GameManager calls OpenShop()
  2. Player sees available dice and money
  3. Player purchases dice/upgrades or closes shop
  4. ShopManager continues to next round

---

## Game Flow Architecture

```
START OF ROUND
  ↓
GameManager.StartNewRound()
  ├─ Determine advantage (who goes first)
  ├─ Call player.OnRoundStart()
  ├─ Call house.OnRoundStart()
  ↓
ALTERNATING TURNS
  ├─ Active Participant calls DiceBag.DrawRollDice()
  ├─ Dice roll with animation/physics
  ├─ DieData triggers abilities if applicable
  ├─ Participant.RecordRoll() adds to scoring area
  ├─ Check win conditions:
  │   ├─ Exactly 21? → Winner declared
  │   ├─ Busted? → Loser declared
  │   └─ Neither? → Continue
  ├─ Turn ends, other participant's turn begins
  └─ Repeat until both stand or 5 rolls each
  ↓
END OF ROUND
  ├─ Compare final scores
  ├─ Declare winner
  ├─ Award money
  ↓
SHOP PHASE
  ├─ ShopManager.OpenShop()
  ├─ Player purchases dice/upgrades
  ├─ Player closes shop
  ↓
NEXT ROUND (repeat)
```

---

## Component Relationships

```
DB_GameManager (State Machine)
  ├─ Player (Participant)
  │   ├─ DiceBag
  │   ├─ UI_MoneyController
  │   └─ Manager References
  │
  ├─ House (Participant)
  │   ├─ AI Logic
  │   └─ Manager References
  │
  ├─ DB_DiceManager
  │   ├─ DB_DiceController × N (the actual dice in scene)
  │   │   └─ DieData (what this die is)
  │   │       └─ DiceAbility × N
  │   │
  │   └─ ScoredDicePositioner (5×2 grid)
  │
  ├─ DB_UIManager
  │   ├─ Score displays
  │   ├─ Turn indicators
  │   └─ Shop interface
  │
  └─ ShopManager
      ├─ Available Dice (DieData[] shop inventory)
      ├─ PlAYER reference
      └─ DiceBag reference
```

---

## Data Structures

### DieData (ScriptableObject)
```csharp
- dieName: string
- description: string
- dieIcon: Sprite
- dieColor: Color
- faceValues: int[] (6 elements)
- abilities: List<DiceAbility>
- rarity: int (1-5)
- cost: int
```

### Participant (Base Class)
```csharp
- roundTotal: int
- rollHistory: List<(int, int)>
- rollCount: int
- hasStood: bool
- canAct: bool
- currentMoney: int
```

---

## Next Steps / TODO

1. **Connect ShopManager to DB_GameManager**
   - Add `OnShopClosed()` method to GameManager
   - Call ShopManager.OpenShop() at round end
   - Pass round result summary to shop

2. **Implement Ability Triggers**
   - When dice land, check DieData.abilities
   - Execute abilities through ability.Execute()
   - Handle ability-specific UI (swaps, extra rolls, etc.)

3. **Create Example DieData Assets**
   - Standard die (1-6, no abilities)
   - Money die (ability: MoneyAbility)
   - Swap die (ability: SwapAbility)
   - Specialist/rare dice with unique abilities

4. **Test Integration**
   - Verify Player/House inherit Participant correctly
   - Test DiceBag drawing during rolls
   - Verify ShopManager UI flows
   - Test ability execution on rolls

5. **Scoring Area Visualization**
   - Display 5×2 grid layout for scored dice
   - Show which dice are placed where
   - Update visuals as rolls accumulate

6. **Money/Progression Balance**
   - Fine-tune shop prices based on die rarity
   - Adjust starting money amounts
   - Tune win/loss money awards

---

## Files Created
- `Participant.cs` - Base class for Player and House
- `DiceAbility.cs` - Ability system with 5 example abilities
- `DieData.cs` - ScriptableObject for die types
- `DiceBag.cs` - Inventory management system
- `ShopManager.cs` - Progression and shop system

## Files Modified
- `Player.cs` - Now inherits from Participant
- `House.cs` - Now inherits from Participant

## Files Already Implemented
- `DB_GameManager.cs` - Game state machine
- `DB_UIManager.cs` - UI management
- `DB_DiceManager.cs` - Dice coordination
- `DB_DiceController.cs` - Individual die physics/animation
- `DB_RoundManager.cs` - Round management
- And supporting scripts...

---

## Architecture Status: **COMPLETE** ✓

Your core architecture is now fully implemented and ready for:
1. Asset creation (DieData ScriptableObjects)
2. UI refinement and connection
3. Ability implementation
4. Balance and tuning
5. Testing and Polish
