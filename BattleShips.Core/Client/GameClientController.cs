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
        public event Action<int, int>? OpponentHitByDisaster;
        public event Action<string?>? MaxPlayersReached;
        public event Action<string?>? OpponentDisconnected;
        public event Action<string?>? GameOver;
        public event Action<string?>? GameCancelled;
        public event Action<List<Point>, List<Point>?, string?>? DisasterOccurred;
        public event Action? DisasterFinished;

        public event Action<int>? StartMinePlacement; // called when server says "place mines"
        public event Action<int>? MinesAck;           // called when server acknowledges mine placement

        public event Action<Guid, List<Point>, string>? MineTriggered;
        public event Action<List<Point>>? CellsHealed;

        public event Action<List<Point>>? MeteorStrike;

        public event Action<int>? ActionPointsUpdated;
        public event Action<string>? PowerUpActivated;



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

            _client.StartMinePlacement += duration =>
            {
                // Call the actual UI method
                _model.StartMinePlacement();
            };
        }

        public bool IsConnected => _client.IsConnected;

        // Expose the client for console interpreter
        public GameClient Client => _client;

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
            _client.OpponentHitByDisaster += (c, r) => OpponentHitByDisaster?.Invoke(c, r);
            _client.MaxPlayersReached += msg => MaxPlayersReached?.Invoke(msg);
            _client.OpponentDisconnected += msg => OpponentDisconnected?.Invoke(msg);
            _client.GameOver += msg => GameOver?.Invoke(msg);
            _client.GameCancelled += OnGameCancelled;
            _client.DisasterOccurred += (cells, hits, type) => DisasterOccurred?.Invoke(cells, hits, type);
            _client.DisasterFinished += () => DisasterFinished?.Invoke();
            _client.StartMinePlacement += secs => StartMinePlacement?.Invoke(secs);
            _client.MinesAck += count => MinesAck?.Invoke(count);
            _client.StartMinePlacement += duration =>
            {
                _model.State = AppState.MineSelection;
                _model.CurrentStatus = "Place your mine(s) on your board";
            };
            
            _client.MinesAck += count =>
            {
                Console.WriteLine($"[Client] MinesAck received, count={count}");
                MinesAck?.Invoke(count);
            };

            _client.GameStarted += youStart =>
            {
                Console.WriteLine($"[Client] GameStarted received, youStart={youStart}");
                _model.OnGameStarted(youStart); 
                GameStarted?.Invoke(youStart);
            };

            _client.MineTriggered += (id, points, category) => MineTriggered?.Invoke(id, points, category);
            _client.CellsHealed += (healedCells) => CellsHealed?.Invoke(healedCells);

            _client.MeteorStrike += (strikePoints) => MeteorStrike?.Invoke(strikePoints);

            _client.ActionPointsUpdated += (ap) =>
            {
                Console.WriteLine($"[ClientController] ActionPointsUpdated received: {ap}");
                _model.ActionPoints = ap; // ← THIS LINE IS MISSING!
                ActionPointsUpdated?.Invoke(ap);
            };
            _client.PowerUpActivated += (powerUp) => PowerUpActivated?.Invoke(powerUp);
        }

        public Task PlaceMines(List<Point> minePositions, List<string> mineCategories)
        {
            Console.WriteLine($"[GameClientController] 📢 PlaceMines called with {minePositions?.Count ?? 0} positions and {mineCategories?.Count ?? 0} categories");
            return _client.PlaceMines(minePositions, mineCategories);
        }

        public Task DebugGameState() => _client.DebugGameState();

        public Task TestConnection(string message) => _client.TestConnection(message);

        public Task ActivatePowerUp(string powerUpName)
        {
            Console.WriteLine($"[GameClientController] Activating powerup: {powerUpName}");
            return _client.ActivatePowerUp(powerUpName);
        }
    }
}
