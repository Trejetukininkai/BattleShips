using System;
using System.Drawing;

namespace BattleShips.Core
{
    // Decorator pattern implementation
    public abstract class EventDecorator : IEventGenerator
    {
        protected IEventGenerator _wrapped;

        protected EventDecorator(IEventGenerator wrapped)
        {
            _wrapped = wrapped;
        }

        // Delegate all methods to wrapped generator
        public virtual int GetDisasterCountdown() => _wrapped.GetDisasterCountdown();
        public virtual bool DecrementCountdown() => _wrapped.DecrementCountdown();
        public virtual void ResetCountdown() => _wrapped.ResetCountdown();
        public virtual bool IsDisasterTime() => _wrapped.IsDisasterTime();
        public virtual List<Point> CauseDisaster() => _wrapped.CauseDisaster();
        public virtual string? GetEventName() => _wrapped.GetEventName();
        public virtual Point SelectRandomCell(int boardSize = Board.Size) => _wrapped.SelectRandomCell(boardSize);
    }

    // Intensity Decorator: Increases disaster intensity by scaling effects based on intensity level
    public class IntensityDecorator : EventDecorator
    {
        public int IntensityLevel { get; set; }

        public IntensityDecorator(IEventGenerator wrapped, int intensityLevel = 1) : base(wrapped)
        {
            IntensityLevel = Math.Max(1, intensityLevel);
        }

        public override List<Point> CauseDisaster()
        {
            // Get base disaster effect
            var affected = _wrapped.CauseDisaster();

            string? eventName = GetEventName();
            if (eventName?.Contains(EventType.Storm.ToString()) == true)
            {
                // Add additional random spots based on intensity
                int additionalSpots = (IntensityLevel - 1) * 3;
                for (int i = 0; i < additionalSpots; i++)
                {
                    Point p;
                    int attempts = 0;
                    do
                    {
                        p = SelectRandomCell();
                        attempts++;
                    } while (affected.Contains(p) && attempts < 100);
                    if (!affected.Contains(p))
                        affected.Add(p);
                }
            }
            else if (eventName?.Contains(EventType.Tsunami.ToString()) == true)
            {
                // Add additional columns: intensity 2 = 2 total, intensity 3 = 3 total
                if (affected.Count > 0)
                {
                    Point center = affected[affected.Count / 2];
                    int additionalColumns = Math.Max(0, IntensityLevel - 1); // 0,1,2
                    for (int i = 1; i <= additionalColumns; i++)
                    {
                        // Add columns to the left and right
                        int leftCol = center.X - i;
                        int rightCol = center.X + i;
                        if (leftCol >= 0)
                        {
                            for (int row = 0; row < Board.Size; row++)
                            {
                                Point p = new Point(leftCol, row);
                                if (!affected.Contains(p)) affected.Add(p);
                            }
                        }
                        if (rightCol < Board.Size && i >= 2)
                        {
                            for (int row = 0; row < Board.Size; row++)
                            {
                                Point p = new Point(rightCol, row);
                                if (!affected.Contains(p)) affected.Add(p);
                            }
                        }
                    }
                }
            }
            else if (eventName?.Contains(EventType.Whirlpool.ToString()) == true)
            {
                if (affected.Count > 0)
                {
                    int avgX = (int)Math.Round(affected.Average(p => p.X));
                    int avgY = (int)Math.Round(affected.Average(p => p.Y));
                    Point center = new Point(avgX, avgY);
                    var newCells = new HashSet<Point>(affected);

                    // Determine radius for X shape
                    // Intensity 1 -> radius 1 (3x3)
                    // Intensity 2 -> radius 2 (5x5)
                    // Intensity 3 -> radius 3 (7x7)
                    int radius = IntensityLevel;

                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int x = center.X + dx;
                            int y = center.Y + dy;

                            if (x < 0 || y < 0 || x >= Board.Size || y >= Board.Size)
                                continue;

                            // X-pattern condition: only cells on the diagonals
                            if (Math.Abs(dx) == Math.Abs(dy))
                                newCells.Add(new Point(x, y));
                        }
                    }

                    affected = newCells.ToList();
                }
            }
            else if (eventName?.Contains("Meteor Strike") == true)
            {
                if (affected.Count > 0)
                {
                    int avgX = (int)Math.Round(affected.Average(p => p.X));
                    int avgY = (int)Math.Round(affected.Average(p => p.Y));
                    Point center = new Point(avgX, avgY);
                    var newCells = new HashSet<Point>(affected);

                    // Determine size: 3x3, 4x4, 5x5
                    int size = IntensityLevel switch
                    {
                        1 => 3,
                        2 => 4,
                        _ => 5
                    };

                    // Calculate half-size for centering
                    // For even sizes (like 4x4), shift one direction to keep shape balanced
                    int half = size / 2;

                    for (int dy = -half; dy <= half; dy++)
                    {
                        for (int dx = -half; dx <= half; dx++)
                        {
                            // For even sizes (4x4), offset slightly so it's centered
                            int x = center.X + dx + (size % 2 == 0 && dx < 0 ? 1 : 0);
                            int y = center.Y + dy + (size % 2 == 0 && dy < 0 ? 1 : 0);

                            if (x < 0 || y < 0 || x >= Board.Size || y >= Board.Size)
                                continue;

                            newCells.Add(new Point(x, y));
                        }
                    }

                    affected = newCells.ToList();
                }
            }

