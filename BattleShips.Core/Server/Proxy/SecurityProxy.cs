using System;
using System.Drawing;

namespace BattleShips.Core
{
    /// <summary>
    /// Security Proxy - Adds security checks before allowing operations.
    /// Validates that the requesting player has permission to perform actions.
    /// </summary>
    public class SecurityProxy : GameInstanceProxy
    {
        private readonly string _requestingPlayer;

        public SecurityProxy(GameInstance gameInstance, string requestingPlayer) : base(gameInstance)
        {
            _requestingPlayer = requestingPlayer;
        }

        public override bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            opponentLost = false;

            // Security check: Is it the requesting player's turn?
            if (_wrapped.CurrentTurn != _requestingPlayer)
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Not {_requestingPlayer}'s turn!");
                return false;
            }

            // Security check: Is the opponent valid?
            if (opponentConnId != _wrapped.PlayerA && opponentConnId != _wrapped.PlayerB)
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Invalid opponent {opponentConnId}");
                return false;
            }

            // Security check: Can't shoot yourself
            if (opponentConnId == _requestingPlayer)
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Cannot shoot yourself!");
                return false;
            }

            Console.WriteLine($"[SecurityProxy] ALLOWED: {_requestingPlayer} shooting at {opponentConnId}");
            return base.RegisterShot(opponentConnId, shot, out opponentLost);
        }

        public override void SwitchTurn()
        {
            // Security check: Only allow turn switch if it's the current player
            if (_wrapped.CurrentTurn != _requestingPlayer)
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Cannot switch turn - not your turn!");
                return;
            }

            Console.WriteLine($"[SecurityProxy] ALLOWED: Turn switch by {_requestingPlayer}");
            base.SwitchTurn();
        }

        public override void RemovePlayer(string connId)
        {
            // Security check: Players can only remove themselves
            if (connId != _requestingPlayer)
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Cannot remove other players!");
                return;
            }

            Console.WriteLine($"[SecurityProxy] ALLOWED: {_requestingPlayer} leaving game");
            base.RemovePlayer(connId);
        }

        public override void AddActionPoints(string connId, int points)
        {
            // Security check: Can't add points to yourself arbitrarily
            // (This would be called by game logic, not players directly)
            if (connId != _requestingPlayer && _requestingPlayer != "SYSTEM")
            {
                Console.WriteLine($"[SecurityProxy] DENIED: Cannot modify other player's action points!");
                return;
            }

            Console.WriteLine($"[SecurityProxy] ALLOWED: Adding {points} action points to {connId}");
            base.AddActionPoints(connId, points);
        }
    }
}
