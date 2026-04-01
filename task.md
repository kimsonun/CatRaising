# CatRaising — 2-Week Sprint Task List

## Milestone 1: Playable Prototype (Week 1)

### Core Infrastructure
- [x] `GameData.cs` — Serializable save data model
- [x] `SaveSystem.cs` — JSON save/load to persistent storage
- [x] `GameManager.cs` — Singleton, game state, first-launch detection
- [x] `TimeManager.cs` — Real-time sync, day/night phase detection

### Cat Scripts
- [x] `CatController.cs` — Main FSM (Idle, Walking, Sitting, Sleeping, BeingPet)
- [x] `CatNeeds.cs` — Four needs with real-time decay
- [x] `CatAnimator.cs` — Animation state management & sprite swaps
- [x] `CatAI.cs` — Autonomous behavior (wander, idle, nap)
- [x] `CatInteraction.cs` — Touch/tap responses (pet, scratch)

### UI
- [x] `HUDManager.cs` — Need bars, bond meter display
- [x] `NeedBarUI.cs` — Individual animated need bar component
- [x] `NamingScreenUI.cs` — First-launch cat naming screen

### Visuals
- [x] Generate placeholder room background (living room)
- [x] Setup guide for Unity Editor (Animator, scene hierarchy)

---

## Milestone 2: Core Loop (Week 2)

### Interactables
- [x] `FoodBowl.cs` — Tap to fill, cat eats from it
- [x] `WaterBowl.cs` — Tap to fill, cat drinks from it
- [x] `DraggableToy.cs` — Drag feather toy for cat to chase

### Systems
- [x] `BondSystem.cs` — Bond level tracking with milestones
- [x] `DayNightController.cs` — Visual lighting changes based on real time

### Effects
- [x] `FloatingText.cs` — Floating text/hearts effect
- [x] Hearts particle prefab setup instructions

### Polish (if time permits)
- [ ] Cat eating/drinking animations (placeholder)
- [x] `SoundEffectHooks.cs` — Sound effect hooks (no audio files needed yet)
- [x] `TutorialHints.cs` — Tutorial hints for first-time players
- [x] `CatIdleBehaviors.cs` — Additional cat idle behaviors (yawn, stretch, groom)
