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
    // simple global tracking to allow only two players per game and a global waiting slot
    private static readonly ConcurrentDictionary<string, GameInstance> Games = new();
    private static readonly ConcurrentDictionary<string, string> PlayerGame = new(); // connectionId -> gameId
    private static readonly object _lock = new();

    // Maximum players per game = 2
    public override async Task OnConnectedAsync()
    {
        var connId = Context.ConnectionId;
        GameInstance? assigned = null;
        lock (_lock)
        {
            // try to find an existing waiting game (has one player and not started)
            assigned = Games.Values.FirstOrDefault(g => !g.HasSecondPlayer && !g.Started);
            if (assigned == null)
            {
                // create new game
                var id = Guid.NewGuid().ToString("N");
                assigned = new GameInstance(id);
                Games[id] = assigned;
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
        if (!PlayerGame.TryGetValue(connId, out var gid) || !Games.TryGetValue(gid, out var g))
        {
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }

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
        if (!PlayerGame.TryGetValue(connId, out var gid) || !Games.TryGetValue(gid, out var g))
        {
            await Clients.Caller.SendAsync("Error", "Game not found");
            return;
        }

        if (!g.Started)
        {
            await Clients.Caller.SendAsync("Error", "Game has not started");
            return;
        }

        if (g.CurrentTurn != connId)
        {
            await Clients.Caller.SendAsync("Error", "Not your turn");
            return;
        }

        var opponentId = g.Other(connId);
        if (opponentId == null)
        {
            await Clients.Caller.SendAsync("Error", "Opponent not connected");
            return;
        }

        var target = new Point(col, row);
        var hit = g.RegisterShot(opponentId, target, out bool opponentLost);

        Console.WriteLine($"Move from {connId}: {col},{row} hit={hit}");

        // inform both clients
        // caller gets MoveResult (col,row,hit, remainingOppShips)
        var remaining = g.GetRemainingShips(opponentId);
        await Clients.Caller.SendAsync("MoveResult", col, row, hit, remaining);
        // opponent gets OpponentMoved (col,row,hit)
        await Clients.Client(opponentId).SendAsync("OpponentMoved", col, row, hit);

        if (opponentLost)
        {
            // game over
            await Clients.Caller.SendAsync("GameOver", "You win!");
            await Clients.Client(opponentId).SendAsync("GameOver", "You lose.");
            // cleanup
            Games.TryRemove(gid, out _);
            if (g.PlayerA != null)
                PlayerGame.TryRemove(g.PlayerA, out _);
            if (g.PlayerB != null)
                PlayerGame.TryRemove(g.PlayerB, out _);
            return;
        }

        // turn logic: if hit, keep turn; if miss, switch
        if (!hit)
        {
            g.SwitchTurn();
        }

        // notify whose turn it is
        await Clients.Client(g.CurrentTurn).SendAsync("YourTurn");
        var other = g.Other(g.CurrentTurn);
        if (other != null)
            await Clients.Client(other).SendAsync("OpponentTurn");
    }

    private async Task StartGameIfReady(GameInstance g)
    {
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
