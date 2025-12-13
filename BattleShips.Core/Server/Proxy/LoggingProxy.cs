using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace BattleShips.Core
{
    /// <summary>
    /// Logging Proxy - Logs all method calls and game actions to a log file.
    /// Useful for debugging, auditing, and game replay.
    /// </summary>
    public class LoggingProxy : GameInstanceProxy
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public LoggingProxy(GameInstance gameInstance, string logFilePath = "game_log.txt") : base(gameInstance)
        {
            // Ensure gameLogs directory exists
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _logFilePath = logFilePath;
            LogMessage($"=== Game Session Started: {gameInstance.Id} ===");
        }

        private void LogMessage(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logEntry = $"[{timestamp}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LoggingProxy] Failed to write log: {ex.Message}");
                }
            }
        }

        public override string? PlayerA
        {
            get => base.PlayerA;
            set
            {
                LogMessage($"PlayerA set to: {value ?? "null"}");
                base.PlayerA = value;
            }
        }

        public override string? PlayerB
        {
            get => base.PlayerB;
            set
            {
                LogMessage($"PlayerB set to: {value ?? "null"}");
                base.PlayerB = value;
            }
        }

        public override bool Started
        {
            get => base.Started;
            set
            {
                LogMessage($"Game started: {value}");
                base.Started = value;
            }
        }

        public override string? CurrentTurn
        {
            get => base.CurrentTurn;
            set
            {
                LogMessage($"Current turn set to: {value ?? "null"}");
                base.CurrentTurn = value;
            }
        }

        public override bool RegisterShot(string opponentConnId, Point shot, out bool opponentLost)
        {
            LogMessage($"RegisterShot - Opponent: {opponentConnId}, Shot: ({shot.X},{shot.Y})");
            bool result = base.RegisterShot(opponentConnId, shot, out opponentLost);
            LogMessage($"  Result: {(result ? "HIT" : "MISS")}, Opponent Lost: {opponentLost}");
            return result;
        }

        public override void SwitchTurn()
        {
            string previousTurn = _wrapped.CurrentTurn ?? "null";
            base.SwitchTurn();
            string newTurn = _wrapped.CurrentTurn ?? "null";
            LogMessage($"SwitchTurn - From: {previousTurn} To: {newTurn}");
        }

        public override int GetRemainingShips(string connId)
        {
            int remaining = base.GetRemainingShips(connId);
            LogMessage($"GetRemainingShips - Player: {connId}, Remaining: {remaining}");
            return remaining;
        }

        public override void RemovePlayer(string connId)
        {
            LogMessage($"RemovePlayer - Player: {connId}");
            base.RemovePlayer(connId);
        }

        public override string? Other(string connId)
        {
            string? other = base.Other(connId);
            LogMessage($"Other - Player: {connId}, Other: {other ?? "null"}");
            return other;
        }

        public override void AddActionPoints(string connId, int points)
        {
            LogMessage($"AddActionPoints - Player: {connId}, Points: {points}");
            base.AddActionPoints(connId, points);
            int total = _wrapped.GetActionPoints(connId);
            LogMessage($"  Total action points for {connId}: {total}");
        }

        public override int GetActionPoints(string connId)
        {
            int points = base.GetActionPoints(connId);
            LogMessage($"GetActionPoints - Player: {connId}, Points: {points}");
            return points;
        }

        // New logging methods for disasters, power-ups, and other actions

        /// <summary>
        /// Logs disaster hits on a player's board
        /// </summary>
        public void RegisterDisasterHits(string playerConnId, List<Point> hitPoints)
        {
            LogMessage($"DISASTER - Player: {playerConnId}, Hits: {hitPoints.Count} cells affected");
            LogMessage($"  Affected cells: {string.Join(", ", hitPoints.Select(p => $"({p.X},{p.Y})"))}");
            _wrapped.RegisterDisasterHits(playerConnId, hitPoints);
        }

        /// <summary>
        /// Logs disaster application with mine interactions
        /// </summary>
        public void ApplyDisasterWithMines(string playerConnId, List<Point> hitPoints, out List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMines)
        {
            LogMessage($"DISASTER WITH MINES - Player: {playerConnId}, Disaster hits: {hitPoints.Count}");
            LogMessage($"  Disaster cells: {string.Join(", ", hitPoints.Select(p => $"({p.X},{p.Y})"))}");

            _wrapped.ApplyDisasterWithMines(playerConnId, hitPoints, out triggeredMines);

            if (triggeredMines.Count > 0)
            {
                LogMessage($"  Triggered {triggeredMines.Count} anti-disaster mine(s)");
                foreach (var mine in triggeredMines)
                {
                    LogMessage($"    Mine {mine.category} triggered - Protected {mine.effectPoints.Count} cell(s)");
                }
            }
            else
            {
                LogMessage($"  No anti-disaster mines triggered");
            }
        }

        /// <summary>
        /// Logs shot registration with mine interactions
        /// </summary>
        public bool RegisterShotWithMines(string opponentConnId, Point shot, out bool opponentLost, out List<(Guid mineId, MineCategory category, List<Point> effectPoints)> triggeredMines)
        {
            LogMessage($"RegisterShotWithMines - Opponent: {opponentConnId}, Shot: ({shot.X},{shot.Y})");

            bool result = _wrapped.RegisterShotWithMines(opponentConnId, shot, out opponentLost, out triggeredMines);

            LogMessage($"  Result: {(result ? "HIT" : "MISS")}, Opponent Lost: {opponentLost}");

            if (triggeredMines.Count > 0)
            {
                LogMessage($"  Triggered {triggeredMines.Count} mine(s)");
                foreach (var mine in triggeredMines)
                {
                    LogMessage($"    Mine {mine.category} - Affected {mine.effectPoints.Count} cell(s): {string.Join(", ", mine.effectPoints.Select(p => $"({p.X},{p.Y})"))}");
                }
            }

            return result;
        }

        /// <summary>
        /// Logs action point deductions (power-up usage)
        /// </summary>
        public void DeductActionPoints(string connId, int cost)
        {
            int beforePoints = _wrapped.GetActionPoints(connId);
            _wrapped.DeductActionPoints(connId, cost);
            int afterPoints = _wrapped.GetActionPoints(connId);
            LogMessage($"POWER-UP USED - Player: {connId}, Cost: {cost} AP, Before: {beforePoints}, After: {afterPoints}");
        }

        /// <summary>
        /// Logs mini-nuke power-up activation/deactivation
        /// </summary>
        public void SetMiniNuke(string connId, bool value)
        {
            LogMessage($"MINI-NUKE - Player: {connId}, Status: {(value ? "ACTIVATED" : "DEACTIVATED")}");
            _wrapped.SetMiniNuke(connId, value);
        }

        /// <summary>
        /// Logs repair power-up application
        /// </summary>
        public bool ApplyRepair(string connId, Point? target = null)
        {
            var repairTarget = target ?? (connId == _wrapped.PlayerA ? _wrapped.RepairTargetA : _wrapped.RepairTargetB);

            bool success = _wrapped.ApplyRepair(connId);

            if (success && repairTarget.HasValue)
            {
                LogMessage($"REPAIR - Player: {connId}, Repaired cell: ({repairTarget.Value.X},{repairTarget.Value.Y}) - SUCCESS");
            }
            else
            {
                LogMessage($"REPAIR - Player: {connId}, Target: {(repairTarget.HasValue ? $"({repairTarget.Value.X},{repairTarget.Value.Y})" : "none")} - FAILED");
            }

            return success;
        }

        /// <summary>
        /// Logs turn count increments
        /// </summary>
        public void IncrementTurnCount()
        {
            _wrapped.IncrementTurnCount();
            int turnCount = _wrapped.GetTurnCount();
            LogMessage($"=== Turn {turnCount} Started ===");
        }

        /// <summary>
        /// Logs mine placement
        /// </summary>
        public void SetPlayerMines(string connId, List<NavalMine> mines)
        {
            LogMessage($"MINE PLACEMENT - Player: {connId}, Placed {mines.Count} mine(s)");
            foreach (var mine in mines)
            {
                LogMessage($"  {mine.Category} mine at ({mine.Position.X},{mine.Position.Y})");
            }
            _wrapped.SetPlayerMines(connId, mines);
        }

        /// <summary>
        /// Logs ship placement
        /// </summary>
        public void SetPlayerShips(string connId, List<Point> shipCells)
        {
            LogMessage($"SHIP PLACEMENT - Player: {connId}, Placed {shipCells.Count} total cells");
            _wrapped.SetPlayerShips(connId, shipCells);

            var ships = connId == _wrapped.PlayerA ? _wrapped.ShipsA : _wrapped.ShipsB;
            LogMessage($"  Created {ships.Count} ship(s)");
            foreach (var ship in ships)
            {
                var cells = ship.GetOccupiedCells();
                LogMessage($"    Ship {ship.Id} ({cells.Count} cells): {string.Join(", ", cells.Select(p => $"({p.X},{p.Y})"))}");
            }
        }
    }
}