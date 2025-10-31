using System.Drawing;
using System.Threading;

namespace BattleShips.Core
{
    // Class not used yet, only for strategy implementation via event generator
    public class GameMode
    {
        public int ShipCount { get; }
        public int BoardX { get; }
        public int BoardY { get; }
        public EventGenerator? EventGenerator;

    private Random _rand = new();
    private bool TempAccelerateNext = false;

    public GameMode(int shipCount, int boardX, int boardY)
    {
        ShipCount = shipCount;
        BoardX = boardX;
        BoardY = boardY;
        EventGenerator = null;
    }

        public bool DecrementCountdown()
        {
            if (EventGenerator == null) return false;

            if (EventGenerator.DecrementCountdown())
            {
                // Disaster time, wrap with decorators for chaining
                string? name = EventGenerator.GetEventName();
                EventGenerator temp = EventGenerator;

                // Check for chained disasters
                if (name?.Contains(EventType.Storm.ToString()) == true)
                {
                    if (_rand.NextDouble() < 0.2) temp = new ChainDecorator(temp, EventType.Tsunami);
                    if (_rand.NextDouble() < 0.2) temp = new ChainDecorator(temp, EventType.Whirlpool);
                    if (_rand.NextDouble() < 0.2) temp = new ChainDecorator(temp, EventType.Storm);
                }
                else if (name?.Contains(EventType.Whirlpool.ToString()) == true || name?.Contains(EventType.Tsunami.ToString()) == true)
                {
                    if (_rand.NextDouble() < 0.2) temp = new ChainDecorator(temp, EventType.Storm);
                }

                // Check for next timer acceleration
                if (name?.Contains(EventType.Storm.ToString()) == true && _rand.NextDouble() < 0.33)
                {
                    TempAccelerateNext = true;
                }

                EventGenerator = temp;
                return true;
            }
            return false;
        }

        public List<Point> CauseDisaster()
        {
            return EventGenerator?.CauseDisaster() ?? new List<Point>();
        }

        public void ResetEventGenerator()
        {
            EventGenerator?.ResetCountdown();
        }

        public void SelectEventBasedOnTurn(int turnCount)
        {
            var eventTypes = Enum.GetValues(typeof(EventType));
            var randomEventType = (EventType)eventTypes.GetValue(_rand.Next(eventTypes.Length))!;

            EventGenerator = (EventGenerator)EventGenerator.CreateDecoratedEventGenerator(randomEventType, turnCount);
            EventGenerator.ResetCountdown();

            // Apply temporary acceleration if set
            if (TempAccelerateNext)
            {
                EventGenerator = new NextCountdownDecorator(EventGenerator);
                EventGenerator.ResetCountdown(); // Set to 1-3
                TempAccelerateNext = false;
            }
        }

        // Original method for compatibility, but deprecated
        public void SelectRandomEventGenerator()
        {
            SelectEventBasedOnTurn(0);
        }
    }
}
