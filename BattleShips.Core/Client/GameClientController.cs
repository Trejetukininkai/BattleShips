using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using BattleShips.Core;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Thin wrapper that exposes GameClient events and methods to the UI layer.
    /// </summary>
    public class GameClientController
    {
        private readonly GameClient _client;
        private readonly GameModel _model;

        public event Action<string?>? Error;
        public event Action<string?>? WaitingForOpponent;
        public event Action<int>? DisasterCountdownChanged;
        public event Action<int>? StartPlacement;
        public event Action<int>? PlacementAck;
        public event Action<bool>? GameStarted;
        public event Action? YourTurn;
        public event Action? OpponentTurn;
        public event Action<int, int, bool, int>? MoveResult;
        public event Action<int, int, bool>? OpponentMoved;
        public event Action<string?>? MaxPlayersReached;
        public event Action<string?>? OpponentDisconnected;
        public event Action<string?>? GameOver;
        public event Action<string?>? GameCancelled;
        public event Action<List<Point>, List<Point>?, string?>? DisasterOccurred;
        public event Action? DisasterFinished;

        private void OnGameCancelled(string? message)
        {
            Console.WriteLine($"[GameClientController] OnGameCancelled invoked with: {message}");
            GameCancelled?.Invoke(message);
        }

        public GameClientController(GameClient client, GameModel model)
        {
            _client = client;
            _model = model;
            WireClient();
        }

        public bool IsConnected => _client.IsConnected;

        public Task ConnectAsync(string baseUrl) => _client.ConnectAsync(baseUrl);
        public Task PlaceShips(List<Point> ships) => _client.PlaceShips(ships);
        public Task MakeMove(int col, int row) => _client.MakeMove(col, row);

        private void WireClient()
        {
            _client.Error += msg => Error?.Invoke(msg);
            _client.WaitingForOpponent += msg => WaitingForOpponent?.Invoke(msg);
            _client.DisasterCountdownChanged += v => DisasterCountdownChanged?.Invoke(v);
            _client.StartPlacement += secs => StartPlacement?.Invoke(secs);
            _client.PlacementAck += count => PlacementAck?.Invoke(count);
            _client.GameStarted += youStart => GameStarted?.Invoke(youStart);
            _client.YourTurn += () => YourTurn?.Invoke();
            _client.OpponentTurn += () => OpponentTurn?.Invoke();
            _client.MoveResult += (c, r, h, rem) => MoveResult?.Invoke(c, r, h, rem);
            _client.OpponentMoved += (c, r, h) => OpponentMoved?.Invoke(c, r, h);
            _client.MaxPlayersReached += msg => MaxPlayersReached?.Invoke(msg);
            _client.OpponentDisconnected += msg => OpponentDisconnected?.Invoke(msg);
            _client.GameOver += msg => GameOver?.Invoke(msg);
            _client.GameCancelled += OnGameCancelled;
            _client.DisasterOccurred += (cells, hits, type) => DisasterOccurred?.Invoke(cells, hits, type);
            _client.DisasterFinished += () => DisasterFinished?.Invoke();
        }
    }
}
