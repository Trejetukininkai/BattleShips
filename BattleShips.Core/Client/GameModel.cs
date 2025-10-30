using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    public enum AppState { Menu, Waiting, Placement, Playing, GameOver }

    // Observable for client side UI
    public class GameModel : INotifyPropertyChanged
    {
        public List<Ship> YourShips { get; } = new();
        public HashSet<Point> YourHitsByOpponent { get; } = new();
        public HashSet<Point> YourFired { get; } = new();
        public HashSet<Point> YourFiredHits { get; } = new();
        public HashSet<Point> AnimatedCells { get; } = new();

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
        public Ship? DraggedShip { get; set; }
        public Point DragOffset { get; set; }

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
        }

        public bool CanPlaceShip(Ship ship, Point position)
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

        public void PlaceShip(Ship ship, Point position)
        {
            if (CanPlaceShip(ship, position))
            {
                ship.Position = position;
                ship.IsPlaced = true;
            }
        }

        public void RemoveShip(Ship ship)
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
            // Add it to YourFiredHits so it shows up on the opponent board (right side)
            YourFired.Add(p);
            YourFiredHits.Add(p);
        }

        protected virtual void OnModelPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
