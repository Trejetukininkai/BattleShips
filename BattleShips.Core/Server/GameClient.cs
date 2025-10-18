using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace BattleShips.Core
{
    public class GameClient : IDisposable
    {
        private HubConnection? _conn;

        public bool IsConnected => _conn != null && _conn.State == HubConnectionState.Connected;

        public event Action<string>? WaitingForOpponent;
        public event Action<int>? StartPlacement;
        public event Action<int>? PlacementAck;
        public event Action<bool>? GameStarted;
        public event Action? YourTurn;
        public event Action? OpponentTurn;
        public event Action<int,int,bool,int>? MoveResult;
        public event Action<int,int,bool>? OpponentMoved;
        public event Action<string>? MaxPlayersReached;
        public event Action<string>? OpponentDisconnected;
        public event Action<string>? GameOver;
        public event Action<string>? Error;
        // affected cells, hits-for-this-client, disaster type name
        public event Action<List<Point>, List<Point>, string?>? DisasterOccurred;
        public event Action<int>? DisasterCountdownChanged;
        public event Action? DisasterFinished;

        public async Task ConnectAsync(string url)
        {
            Console.WriteLine($"[GameClient] ConnectAsync -> {url}");
            if (_conn != null)
            {
                await _conn.StopAsync();
                await _conn.DisposeAsync();
                _conn = null;
            }

            _conn = new HubConnectionBuilder()
                .WithUrl(url.TrimEnd('/') + "/game")
                .WithAutomaticReconnect()
                .Build();

            // map hub events to local events
            Console.WriteLine("[GameClient] Mapping hub events");
            _conn.On("DisasterFinished", () => { Console.WriteLine("[GameClient] DisasterFinished"); DisasterFinished?.Invoke(); });
            _conn.On<string>("WaitingForOpponent", s => { Console.WriteLine($"[GameClient] WaitingForOpponent: {s}"); WaitingForOpponent?.Invoke(s); });
            _conn.On<int>("StartPlacement", s => { Console.WriteLine($"[GameClient] StartPlacement: {s}"); StartPlacement?.Invoke(s); });
            _conn.On<int>("PlacementAck", c => { Console.WriteLine($"[GameClient] PlacementAck: {c}"); PlacementAck?.Invoke(c); });
            _conn.On<bool>("GameStarted", b => { Console.WriteLine($"[GameClient] GameStarted: {b}"); GameStarted?.Invoke(b); });
            _conn.On("YourTurn", () => { Console.WriteLine("[GameClient] YourTurn"); YourTurn?.Invoke(); });
            _conn.On("OpponentTurn", () => { Console.WriteLine("[GameClient] OpponentTurn"); OpponentTurn?.Invoke(); });
            _conn.On<int,int,bool,int>("MoveResult", (c, r, h, rem) => { Console.WriteLine($"[GameClient] MoveResult ({c},{r}) hit={h} rem={rem}"); MoveResult?.Invoke(c, r, h, rem); });
            _conn.On<int,int,bool>("OpponentMoved", (c, r, h) => { Console.WriteLine($"[GameClient] OpponentMoved ({c},{r}) hit={h}"); OpponentMoved?.Invoke(c, r, h); });
            _conn.On<string>("MaxPlayersReached", m => { Console.WriteLine($"[GameClient] MaxPlayersReached: {m}"); MaxPlayersReached?.Invoke(m); });
            _conn.On<string>("OpponentDisconnected", m => { Console.WriteLine($"[GameClient] OpponentDisconnected: {m}"); OpponentDisconnected?.Invoke(m); });
            _conn.On<string>("GameOver", m => { Console.WriteLine($"[GameClient] GameOver: {m}"); GameOver?.Invoke(m); });
            _conn.On<string>("Error", m => { Console.WriteLine($"[GameClient] Error: {m}"); Error?.Invoke(m); });
            _conn.On<List<Point>, List<Point>, string?>("DisasterOccurred", (affected, hitsForMe, type) => { Console.WriteLine($"[GameClient] DisasterOccurred -> {affected.Count} cells type={type}"); DisasterOccurred?.Invoke(affected, hitsForMe, type); });
            _conn.On<int>("DisasterCountdown", v => { Console.WriteLine($"[GameClient] DisasterCountdown -> {v}"); DisasterCountdownChanged?.Invoke(v); });

            await _conn.StartAsync();
            Console.WriteLine("[GameClient] Hub connection started");
            await _conn.SendAsync("Ping", "client-hello");
            Console.WriteLine("[GameClient] Ping sent");
        }

        public Task PlaceShips(List<Point> ships)
        {
            if (_conn == null) throw new InvalidOperationException("Not connected");
            return _conn.SendAsync("PlaceShips", ships);
        }

        public Task MakeMove(int col, int row)
        {
            if (_conn == null) throw new InvalidOperationException("Not connected");
            return _conn.SendAsync("MakeMove", col, row);
        }

        public async Task DisconnectAsync()
        {
            if (_conn != null)
            {
                await _conn.StopAsync();
                await _conn.DisposeAsync();
                _conn = null;
            }
        }

        public void Dispose()
        {
            try { _ = DisconnectAsync(); } catch { }
        }
    }
}