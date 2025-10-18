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
    // private readonly IHubContext<GameHub>? _hubContext;
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
            assigned.PlacementDeadline = DateTime.UtcNow.AddSeconds(60);
            // notify both clients
            var playerA = assigned.PlayerA;
            var playerB = assigned.PlayerB;
            if (playerA != null)
                await Clients.Client(playerA).SendAsync("StartPlacement", 60);
            if (playerB != null)
                await Clients.Client(playerB).SendAsync("StartPlacement", 60);

            // start a background task to enforce placement timeout
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(60));
                // start game even if not all placed
                lock (_lock)
                {
                    if (!assigned.Started)
                    {
                        assigned.Started = true; // force start
                    }
                }
                await StartGameIfReady(assigned);
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

    // Client -> server: place ships (list of points), ships are 1x1 cells; expect 10 positions
    public async Task PlaceShips(List<Point> ships)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] PlaceShips called by {connId} with {ships?.Count ?? 0} positions");
        if (!PlayerGame.TryGetValue(connId, out var gid) || !Games.TryGetValue(gid, out var g))
        {
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }
        Console.WriteLine($"[Server] Assigning {ships?.Count ?? 0} ships to player {connId} in game {gid}");

        // accept up to 10 unique valid positions
        var unique = ships?.Distinct().Take(10).ToList() ?? new List<Point>();
        if (unique.Count > 10) unique = unique.Take(10).ToList();

        g.SetPlayerShips(connId, new HashSet<Point>(unique));
        await Clients.Caller.SendAsync("PlacementAck", unique.Count);

        // if both ready start immediately
        if (g.ReadyA && g.ReadyB && !g.Started)
        {
            g.Started = true;
            await StartGameIfReady(g);
        }
    }

    // Client -> server: make move at col,row (col=X,row=Y)
    public async Task MakeMove(int col, int row)
    {
        var connId = Context.ConnectionId;
        Console.WriteLine($"[Server] MakeMove called by {connId} => ({col},{row})");
        GameInstance? g;
        string? gid;
        if (!PlayerGame.TryGetValue(connId, out gid) || !Games.TryGetValue(gid, out g))
        {
            Console.WriteLine($"[Server] MakeMove: game not found for conn {connId}");
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }
        // reject moves while a disaster event is in progress (server authoritative)
        if (g.EventInProgress)
        {
            Console.WriteLine($"[Server] MakeMove rejected: event in progress for game={gid} conn={connId}");
            await Clients.Caller.SendAsync("Error", "Event in progress");
            return;
        }
        Console.WriteLine($"[Server] MakeMove: found game {gid} currentTurn={g.CurrentTurn}");

        // prepare data to send after we release the lock
        #pragma warning disable CS0219 // Variable is assigned but its value is never used
        bool callerLost = false;

        var toCaller_MoveResult = (col:0, row:0, hit:false, remaining:0);
        var toOpponent_OpponentMoved = (col:0, row:0, hit:false);
        string? toCaller_GameOver = null;
        string? toOpponent_GameOver = null;
        int countdownToSend = -1;
        #pragma warning restore CS0219 // Variable is assigned but its value is never used
        List<Point>? disasterAffected = null;
        List<Point>? hitsForA = null;
        List<Point>? hitsForB = null;

        // send the disaster generator name so client can display it during animation
        string? disasterTypeName = null;

        lock (_lock)
        {
            Console.WriteLine($"[Server] MakeMove: inside lock for game={gid}");
            // validate turn & state while locked
            if (!g.Started)
            {
                Console.WriteLine($"[Server] MakeMove: game not started yet for {gid}");
                // still send error outside lock
                // mark by setting toCaller_GameOver as special message (or simply return)
                // simpler: throw out and send error immediately (no state change) 
                // but here we'll check and return
                // NOTE: do not await under lock in production
            }

            if (g.CurrentTurn != connId)
            {
                Console.WriteLine($"[Server] MakeMove: NOT your turn! conn={connId} currentTurn={g.CurrentTurn}");
                toCaller_GameOver = "Not your turn";
            }
            else
            {
                Console.WriteLine($"[Server] MakeMove: accepted. Registering shot for game={gid}");
                var target = new Point(col, row);
                bool hit = g.RegisterShot(g.Other(connId)!, target, out bool opponentLost);
                Console.WriteLine($"[Server] Shot result: hit={hit} opponentLost={opponentLost}");

                // prepare move result to send
                var remaining = g.GetRemainingShips(g.Other(connId)!);
                toCaller_MoveResult = (col, row, hit, remaining);
                toOpponent_OpponentMoved = (col, row, hit);

                if (opponentLost)
                {
                    // prepare game over messages and cleanup
                    toCaller_GameOver = "You win!";
                    toOpponent_GameOver = "You lose.";
                    // remove from dictionaries right away
                    Games.TryRemove(gid, out _);
                    if (g.PlayerA != null) PlayerGame.TryRemove(g.PlayerA, out _);
                    if (g.PlayerB != null) PlayerGame.TryRemove(g.PlayerB, out _);
                }
                else
                {
                    // turn logic
                    if (!hit)
                    {
                        g.SwitchTurn();
                    }

                    // --- disaster: decrement countdown per turn, execute when triggers ---
                    if (g.GameMode != null)
                    {
                        var before = g.GameMode.EventGenerator?.GetDisasterCountdown() ?? -1;
                        // Console.WriteLine($"[Server] Before decrement: game={gid} countdown={before}");
                        if (g.GameMode.DecrementCountdown())
                        {
                            Console.WriteLine($"[Server] Disaster triggered for game={gid}");
                            var gen = g.GameMode.EventGenerator;
                            disasterTypeName = gen?.GetEventName();
                            Console.WriteLine($"[Server] Generator type: {gen?.GetType().Name}");
                            // Generate affected cells and apply to server state while locked
                            var affected = gen!.CauseDisaster() ?? new List<Point>();
                            Console.WriteLine($"[Server] Disaster affected count: {affected.Count}");
                            if (affected.Count > 0)
                            {
                                disasterAffected = affected;
                                hitsForA = new List<Point>();
                                hitsForB = new List<Point>();
                                foreach (var p in affected)
                                {
                                    if (g.ShipsA.Remove(p)) hitsForA.Add(p);
                                    if (g.ShipsB.Remove(p)) hitsForB.Add(p);
                                }
                            }
                            g.EventInProgress = true;
                            // pick next generator and reset countdown (do this under lock)
                            g.GameMode.SelectRandomEventGenerator();
                            g.GameMode.ResetEventGenerator();
                        }
                        var after = g.GameMode.EventGenerator?.GetDisasterCountdown() ?? -1;
                        // Console.WriteLine($"[Server] After decrement: game={gid} countdown={after}");
                    }

                    // compute countdown to send
                    countdownToSend = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
                }
            }
        } // end lock

        // send immediate error if not your turn
        if (toCaller_GameOver == "Not your turn")
        {
            Console.WriteLine($"[Server] Rejecting move: Not your turn for conn={connId} (currentTurn={g.CurrentTurn})");
            await Clients.Caller.SendAsync("Error", "Not your turn");
            return;
        }

        // send normal move notifications
        await Clients.Caller.SendAsync("MoveResult", toCaller_MoveResult.col, toCaller_MoveResult.row, toCaller_MoveResult.hit, toCaller_MoveResult.remaining);
        var opp = g.Other(connId);
        if (opp != null)
            await Clients.Client(opp).SendAsync("OpponentMoved", toOpponent_OpponentMoved.col, toOpponent_OpponentMoved.row, toOpponent_OpponentMoved.hit);

        // send turn notifications (current turn might have changed inside lock)
        if (Games.TryGetValue(gid, out var freshGame))
        {
            if (freshGame.CurrentTurn != null)
            {
                await Clients.Client(freshGame.CurrentTurn).SendAsync("YourTurn");
                var other = freshGame.Other(freshGame.CurrentTurn);
                if (other != null)
                    await Clients.Client(other).SendAsync("OpponentTurn");
            }
        }

        // if disaster occurred, notify both clients (affected + which were hits for that client)
        if (disasterAffected != null)
        {
            Console.WriteLine($"[Server] Sending DisasterOccurred to players (type={disasterTypeName})");
            if (g.PlayerA != null)
                await Clients.Client(g.PlayerA).SendAsync("DisasterOccurred", disasterAffected, hitsForA, disasterTypeName);
            if (g.PlayerB != null)
                await Clients.Client(g.PlayerB).SendAsync("DisasterOccurred", disasterAffected, hitsForB, disasterTypeName);

            // also notify remaining counts
            if (g.PlayerA != null)
                await Clients.Client(g.PlayerA).SendAsync("DisasterResult", g.ShipsA.Count);
            if (g.PlayerB != null)
                await Clients.Client(g.PlayerB).SendAsync("DisasterResult", g.ShipsB.Count);

            // schedule clearing of EventInProgress and inform clients when finished
            try
            {
                var affectedCount = disasterAffected.Count;
                var animationMs = affectedCount * (300 + 120) + 400; // match client animation timings + buffer
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(animationMs);
                        lock (_lock) { g.EventInProgress = false; }
                        // if (g.PlayerA != null) await _hubContext!.Clients.Client(g.PlayerA).SendAsync("DisasterFinished");
                        // if (g.PlayerB != null) await _hubContext!.Clients.Client(g.PlayerB).SendAsync("DisasterFinished");
                        Console.WriteLine($"[Server] Disaster finished for game={gid}");
                    }
                    catch (Exception ex) { Console.WriteLine($"[Server] Disaster finish task failed: {ex}"); }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Server] Failed to schedule disaster finish: {ex}");
            }
            finally
            {
                // reset countdown to send since event just occurred
                countdownToSend = g.GameMode?.EventGenerator?.GetDisasterCountdown() ?? -1;
            }
        }

        // broadcast updated countdown if computed
        if (countdownToSend >= -1)
        {
            Console.WriteLine($"[Server] Broadcasting DisasterCountdown={countdownToSend} for game={gid}");
            if (g.PlayerA != null) await Clients.Client(g.PlayerA).SendAsync("DisasterCountdown", countdownToSend);
            if (g.PlayerB != null) await Clients.Client(g.PlayerB).SendAsync("DisasterCountdown", countdownToSend);
        }

        // finally send game over messages (if prepared)
        if (toCaller_GameOver == "You win!")
        {
            if (connId != null) await Clients.Caller.SendAsync("GameOver", "You win!");
            var opponentId = g.Other(connId!);
            if (opponentId != null) await Clients.Client(opponentId).SendAsync("GameOver", "You lose.");
        }
    }

    private async Task StartGameIfReady(GameInstance g)
    {
        Console.WriteLine($"[Server] StartGameIfReady called for game {g.Id}");
        // ensure both players exist
        if (g.PlayerA == null || g.PlayerB == null) return;

        // default ships for any player who didn't place: empty set
        if (!g.ReadyA) g.ShipsA = new HashSet<Point>();
        if (!g.ReadyB) g.ShipsB = new HashSet<Point>();

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
    public async Task Ping(string who) => await Clients.Caller.SendAsync("Pong", $"hi {who}");

    public async Task SendHello(HelloMessage msg)
    {
        Console.WriteLine($"Received from {msg.From}: {msg.Text}");
        await Clients.All.SendAsync("ReceiveHello", msg);
    }

}
