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