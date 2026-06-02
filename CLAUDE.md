# Freeline — Claude Code Context

## Game Summary
Freeline is a mobile 2D cozy life simulation game. The player is Maya, a freelance digital artist living alone in her apartment. She takes drawing commissions, publishes a webtoon chapter by chapter, manages daily needs (energy, sleep, hunger), and grows her career and living space over time. Tone: warm, slightly melancholic, slice-of-life. No combat, no fail states — just life.

## Unity Environment
- **Unity Version:** 6000.4.4f1 (Unity 6)
- **Render Pipeline:** Universal Render Pipeline (URP) 2D
- **Target Platform:** Mobile (iOS + Android)
- **Orientation:** Portrait only (locked). Never assume landscape.
- **Input:** Unity Input System package (not legacy Input)

## Folder Conventions
All game content lives under `Assets/`:

```
Scripts/
  Core/        ← GameManager, SceneLoader, bootstrapping
  Managers/    ← TimeManager, EnergyManager, JobManager, SaveManager, etc.
  UI/          ← UI view controllers, HUD scripts
  Player/      ← Player stats, state machine
  Jobs/        ← Commission/job system logic
  Webtoon/     ← Webtoon creation and publishing flow
  World/       ← Room/apartment interaction, interactables
  Data/        ← Serializable data models, save structs
  Utils/       ← Extensions, helpers, constants
Scenes/        ← One scene per major screen/room
Prefabs/
  UI/          ← UI prefabs (panels, popups, buttons)
  Characters/  ← Maya sprite prefabs
  World/       ← Furniture, props, interactables
  Effects/     ← Particles, visual effects
Sprites/
  Characters/  ← Maya and NPC sprite sheets
  World/       ← Room backgrounds, furniture art
  UI/          ← Decorative UI art (not Icons)
  Jobs/        ← Job preview thumbnails
  Backgrounds/ ← Full-screen background art
UI/
  Panels/      ← Panel/overlay UI art
  Icons/       ← Icon sprites
  Fonts/       ← Font assets
Audio/
  Music/       ← BGM tracks
  SFX/         ← Sound effects
ScriptableObjects/
  Jobs/        ← JobData SO assets
  Characters/  ← Character SO assets
  Items/       ← Item/furniture SO assets
Animations/
  Characters/
  UI/
Materials/     ← URP 2D materials
Data/          ← JSON configs, save templates
```

## Code Conventions
- **Namespace:** `Freeline` for all game scripts
- **Singletons:** Manager classes use the `Singleton<T>` base or inline DontDestroyOnLoad pattern in `GameManager`. Do not make every class a singleton — prefer event-driven or direct manager references.
- **ScriptableObjects:** Prefer SO-based data for anything the designer tweaks (job stats, energy costs, dialogue). No magic numbers in scripts.
- **Events:** Use C# `Action`/`event` for loose coupling between managers. Avoid `FindObjectOfType` at runtime.
- **No logic in `Update`** unless truly per-frame. Prefer coroutines, events, or state machines.
- **Mobile:** Touch input only via Input System. No mouse-specific code paths. UI must be finger-friendly (min 48px touch targets). All layouts use anchors, not fixed positions.
- **Comments:** Only when the WHY is non-obvious. No narration comments.

## Core Systems (to be built)
- `GameManager` — singleton hub, game state machine, scene coordination
- `TimeManager` — in-game clock, day/night cycle, date tracking
- `EnergyManager` — energy + sleep + hunger stats, decay over time
- `JobManager` — available commissions, deadlines, completion
- `WebtoonManager` — chapter drafting, publishing, reader stats
- `SaveManager` — JSON serialization to persistent storage
- `UIManager` — panel stack, transitions

## Key Scenes (planned)
- `Bootstrap` — loads managers, then transitions to correct scene
- `Apartment` — main hub: bedroom, desk, kitchen, living room
- `DrawingDesk` — focused drawing minigame screen
- `JobBoard` — browse/accept commissions
- `WebtoonStudio` — chapter editor and publish screen
- `MainMenu` — title, new game, continue

