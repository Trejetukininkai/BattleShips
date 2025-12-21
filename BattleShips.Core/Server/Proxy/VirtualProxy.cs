using System;
using System.Drawing;

namespace BattleShips.Core
{
    /// <summary>
    /// Virtual Proxy - Creates a temporary/waiting game instance until both players join.
    /// Only initializes the full GameInstance when the second player connects.
    /// </summary>
    public class VirtualProxy : GameInstanceProxy
    {
        private GameInstance? _realGame;
        private readonly string _gameId;
        private bool _isInitialized;

        public VirtualProxy(string gameId) : base(null!)
        {
            _gameId = gameId;
            _isInitialized = false;
            // Create a minimal temporary game instance
            _wrapped = new GameInstance(gameId);
        }

        /// <summary>
        /// Gets the underlying GameInstance (for GameManager integration)
        /// </summary>
        public GameInstance GetWrappedInstance() => _wrapped;

        public override bool HasSecondPlayer
        {
            get
            {
                if (!_isInitialized && _wrapped.HasSecondPlayer)
                {
                    InitializeRealGame();
                }
                return _wrapped.HasSecondPlayer;
            }
        }

        public override string? PlayerB
        {
            get => _wrapped.PlayerB;
            set
            {
                _wrapped.PlayerB = value;
                if (value != null && !_isInitialized)
                {
                    InitializeRealGame();
                }
            }
        }

        private void InitializeRealGame()
        {
            if (_isInitialized) return;

            Console.WriteLine($"[VirtualProxy] Initializing full game instance for {_gameId} - Both players connected!");

            // Transfer data from temporary to real game
            _realGame = new GameInstance(_gameId)
            {
                PlayerA = _wrapped.PlayerA,
                PlayerB = _wrapped.PlayerB,
                Started = true,
                CurrentTurn = _wrapped.PlayerA // First player starts
            };

            _wrapped = _realGame;
            _isInitialized = true;
        }

        public override bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            if (!_isInitialized)
            {
                opponentLost = false;
                Console.WriteLine("[VirtualProxy] Cannot shoot - waiting for second player!");
                return false;
            }
            return base.RegisterShot(opponentConnId, shot, out opponentLost);
        }

        public bool IsGameReady => _isInitialized;
    }
}
