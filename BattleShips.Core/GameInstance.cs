// --- internal simple Game model ---
using System.Drawing;

public class GameInstance
    {
        public string Id { get; }
        public string? PlayerA { get; set; }
        public string? PlayerB { get; set; }
        public HashSet<Point> ShipsA { get; set; } = new();
        public HashSet<Point> ShipsB { get; set; } = new();
        public bool ReadyA { get; set; }
        public bool ReadyB { get; set; }
        public bool Started { get; set; }
        public DateTime PlacementDeadline { get; set; }
        public string? CurrentTurn { get; set; }

        public GameInstance(string id) { Id = id; }

        public int PlayerCount => (HasFirstPlayer ? 1 : 0) + (HasSecondPlayer ? 1 : 0);
        public bool HasFirstPlayer => PlayerA != null;
        public bool HasSecondPlayer => PlayerB != null;

        public void RemovePlayer(string connId)
        {
            if (PlayerA == connId) PlayerA = null;
            if (PlayerB == connId) PlayerB = null;
        }

        public string? Other(string connId)
        {
            if (PlayerA == connId) return PlayerB;
            if (PlayerB == connId) return PlayerA;
            return null;
        }

        public void SetPlayerShips(string connId, HashSet<Point> ships)
        {
            if (PlayerA == connId) { ShipsA = ships; ReadyA = true; }
            else if (PlayerB == connId) { ShipsB = ships; ReadyB = true; }
        }

        public int GetRemainingShips(string connId)
        {
            if (PlayerA == connId) return ShipsA.Count;
            if (PlayerB == connId) return ShipsB.Count;
            return 0;
        }

        // Register shot on opponent; returns hit, and out opponentLost
        public bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            var hit = false;
            if (PlayerA == opponentConnId)
            {
                if (ShipsA.Remove(shot)) hit = true;
                opponentLost = ShipsA.Count == 0;
            }
            else
            {
                if (ShipsB.Remove(shot)) hit = true;
                opponentLost = ShipsB.Count == 0;
            }
            return hit;
        }

        public void SwitchTurn()
        {
            if (CurrentTurn == PlayerA) CurrentTurn = PlayerB;
            else CurrentTurn = PlayerA;
        }
    }