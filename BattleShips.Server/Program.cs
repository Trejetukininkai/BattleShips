// SITAM PROJEKTE B US SERVERIO PUSES LOGIKA
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BattleShips.Core;
using BattleShips.Core.Server;
using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();

var app = builder.Build();

// Load environment variables from .env if present
var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
if (File.Exists(envFilePath))
{
    DotNetEnv.Env.Load(envFilePath);
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";

app.MapHub<GameHub>("/game");
app.Urls.Add($"http://localhost:{port}");

Console.WriteLine($"BattleShips server listening on http://localhost:{port}/game");
await app.RunAsync();

public class GameHub : Hub
{
    public const int ShipPlacementTimeSeconds = 120;
    public const int MinePlacementTimeSeconds = 120;

    private record MoveResult
    {
        public int Col, Row, Remaining, Countdown;
        public bool Hit;
        public string? ErrorMessage, GameOverCaller, GameOverOpponent;
        public DisasterResult? Disaster;
    }

    private record DisasterResult
    {
        public string? TypeName;
        public List<Point> Affected = new();
        public List<Point> HitsForA = new();
        public List<Point> HitsForB = new();
    }

    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(IHubContext<GameHub> hubContext)
    {
        _hubContext = hubContext;
    }

    // Use GameManager singleton for all game state management
    private readonly GameManager _gameManager = GameManager.Instance;
    private static readonly object _lock = new();

    // Maximum players per game = 2
    public override async Task OnConnectedAsync()
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] OnConnectedAsync: conn={connId}");
        
        // Use GameManager to assign player to game
        var assigned = _gameManager.AssignPlayerToGame(connId);

        Console.WriteLine($"Client connected: {connId} -> game {assigned.Id} (players={assigned.PlayerCount})");

        if (assigned.PlayerCount == 1)
        {
            // tell the first player to wait
            await Clients.Caller.SendAsync("WaitingForOpponent", "Waiting for opponent to join...");
        }
        else if (assigned.PlayerCount == 2)
        {
            // both players connected -> start placement phase with 60s timer
            assigned.PlacementDeadline = DateTime.UtcNow.AddSeconds(ShipPlacementTimeSeconds);
            // notify both clients
            var playerA = assigned.PlayerA;
            var playerB = assigned.PlayerB;
            if (playerA != null)
                await Clients.Client(playerA).SendAsync("StartPlacement", ShipPlacementTimeSeconds);
            if (playerB != null)
                await Clients.Client(playerB).SendAsync("StartPlacement", ShipPlacementTimeSeconds);

            // start a background task to enforce placement timeout
            var gameId = assigned.Id; // capture game id before async task
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(ShipPlacementTimeSeconds));
                    Console.WriteLine($"[Server] Timeout expired for game {gameId}, checking ship placement");

                    // timeout expired - set cancellation flag (will be handled in hub methods)
                    lock (_lock)
                    {
                        var game = _gameManager.GetGame(gameId);
                        if (game != null && !game.Started)
                        {
                            Console.WriteLine($"[Server] Setting cancellation flag for game {gameId} due to timeout");
                            game.Started = true; // mark timeout expired
                            _ = StartGameIfReady(game); // fire and forget - will cancel if ships weren't placed
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Error in placement timeout task for game {gameId}: {ex.Message}");
                }
            });
        }

        await base.OnConnectedAsync();
    }

    private async Task StartActualGame(GameInstance g)
    {
        Console.WriteLine($"[Server] StartActualGame called for game {g.Id}");

        if (g.PlayerA == null || g.PlayerB == null)
        {
            Console.WriteLine($"[Server] Cannot start game - missing players");
            return;
        }

        g.CurrentTurn = g.PlayerA;
        g.Started = true;

        if (g.GameMode != null)
            g.GameMode.SelectEventBasedOnTurn(g.GetTurnCount());

        await Clients.Client(g.PlayerA).SendAsync("GameStarted", g.CurrentTurn == g.PlayerA);
        await Clients.Client(g.PlayerB).SendAsync("GameStarted", g.CurrentTurn == g.PlayerB);

        var initialCountdown = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
        Console.WriteLine($"[Server] Sending initial DisasterCountdown={initialCountdown}");

        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterCountdown", initialCountdown);
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterCountdown", initialCountdown);

        await Clients.Client(g.CurrentTurn).SendAsync("YourTurn");
        var other = g.Other(g.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");

        Console.WriteLine($"[Server] Game {g.Id} successfully started!");
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        
        // Use GameManager to handle player removal
        var (game, gameId) = _gameManager.RemovePlayer(connId);
        
        if (game != null && gameId != null)
        {
            // Notify opponent
            var other = game.Other(connId);
            if (other != null)
            {
                await Clients.Client(other).SendAsync("OpponentDisconnected", "Opponent disconnected.");
            }
        }

        Console.WriteLine($"Client disconnected: {connId}");
        await base.OnDisconnectedAsync(exception);
    }

    // place ships (list of points), ships are now multi-cell rectangles
    public async Task PlaceShips(List<Point> shipCells)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] PlaceShips called by {connId} with {shipCells?.Count ?? 0} positions");

        if (!_gameManager.ValidatePlayerInGame(connId, out var g, out var gid))
        {
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }

        if (g!.ShouldCancelOnNextAction)
        {
            await CancelGame(g, gid!, "Placement timeout exceeded");
            return;
        }

        var validationError = ValidateShipPlacement(shipCells);
        if (validationError != null)
        {
            await CancelGame(g, gid!, validationError);
            return;
        }

        g.SetPlayerShips(connId, shipCells ?? new List<Point>());

        if (connId == g.PlayerA) g.ShipsReadyA = true;
        else if (connId == g.PlayerB) g.ShipsReadyB = true;

        await Clients.Caller.SendAsync("PlacementAck", shipCells?.Count ?? 0);

        if (g.ShipsReadyA && g.ShipsReadyB)
        {
            Console.WriteLine($"[Server] Both players placed ships for game {g.Id}, notifying for mine placement");

            g.MinesReadyA = false;
            g.MinesReadyB = false;

            var tasks = new List<Task>();
            if (g.PlayerA != null)
                tasks.Add(_hubContext.Clients.Client(g.PlayerA)
                    .SendAsync("StartMinePlacement", GameHub.MinePlacementTimeSeconds));

            if (g.PlayerB != null)
                tasks.Add(_hubContext.Clients.Client(g.PlayerB)
                    .SendAsync("StartMinePlacement", GameHub.MinePlacementTimeSeconds));

            await Task.WhenAll(tasks);
        }
    }



    public async Task PlaceMines(List<Point> minePositions, List<string> mineCategories)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] 🔥 PlaceMines called by {connId} with {minePositions?.Count ?? 0} mines");

        if (!_gameManager.ValidatePlayerInGame(connId, out var g, out var gid))
        {
            Console.WriteLine($"[Server] ❌ Game not found for connection {connId}");
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }

        Console.WriteLine($"[Server] ✅ Found game {gid} for {connId}");


        var mines = new List<NavalMine>();
        if (minePositions != null && mineCategories != null && minePositions.Count == mineCategories.Count)
        {
            for (int i = 0; i < minePositions.Count; i++)
            {
                if (Enum.TryParse<MineCategory>(mineCategories[i], out var category))
                {
                    // Check if mine position overlaps with ships
                    var playerShips = connId == g.PlayerA ? g.ShipsA : g.ShipsB;
                    var hasShip = playerShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(minePositions[i]));

                    if (hasShip)
                    {
                        Console.WriteLine($"[Server] ❌ Mine placement rejected: position ({minePositions[i].X},{minePositions[i].Y}) has a ship");
                        await Clients.Caller.SendAsync("Error", $"Cannot place mine on ship at position ({minePositions[i].X},{minePositions[i].Y})");
                        return;
                    }

                    mines.Add(NavalMineFactory.CreateMine(minePositions[i], connId, category));
                    Console.WriteLine($"[Server] Created mine: {category} at ({minePositions[i].X},{minePositions[i].Y})");
                }
            }
        }

        g.SetPlayerMines(connId, mines);

        if (connId == g.PlayerA)
        {
            g.MinesReadyA = true;
            Console.WriteLine($"[Server] ✅ PlayerA mines ready. MinesReadyA={g.MinesReadyA}");
        }
        else if (connId == g.PlayerB)
        {
            g.MinesReadyB = true;
            Console.WriteLine($"[Server] ✅ PlayerB mines ready. MinesReadyB={g.MinesReadyB}");
        }

        Console.WriteLine($"[Server] 📊 Game State - MinesReadyA: {g.MinesReadyA}, MinesReadyB: {g.MinesReadyB}");

        await Clients.Caller.SendAsync("MinesAck", mines.Count);

        if (g.MinesReadyA && g.MinesReadyB)
        {
            Console.WriteLine($"[Server] 🎉🎉🎉 BOTH PLAYERS READY! Starting game {g.Id}!");
            await StartActualGame(g);
        }
        else
        {
            Console.WriteLine($"[Server] ⏳ Waiting for other player... (A: {g.MinesReadyA}, B: {g.MinesReadyB})");
        }
    }

    public async Task DebugGameState()
    {
        var connId = Context.ConnectionId;
        if (!_gameManager.ValidatePlayerInGame(connId, out var g, out var gid))
            return;

        Console.WriteLine($"[DEBUG] Game {gid}");
        Console.WriteLine($"[DEBUG] - Players: A={g.PlayerA}, B={g.PlayerB}");
        Console.WriteLine($"[DEBUG] - ShipsReady: A={g.ShipsReadyA}, B={g.ShipsReadyB}");
        Console.WriteLine($"[DEBUG] - MinesReady: A={g.MinesReadyA}, B={g.MinesReadyB}");
        Console.WriteLine($"[DEBUG] - Started: {g.Started}");
        Console.WriteLine($"[DEBUG] - CurrentTurn: {g.CurrentTurn}");
    }

    public async Task TestConnection(string message)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] TestConnection received from {connId}: {message}");
        await Clients.Caller.SendAsync("TestResponse", $"Server received: {message}");
    }

    private string? ValidateShipPlacement(List<Point>? shipCells)
    {
        if (shipCells == null || shipCells.Count == 0)
            return null; 

        if (shipCells.Count != shipCells.Distinct().Count())
            return "Ship placement contains duplicate positions";

        foreach (var cell in shipCells)
        {
            if (cell.X < 0 || cell.X >= Board.Size || cell.Y < 0 || cell.Y >= Board.Size)
                return $"Ship cell at ({cell.X}, {cell.Y}) is outside board bounds (0-{Board.Size - 1})";
        }

        // Check if total ship cells matches expected fleet (5+4+3+3+2 = 17 cells)
        var expectedCells = FleetConfiguration.StandardFleet.Sum();
        if (shipCells.Count != expectedCells)
            return $"Expected {expectedCells} ship cells, got {shipCells.Count}";

        return null; // Valid placement
    }

    private async Task CancelGame(GameInstance g, string gid, string reason)
    {
        Console.WriteLine($"[Server] Cancelling game {gid}: {reason}");

        var message = $"Game cancelled: {reason}";
        Console.WriteLine($"[Server] Sending GameCancelled to playerA={g.PlayerA} and playerB={g.PlayerB}");

        if (g.PlayerA != null)
        {
            try
            {
                await _hubContext.Clients.Client(g.PlayerA).SendAsync("GameCancelled", message);
                Console.WriteLine($"[Server] GameCancelled sent to playerA {g.PlayerA}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Failed to send GameCancelled to playerA {g.PlayerA}: {ex.Message}");
            }
        }

        if (g.PlayerB != null)
        {
            try
            {
                await _hubContext.Clients.Client(g.PlayerB).SendAsync("GameCancelled", message);
                Console.WriteLine($"[Server] GameCancelled sent to playerB {g.PlayerB}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Failed to send GameCancelled to playerB {g.PlayerB}: {ex.Message}");
            }
        }

        CleanupGame(gid, g);
    }

    public async Task MakeMove(int col, int row)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] MakeMove called by {connId} => ({col},{row})");

        if (!TryGetGame(connId, out var g, out var gid))
        {
            await SendError(connId, "Game not found");
            return;
        }

        if (g!.EventInProgress)
        {
            await SendError(connId, "Event in progress");
            return;
        }

        Console.WriteLine($"[Server] MakeMove: found game {gid} currentTurn={g.CurrentTurn}");

        MoveResult result;
        List<(Guid, MineCategory, List<Point>)> triggeredMines;

        lock (_lock)
        {
            result = HandleMoveUnderLock(g!, gid!, connId, col, row, out triggeredMines);
        }

        // Process mine notifications outside the lock
        await ProcessMineNotifications(g!, gid!, connId, triggeredMines);

        await ProcessMoveResults(g!, gid!, connId, result);
    }

    private bool TryGetGame(string connId, out GameInstance? game, out string? gid)
    {
        return _gameManager.ValidatePlayerInGame(connId, out game, out gid);
    }
    private async Task SendError(string connId, string message)
    {
        Console.WriteLine($"[Server] Error for {connId}: {message}");
        await Clients.Client(connId).SendAsync("Error", message);
    }
    private MoveResult HandleMoveUnderLock(GameInstance g, string gid, string connId, int col, int row, out List<(Guid, MineCategory, List<Point>)> triggeredMines)
    {
        var result = new MoveResult();
        triggeredMines = new List<(Guid, MineCategory, List<Point>)>();

        // Increment turn count on every move
        g.IncrementTurnCount();

        if (!g.Started)
        {
            result.ErrorMessage = "Game not started";
            return result;
        }

        if (g.CurrentTurn != connId)
        {
            result.ErrorMessage = "Not your turn";
            return result;
        }

        Console.WriteLine($"[Server] Registering shot for game={gid}");
        var target = new Point(col, row);
        bool hit = g.RegisterShotWithMines(g.Other(connId)!, target, out bool opponentLost, out triggeredMines);

        result.Col = col;
        result.Row = row;
        result.Hit = hit;
        result.Remaining = g.GetRemainingShips(g.Other(connId)!);

        if (opponentLost)
        {
            result.GameOverCaller = "You win!";
            result.GameOverOpponent = "You lose.";
            CleanupGame(gid, g);
        }
        else
        {
            if (!hit)
                g.SwitchTurn();

            result.Disaster = CheckForDisaster(g, gid);
        }

        result.Countdown = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
        return result;
    }

    private async Task ProcessMineNotifications(GameInstance g, string gid, string connId, List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMines)
    {
        if (triggeredMines.Any())
        {
            Console.WriteLine($"[Server] 💥 {triggeredMines.Count} mine(s) triggered!");

            // Collect all effects
            var allHealedCells = new List<Point>();
            var allMeteorStrikeCells = new List<Point>();
            var meteorHitsForA = new List<Point>(); 
            var meteorHitsForB = new List<Point>(); 

            foreach (var (mineId, category, effectPoints) in triggeredMines)
            {
                Console.WriteLine($"[Server] Mine {mineId} ({category}) triggered with {effectPoints.Count} effect points");

                if (category == MineCategory.AntiEnemy_Ricochet || category == MineCategory.AntiDisaster_Ricochet)
                {
                    Console.WriteLine($"[Server] 🌋 Meteor strike detected with {effectPoints.Count} impact points");
                    allMeteorStrikeCells.AddRange(effectPoints);

                    ApplyMeteorStrikeDamage(g, effectPoints, out var hitsA, out var hitsB);
                    meteorHitsForA.AddRange(hitsA);
                    meteorHitsForB.AddRange(hitsB);
                }
                else
                {
                    allHealedCells.AddRange(effectPoints);
                }

                // Send mine trigger event for visualization
                if (g.PlayerA != null)
                    await _hubContext.Clients.Client(g.PlayerA).SendAsync("MineTriggered", mineId, effectPoints, category.ToString());
                if (g.PlayerB != null)
                    await _hubContext.Clients.Client(g.PlayerB).SendAsync("MineTriggered", mineId, effectPoints, category.ToString());
            }

            // Send the healed cells to the owner of the mine
            if (allHealedCells.Any())
            {
                Console.WriteLine($"[Server] 📢 Sending {allHealedCells.Count} healed cells to mine owner");
                var mineOwner = g.Other(connId);
                if (mineOwner != null)
                {
                    await _hubContext.Clients.Client(mineOwner).SendAsync("CellsHealed", allHealedCells);
                }
            }

            // Send meteor strike hits to both players
            if (allMeteorStrikeCells.Any())
            {
                Console.WriteLine($"[Server] 📢 Sending meteor strike: {meteorHitsForA.Count} hits for A, {meteorHitsForB.Count} hits for B");

                if (g.PlayerA != null)
                {
                    // Send meteor strike visualization
                    await _hubContext.Clients.Client(g.PlayerA).SendAsync("MeteorStrike", allMeteorStrikeCells);
                    // Send actual hits on Player A's ships
                    if (meteorHitsForA.Any())
                    {
                        foreach (var hit in meteorHitsForA)
                        {
                            await _hubContext.Clients.Client(g.PlayerA).SendAsync("OpponentMoved", hit.X, hit.Y, true);
                        }
                    }
                    // Send hits on Player B's ships (visible on opponent board)
                    if (meteorHitsForB.Any())
                    {
                        foreach (var hit in meteorHitsForB)
                        {
                            await _hubContext.Clients.Client(g.PlayerA).SendAsync("OpponentHitByDisaster", hit.X, hit.Y);
                        }
                    }
                }

                if (g.PlayerB != null)
                {
                    // Send meteor strike visualization
                    await _hubContext.Clients.Client(g.PlayerB).SendAsync("MeteorStrike", allMeteorStrikeCells);
                    // Send actual hits on Player B's ships
                    if (meteorHitsForB.Any())
                    {
                        foreach (var hit in meteorHitsForB)
                        {
                            await _hubContext.Clients.Client(g.PlayerB).SendAsync("OpponentMoved", hit.X, hit.Y, true);
                        }
                    }
                    // Send hits on Player A's ships (visible on opponent board)
                    if (meteorHitsForA.Any())
                    {
                        foreach (var hit in meteorHitsForA)
                        {
                            await _hubContext.Clients.Client(g.PlayerB).SendAsync("OpponentHitByDisaster", hit.X, hit.Y);
                        }
                    }
                }
            }
        }
    }

    // Apply meteor strike damage and return which ships were hit
    private void ApplyMeteorStrikeDamage(GameInstance g, List<Point> strikePoints, out List<Point> hitsForA, out List<Point> hitsForB)
    {
        hitsForA = new List<Point>();
        hitsForB = new List<Point>();

        foreach (var point in strikePoints)
        {
            // Damage Player A's ships if hit
            if (g.PlayerA != null && g.ShipsA.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(point)))
            {
                g.HitCellsA.Add(point);
                hitsForA.Add(point);
                Console.WriteLine($"[Server] Meteor hit Player A at ({point.X},{point.Y})");
            }

            // Damage Player B's ships if hit
            if (g.PlayerB != null && g.ShipsB.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(point)))
            {
                g.HitCellsB.Add(point);
                hitsForB.Add(point);
                Console.WriteLine($"[Server] Meteor hit Player B at ({point.X},{point.Y})");
            }
        }
    }


    private DisasterResult? CheckForDisaster(GameInstance g, string gid)
    {
        if (g.GameMode == null) return null;

        if (!g.GameMode.DecrementCountdown()) return null;

        var gen = g.GameMode.EventGenerator;
        var affected = gen?.CauseDisaster() ?? new List<Point>();

        if (affected.Count == 0) return null;

        var hitsForA = new List<Point>();
        var hitsForB = new List<Point>();

        foreach (var p in affected)
        {
            // Check if disaster hits any ship cells for player A
            foreach (var ship in g.ShipsA.ToList())
            {
                if (ship.GetOccupiedCells().Contains(p))
                {
                    hitsForA.Add(p);
                    break;
                }
            }
            
            // Check if disaster hits any ship cells for player B
            foreach (var ship in g.ShipsB.ToList())
            {
                if (ship.GetOccupiedCells().Contains(p))
                {
                    hitsForB.Add(p);
                    break;
                }
            }
        }

        g.EventInProgress = true;
        g.GameMode.SelectEventBasedOnTurn(g.GetTurnCount());

        if (affected.Count == 0) return null;

        return new DisasterResult
        {
            TypeName = gen?.GetEventName(),
            Affected = affected,
            HitsForA = hitsForA,
            HitsForB = hitsForB
        };
    }

    private async Task ProcessMoveResults(GameInstance g, string gid, string connId, MoveResult result)
    {
        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            await SendError(connId, result.ErrorMessage);
            return;
        }

        // Send move updates
        await Clients.Caller.SendAsync("MoveResult", result.Col, result.Row, result.Hit, result.Remaining);

        var opp = g.Other(connId);
        if (opp != null)
            await Clients.Client(opp).SendAsync("OpponentMoved", result.Col, result.Row, result.Hit);

        // Turn updates
        await BroadcastTurnUpdates(gid, g);

        // Disaster updates
        if (result.Disaster != null)
            await HandleDisasterNotifications(g, gid, result.Disaster);

        // Game over
        if (result.GameOverCaller != null)
        {
            await Clients.Caller.SendAsync("GameOver", result.GameOverCaller);
            if (opp != null) await Clients.Client(opp).SendAsync("GameOver", result.GameOverOpponent);
        }

        // Countdown
        await BroadcastCountdown(g, g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1);
    }

    private void CleanupGame(string gid, GameInstance g)
    {
        _gameManager.RemoveGame(gid, g);
    }

    private async Task BroadcastTurnUpdates(string gid, GameInstance g)
    {
        var freshGame = _gameManager.GetGame(gid);
        if (freshGame?.CurrentTurn == null) return;

        await Clients.Client(freshGame.CurrentTurn).SendAsync("YourTurn");
        var other = freshGame.Other(freshGame.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");
    }
    private async Task HandleDisasterNotifications(GameInstance g, string gid, DisasterResult disaster)
    {
        Console.WriteLine($"[Server] Sending DisasterOccurred to players (type={disaster.TypeName})");

        if (g.PlayerA != null)
        {
            List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMinesA;

            g.ApplyDisasterWithMines(g.PlayerA, disaster.Affected, out triggeredMinesA);

            if (triggeredMinesA.Any())
            {
                Console.WriteLine($"[Server] 💥 {triggeredMinesA.Count} anti-disaster mine(s) triggered for PlayerA!");
                await ProcessMineNotifications(g, gid, g.PlayerA, triggeredMinesA);
            }

            g.RegisterDisasterHits(g.PlayerA, disaster.HitsForA);
        }

        if (g.PlayerB != null)
        {
            List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMinesB;

            g.ApplyDisasterWithMines(g.PlayerB, disaster.Affected, out triggeredMinesB);

            if (triggeredMinesB.Any())
            {
                Console.WriteLine($"[Server] 💥 {triggeredMinesB.Count} anti-disaster mine(s) triggered for PlayerB!");
                await ProcessMineNotifications(g, gid, g.PlayerB, triggeredMinesB);
            }

            g.RegisterDisasterHits(g.PlayerB, disaster.HitsForB);
        }

        // Check for game over after disaster
        bool playerALost = g.PlayerA != null && g.GetRemainingShips(g.PlayerA) == 0;
        bool playerBLost = g.PlayerB != null && g.GetRemainingShips(g.PlayerB) == 0;

        // Send disaster notifications
        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterOccurred", disaster.Affected, disaster.HitsForA, disaster.TypeName);
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterOccurred", disaster.Affected, disaster.HitsForB, disaster.TypeName);

        // Send updated remaining ship counts
        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterResult", g.GetRemainingShips(g.PlayerA));
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterResult", g.GetRemainingShips(g.PlayerB));

        // Send disaster hit notifications so players can see hits on their opponent's board
        // Player A should see hits that occurred on Player B's board (HitsForB) - these show on opponent board (right side)
        if (g.PlayerA != null && disaster.HitsForB.Count > 0)
        {
            foreach (var hit in disaster.HitsForB)
            {
                await Clients.Client(g.PlayerA).SendAsync("OpponentHitByDisaster", hit.X, hit.Y);
            }
        }
        // Player B should see hits that occurred on Player A's board (HitsForA) - these show on opponent board (right side)
        if (g.PlayerB != null && disaster.HitsForA.Count > 0)
        {
            foreach (var hit in disaster.HitsForA)
            {
                await Clients.Client(g.PlayerB).SendAsync("OpponentHitByDisaster", hit.X, hit.Y);
            }
        }

        // Handle game over from disaster
        if (playerALost && !playerBLost)
        {
            if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("GameOver", "You lose.");
            if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("GameOver", "You win!");
            CleanupGame(gid, g);
            return;
        }
        else if (playerBLost && !playerALost)
        {
            if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("GameOver", "You win!");
            if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("GameOver", "You lose.");
            CleanupGame(gid, g);
            return;
        }
        else if (playerALost && playerBLost)
        {
            if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("GameOver", "Draw - both players eliminated!");
            if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("GameOver", "Draw - both players eliminated!");
            CleanupGame(gid, g);
            return;
        }

        // Schedule animation cleanup (non-blocking)
        try
        {
            var affectedCount = disaster.Affected.Count;
            var animationMs = affectedCount * (300 + 120) + 400;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(animationMs);
                    lock (_lock) { g.EventInProgress = false; }
                    Console.WriteLine($"[Server] Disaster finished for game={gid}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] Disaster finish task failed: {ex}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Server] Failed to schedule disaster finish: {ex}");
        }
    }

    private async Task BroadcastCountdown(GameInstance g, int countdown)
    {
        if (countdown < 0) return;

        Console.WriteLine($"[Server] Broadcasting DisasterCountdown={countdown}");
        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterCountdown", countdown);
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterCountdown", countdown);
    }



    private async Task StartGameIfReady(GameInstance g)
    {
        Console.WriteLine($"[Server] StartGameIfReady called for game {g.Id}");
        if (g.PlayerA == null || g.PlayerB == null) return;

        if (!await CheckShipPlacement(g)) return; // Game was cancelled

        // choose starter (PlayerA)
        g.CurrentTurn = g.PlayerA;
        g.Started = true;

        if (g.GameMode != null)
            g.GameMode.SelectEventBasedOnTurn(g.GetTurnCount());

        await Clients.Client(g.PlayerA).SendAsync("GameStarted", g.CurrentTurn == g.PlayerA);
        await Clients.Client(g.PlayerB).SendAsync("GameStarted", g.CurrentTurn == g.PlayerB);

        var initialCountdown = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
        Console.WriteLine($"[Server] Sending initial DisasterCountdown={initialCountdown} to {g.PlayerA} and {g.PlayerB}");
        if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("DisasterCountdown", initialCountdown);
        if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("DisasterCountdown", initialCountdown);

        await Clients.Client(g.CurrentTurn).SendAsync("YourTurn");
        var other = g.Other(g.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");
    }

    private async Task<bool> CheckShipPlacement(GameInstance game)
    {   
        if (!game.ReadyA || !game.ReadyB)
        {
            var playerAName = game.PlayerA ?? "Player A";
            var playerBName = game.PlayerB ?? "Player B";
            
            string reason;
            if (!game.ReadyA && !game.ReadyB)
                reason = "Neither player placed ships in time";
            else if (!game.ReadyA)
                reason = "Player A did not place ships in time";
            else
                reason = "Player B did not place ships in time";

            Console.WriteLine($"[Server] Game {game.Id} cancelled: {reason}");
            
            var gid = game.Id;
            await CancelGame(game, gid, reason);
            
            return false; // Game cancelled
        }
        return true;
    }
    
    public async Task Ping(string who) => await Clients.Caller.SendAsync("Pong", $"hi {who}");

    public async Task SendHello(HelloMessage msg)
    {
        Console.WriteLine($"Received from {msg.From}: {msg.Text}");
        await Clients.All.SendAsync("ReceiveHello", msg);
    }
}
