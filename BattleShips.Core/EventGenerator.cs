using System.Drawing;

namespace BattleShips.Core
{
    public enum EventType
    {
        Storm,
        Tsunami,
        Whirlpool,
        MeteorStrike
    }
    public abstract class EventGenerator
    {
        private const int DisasterIntervalMin = 2;
        private const int DisasterIntervalMax = 4;
        private static readonly Random _rand = new();
        private int DisasterCountdown = (int)_rand.NextInt64(DisasterIntervalMin, DisasterIntervalMax);
        

        public int GetDisasterCountdown() => DisasterCountdown;
        /// <summary>
        /// Decrements the countdown. 
        /// </summary>
        /// <returns>Returns true if disaster should occur now.</returns>
        public bool DecrementCountdown()
        {
            DisasterCountdown--;
            return DisasterCountdown <= 0;
        }
        public void ResetCountdown()
        {
            DisasterCountdown = (int)_rand.NextInt64(DisasterIntervalMin, DisasterIntervalMax);
        }
        public bool IsDisasterTime() => DisasterCountdown <= 0;
        public abstract List<Point> CauseDisaster(); // returns affected cells
        public abstract String? GetEventName();
        protected Point SelectRandomCell(int boardSize = Board.Size) // TODO: use board size from GameMode, not Board.cs
        {
            int x = _rand.Next(0, boardSize);
            int y = _rand.Next(0, boardSize);
            return new Point(x, y);
        }
    }

    public class StormGenerator : EventGenerator
    {
        // Storm: randomly wipe 5 distinct spots on the board
        public override List<Point> CauseDisaster()
        {
            if (!IsDisasterTime()) return new List<Point>();

            var picks = new HashSet<Point>();
            while (picks.Count < 5)
            {
                var p = SelectRandomCell();
                picks.Add(p);
            }

            var affected = picks.ToList();
            
            return affected;
        }
        public override string? GetEventName() => EventType.Storm.ToString();
    }
    public class TsunamiGenerator : EventGenerator
    {
        // Tsunami affects an entire column randomly
        public override List<Point> CauseDisaster()
        {
            if (IsDisasterTime())
            {
                var targetCell = SelectRandomCell();

                // lambda that selects all cells in a given column (col) for a board of given size
                Func<int, int, List<Point>> columnSelector = (col, size) =>
                    Enumerable.Range(0, size).Select(row => new Point(col, row)).ToList();

                var affectedCells = columnSelector(targetCell.X, Board.Size);

                return affectedCells;
            }
            return new List<Point>();
        }
        public override string? GetEventName() => EventType.Tsunami.ToString();
    }
    public class WhirlpoolGenerator : EventGenerator
    {
        public override List<Point> CauseDisaster()
        {
            if (!IsDisasterTime()) return new List<Point>();

            var center = SelectRandomCell();
            var cells = new List<Point>();

            // compute 3x3 bounds centered on center, clipped to board
            int cx = center.X;
            int cy = center.Y;
            var xs = Enumerable.Range(cx - 1, 3).Where(x => x >= 0 && x < Board.Size).ToArray();
            var ys = Enumerable.Range(cy - 1, 3).Where(y => y >= 0 && y < Board.Size).ToArray();

            // build 3x3 grid points (if smaller near edges grid is reduced)
            var grid = new List<Point>();
            foreach (var y in ys)
                foreach (var x in xs)
                    grid.Add(new Point(x, y));

            // spiral order for up to 3x3 — precomputed offsets relative to top-left of the 3x3 block
            // We'll construct spiral based on available coords:
            var block = new Point[xs.Length, ys.Length];
            // map coordinates into a small index grid for traversal
            var coordMap = new Point[xs.Length, ys.Length];
            for (int j = 0; j < ys.Length; j++)
                for (int i = 0; i < xs.Length; i++)
                    coordMap[i, j] = new Point(xs[i], ys[j]);

            // produce spiral indices for sizes 1..3
            var spiral = new List<Point>();
            int w = xs.Length, h = ys.Length;
            int left = 0, top = 0, right = w - 1, bottom = h - 1;
            while (left <= right && top <= bottom)
            {
                // top row
                for (int x = left; x <= right; x++) spiral.Add(coordMap[x, top]);
                top++;
                // right column
                for (int y = top; y <= bottom; y++) spiral.Add(coordMap[right, y]);
                right--;
                if (top <= bottom)
                {
                    for (int x = right; x >= left; x--) spiral.Add(coordMap[x, bottom]);
                    bottom--;
                }
                if (left <= right)
                {
                    for (int y = bottom; y >= top; y--) spiral.Add(coordMap[left, y]);
                    left++;
                }
            }

            // affect every other cell in spiral order to simulate "pull"
            for (int i = 0; i < spiral.Count; i++)
            {
                if (i % 2 == 0) // take every 2nd cell
                    cells.Add(spiral[i]);
            }

            return cells;
        }
        public override string? GetEventName() => EventType.Whirlpool.ToString();
    }
    public class MeteorStrikeGenerator : EventGenerator
    {
        public override List<Point> CauseDisaster()
        {
            if (!IsDisasterTime()) return new List<Point>();

            var center = SelectRandomCell();
            var affected = new List<Point>();

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int x = center.X + dx;
                    int y = center.Y + dy;
                    if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size)
                        affected.Add(new Point(x, y));
                }
            }

            return affected;
        }
        public override string? GetEventName() => "Meteor Strike";
    }
}