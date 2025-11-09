using BattleShips.Core.Client;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    public enum AppState { Menu, Waiting, Placement, Playing, GameOver, MineSelection }

    // Observable for client side UI
    public class GameModel : INotifyPropertyChanged
    {
        public List<IShip> YourShips { get; } = new();
        public HashSet<Point> YourHitsByOpponent { get; } = new();
        public HashSet<Point> YourFired { get; } = new();
        public HashSet<Point> YourFiredHits { get; } = new();
        public HashSet<Point> AnimatedCells { get; } = new();

        public List<NavalMine> YourMines { get; } = new();

        public MineCategory? SelectedMineCategory { get; set; }

        public bool IsMinePlacementPhase => State == AppState.MineSelection;



        // for command design patterns - undoing
        private readonly Stack<ICommand> _placementHistory = new();

        // UI state properties with change notifications
        private string _currentStatus = "Ready to start your naval adventure";
        private AppState _state = AppState.Menu;
        private bool _isMyTurn;
        private int _placementSecondsLeft;
        private int _disasterCountdown = -1;
        private string? _currentDisasterName;
        private bool _isDisasterAnimating;
        private bool? _lastMoveResult; // null = no recent result, true = hit, false = miss

        // current disaster info (set while animating)
        public string? CurrentDisasterName
        {
            get => _currentDisasterName;
            set
            {
                if (_currentDisasterName != value)
                {
                    _currentDisasterName = value;
                    OnModelPropertyChanged(nameof(CurrentDisasterName));
                }
            }
        }

        public bool IsDisasterAnimating
        {
            get => _isDisasterAnimating;
            set
            {
                if (_isDisasterAnimating != value)
                {
                    _isDisasterAnimating = value;
                    OnModelPropertyChanged(nameof(IsDisasterAnimating));
                }
            }
        }

        public AppState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnModelPropertyChanged(nameof(State));
                }
            }
        }

        public bool IsMyTurn
        {
            get => _isMyTurn;
            set
            {
                if (_isMyTurn != value)
                {
                    _isMyTurn = value;
                    OnModelPropertyChanged(nameof(IsMyTurn));
                }
            }
        }

        public int PlacementSecondsLeft
        {
            get => _placementSecondsLeft;
            set
            {
                if (_placementSecondsLeft != value)
                {
                    _placementSecondsLeft = value;
                    OnModelPropertyChanged(nameof(PlacementSecondsLeft));
                }
            }
        }

        public int DisasterCountdown
        {
            get => _disasterCountdown;
            set
            {
                if (_disasterCountdown != value)
                {
                    _disasterCountdown = value;
                    OnModelPropertyChanged(nameof(DisasterCountdown));
                }
            }
        }

        public string CurrentStatus
        {
            get => _currentStatus;
            set
            {
                if (_currentStatus != value)
                {
                    _currentStatus = value;
                    OnModelPropertyChanged(nameof(CurrentStatus));
                }
            }
        }

        // Move result tracking for SFX
        public bool? LastMoveResult
        {
            get => _lastMoveResult;
            set
            {
                _lastMoveResult = value;
                OnModelPropertyChanged(nameof(LastMoveResult));
            }
        }

        // Drag and drop state
        public IShip? DraggedShip { get; set; }
        public Point DragOffset { get; set; }

        private int _actionPoints;
        public int ActionPoints
        {
            get => _actionPoints;
            set
            {
                if (_actionPoints != value)
                {
                    _actionPoints = value;
                    OnModelPropertyChanged(nameof(ActionPoints));
                    OnModelPropertyChanged(nameof(ActionPointsText));
                }
            }
        }

        public string ActionPointsText => $"AP: {ActionPoints}";

        // PowerUp states
        public bool HasMiniNukeActive { get; set; }
        public bool IsSelectingRepairTarget { get; set; }
        public Point? RepairTarget { get; set; }

        // Available powerups
        public List<IPowerUp> AvailablePowerUps { get; } = PowerUpFactory.GetAllPowerUps();

        public event PropertyChangedEventHandler? PropertyChanged;
        public void Reset()
        {
            YourShips.Clear();
            YourHitsByOpponent.Clear();
            YourFired.Clear();
            YourFiredHits.Clear();
            AnimatedCells.Clear();
            IsMyTurn = false;
            PlacementSecondsLeft = 0;
            DisasterCountdown = -1;
            CurrentDisasterName = null;
            IsDisasterAnimating = false;
            CurrentStatus = "Ready to start your naval adventure";
            DraggedShip = null;
            DragOffset = Point.Empty;
            State = AppState.Menu;
            _placementHistory.Clear();
            YourMines.Clear();
            SelectedMineCategory = null;

            ActionPoints = 0;
            HasMiniNukeActive = false;
            IsSelectingRepairTarget = false;
            RepairTarget = null;

        }

        public void IncrementActionPoints()
        {
            ActionPoints++;
            Console.WriteLine($"[GameModel] Action Points: {ActionPoints}");
        }

        public bool CanActivatePowerUp(IPowerUp powerUp)
        {
            return powerUp.CanActivate(this, ActionPoints);
        }

        public void ActivatePowerUp(IPowerUp powerUp, GameClientController controller)
        {
            if (CanActivatePowerUp(powerUp))
            {
                ActionPoints -= powerUp.Cost;
                powerUp.ActivatePowerUp(this, controller);
            }
        }

        public void UndoLastShipPlacement()
        {
            if (_placementHistory.Count == 0)
                return;

            var lastCommand = _placementHistory.Pop();
            lastCommand.Undo();
        }


        public bool CanPlaceShip(IShip ship, Point position)
        {
            ship.Position = position;

            // Check if ship is within board bounds
            if (!ship.IsValidPosition(Board.Size))
                return false;

            // Check for collisions with existing ships
            var newCells = ship.GetOccupiedCells();
            foreach (var existingShip in YourShips.Where(s => s.IsPlaced))
            {
                if (existingShip == ship) continue;

                var existingCells = existingShip.GetOccupiedCells();
                if (newCells.Any(cell => existingCells.Contains(cell)))
                    return false;
            }

            return true;
        }

        public void PlaceShip(IShip ship, Point position)
        {
            var command = new PlaceShipCommand(this, ship, position);
            command.Execute();
            _placementHistory.Push(command);
        }

        public void RemoveShip(IShip ship)
        {
            ship.IsPlaced = false;
            ship.Position = Point.Empty;
        }

        public List<Point> GetAllShipCells()
        {
            return YourShips.Where(s => s.IsPlaced)
                           .SelectMany(s => s.GetOccupiedCells())
                           .ToList();
        }

        public void ApplyMoveResult(Point p, bool hit)
        {
            YourFired.Add(p);
            if (hit) YourFiredHits.Add(p);
            LastMoveResult = hit; // Trigger SFX notification
        }

        public void ApplyOpponentMove(Point p, bool hit)
        {
            YourHitsByOpponent.Add(p);
            if (hit)
            {
                // Mark the hit cell but don't remove from ship list
                // The server will handle ship destruction logic
            }
            LastMoveResult = hit; // Trigger SFX notification
        }

        public void ApplyOpponentHitByDisaster(Point p)
        {
            // This represents a hit on the opponent's board that you can see (disaster hit)
            YourFired.Add(p);
            YourFiredHits.Add(p);
        }

        public virtual void OnModelPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public void PlaceMineClientSide(Point pos)
        {
            if (SelectedMineCategory == null) return;
            var mine = NavalMineFactory.CreateMine(pos, /*ownerConnId*/ "LOCAL", SelectedMineCategory.Value);
            YourMines.Add(mine);
            OnModelPropertyChanged(nameof(YourMines));
        }

        // Called when server reports mine detonations
        public void OnMinesTriggered(List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggers)
        {
            foreach (var t in triggers)
            {
                AnimatedCells.UnionWith(t.effectPoints); // show animations                                    
            }
            OnModelPropertyChanged(nameof(AnimatedCells));
        }

        public void PlaceMine(Point cell)
        {
            if (SelectedMineCategory == null) return;

            // Check if cell already has a mine
            if (YourMines.Any(m => m.Position == cell))
                return;

            YourMines.Add(NavalMineFactory.CreateMine(cell, "LOCAL", SelectedMineCategory.Value));
            SelectedMineCategory = null; // deselect after placement
            OnModelPropertyChanged(nameof(YourMines));
        }

        public void HealCells(List<Point> healedCells)
        {
            foreach (var cell in healedCells)
            {
                YourHitsByOpponent.Remove(cell);
            }
            OnModelPropertyChanged(nameof(YourHitsByOpponent));
        }

        public void StartMinePlacement()
        {
            State = AppState.MineSelection;
            CurrentStatus = "Place your mine(s) on your board";
        }

        // When game starts from server
        public void OnGameStarted(bool youStart)
        {
            State = AppState.Playing; 
            CurrentStatus = youStart ? "Your turn - click opponent's board to fire!" : "Opponent's turn";
            _isMyTurn = youStart;
        }

    }
}
