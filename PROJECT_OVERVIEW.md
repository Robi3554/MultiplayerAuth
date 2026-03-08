# MultiplayerAuth — Project Overview

> **Engine:** Unity 6000.2.9f1 (Unity 6)
> **Networking:** FishNet (server-authoritative multiplayer)
> **Render Pipeline:** URP 17.2
> **Input:** New Input System 1.14.2
> **Other key packages:** Cinemachine 3.1.4, Animation Rigging 1.3.0, AI Navigation 2.0.9, Timeline 1.8.9

---

## Table of Contents

1. [High-Level Architecture](#high-level-architecture)
2. [Project Structure](#project-structure)
3. [Game Flow](#game-flow)
4. [Networking Model](#networking-model)
5. [Systems Breakdown](#systems-breakdown)
   - [Connection & Bootstrap](#1-connection--bootstrap)
   - [Player Spawning & Initialization](#2-player-spawning--initialization)
   - [Movement](#3-movement)
   - [Combat — Weapons](#4-combat--weapons)
   - [Combat — Melee](#5-combat--melee)
   - [Combat — Projectiles](#6-combat--projectiles)
   - [Player Stats & Health](#7-player-stats--health)
   - [Game Mode (Deathmatch)](#8-game-mode-deathmatch)
   - [AI Enemies](#9-ai-enemies)
   - [Pickups & Powerups](#10-pickups--powerups)
   - [Camera System](#11-camera-system)
   - [UI & HUD](#12-ui--hud)
   - [Scoreboard](#13-scoreboard)
   - [Emote System](#14-emote-system)
6. [Data Flow Diagram](#data-flow-diagram)
7. [Build Pipeline](#build-pipeline)
8. [Known Limitations & WIP](#known-limitations--wip)

---

## High-Level Architecture

This is a **server-authoritative multiplayer third-person shooter** built with FishNet. The game supports:

- **Dedicated server**, **host+client**, and **client-only** build configurations
- **Deathmatch** game mode (first to N kills wins)
- **Hitscan**, **projectile**, and **melee** weapon types
- **AI enemies** (patrolling/chasing/fleeing robots, stationary turrets)
- **Pickups** (health packs) and **powerups** (speed buff, big-head/damage buff)
- **Keyboard/Mouse** and **Gamepad** input with the New Input System
- **ParrelSync** support for multi-editor testing

There is **no lobby system, matchmaking, or authentication**. Players enter a username on a main menu, provide a server IP, and connect directly.

---

## Project Structure

```
Assets/
├── _Scripts/
│   ├── AI/                 # Robot enemies, turret, spawn manager
│   ├── Camera/             # Camera shake, billboard, perspective toggle
│   ├── Combat/
│   │   ├── Melee/          # Sword/melee attack system
│   │   ├── Projectile/     # Projectile weapon & projectile entity
│   │   └── RaycastShooting/# Hitscan weapon implementation
│   ├── Editor/             # Custom build scripts
│   ├── Managers/           # Bootstrap, game mode, player manager, menus
│   ├── PickUp/             # Pickup base class & health pickup
│   ├── PlayerScripts/      # Movement, stats, weapons, scoreboard, emotes
│   └── Powerups/           # Buff ScriptableObjects (speed, big head)
├── _Prefabs/
│   ├── Characters/         # Player.prefab, PlayerV2.prefab, Pause Menu
│   ├── NPCs/               # Kamikaze Robot, Lil' Robot
│   ├── Props/              # Medkit
│   ├── VFX/                # Flash, line tracer, damage indicators
│   └── Weapons/            # AR, Pistol, SMG, Shotgun, Sniper, Burst Rifle, Energy Pistol, Melee
├── _UI/
│   ├── NewUI/              # Game icons, UI element spritesheets
│   └── PlayerHUD/          # HUD bar sprites, minimap, PVP objective icons
├── Scenes/
│   ├── WelcomeScreen.unity # Main menu scene
│   └── SampleScene.unity   # Game scene
├── Powerups/               # ScriptableObject assets (Big Head Buff, Speed Buff)
├── FishNet/                # FishNet framework (imported)
└── PlayerControls.inputactions  # Input action definitions
```

---

## Game Flow

```
┌───────────────────────────────────────────────────────────────────┐
│                        WELCOME SCREEN                             │
│                                                                   │
│  Player enters username + IP address                              │
│  MainMenuController → writes to ConnectionInfo (DontDestroyOnLoad)│
│  Loads SampleScene                                                │
└─────────────────────────────┬─────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────────┐
│                     NETWORK BOOTSTRAP                             │
│                                                                   │
│  NetworkBootstrap reads ConnectionInfo.IpAddress                  │
│  Configures Tugboat transport (address + port)                    │
│  Starts connection based on build type:                           │
│    • DEDICATED_SERVER → Server only                               │
│    • CLIENT           → Client only                               │
│    • ParrelSync clone → Client                                    │
│    • ParrelSync original → Host (server + client)                 │
│    • Default          → Prompts for bind address                  │
└─────────────────────────────┬─────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────────┐
│                     PLAYER SPAWNING                               │
│                                                                   │
│  PlayerSpawner listens to OnClientLoadedStartScenes               │
│  Instantiates Player prefab at random spawn point                 │
│  Gives ownership to connecting client                             │
│  PlayerNetworkInitializer.OnStartServer() registers in            │
│    PlayerManager.players dictionary (keyed by ClientId)           │
│  PlayerStats sets username from ConnectionInfo                    │
│  PlayerCameraSetter configures Cinemachine on owner client        │
│  ScoreboardManager registers the new player entry                 │
└─────────────────────────────┬─────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────────┐
│                       GAMEPLAY LOOP                               │
│                                                                   │
│  ┌─── Movement ─────────────────────────────────────────────┐     │
│  │ PredictionMoving: WASD/stick → Rigidbody (owner-auth)    │     │
│  │ Sprint, jump, dash                                       │     │
│  │ Animator driven by velocity                              │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌─── Combat ───────────────────────────────────────────────┐     │
│  │ Owner fires → ServerRpc → Server validates/applies damage│     │
│  │ ObserversRpc replicates VFX/SFX to all clients           │     │
│  │ Three weapon types: Hitscan, Projectile, Melee           │     │
│  │ Weapon switching synced via SyncVar                       │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌─── Damage Pipeline ──────────────────────────────────────┐     │
│  │ Hit detected → PlayerManager.DamagePlayer() [Server]     │     │
│  │   → PlayerStats.TakeDamage()                             │     │
│  │   → Health reaches 0 → PlayerManager.PlayerKilled()      │     │
│  │     → Increment kills/deaths                             │     │
│  │     → GameModeManager.OnPlayerKill() checks win condition│     │
│  │     → Respawn after delay at random spawn point           │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌─── AI Enemies ───────────────────────────────────────────┐     │
│  │ RobotSpawnManager spawns robots on timer (server-only)   │     │
│  │ KamikazeRobot: patrol → chase → explode on contact       │     │
│  │ LittleRobot: patrol → flee → drops powerup when killed   │     │
│  │ Turret: stationary, targets nearest player, fires projs  │     │
│  └──────────────────────────────────────────────────────────┘     │
│                                                                   │
│  ┌─── Win Condition ────────────────────────────────────────┐     │
│  │ First player to reach killsToWin triggers:               │     │
│  │   → Winner announced to all (ObserversRpc)               │     │
│  │   → Countdown displayed                                  │     │
│  │   → All player stats reset, all players respawned        │     │
│  └──────────────────────────────────────────────────────────┘     │
└───────────────────────────────────────────────────────────────────┘
```

---

## Networking Model

| Aspect | Authority | Mechanism |
|---|---|---|
| **Movement** | Owner (client) | Rigidbody physics on owner; no server reconciliation |
| **Damage / Health** | Server | `PlayerManager.DamagePlayer()` runs `[Server]`-side only |
| **Kills / Deaths** | Server | SyncVars on `PlayerStats`, modified server-side |
| **Weapon Switching** | Server | Owner → `[ServerRpc]` → Server sets `SyncVar<int>` → all clients react via `OnChange` |
| **Hitscan Shots** | Client-reported, server-applied | Owner raycasts → `[ServerRpc]` reports hit → server applies damage |
| **Projectiles** | Server | Server spawns `ProjectileScript` NetworkObject with `ServerManager.Spawn()` |
| **Melee** | Server-validated | Owner requests slash → `[ServerRpc]` → server enables collider → collision deals damage |
| **AI (Robots/Turrets)** | Server | All AI logic gated by `IsServerInitialized` |
| **Pickups** | Server-validated | Client triggers collision → `[ServerRpc(RequireOwnership=false)]` → server validates and applies |
| **Game Mode** | Server | `GameModeManager` tracks state in SyncVars, announces via `[ObserversRpc]` |
| **Username** | Client-set, server-synced | `[ServerRpc]` → `SyncVar` + `[ObserversRpc(BufferLast=true)]` |

### FishNet Features Used

- **SyncVar** with `OnChange` callbacks (health, kills, deaths, username, weapon index, game state)
- **[ServerRpc]** for client → server requests (hit reports, weapon change, pickup, melee, username)
- **[ObserversRpc]** for server → all clients broadcasts (VFX, SFX, winner announcements, head size)
- **[TargetRpc]** for server → specific client (respawn, reload, hit feedback, camera shake)
- **NetworkAnimator** for animation sync (emotes, melee triggers)
- **ServerManager.Spawn() / Despawn()** for projectiles, robots, pickups
- **ObjectPool** (`GetPooledInstantiated`) for player prefab instantiation
- **`[Replicate]`/`[Reconcile]`** present only in `PredictionShooting.cs` (WIP/prototype)

---

## Systems Breakdown

### 1. Connection & Bootstrap

| Script | Base Class | Role |
|---|---|---|
| `ConnectionInfo` | MonoBehaviour | Persistent singleton holding static IP and username across scene loads |
| `MainMenuController` | MonoBehaviour | Main menu UI (client-only). IP/username input, sanitization, scene loading |
| `NetworkBootstrap` | MonoBehaviour | Configures Tugboat transport and starts connection based on build defines |
| `NetworkCommandLineArgs` | MonoBehaviour | Reads `-port` CLI arg for dedicated server deployments |

**Flow:** `MainMenuController` → writes `ConnectionInfo.IpAddress` & `ConnectionInfo.username` → loads game scene → `NetworkBootstrap` reads IP and starts FishNet connection.

### 2. Player Spawning & Initialization

| Script | Base Class | Role |
|---|---|---|
| `PlayerSpawner` | NetworkBehaviour | Spawns player prefab at random point when client loads scene |
| `PlayerNetworkInitializer` | NetworkBehaviour | Networking hub on each player — registers in `PlayerManager`, provides RPCs for weapon hit/shot/reload/projectile events |
| `PlayerManager` | NetworkBehaviour | Singleton server-side authority — tracks all players, deals damage, handles kills/respawns |

**Flow:** Client loads → `PlayerSpawner` spawns prefab → `PlayerNetworkInitializer.OnStartServer()` registers in `PlayerManager.players[clientId]` → `PlayerStats` sets username → `ScoreboardManager` adds entry.

### 3. Movement

| Script | Base Class | Role |
|---|---|---|
| `PredictionMoving` | NetworkBehaviour | Owner-authoritative movement via Rigidbody. WASD + sprint + jump + dash. Drives animator. |

- Uses Input System callbacks (`OnMove`, `OnJump`, `OnDash`, `OnSprint`)
- Supports mouse and joystick toggling (`ToggleInputMode`)
- Movement is purely client-side on owner — **no FishNet prediction/reconciliation**
- Animator parameters synced via `NetworkAnimator`

### 4. Combat — Weapons

| Script | Base Class | Role |
|---|---|---|
| `Weapon` | MonoBehaviour | Abstract base: ammo, fire rate, reload, muzzle flash, wall-block check |
| `RaycastShoot` | Weapon | Hitscan weapon — raycast on fire, reports hits to server via RPC |
| `ProjectileShooting` | Weapon | Projectile weapon — tells server to spawn networked projectile |
| `WeaponInfo` | MonoBehaviour | Data holder for weapon HUD icon sprite |
| `WeaponHUD` | MonoBehaviour | Listens to weapon change events, updates HUD icon |
| `AmmoCounter` | MonoBehaviour | Standalone ammo display (appears unused) |
| `ChangeWeapons` | NetworkBehaviour | Weapon switching — SyncVar index, ServerRpc to change, updates Rig layers |

**Weapon Arsenal (Prefabs):**
| Weapon | Type |
|---|---|
| Pistol | Hitscan |
| SMG | Hitscan |
| Assault Rifle / AR | Hitscan |
| Burst Rifle | Hitscan |
| Shotgun | Hitscan |
| Sniper | Hitscan |
| Energy Pistol | Hitscan |
| ProjectileGun | Projectile |
| Melee (Sword) | Melee |

### 5. Combat — Melee

| Script | Base Class | Role |
|---|---|---|
| `PredictionMelee` | NetworkBehaviour | Melee weapon with cooldown. Owner requests slash via ServerRpc, server validates and enables collider. |
| `meleeCollision` | MonoBehaviour | Forwards trigger collisions from melee collider to `PredictionMelee.DealDamage()` |
| `GamerGirlAnimatorProxy` | MonoBehaviour | Animation event proxy — enables/disables collider, plays slash VFX |

**Flow:** Owner clicks → `[ServerRpc]` slash request → server enables `Slash` flag → melee animation plays → animation event enables MeshCollider → `meleeCollision.OnTriggerEnter()` → `PredictionMelee.DealDamage()` → `PlayerManager.DamagePlayer()`.

### 6. Combat — Projectiles

| Script | Base Class | Role |
|---|---|---|
| `ProjectileScript` | NetworkBehaviour | Server-authoritative projectile with damage falloff over distance |
| `ProjectileShooting` | Weapon | Requests server to spawn projectile via RPC |
| `LineProjectile` | MonoBehaviour | Visual-only bullet tracer (LineRenderer animation) |
| `PredictionShooting` | NetworkBehaviour | **WIP/Prototype** — uses `[Replicate]`/`[Reconcile]`, not actively used |

**Projectile features:**
- Damage falloff: lerps from full damage → `minDamage` based on distance traveled
- Server-spawned and despawned (`ServerManager.Spawn/Despawn`)
- Late-joiner support via `[ObserversRpc(BufferLast=true)]`
- On collision: damages players via `PlayerManager`, despawns on walls

### 7. Player Stats & Health

| Script | Base Class | Role |
|---|---|---|
| `PlayerStats` | NetworkBehaviour | Holds all per-player stats as SyncVars: health, kills, deaths, username |

**Features:**
- SyncVar health (0–100), kills, deaths, username
- Respawn immunity window (prevents damage during respawn)
- Damage multiplier (`damageMult`) modified by powerups
- Big-head mode (cosmetic + damage buff)
- Hit feedback: `[TargetRpc]` for sound + camera shake on the victim
- Damage VFX: `[ObserversRpc]` spawns floating damage numbers
- Health bar UI updates on owner client

### 8. Game Mode (Deathmatch)

| Script | Base Class | Role |
|---|---|---|
| `GameModeManager` | NetworkBehaviour | Singleton managing kills-to-win deathmatch |

**Flow:**
1. Player kills another → `PlayerStats.AddKill()` → `GameModeManager.OnPlayerKill()`
2. If kills >= `killsToWin` → `PlayerWon()` sets `SyncVar winnerName`, `SyncVar isGameActive = false`
3. `[ObserversRpc]` announces winner to all clients
4. Countdown displayed via `[ObserversRpc]` updates
5. `RestartGame()` → `PlayerManager.ResetAllPlayers()` → all stats reset, all players respawned
6. `isGameActive` set back to true

### 9. AI Enemies

| Script | Base Class | Role |
|---|---|---|
| `KamikazeRobot` | NetworkBehaviour | Patrols waypoints, chases nearest player, explodes on contact (50 damage) |
| `LittleRobot` | NetworkBehaviour | Patrols waypoints, flees from players, grants powerup when killed |
| `RobotSpawnManager` | NetworkBehaviour | Server singleton — spawns one robot at a time on a timer |
| `Turret` | NetworkBehaviour | Stationary, targets nearest player in range, fires projectiles |

**AI runs entirely server-side** (gated by `IsServerInitialized`). Uses Unity NavMeshAgent for pathfinding.

| Enemy | Behavior | On Death/Contact |
|---|---|---|
| Kamikaze Robot | Patrol → Chase → Explode | Deals 50 damage to player via `PlayerManager` |
| Little Robot | Patrol → Flee | Triggers `PowerupEffect` on killer |
| Turret | Stationary → Aim → Fire | Spawns `ProjectileScript` projectiles (attacker ID: -1) |

### 10. Pickups & Powerups

| Script | Base Class | Role |
|---|---|---|
| `PickUpObject` | NetworkBehaviour | Base pickup class — collision detection, `[ServerRpc]` validation |
| `HealthPickUp` | PickUpObject | Heals player by 50 HP |
| `PickUpRespawn` | NetworkBehaviour | Server-side respawn timer for consumed pickups |
| `PowerupEffect` | ScriptableObject | Abstract base for buff effects |
| `SpeedBuff` | PowerupEffect | Increases `PredictionMoving.moveRate` |
| `BigHeadBuff` | PowerupEffect | Inflates head + increases damage multiplier for 10 seconds |

**Pickup Flow:** Player enters trigger → `[ServerRpc(RequireOwnership=false)]` → server validates (not already picked up) → `ItemPickUp()` applies effect → `PickUpRespawn` despawns object and queues respawn.

**Powerup Assets:**
- `Speed Buff.asset` — granted when killing a `LittleRobot`
- `Big Head Buff.asset` — granted when killing a `LittleRobot`

### 11. Camera System

| Script | Base Class | Role |
|---|---|---|
| `PlayerCameraSetter` | NetworkBehaviour | Configures Cinemachine follow/look-at on owner client |
| `CameraShake` | MonoBehaviour | Singleton — Cinemachine noise-based screen shake on damage |
| `NetworkedCinemachineUpdater` | MonoBehaviour | Manual Cinemachine tick sync with FishNet tick rate |
| `Billboard` | MonoBehaviour | Makes UI elements (e.g., nameplates) face the camera |
| `ChangePerspective` | — | **Commented out** — was for camera perspective toggling |

### 12. UI & HUD

- **Main Menu** (`WelcomeScreen.unity`): username + IP input, connect/localhost buttons
- **Player HUD** (`PlayerHUD.prefab`): health bar, ammo display, weapon icon
- **Pause Menu** (`PauseMenuManager`): Escape toggle, leave match, quit game — switches Input System action maps
- **Winner Panel**: displayed via `GameModeManager` ObserversRpc with countdown
- **Damage Numbers**: floating VFX spawned on hit via ObserversRpc
- **UI Art**: sci-fi themed icon/element spritesheets in `_UI/NewUI/` and `_UI/PlayerHUD/`

### 13. Scoreboard

| Script | Base Class | Role |
|---|---|---|
| `ScoreboardManager` | NetworkBehaviour | Singleton — Tab-toggled overlay, manages entry list |
| `ScoreboardEntry` | MonoBehaviour | Single row: username, kills, deaths, health |

Players register on `OnStartClient`, unregister on `OnStopClient`. Updates every frame while visible.

### 14. Emote System

| Script | Base Class | Role |
|---|---|---|
| `EmoteSystem` | NetworkBehaviour | F1/F2/F3 triggers emote animations via NetworkAnimator |

---

## Data Flow Diagram

```
┌──────────────┐          ┌────────────────┐
│ MainMenu     │─writes──►│ ConnectionInfo │──read by──► NetworkBootstrap
│ Controller   │          │ (IP, username) │──read by──► PlayerStats
└──────────────┘          └────────────────┘

PlayerSpawner ──spawns──► Player Prefab
  ├── PlayerNetworkInitializer ──registers──► PlayerManager.players
  ├── PlayerStats ──registers──► ScoreboardManager
  ├── PlayerCameraSetter ──configures──► CinemachineCamera
  ├── PredictionMoving (owner-auth movement)
  ├── ChangeWeapons ──events──► WeaponHUD
  ├── EmoteSystem
  └── Weapons (children):
      ├── RaycastShoot ──── ServerRpc ──► PlayerNetworkInitializer ──► PlayerManager.DamagePlayer()
      ├── ProjectileShooting ─ ServerRpc ─► PlayerNetworkInitializer ──spawns──► ProjectileScript
      └── PredictionMelee ──── ServerRpc ──► Server collision ──► PlayerManager.DamagePlayer()
                          └──► RobotSpawnManager.DespawnRobot()

PlayerManager.DamagePlayer() ──►  PlayerStats.TakeDamage()
PlayerStats.AddKill()        ──►  GameModeManager.OnPlayerKill()
GameModeManager.RestartGame()──►  PlayerManager.ResetAllPlayers()

RobotSpawnManager ──spawns──► KamikazeRobot  ──on contact──► PlayerManager.DamagePlayer()
                  ──spawns──► LittleRobot    ──on death────► PowerupEffect.TriggerEffect()
Turret            ──spawns──► ProjectileScript──on hit──────► PlayerManager.DamagePlayer()

PickUpObject / HealthPickUp ──ServerRpc──► PlayerStats.HealPlayer() + PickUpRespawn
```

---

## Build Pipeline

The project includes a custom editor build script (`GameBuilder.cs`) with two menu items:

| Build | Scripting Backend | Configuration |
|---|---|---|
| **Windows Dev Client** | Mono | Development build with debugging |
| **Windows Release Client** | IL2CPP | Release/optimized |

Both builds include two scenes:
1. `Scenes/WelcomeScreen` (main menu)
2. `Scenes/SampleScene` (gameplay)

**Build defines:**
- `DEDICATED_SERVER` — headless server mode (auto-starts server)
- `CLIENT` — client-only mode (main menu + connect)
- Default / ParrelSync — host or editor testing

---

## Known Limitations & WIP

| Area | Status | Notes |
|---|---|---|
| **Movement prediction** | Not implemented | `PredictionMoving` is owner-authoritative only — no `[Replicate]`/`[Reconcile]`. Susceptible to cheating. |
| **Shooting prediction** | WIP/Prototype | `PredictionShooting.cs` has `[Replicate]`/`[Reconcile]` stubs but `proj.Initialize()` is commented out. Not used in production weapons. |
| **Hit validation** | Minimal | Hitscan hits are client-reported via ServerRpc. Server applies damage without re-validating the raycast. |
| **Lobby / Matchmaking** | None | Direct IP connection only. No room system, no player limit enforcement visible in code. |
| **Authentication** | None | Username is client-provided, sanitized to alphanumeric (max 20 chars). No account system. |
| **Camera perspective toggle** | Commented out | `ChangePerspective.cs` is entirely commented out. |
| **Player spawner** | Partial | `PlayerSpawnerCustom` currently only spawns for `conn.IsHost` — non-host clients may use a different code path. |
| **Ammo counter** | Possibly unused | `AmmoCounter.cs` exists but ammo display is also handled inside `Weapon.Update()`. |
