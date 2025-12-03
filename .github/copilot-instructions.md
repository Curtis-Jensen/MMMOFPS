# MMMOFPS Codebase Instructions for AI Agents

## Project Overview
This is a **multiplayer FPS game** built on **Unity 2021.3.45f2** using **Photon PUN 2** for networking. The project combines the Unity FPS Microgame with custom multiplayer networking infrastructure via Photon and a modular architecture across multiple C# assemblies.

## Architecture

### Assembly Structure
The project uses **modular assembly definitions** (`*.asmdef`) to enforce clean dependencies:

- **`fps.Game`** — Core game logic (actors, managers, health, weapons, damage, events)
- **`fps.Gameplay`** — Player-specific mechanics (character controller, weapon management, objectives)
- **`fps.AI`** — Enemy AI (detection, navigation, combat behavior)
- **`fps.UI`** — UI systems (health bars, ammo counters, in-game menus)
- **`fps.Editor`** — Editor-only tools (MiniProfiler, prefab replacers)
- **`Assembly-CSharp`** — Root game scripts and scene setup

### Key Component Patterns

**Core Managers** (`Assets/FPS/Scripts/Game/Managers/`):
- `EventManager` — Static pub/sub system for game events (derived from `GameEvent` base class)
- `GameFlowManager` — Win/loss conditions, scene loading, UI fade transitions
- `ActorsManager` — Tracks all actors (player + enemies) in the scene
- `ObjectiveManager` — Manages mission objectives and completion state
- `AudioManager` — Centralized audio playback control

**Shared Systems** (`Assets/FPS/Scripts/Game/Shared/`):
- `Health` — Damage handling component (used by both player and enemies)
- `Damageable` — Marks objects that can receive damage
- `WeaponController` — Base weapon firing logic
- `Actor` — Base class for player/enemies with team ID and actor lists

**AI System** (`Assets/FPS/Scripts/AI/`):
- `EnemyController` — Enemy main component requiring `Health`, `Actor`, `NavMeshAgent`
- `DetectionModule` — Sight-based target detection with line-of-sight checks
- `NavigationModule` — NavMesh movement with configurable speed/angular speed
- `WeaponModules` — Encapsulated weapon selection and firing per enemy

### Networking Integration
- **Photon PUN 2** integration via `RoomManager.cs` (root level)
- Uses `MonoBehaviourPunCallbacks` for network lifecycle: `OnConnectedToMaster()`, `OnJoinedLobby()`, `OnJoinedRoom()`
- Player prefabs observed via `PhotonView` for state synchronization
- NavMesh data stored as assets (see `NavMeshComponents` from Unity-Technologies)

## Data Flow & Events

**Event System** uses static `EventManager` broadcasting:
```csharp
// Listen
EventManager.AddListener<AllObjectivesCompletedEvent>(OnAllObjectivesCompleted);
// Broadcast
EventManager.Broadcast(new PlayerDeathEvent());
```

**Common Events**: `AllObjectivesCompletedEvent`, `PlayerDeathEvent`, `TargetDetectedEvent`, `AmmoChangedEvent`

**Game Loop Flow**:
1. Player spawns via `PlayerCharacterController`
2. Objectives tracked by `ObjectiveManager`
3. Enemies detect and pursue via `DetectionModule` → `NavigationModule`
4. Damage flows through `Health` → `DebugUtility` logging
5. Victory → `EventManager.Broadcast(AllObjectivesCompletedEvent)` → `GameFlowManager` loads win scene

## Development Workflows

### Building & Running
- Open project in **Unity 2021.3.45f2**
- Select play scene (typically in `Assets/FPS/Scenes/`)
- Press Play to test locally
- For networking: run multiple instances or connect to Photon cloud (requires AppId in PhotonNetworkingSettings)

### Common Code Patterns

**Finding Manager Instances**:
```csharp
var gameFlowManager = FindObjectOfType<GameFlowManager>();
```

**Subscribing to Events**:
```csharp
void Awake() {
    EventManager.AddListener<PlayerDeathEvent>(OnPlayerDeath);
}
void OnDestroy() {
    EventManager.RemoveListener<PlayerDeathEvent>(OnPlayerDeath);
}
```

**Accessing Player Components**:
```csharp
PlayerCharacterController player = FindObjectOfType<PlayerCharacterController>();
Health health = player.GetComponent<Health>();
```

## Critical Files & Patterns

| File | Purpose |
|------|---------|
| `EventManager.cs` | Static event pub/sub — all cross-component communication |
| `EnemyController.cs` | Enemy AI entry point; requires `DetectionModule` + `NavigationModule` |
| `GameFlowManager.cs` | Win/loss flow; listens to objective and player death events |
| `PlayerCharacterController.cs` | Player movement, stance, input handling |
| `Health.cs` | Damage tracking; fires `OnDamaged` events for UI/effects |
| `RoomManager.cs` | Photon network entry point |

## Assembly Definition Dependencies
- `fps.Game` → no dependencies (foundation)
- `fps.Gameplay` → depends on `fps.Game`
- `fps.AI` → depends on `fps.Game`
- `fps.UI` → depends on `fps.Game`, `fps.Gameplay`
- `fps.Editor` → depends on `fps.Game`, `fps.AI`, `fps.Gameplay`

## Code Conventions
- **Namespaces**: `Unity.FPS.Game`, `Unity.FPS.Gameplay`, `Unity.FPS.AI`, `Unity.FPS.UI`
- **Private fields**: `m_FieldName` prefix
- **Serializable fields**: Use `[SerializeField]` or `public` with `[Tooltip]` documentation
- **Editor-only**: Wrap in `#if UNITY_EDITOR` preprocessor directives
- **Error logging**: Use `DebugUtility` methods instead of direct `Debug.Log` for context

## Rendering & Performance
- Uses **Universal Render Pipeline (URP)** 12.1.15
- ProBuilder 5.2 for geometry tools
- `MeshCombiner` utility for batching static geometry
- NavMesh Components from Unity-Technologies (custom version in Assets, not a package)
