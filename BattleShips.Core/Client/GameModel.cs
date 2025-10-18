using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    public enum AppState { Menu, Waiting, Placement, Playing, GameOver }

    public class GameModel
    {
        public HashSet<Point> YourShips { get; } = new();
        public HashSet<Point> YourHitsByOpponent { get; } = new();
        public HashSet<Point> YourFired { get; } = new();
        public HashSet<Point> YourFiredHits { get; } = new();
        public HashSet<Point> AnimatedCells { get; } = new();

        // current disaster info (set while animating)
        public string? CurrentDisasterName { get; set; }
        public bool IsDisasterAnimating { get; set; }

        public AppState State { get; set; } = AppState.Menu;
        public bool IsMyTurn { get; set; }
        public int PlacementSecondsLeft { get; set; }
        public int DisasterCountdown { get; set; } = -1;

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
            State = AppState.Menu;
        }

        public bool ToggleShip(Point p, int max = 10)
        {
            if (YourShips.Contains(p)) { YourShips.Remove(p); return true; }
            if (YourShips.Count >= max) return false;
            YourShips.Add(p); return true;
        }

        public void ApplyMoveResult(Point p, bool hit)
        {
            YourFired.Add(p);
            if (hit) YourFiredHits.Add(p);
        }

        public void ApplyOpponentMove(Point p, bool hit)
        {
            YourHitsByOpponent.Add(p);
            if (hit) YourShips.Remove(p);
        }
    }
}