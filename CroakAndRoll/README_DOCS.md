# Croak and Roll - Documentation Index

Welcome to the Croak and Roll game architecture documentation!

---

## 📚 Documentation Files

### 1. **GAME_ARCHITECTURE_ANALYSIS.md** (Main Document)
**Purpose**: Comprehensive analysis of the entire game system

**Contains**:
- Game overview and mechanics
- Complete script breakdown and responsibilities
- Identified problems and their severity
- Simplification recommendations with priorities
- Questions to answer before refactoring

**Best for**: Understanding the big picture and current state

**Start here if**: You want to understand how everything works together

---

### 2. **QUICK_REFERENCE.md** (Cheat Sheet)
**Purpose**: Fast lookup guide for daily development

**Contains**:
- Simple table of what each script does
- Condensed game flow diagram
- Quick problem summaries
- Cleanup checklist
- Common file locations
- Key code path traces

**Best for**: Quick answers during development

**Start here if**: You need to find something specific quickly

---

### 3. **REFACTORING_PLAN.md** (Action Plan)
**Purpose**: Step-by-step guide to simplify the codebase

**Contains**:
- Pre-refactoring decision checklist
- Phased implementation plan (4 phases)
- Specific code to delete/modify
- Testing checklist after each phase
- Estimated time per phase (13-19 hours total)
- Git commit strategy
- Common pitfalls to avoid

**Best for**: Actually doing the refactoring work

**Start here if**: You're ready to clean up the code

---

### 4. **VISUAL_ARCHITECTURE.md** (Diagrams)
**Purpose**: Visual representation of systems and flow

**Contains**:
- ASCII diagrams of architecture
- Step-by-step game flow visualization
- Data flow diagrams
- State machine diagram
- Component interaction matrix
- Before/after architecture comparison

**Best for**: Visual learners and presentations

**Start here if**: You prefer diagrams over text

---

## 🎯 How to Use This Documentation

### Scenario 1: New to the Project
```
1. Read: GAME_ARCHITECTURE_ANALYSIS.md (sections 1-4)
2. Look at: VISUAL_ARCHITECTURE.md (High-Level System Overview)
3. Reference: QUICK_REFERENCE.md (bookmark for later)
```

### Scenario 2: Need to Fix a Bug
```
1. Check: QUICK_REFERENCE.md → "Where Things Are Called From"
2. Review: VISUAL_ARCHITECTURE.md → "Critical Code Paths"
3. Reference: GAME_ARCHITECTURE_ANALYSIS.md → specific script section
```

### Scenario 3: Ready to Refactor
```
1. Review: GAME_ARCHITECTURE_ANALYSIS.md → "Identified Problems"
2. Answer decisions in: REFACTORING_PLAN.md → "Pre-Refactoring Decisions"
3. Follow: REFACTORING_PLAN.md → Phase by phase
4. Check progress: REFACTORING_PLAN.md → "Final Testing Checklist"
```

### Scenario 4: Explaining to Team Member
```
1. Show: VISUAL_ARCHITECTURE.md → "Complete Game Flow"
2. Walk through: QUICK_REFERENCE.md → "What Each Script Does"
3. Discuss: GAME_ARCHITECTURE_ANALYSIS.md → "Identified Problems"
```

---

## 🔍 Quick Navigation

### By Topic

#### Game Flow
- **VISUAL_ARCHITECTURE.md** → "Complete Game Flow (Step by Step)"
- **QUICK_REFERENCE.md** → "Current Game Flow (Simplified)"

#### Scripts & Components
- **GAME_ARCHITECTURE_ANALYSIS.md** → "Script Architecture"
- **QUICK_REFERENCE.md** → "What Each Script Does"

#### Problems & Issues
- **GAME_ARCHITECTURE_ANALYSIS.md** → "Identified Problems"
- **QUICK_REFERENCE.md** → "Main Problems & Solutions"

#### Refactoring
- **REFACTORING_PLAN.md** → All phases
- **GAME_ARCHITECTURE_ANALYSIS.md** → "Simplification Recommendations"

#### Architecture Diagrams
- **VISUAL_ARCHITECTURE.md** → All diagrams

---

## 📊 Document Summary

