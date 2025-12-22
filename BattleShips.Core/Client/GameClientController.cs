using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Thin wrapper that exposes GameClient events and methods to the UI layer.
    /// </summary>
    public class GameClientController
    {
        private readonly GameClient _client;
        private readonly GameModel _model;

        private readonly IGameMediator _mediator;

        // Chain of Responsibility Pattern components
        private BattleManager _battleManager;
        private List<IShip> _playerShips;
        private List<IShip> _enemyShips;

        // Events
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

        // Chain of Responsibility Events
        public event Action<string>? OnAttackResult;
        public event Action<IShip>? OnShipDestroyed;

        private void OnGameCancelled(string? message)
        {
            Console.WriteLine($"[GameClientController] OnGameCancelled invoked with: {message}");
            GameCancelled?.Invoke(message);
        }

        public GameClientController(GameClient client, GameModel model, IGameMediator mediator = null)
        {
            _client = client;
            _model = model;
            _mediator = mediator;

            // Initialize Chain of Responsibility components
            _battleManager = new BattleManager();
            _playerShips = new List<IShip>();
            _enemyShips = new List<IShip>();

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

        // Chain of Responsibility: Process player attack
        public async Task MakeMove(int col, int row)
        {
            Console.WriteLine($"[GameClientController] Player attacking ({col},{row})");

            // Process through Chain of Responsibility first
            var damageResult = ProcessPlayerAttack(col, row);

            // Notify UI about the attack result
            OnAttackResult?.Invoke(damageResult.Message);

            if (damageResult.WasHit)
            {
                Console.WriteLine($"HIT! {damageResult.Message}");
                if (damageResult.ShipDestroyed && damageResult.AffectedShip != null)
                {
                    OnShipDestroyed?.Invoke(damageResult.AffectedShip);
                    Console.WriteLine($"Ship destroyed: {damageResult.AffectedShip.GetType().Name}");
                }
            }
            else
            {
                Console.WriteLine($"MISS: {damageResult.Message}");
            }

            // Send the move to server (actual game logic)
            await _client.MakeMove(col, row);
        }

        #region Chain of Responsibility Methods

        /// <summary>
        /// Initialize ships for Chain of Responsibility processing
        /// </summary>
        public void InitializeShips(List<Point> playerShipPositions)
        {
            // Create player and enemy fleets
            _playerShips = FleetConfiguration.CreateStandardFleet();
            _enemyShips = FleetConfiguration.CreateStandardFleet();

            // Position player ships based on provided positions
            PositionPlayerShips(playerShipPositions);

            // Position enemy ships at random locations
            PositionEnemyShips();

            // Set ships in battle manager
            _battleManager.SetShips(_playerShips, _enemyShips);

            Console.WriteLine($"[Chain] Initialized {_playerShips.Count} player ships and {_enemyShips.Count} enemy ships");
        }

        private void PositionPlayerShips(List<Point> positions)
        {
            if (positions == null || positions.Count == 0) return;

            // Simple positioning - you might want to improve this
            for (int i = 0; i < Math.Min(_playerShips.Count, positions.Count); i++)
            {
                _playerShips[i].Position = positions[i];
                _playerShips[i].IsPlaced = true;
            }
        }

        private void PositionEnemyShips()
        {
            var random = new Random();
            int boardSize = 10; // Assuming 10x10 board

            foreach (var ship in _enemyShips)
            {
                // Place enemy ships randomly on the right side of board (columns 5-9)
                ship.Position = new Point(random.Next(5, boardSize - ship.Length), random.Next(0, boardSize));
                ship.Orientation = random.Next(2) == 0 ? ShipOrientation.Horizontal : ShipOrientation.Vertical;
                ship.IsPlaced = true;
            }
        }

        /// <summary>
        /// Process player attack on enemy ships using Chain of Responsibility
        /// </summary>
        public DamageResult ProcessPlayerAttack(int x, int y)
        {
            return _battleManager.PlayerAttack(x, y);
        }

        /// <summary>
        /// Process enemy attack on player ships using Chain of Responsibility
        /// </summary>
        public DamageResult ProcessEnemyAttack(int x, int y)
        {
            return _battleManager.EnemyAttack(x, y);
        }

        /// <summary>
        /// Reset Chain of Responsibility for new game
        /// </summary>
        public void ResetBattleSystem()
        {
            _battleManager = new BattleManager();
            _playerShips.Clear();
            _enemyShips.Clear();
            Console.WriteLine("[Chain] Battle system reset");
        }

        /// <summary>
        /// Get player ships (for UI display or debugging)
        /// </summary>
        public List<IShip> GetPlayerShips() => _playerShips;

        /// <summary>
        /// Get enemy ships (for UI display or debugging)
        /// </summary>
        public List<IShip> GetEnemyShips() => _enemyShips;

        #endregion

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

            // Handle player's move result from server
            _client.MoveResult += (c, r, h, rem) =>
            {
                MoveResult?.Invoke(c, r, h, rem);

                // Chain of Responsibility: Update our local game state
                if (h) // If hit
                {
                    var damageResult = ProcessPlayerAttack(c, r);
                    Console.WriteLine($"[Chain] Enemy ship hit at ({c},{r}): {damageResult.Message}");
                }
            };

            _client.GameStarted += youStart =>
            {
                Console.WriteLine($"[Client] GameStarted received, youStart={youStart}");
                _model.OnGameStarted(youStart);

                // Notify via mediator
                _mediator?.SendNotification("GameClient", "GameStarted", youStart);

                GameStarted?.Invoke(youStart);
            };

            // Add mediator notifications for other events
            _client.MoveResult += (c, r, h, rem) =>
            {
                MoveResult?.Invoke(c, r, h, rem);

                // Notify via mediator
                if (h)
                {
                    _mediator?.SendNotification("GameClient", "ShipHit", new { X = c, Y = r });
                }
                else
                {
                    _mediator?.SendNotification("GameClient", "ShipMissed", new { X = c, Y = r });
                }
            };

            _client.GameOver += msg =>
            {
                GameOver?.Invoke(msg);

                // Notify via mediator
                _mediator?.SendNotification("GameClient", "GameOver", msg);
            };

            // Handle opponent's move
            _client.OpponentMoved += (c, r, h) =>
            {
                OpponentMoved?.Invoke(c, r, h);

                // Chain of Responsibility: Process enemy attack on our ships
                if (h)
                {
                    var damageResult = ProcessEnemyAttack(c, r);
                    Console.WriteLine($"[Chain] Our ship hit at ({c},{r}): {damageResult.Message}");

                    if (damageResult.ShipDestroyed && damageResult.AffectedShip != null)
                    {
                        OnShipDestroyed?.Invoke(damageResult.AffectedShip);
                    }
                }
            };

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
                _model.ActionPoints = ap;
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

        /// <summary>
        /// Start a new game with Chain of Responsibility
        /// </summary>
        public void StartNewGame(List<Point> shipPositions)
        {
            ResetBattleSystem();
            InitializeShips(shipPositions);
            Console.WriteLine("[Chain] New game started with Chain of Responsibility");
        }
    }
}