            return affected;
        }

        public override string? GetEventName() => $"Intensified {IntensityLevel} {_wrapped.GetEventName()}";
    }

    // Chain Decorator: Causes a secondary disaster effect
    public class ChainDecorator : EventDecorator
    {
        public EventType ChainType { get; set; }

        public ChainDecorator(IEventGenerator wrapped, EventType chainType) : base(wrapped)
        {
            ChainType = chainType;
        }

        public override List<Point> CauseDisaster()
        {
            var affected = _wrapped.CauseDisaster();
            if (_wrapped.IsDisasterTime())
            {
                EventGenerator chainGenerator = ChainType switch
                {
                    EventType.Storm => new StormGenerator(),
                    EventType.Tsunami => new TsunamiGenerator(),
                    EventType.Whirlpool => new WhirlpoolGenerator(),
                    EventType.MeteorStrike => new MeteorStrikeGenerator(),
                    _ => new StormGenerator()
                };
                while (!chainGenerator.IsDisasterTime())
                {
                    chainGenerator.DecrementCountdown();
                }
                var chainAffected = chainGenerator.CauseDisaster();
                affected.AddRange(chainAffected);
            }
            return affected;
        }

        public override string? GetEventName() => $"{_wrapped.GetEventName()} Chained with {ChainType}";
    }

    // NextCountdown Decorator: Reduces countdown interval for more frequent disasters
    public class NextCountdownDecorator : EventDecorator
    {
        private const int FasterDisasterIntervalMin = 1;
        private const int FasterDisasterIntervalMax = 3;
        private int _localCountdown;
        private static readonly Random _rand = new();

        public NextCountdownDecorator(IEventGenerator wrapped) : base(wrapped)
        {
            ResetCountdown();
        }

        public override void ResetCountdown()
        {
            _localCountdown = (int)_rand.NextInt64(FasterDisasterIntervalMin, FasterDisasterIntervalMax);
        }

        public override int GetDisasterCountdown() => _localCountdown;

        public override bool DecrementCountdown()
        {
            _localCountdown--;
            return _localCountdown <= 0;
        }

        public override bool IsDisasterTime() => _localCountdown <= 0;

        public override List<Point> CauseDisaster() => _wrapped.CauseDisaster();

        public override string? GetEventName() => $"Accelerated {_wrapped.GetEventName()}";
    }
}