## Current Progress

### Completed Systems
| System | File | Notes |
|--------|------|-------|
| `GameManager` | `Scripts/Core/GameManager.cs` | Singleton, DontDestroyOnLoad, holds all manager refs, GameState enum |
| `TimeManager` | `Scripts/Managers/TimeManager.cs` | Discrete clock (float hours), sleep window, auto-sleep at 24:00, OnDayEnded/OnNewDayStarted events. Config: `TimeConfig` SO |
| `EnergyManager` | `Scripts/Managers/EnergyManager.cs` | 0–100 bar, hunger penalty on restore, food buffs (speedMultiplier + durationHours), DefaultExecutionOrder(-10). Config: `EnergyConfig` SO |
| `SaveManager` | `Scripts/Managers/SaveManager.cs` | JsonUtility → `freeline_save.json`, auto-save on OnNewDayStarted, LoadGame/ApplyToManagers/CaptureFromManagers |
| `JobManager` | `Scripts/Managers/JobManager.cs` | Board of 3 jobs, state machine (Idle→BoardShowing→JobSelected→JobActive), energy gating, daily refresh limit, earlyFinishBonusActive hook. Config: `JobConfig` SO |
| `WebtoonManager` | `Scripts/Managers/WebtoonManager.cs` | Chapter production, follower gain (viral roll, equipment bonus, quality bonus), daily decay after grace period, passive coin income. Config: `WebtoonConfig` SO. DefaultExecutionOrder(-5) |
| `BootstrapManager` | `Scripts/Core/BootstrapManager.cs` | DefaultExecutionOrder(-100), runs LoadGame→ApplyToManagers→SetState(Apartment)→GenerateJobBoard, logs status report |
| `DebugTestRunner` | `Scripts/Core/DebugTestRunner.cs` | #if UNITY_EDITOR only. Keys: J=complete job, W=produce chapter, F=feed, N=new day, R=reset. Uses InputSystem (Keyboard.current) |
| `HUDManager` | `Scripts/UI/HUDManager.cs` | Canvas (Screen Space Overlay, 1080×1920), top panel (clock, energy slider, hunger, coins, gems), bottom nav bar (5 buttons). ContextMenu builder auto-populates SerializeField refs |

### Data / ScriptableObjects
| Class | File | Purpose |
|-------|------|---------|
| `SaveData` | `Scripts/Data/SaveData.cs` | Flat + nested (`WebtoonData`) serializable save state |
| `WebtoonData` | `Scripts/Data/WebtoonData.cs` | Followers, chapters published, daysSinceLastChapter, lifetimeEarnings |
| `JobData` | `Scripts/Data/JobData.cs` | Per-job SO: title, payout, energyCost, durationHours, difficulty, type, requiredLevel |
| `TimeConfig` | `Scripts/Managers/TimeConfig.cs` | startHour, sleepWindowStart, dayEndHour |
| `EnergyConfig` | `Scripts/Managers/EnergyConfig.cs` | maxEnergy, warningThreshold, hungerThreshold, hungerPenaltyMultiplier |
| `JobConfig` | `Scripts/Managers/JobConfig.cs` | boardSize, maxDailyRefreshes, tipBonusMultiplier |
| `WebtoonConfig` | `Scripts/Managers/WebtoonConfig.cs` | chapterProductionHours, chapterEnergyCost, followerGain, decayRate, viral params |

### Known Issues / Pending
- **HUD emoji warnings:** `coinText`, `gemText`, `hungerText` placeholder strings changed from emoji to plain text (`COIN:`, `GEM:`, `FOOD:`). Real icons will be sprites.

### Next Task
- **Job Board UI** — panel showing the 3 current board jobs, select/start flow, refresh button, energy-blocked state feedback.

## Code Documentation Standards
- All classes and public/internal methods must have `/// <summary>` XML comments in Turkish
- Inline `//` comments only where the WHY is non-obvious — no narration comments
- New files must follow this standard from the start
- Existing files are documented incrementally as they are touched
