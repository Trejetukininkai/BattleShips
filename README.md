## BattleShips

Minimal SignalR demo with a WinForms client and ASP.NET Core server, plus a shared Core library.

### Projects
- **BattleShips.Server**: ASP.NET Core SignalR hub (`/game`).
- **BattleShips.Client**: WinForms app that connects to the hub and draws a 10×10 grid.
- **BattleShips.Core**: Shared types (e.g., `HelloMessage`, `Board.Size`).

### Prerequisites
- .NET 8 SDK
- Visual Studio 2022 or VS Code (optional)

### Environment variables
Supports `.env` files via DotNetEnv.

- Server: create `BattleShips.Server/.env`
```ini
PORT=5000
```

- Client: create `BattleShips.Client/.env`
```ini
API_URL=http://localhost:5000
```

Notes:
- Server loads `.env` from its content root.
- Client traverses upward from the executable folder to find `.env` in the project root.

### Run (Visual Studio)
1. Set multiple startup projects: start `BattleShips.Server` first, then `BattleShips.Client`.
2. Run. The server listens at `http://localhost:${PORT}/game`.
3. Client window title updates to show Ping/Pong, and a 10×10 board is drawn.

### Run (CLI)
In two terminals:
```bash
dotnet run --project BattleShips.Server
dotnet run --project BattleShips.Client
```

### What it does
- Server hub (`GameHub`):
  - `Ping(string who)` → sends `Pong` to the caller
  - `SendHello(HelloMessage msg)` → echoes message to all clients
- Client:
  - Connects to `${API_URL}/game`
  - Shows 10×10 grid with A–J and 1–10 labels
  - Click a cell to toggle an orange marker

### Troubleshooting
- Git errors about `.vs/*.vsidx`: add to `.gitignore` and remove from index
```gitignore
.vs/
**/bin/
**/obj/
*.vsidx
*.user
.env
.vscode/
```
```bash
git rm -r --cached .vs
git add .gitignore
git add .
git commit -m "Ignore IDE/build artifacts"
```

### Next steps
- Add game commands (e.g., `ShootAt`, `PlaceShip`) to Core and Server.
- Broadcast updates so multiple clients see the same board state.