| Document | Pages | Detail Level | Use Case |
|----------|-------|--------------|----------|
| GAME_ARCHITECTURE_ANALYSIS | ~12 | High | Comprehensive understanding |
| QUICK_REFERENCE | ~8 | Low-Medium | Daily reference |
| REFACTORING_PLAN | ~10 | High | Implementation guide |
| VISUAL_ARCHITECTURE | ~6 | Medium | Visual understanding |

---

## ✅ Key Findings Summary

### Main Problems Identified
1. **Legacy Code** - Two game systems (old + new) coexist
2. **Circular Dependencies** - Managers reference each other
3. **Responsibility Overlap** - GameManager + AlternatingRoundManager duplicate logic
4. **Complex Rules** - 200+ lines embedded in GameManager
5. **Incomplete Features** - Heat, Lives, Perks partially implemented

### Recommended Solution (3-Phase Approach)
1. **Remove** legacy code and unused features (~3-5 hours)
2. **Consolidate** managers into single GameController (~4-5 hours)
3. **Extract** rule system to separate class (~3-4 hours)

### Expected Impact
- **30% reduction** in code size
- **Clearer architecture** with single responsibilities
- **Easier maintenance** and testing
- **Better performance** (optional optimizations)

---

## 🎓 Understanding the Game

### Core Concept
Croak and Roll is a dice-based game similar to Blackjack:
- **Goal**: Get as close to 21 as possible without going over
- **Mechanic**: Roll 2 dice per turn, alternating with opponent
- **Special**: Matching dice can be destroyed, ±1 dice can be swapped
- **Win**: Hit 21, opponent busts, or have higher score when both stand

### Key Components
- **GameManager**: Controls everything (state machine)
- **AlternatingRoundManager**: Manages turns (should be merged)
- **DiceManager**: Spawns and rolls physical dice
- **Player/House**: Handle input and AI
- **Rule System**: Special dice actions (currently embedded)

### Game Loop
```
Round Start → Determine Advantage → Alternating Turns → 
Check Win Conditions → Round End → Next Round
```

---

## 🚀 Next Steps

1. **Read** GAME_ARCHITECTURE_ANALYSIS.md completely
2. **Answer** design questions (section "Questions to Answer")
3. **Decide** which features to keep (Perks, Heat, Lives)
4. **Review** REFACTORING_PLAN.md phases
5. **Start** with Phase 1: Remove Legacy Code

---

## 📞 Contact & Questions

If documentation is unclear or you have questions:

1. Check the relevant document's detailed section
2. Review VISUAL_ARCHITECTURE.md for visual explanation
3. Cross-reference QUICK_REFERENCE.md for quick answers

---

## 📝 Document Generation Info

- **Generated**: 2026-02-21
- **Project**: Croak and Roll (Unity)
- **Unity Version**: [Check ProjectSettings/ProjectVersion.txt]
- **Main Scene**: [Likely in Assets/Scenes/]

---

## 🔄 Keeping Documentation Updated

After making changes:

1. Update affected diagrams in VISUAL_ARCHITECTURE.md
2. Update script tables in QUICK_REFERENCE.md
3. Mark completed tasks in REFACTORING_PLAN.md
4. Add notes about new issues in GAME_ARCHITECTURE_ANALYSIS.md

---

## 📁 File Locations

All documentation is in the root project folder:
```
CroakAndRoll/
├── README_DOCS.md (this file)
├── GAME_ARCHITECTURE_ANALYSIS.md
├── QUICK_REFERENCE.md
├── REFACTORING_PLAN.md
└── VISUAL_ARCHITECTURE.md
```

Game code is in:
```
CroakAndRoll/Assets/Scripts/
├── DB_GameManager.cs
├── DB_AlternatingRoundManager.cs
├── DB_DiceManager.cs
├── DB_UIManager.cs
├── DB_RoundManager.cs
├── Player.cs
├── House.cs
└── [many more...]
```

---

## 🎯 Success Metrics

You'll know the documentation is helpful when:

✅ New team members understand the game in <2 hours
✅ Can find any script's purpose in <1 minute
✅ Can trace code paths without debugger
✅ Refactoring plan is clear and actionable
✅ All major problems are documented

---

**Good luck with your refactoring! Start with GAME_ARCHITECTURE_ANALYSIS.md to get the full picture.** 🎲🐸
