#  M.E.C.H.A
M.E.C.H.A stand for Most Earning Capture or Hidden Altogether which is a multiplayer stealth game made using Unity and Netcode for GameObjects, where players take on two different roles: Owner and Thief. It includes features like close-proximity voice chat, a system to connect players, traps, and competitive rounds. In this exciting game, one player hides valuable items while the other tries to steal them, creating a thrilling chase.
## Overview
M.E.C.H.A is a multiplayer game where players take on different roles. It's made using Unity's Netcode, allowing you to play online without needing to change any settings on your router. 

In each round, there are two players: one is the Owner (defender) who hides valuable items around a map and sets traps, while the other is the Thief (attacker) who tries to break in, unlock doors, and find the hidden valuables. Each round lasts 90 seconds, which includes 30 seconds to set up and 60 seconds to take action. After three rounds, the player with the most money earned wins the game.
## Features
### Core Game Systems
* Asymmetric Multiplayer Gameplay
  * Two distinct roles with unique abilities and objectives
  * Owner: Hide valuables, set EMP traps, protect territory
  * Thief: Steal valuables, pick locks, search containers
  * Role swapping each round for balanced competition
* Network Architecture
  * Unity Netcode for GameObjects for reliable networking
  * Unity Relay service for seamless online matchmaking
  * Client-server model with server authority
  * NetworkVariables for automatic state synchronization
* Round-Based Competition
  * 3 rounds per match
  * 30-second Setup Phase (Owner hides items)
  * 60-second Action Phase (Thief searches)
  * Round-end scoring and transition
  * Automatic role swapping between rounds
### Role-Specific Mechanics
* Owner (Defender)
  * Hide valuables in containers around the map (max 5 items)
  * Deploy EMP traps that slow the Thief by 50% for 4 seconds
  * 3 EMP traps per round
  * Visual trap preview with placement validation
  * Must protect valuables from being stolen
* Thief (Attacker)
  * Search containers (3-second channeling time)
  * Use lockpicks to bypass locked doors instantly
  * 3 lockpicks per round
  * Steal valuables from containers (max 5 items)
## Technologies Used
* Unity 2022.3+ - Game engine
* Netcode for GameObjects
* Unity Relay
* Unity Scene Management
## Gameplay Mechanics
### Phase Flow
1. Waiting (Pre-match)
2. Setup Phase (30s)
3. Action Phase (60s)
4. Round End (5s)
### Scoring Formula
* Owner Round Money = (Hidden Items Survived × $100) + (Stolen Items Caught × $200)
* Thief Round Money = (Valuables Stolen × $200)
* Hidden Items Survived = max(0, 5 - valuablesToHide - valuablesStolenByOpponent)
* Stolen Items Caught = valuablesStolen (from opponent)
## Control
| Key | Action |
|-----|--------|
| WASD / Arrow Keys | Move |
| E | Interact (Hide/Search/Open) |
| 1 | Toggle Trap/Lockpick Selection |
| Spacebar | Place Trap |

## Project Structure

```bash
M.E.C.H.A/
├── Scripts/
│   ├── Network/
│   │   ├── RelayManager.cs
│   │   └── NetworkMatchManager.cs
│   ├── Player/
│   │   ├── PlayerMovement.cs
│   │   ├── PlayerSpawner.cs
│   │   └── NetworkPlayerUI.cs
│   ├── Interaction/
│   │   ├── InteractionController.cs
│   │   ├── BaseInteractable.cs
│   │   ├── NetworkLootContainer.cs
│   │   └── NetworkDoor.cs
│   ├── Traps/
│   │   ├── TrapPlacementController.cs
│   │   └── NetworkEmpTrap.cs
│   └── UI/
│       └── GameOverUIManager.cs
├── Scenes/
│   ├── Lobby.unity
│   ├── GameScene.unity
│   └── GameOver.unity
├── Prefabs/
│   ├── Player.prefab
│   ├── EMPTrap.prefab
│   └── LootContainer.prefab
└── README.md
```
## Installation
1. Install Unity Hub and Unity 2022.3 or newer from unity.com/download
2. Clone or download this repository
3. Open the project in Unity Hub by clicking "Open" and selecting the project folder
4. Wait for Unity to import all packages and compile scripts
5. In Unity, go to File → Build Settings
6. Click Add Open Scenes to add Lobby, GameScene, and GameOver scenes
7. Click Build and Run to compile and play
## Screenshots
### Main Menu
<img width="906" height="515" alt="image" src="https://github.com/user-attachments/assets/97837d3e-6bb4-41c6-91a8-d6e8f9f0a520" />

### Gameplay
<img width="853" height="507" alt="image" src="https://github.com/user-attachments/assets/e0cff4b7-e944-42ef-9c88-995e22f7b559" />
<img width="905" height="500" alt="image" src="https://github.com/user-attachments/assets/6373f9fd-81e2-4b60-9de4-0c719f2c940f" />

## Authors
* LAM MING KANG
* LOW WEN JUN
* WONG KAR MING
* LAI YAO XUAN
## License
This project is developed for educational purposes.
