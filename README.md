# ⚔️ CANNONS ONLINE

A premium, fast-paced multiplayer artillery game built with **Unity 6** and **Netcode for GameObjects**. Battle your friends across the globe in a world of destructible environments and tactical precision.

## 🚀 Features

*   **⚡ Real-time Multiplayer**: Fully synchronized gameplay using Unity Netcode. Host a session or join via IP.
*   **🏙️ Destructible Environments**: Buildings aren't just for cover—they take instant, visual damage from every blast.
*   **🏆 3-Point Championship**: Matches are played in a "Best of 3" format. First to 3 kills is crowned the Champion!
*   **🎲 Smart Level Randomization**: Never take the same shot twice. Every point shuffles building positions and player spawns to keep the strategy fresh.
*   **💎 Premium HUD**: A high-end Midnight Blue, Gold, and Cyan aesthetic with full resolution scaling (looks great on everything from 720p to 4K).
*   **⚖️ Fair Play System**: Randomized spawns ensure that "memorizing" shot angles won't win the game—you have to adapt!

## 🎮 How to Play

### Controls
| Action | Key |
| :--- | :--- |
| **Aim Up/Down** | Arrow Up / Arrow Down |
| **Adjust Power** | Arrow Left / Arrow Right |
| **Fire Cannon** | Spacebar |
| **Restart Match** | 'R' (Host Only) |
| **Quit Game** | Escape |

### Objective
Destroy the enemy cannon 3 times before they destroy yours. Use the buildings for cover, but watch out—the environment will crumble as the battle progresses!

## 🛠️ Technical Setup

*   **Engine**: Unity 6 (6000.x)
*   **Networking**: Unity Netcode for GameObjects
*   **Transport**: Unity Transport (UTP)
*   **UI**: Immediate Mode GUI (IMGUI) with custom high-DPI scaling matrix.

## 📦 Installation & Building

1. Clone the repository:
   ```bash
   git clone https://github.com/DavidAAbbott/Cannons.git
   ```
2. Open the project in **Unity 6**.
3. Ensure the `DefaultNetworkPrefabs` are assigned in the `NetworkManager`.
4. Build the project for **Windows/Mac/Linux**.
5. Run two instances: **Host** one and **Join** as a Client using `127.0.0.1` (local) or your Public IP (remote).

---
*Created with ❤️ for Artillery enthusiasts.*
