using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace BattleShips.Client
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

        public async Task ConnectAsync(string url)
        {
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
            _conn.On<string>("WaitingForOpponent", s => WaitingForOpponent?.Invoke(s));
            _conn.On<int>("StartPlacement", s => StartPlacement?.Invoke(s));
            _conn.On<int>("PlacementAck", c => PlacementAck?.Invoke(c));
            _conn.On<bool>("GameStarted", b => GameStarted?.Invoke(b));
            _conn.On("YourTurn", () => YourTurn?.Invoke());
            _conn.On("OpponentTurn", () => OpponentTurn?.Invoke());
            _conn.On<int,int,bool,int>("MoveResult", (c, r, h, rem) => MoveResult?.Invoke(c, r, h, rem));
            _conn.On<int,int,bool>("OpponentMoved", (c, r, h) => OpponentMoved?.Invoke(c, r, h));
            _conn.On<string>("MaxPlayersReached", m => MaxPlayersReached?.Invoke(m));
            _conn.On<string>("OpponentDisconnected", m => OpponentDisconnected?.Invoke(m));
            _conn.On<string>("GameOver", m => GameOver?.Invoke(m));
            _conn.On<string>("Error", m => Error?.Invoke(m));

            await _conn.StartAsync();
            await _conn.SendAsync("Ping", "client-hello");
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