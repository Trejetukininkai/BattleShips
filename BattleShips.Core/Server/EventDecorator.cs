using System;
using System.Drawing;

namespace BattleShips.Core
{
    // Decorator pattern implementation
    public abstract class EventDecorator : EventGenerator
    {
        protected EventGenerator _wrapped;

        protected EventDecorator(EventGenerator wrapped)
        {
            _wrapped = wrapped;
        }

        // Delegate countdown-related methods to wrapped generator
        public override int GetDisasterCountdown() => _wrapped.GetDisasterCountdown();
        public override bool DecrementCountdown() => _wrapped.DecrementCountdown();
        public override void ResetCountdown() => _wrapped.ResetCountdown();
        public override bool IsDisasterTime() => _wrapped.IsDisasterTime();

        public override string? GetEventName() => _wrapped.GetEventName();

        // Protected helper to access wrapped's random selection
        protected Point SelectRandomCellWrapped() => _wrapped.SelectRandomCell();
    }

    // Intensity Decorator: Increases disaster intensity by scaling effects based on intensity level
    public class IntensityDecorator : EventDecorator
    {
        private readonly int _intensityLevel;

        public IntensityDecorator(EventGenerator wrapped, int intensityLevel = 1) : base(wrapped)
        {
            _intensityLevel = Math.Max(3, intensityLevel); // For testing, ensure at least 3
        }

        public override List<Point> CauseDisaster()
        {
            var affected = _wrapped.CauseDisaster();
            if (_wrapped.IsDisasterTime())
            {
                string? eventName = _wrapped.GetEventName();
                if (eventName?.Contains(EventType.Storm.ToString()) == true)
                {
                    // Increase the number of spots significantly
                    int additionalSpots = _intensityLevel * 2; // More spots
                    for (int i = 0; i < additionalSpots; i++)
                    {
                        Point p;
                        do
                        {
                            p = SelectRandomCell();
                        } while (affected.Contains(p));
                        affected.Add(p);
                    }
                }
                else if (eventName?.Contains(EventType.Tsunami.ToString()) == true)
                {
                    // Affect more columns, but max total 3 columns
                    Point approximateCenter = affected[affected.Count / 2]; // Approximate the original column
                    int radius = Math.Min(_intensityLevel, 1); // Max radius 1 to limit to 3 total columns
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int col = approximateCenter.X + dx;
                        if (col >= 0 && col < Board.Size)
                        {
                            for (int row = 0; row < Board.Size; row++)
                            {
                                Point p = new Point(col, row);
                                if (!affected.Contains(p)) affected.Add(p);
                            }
                        }
                    }
                }
                else if (eventName?.Contains(EventType.Whirlpool.ToString()) == true)
                {
                    // Much larger effective area for testing
                    if (affected.Count > 0)
                    {
                        Point center = affected[affected.Count / 2];
                        int radius = _intensityLevel * 4; // Even larger radius to show effect
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int x = center.X + dx;
                                int y = center.Y + dy;
                                if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size)
                                {
                                    Point p = new Point(x, y);
                                    if (!affected.Contains(p)) affected.Add(p);
                                }
                            }
                        }
                    }
                }
                else if (eventName?.Contains("Meteor Strike") == true)
                {
                    // Much larger AOE for testing
                    if (affected.Count > 0)
                    {
                        Point center = affected[4]; // Center of the 3x3
                        int radius = _intensityLevel * 2; // Larger radius to make it more dramatic
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            for (int dy = -radius; dy <= radius; dy++)
                            {
                                int x = center.X + dx;
                                int y = center.Y + dy;
                                if (x >= 0 && x < Board.Size && y >= 0 && y < Board.Size)
                                    if (!affected.Contains(new Point(x, y))) affected.Add(new Point(x, y));
                            }
                        }
                    }
                }
            }
            return affected;
        }

        public override string? GetEventName() => $"Intensified {_wrapped.GetEventName()}";
    }

    // Chain Decorator: Causes a secondary disaster effect
    public class ChainDecorator : EventDecorator
    {
        private readonly EventType _chainType;

        public ChainDecorator(EventGenerator wrapped, EventType chainType) : base(wrapped)
        {
            _chainType = chainType;
        }

        public override List<Point> CauseDisaster()
        {
            var affected = _wrapped.CauseDisaster();
            if (_wrapped.IsDisasterTime())
            {
                EventGenerator chainGenerator = _chainType switch
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

        public override string? GetEventName() => $"Chain {_wrapped.GetEventName()}";
    }

    // NextCountdown Decorator: Reduces countdown interval for more frequent disasters
    public class NextCountdownDecorator : EventDecorator
    {
        private const int FasterDisasterIntervalMin = 1;
        private const int FasterDisasterIntervalMax = 3;

        public NextCountdownDecorator(EventGenerator wrapped) : base(wrapped) { }

        public override void ResetCountdown()
        {
            DisasterCountdown = (int)_rand.NextInt64(FasterDisasterIntervalMin, FasterDisasterIntervalMax);
        }

        public override int GetDisasterCountdown() => DisasterCountdown;

        public override bool DecrementCountdown()
        {
            DisasterCountdown--;
            return DisasterCountdown <= 0;
        }

        public override bool IsDisasterTime() => DisasterCountdown <= 0;

        public override List<Point> CauseDisaster() => _wrapped.CauseDisaster();

        public override string? GetEventName() => $"Accelerated {_wrapped.GetEventName()}";
    }
}
