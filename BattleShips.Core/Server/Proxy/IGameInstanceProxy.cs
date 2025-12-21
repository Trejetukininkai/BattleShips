using System.Collections.Generic;
using System.Drawing;

namespace BattleShips.Core
{
    /// <summary>
    /// Interface for GameInstance proxy pattern
    /// </summary>
    public interface IGameInstanceProxy
    {
        string Id { get; }
        string? PlayerA { get; set; }
        string? PlayerB { get; set; }
        bool Started { get; set; }
        string? CurrentTurn { get; set; }
        int PlayerCount { get; }
        bool HasFirstPlayer { get; }
        bool HasSecondPlayer { get; }

        // Key game operations
        bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost);
        void SwitchTurn();
        int GetRemainingShips(string connId);
        void RemovePlayer(string connId);
        string? Other(string connId);
        void AddActionPoints(string connId, int points);
        int GetActionPoints(string connId);
    }
}