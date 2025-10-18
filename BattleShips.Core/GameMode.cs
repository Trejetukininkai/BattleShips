namespace BattleShips.Core
{
    // Class not used yet, only for strategy implementation via event generator
    public class GameMode
    {
        public int ShipCount { get; }
        public int BoardX { get; }
        public int BoardY { get; }
        public EventGenerator? EventGenerator;

        public GameMode(int shipCount, int boardX, int boardY)
        {
            ShipCount = shipCount;
            BoardX = boardX;
            BoardY = boardY;
            EventGenerator = null;
            SelectRandomEventGenerator();
        }
        public bool DecrementCountdown()
        {
            return EventGenerator != null && EventGenerator.DecrementCountdown();
        }
        public void ResetEventGenerator()
        {
            EventGenerator?.ResetCountdown();
        }
        public void SelectRandomEventGenerator()
        {
            var eventTypes = Enum.GetValues(typeof(EventType));
            var randomEventType = (EventType)eventTypes.GetValue(new Random().Next(eventTypes.Length))!;
            EventGenerator = randomEventType switch
            {
                EventType.Storm => new StormGenerator(),
                EventType.Tsunami => new TsunamiGenerator(),
                EventType.Whirlpool => new WhirlpoolGenerator(),
                EventType.MeteorStrike => new MeteorStrikeGenerator(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}