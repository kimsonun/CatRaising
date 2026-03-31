# CatRaising — 2-Week Sprint Task List

## Milestone 1: Playable Prototype (Week 1)

### Core Infrastructure
- [/] `GameData.cs` — Serializable save data model
- [/] `SaveSystem.cs` — JSON save/load to persistent storage
- [/] `GameManager.cs` — Singleton, game state, first-launch detection
- [/] `TimeManager.cs` — Real-time sync, day/night phase detection

### Cat Scripts
- [/] `CatController.cs` — Main FSM (Idle, Walking, Sitting, Sleeping, BeingPet)
- [/] `CatNeeds.cs` — Four needs with real-time decay
- [/] `CatAnimator.cs` — Animation state management & sprite swaps
- [/] `CatAI.cs` — Autonomous behavior (wander, idle, nap)
- [/] `CatInteraction.cs` — Touch/tap responses (pet, scratch)

### UI
- [/] `HUDManager.cs` — Need bars, bond meter display
- [/] `NeedBarUI.cs` — Individual animated need bar component
- [/] `NamingScreenUI.cs` — First-launch cat naming screen

### Visuals
- [/] Generate placeholder room background (living room)
- [ ] Setup guide for Unity Editor (Animator, scene hierarchy)

---

## Milestone 2: Core Loop (Week 2)

### Interactables
- [/] `FoodBowl.cs` — Tap to fill, cat eats from it
- [/] `WaterBowl.cs` — Tap to fill, cat drinks from it
- [/] `DraggableToy.cs` — Drag feather toy for cat to chase

### Systems
- [/] `BondSystem.cs` — Bond level tracking with milestones
- [/] `DayNightController.cs` — Visual lighting changes based on real time

### Effects
- [/] `FloatingText.cs` — Floating text/hearts effect
- [/] Hearts particle prefab setup instructions

### Polish (if time permits)
- [ ] Cat eating/drinking animations (placeholder)
- [ ] Sound effect hooks (no audio files needed yet)
- [ ] Tutorial hints for first-time players
- [ ] Additional cat idle behaviors (yawn, stretch, groom)
