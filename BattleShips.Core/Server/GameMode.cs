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
        public IEventGenerator? EventGenerator;

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

            return EventGenerator.DecrementCountdown();
        }

        public List<Point> CauseDisaster()
        {
            return EventGenerator?.CauseDisaster() ?? new List<Point>();
        }

        public void ResetEventGenerator()
        {
            EventGenerator?.ResetCountdown();
        }

        // Select event generator based on turn count with scaling difficulty
        // Rules: Turns 0-24 - no decorators, regular events
        //        Turns 25-49 - intensity 1-5 linear scaling, max 25/30 cells
        //        Turns 50+ - max intensity - 5
        //        If event is Storm, 50% chance to chain tsunami or whirlpool,
        //                           50% chance to accelerate next event 
        public void SelectEventBasedOnTurn(int turnCount)
        {
            var eventTypes = Enum.GetValues(typeof(EventType));
            var randomEventType = (EventType)eventTypes.GetValue(_rand.Next(eventTypes.Length))!;

            DecoratorType decorators = DecoratorType.None;

            if (turnCount >= 25)
            {
                // Calculate intensity:
                int intensity;
                if (turnCount < 50)
                {
                    intensity = 2; 
                } else
                {
                    intensity = 3; 
                }

                decorators |= DecoratorType.Intensity;

                // Special rules for Storm events
                if (randomEventType == EventType.Storm)
                {
                    // 50% chance to chain tsunami or whirlpool
                    if (_rand.NextDouble() < 0.50)
                    {
                        decorators |= DecoratorType.Chain;
                    }

                    // 50% chance to accelerate next event
                    if (_rand.NextDouble() < 0.50)
                    {
                        decorators |= DecoratorType.Accelerated;
                    }
                }

                EventGenerator = Core.EventGenerator.CreateDecoratedEventGenerator(
                    randomEventType,
                    intensity,
                    decorators);

                // Set chain type for storm events
                if (randomEventType == EventType.Storm && (decorators & DecoratorType.Chain) != 0)
                {
                    if (EventGenerator is ChainDecorator chainDecorator)
                    {
                        chainDecorator.ChainType = _rand.NextDouble() < 0.5 ? EventType.Tsunami : EventType.Whirlpool;
                    }
                }
            }
            else
            {
                // Use base event without decorators for first 25 turns
                EventGenerator = Core.EventGenerator.CreateDecoratedEventGenerator(
                    randomEventType,
                    1,
                    DecoratorType.None);
            }

            EventGenerator.ResetCountdown();
        }

        // // For testing
        // public void SelectEventBasedOnTurn(int turnCount)
        // {
        //     var eventTypes = Enum.GetValues(typeof(EventType));
        //     var randomEventType = (EventType)eventTypes.GetValue(_rand.Next(eventTypes.Length))!;

        //     DecoratorType decorators = DecoratorType.None;
        //     int intensity = 3;
        //     decorators |= DecoratorType.Intensity;
        //     EventGenerator = Core.EventGenerator.CreateDecoratedEventGenerator(
        //             EventType.Whirlpool,
        //             intensity,
        //             decorators);
        //     EventGenerator.ResetCountdown();
        // }

        // Original method for compatibility, but deprecated
        public void SelectRandomEventGenerator()
        {
            SelectEventBasedOnTurn(0);
        }
    }
}
