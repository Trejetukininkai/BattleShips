// SITAM PROJEKTE B US SERVERIO PUSES LOGIKA
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BattleShips.Core;
using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

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
    public const int ShipPlacementTimeSeconds = 60;
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

    // simple global tracking to allow only two players per game and a global waiting slot
    private static readonly ConcurrentDictionary<string, GameInstance> Games = new();
    private static readonly ConcurrentDictionary<string, string> PlayerGame = new(); // connectionId -> gameId
    private static readonly object _lock = new();

    // Maximum players per game = 2
    public override async Task OnConnectedAsync()
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] OnConnectedAsync: conn={connId}");
        GameInstance? assigned = null;
        lock (_lock)
        {
            // try to find an existing waiting game (has one player and not started)
            assigned = Games.Values.FirstOrDefault(g => !g.HasSecondPlayer && !g.Started);
            if (assigned == null)
            {
                // create new game
                var id = Guid.NewGuid().ToString("N");
                assigned = new GameInstance(id)
                {
                    // initialize game mode so disaster countdown / generator exists
                    GameMode = new GameMode(shipCount: 10, boardX: Board.Size, boardY: Board.Size)
                };
                Console.WriteLine($"[Server] Created new GameInstance id={id} with GameMode");
                Games[id] = assigned;
            }
            else
            {
                Console.WriteLine($"[Server] Reusing game id={assigned.Id} for new connection");
            }

            // add player
            if (!assigned.HasFirstPlayer)
                assigned.PlayerA = connId;
            else if (!assigned.HasSecondPlayer)
                assigned.PlayerB = connId;

            PlayerGame[connId] = assigned.Id;
        }

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
                        if (Games.TryGetValue(gameId, out var game) && !game.Started)
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        if (PlayerGame.TryRemove(connId, out var gid))
        {
            if (Games.TryGetValue(gid, out var g))
            {
                // notify opponent
                var other = g.Other(connId);
                if (other != null)
                {
                    await Clients.Client(other).SendAsync("OpponentDisconnected", "Opponent disconnected.");
                }

                // remove game if no players left
                if (!g.HasFirstPlayer && !g.HasSecondPlayer)
                {
                    Games.TryRemove(gid, out _);
                }
                else
                {
                    // clear slot
                    g.RemovePlayer(connId);
                }
            }
        }

        Console.WriteLine($"Client disconnected: {connId}");
        await base.OnDisconnectedAsync(exception);
    }

    // Client -> server: place ships (list of points), ships are now multi-cell rectangles
    public async Task PlaceShips(List<Point> shipCells)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] PlaceShips called by {connId} with {shipCells?.Count ?? 0} positions");
        if (!PlayerGame.TryGetValue(connId, out var gid) || !Games.TryGetValue(gid, out var g))
        {
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }

        // Check if game should be cancelled due to timeout
        if (g.ShouldCancelOnNextAction)
        {
            Console.WriteLine($"[Server] Game {gid} timeout exceeded - cancelling on PlaceShips");
            await CancelGame(g, gid, "Placement timeout exceeded");
            return;
        }

        // Validate ship placements
        var validationError = ValidateShipPlacement(shipCells);
        if (validationError != null)
        {
            Console.WriteLine($"[Server] Ship placement validation failed for {connId}: {validationError}");
            await CancelGame(g, gid, validationError);
            return;
        }

        // Accept ship placement
        g.SetPlayerShips(connId, shipCells ?? new List<Point>());
        await Clients.Caller.SendAsync("PlacementAck", shipCells?.Count ?? 0);

        // if both ready start immediately
        if (g.ReadyA && g.ReadyB && !g.Started)
        {
            g.Started = true;
            await StartGameIfReady(g);
        }
    }

    private string? ValidateShipPlacement(List<Point>? shipCells)
    {
        if (shipCells == null || shipCells.Count == 0)
            return null; // Allow empty placements (game will start with 0 ships)

        // Check for duplicate positions
        if (shipCells.Count != shipCells.Distinct().Count())
            return "Ship placement contains duplicate positions";

        // Check if ship cells are within board bounds
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

        // Notify both players
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

        // Clean up game instance
        CleanupGame(gid, g);
    }

    // Client -> server: make move at col,row (col=X,row=Y)
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

        lock (_lock)
        {
            result = HandleMoveUnderLock(g, gid, connId, col, row);
        }

        await ProcessMoveResults(g, gid, connId, result);
    }

    private bool TryGetGame(string connId, out GameInstance? game, out string gid)
    {
        game = null!;
        gid = null!;
        if (!PlayerGame.TryGetValue(connId, out gid!) || !Games.TryGetValue(gid, out game))
        {
            Console.WriteLine($"[Server] MakeMove: game not found for conn {connId}");
            return false;
        }
        return true;
    }
    private async Task SendError(string connId, string message)
    {
        Console.WriteLine($"[Server] Error for {connId}: {message}");
        await Clients.Client(connId).SendAsync("Error", message);
    }
    private MoveResult HandleMoveUnderLock(GameInstance g, string gid, string connId, int col, int row)
    {
        var result = new MoveResult();

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
        bool hit = g.RegisterShot(g.Other(connId)!, target, out bool opponentLost);

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
        g.GameMode.SelectRandomEventGenerator();
        g.GameMode.ResetEventGenerator();

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
        await BroadcastCountdown(g, result.Countdown);
    }

    private void CleanupGame(string gid, GameInstance g)
    {
        Games.TryRemove(gid, out _);
        if (g.PlayerA != null) PlayerGame.TryRemove(g.PlayerA, out _);
        if (g.PlayerB != null) PlayerGame.TryRemove(g.PlayerB, out _);
        Console.WriteLine($"[Server] Cleaned up game {gid}");
    }

    private async Task BroadcastTurnUpdates(string gid, GameInstance g)
    {
        if (!Games.TryGetValue(gid, out var freshGame)) return;
        if (freshGame.CurrentTurn == null) return;

        await Clients.Client(freshGame.CurrentTurn).SendAsync("YourTurn");
        var other = freshGame.Other(freshGame.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");
    }
    private async Task HandleDisasterNotifications(GameInstance g, string gid, DisasterResult disaster)
    {
        Console.WriteLine($"[Server] Sending DisasterOccurred to players (type={disaster.TypeName})");

        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterOccurred", disaster.Affected, disaster.HitsForA, disaster.TypeName);
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterOccurred", disaster.Affected, disaster.HitsForB, disaster.TypeName);

        // Remaining ships after disaster
        if (g.PlayerA != null)
            await Clients.Client(g.PlayerA).SendAsync("DisasterResult", g.ShipsA.Count);
        if (g.PlayerB != null)
            await Clients.Client(g.PlayerB).SendAsync("DisasterResult", g.ShipsB.Count);

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
        // ensure both players exist
        if (g.PlayerA == null || g.PlayerB == null) return;

        // Check if both players placed ships
        if (!await CheckShipPlacement(g)) return; // Game was cancelled

        // choose starter (PlayerA)
        g.CurrentTurn = g.PlayerA;
        g.Started = true;

        // inform both clients that game started and who begins
        await Clients.Client(g.PlayerA).SendAsync("GameStarted", g.CurrentTurn == g.PlayerA);
        await Clients.Client(g.PlayerB).SendAsync("GameStarted", g.CurrentTurn == g.PlayerB);

        // send initial disaster countdown to both clients (if game mode exists)
        var initialCountdown = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
        Console.WriteLine($"[Server] Sending initial DisasterCountdown={initialCountdown} to {g.PlayerA} and {g.PlayerB}");
        if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("DisasterCountdown", initialCountdown);
        if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("DisasterCountdown", initialCountdown);

        // send initial turn notifications
        await Clients.Client(g.CurrentTurn).SendAsync("YourTurn");
        var other = g.Other(g.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");
    }

    private async Task<bool> CheckShipPlacement(GameInstance game)
    {   
        // If at least one player didn't place ships, cancel game and notify both players
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
            
            // Find the game ID for this instance
            var gid = Games.FirstOrDefault(x => x.Value == game).Key;
            if (gid != null)
            {
                await CancelGame(game, gid, reason);
            }
            
            return false; // Game cancelled
        }

        // Both players placed ships - game can proceed
        return true;
    }
    
    public async Task Ping(string who) => await Clients.Caller.SendAsync("Pong", $"hi {who}");

    public async Task SendHello(HelloMessage msg)
    {
        Console.WriteLine($"Received from {msg.From}: {msg.Text}");
        await Clients.All.SendAsync("ReceiveHello", msg);
    }

}
