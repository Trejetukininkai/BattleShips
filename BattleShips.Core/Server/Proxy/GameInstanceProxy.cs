using System.Drawing;

namespace BattleShips.Core
{
    /// <summary>
    /// Base proxy class that wraps GameInstance
    /// </summary>
    public class GameInstanceProxy : IGameInstanceProxy
    {
        protected GameInstance _wrapped;

        public GameInstanceProxy(GameInstance gameInstance)
        {
            _wrapped = gameInstance;
        }

        /// <summary>
        /// Gets the wrapped GameInstance - used by GameManager to return the actual instance
        /// </summary>
        public GameInstance GetWrappedInstance() => _wrapped;

        public virtual string Id => _wrapped.Id;
        public virtual string? PlayerA
        {
            get => _wrapped.PlayerA;
            set => _wrapped.PlayerA = value;
        }
        public virtual string? PlayerB
        {
            get => _wrapped.PlayerB;
            set => _wrapped.PlayerB = value;
        }
        public virtual bool Started
        {
            get => _wrapped.Started;
            set => _wrapped.Started = value;
        }
        public virtual string? CurrentTurn
        {
            get => _wrapped.CurrentTurn;
            set => _wrapped.CurrentTurn = value;
        }
        public virtual int PlayerCount => _wrapped.PlayerCount;
        public virtual bool HasFirstPlayer => _wrapped.HasFirstPlayer;
        public virtual bool HasSecondPlayer => _wrapped.HasSecondPlayer;

        public virtual bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            return _wrapped.RegisterShot(opponentConnId, shot, out opponentLost);
        }

        public virtual void SwitchTurn()
        {
            _wrapped.SwitchTurn();
        }

        public virtual int GetRemainingShips(string connId)
        {
            return _wrapped.GetRemainingShips(connId);
        }

        public virtual void RemovePlayer(string connId)
        {
            _wrapped.RemovePlayer(connId);
        }

        public virtual string? Other(string connId)
        {
            return _wrapped.Other(connId);
        }

        public virtual void AddActionPoints(string connId, int points)
        {
            _wrapped.AddActionPoints(connId, points);
        }

        public virtual int GetActionPoints(string connId)
        {
            return _wrapped.GetActionPoints(connId);
        }
    }
}
