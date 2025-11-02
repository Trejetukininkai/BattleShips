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

    // Enum to control which decorators to apply
    [Flags]
    public enum DecoratorType
    {
        None = 0,
        Intensity = 1,
        Accelerated = 2,
        Chain = 4,
        All = Intensity | Accelerated | Chain
    }
        
    public interface IEventGenerator
    {
        int GetDisasterCountdown();
        bool DecrementCountdown();
        void ResetCountdown();
        bool IsDisasterTime();
        List<Point> CauseDisaster();
        string? GetEventName();
        Point SelectRandomCell(int boardSize = Board.Size);
    }


}