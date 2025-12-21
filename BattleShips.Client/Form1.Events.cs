using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShips.Core;
using BattleShips.Core.Client;

namespace BattleShips.Client
{
    public partial class Form1 : Form
    {
        // ------------------------------
        //  Controller Events
        // ------------------------------
        private void WireControllerEvents()
        {
            _controller.DisasterCountdownChanged += v => BeginInvoke(() =>
            {
                _model.DisasterCountdown = v;
                UpdateCountdownLabel();
                Invalidate();
            });

            _controller.WaitingForOpponent += msg => BeginInvoke(() =>
            {
                _model.State = AppState.Waiting;
                _model.CurrentStatus = msg ?? "Waiting for opponent...";
            });

            _controller.StartPlacement += secs => BeginInvoke(() =>
            {
                // Don't override state if we're already playing (from reconnection)
                if (_model.State == AppState.Playing)
                {
                    Console.WriteLine($"[Form1] Ignoring StartPlacement because state is already Playing (reconnection)");
                    return;
                }

                _model.State = AppState.Placement;
                _model.PlacementSecondsLeft = secs;
                var placedCount = _model.YourShips.Count(s => s.IsPlaced);
                var totalCount = _model.YourShips.Count;
                _model.CurrentStatus = $"Placement: drag ships from palette below ({placedCount}/{totalCount})";
                _uiTimer!.Start();
            });

            _controller.PlacementAck += count => BeginInvoke(() =>
            {
                _model.State = AppState.Waiting;
                _uiTimer?.Stop();
                _model.PlacementSecondsLeft = 0;
                _model.CurrentStatus = $"Placed {count} ships. Waiting for opponent...";
            });

            _controller.GameStarted += youStart => BeginInvoke(() =>
            {
                _model.State = AppState.Playing;
                _model.IsMyTurn = youStart;
                _model.CurrentStatus = youStart ? "Your turn" : "Opponent's turn";
                _uiTimer?.Stop();
                _model.PlacementSecondsLeft = 0;
            });

            _controller.YourTurn += () => BeginInvoke(() =>
            {
                _awaitingMove = false;
                _model.IsMyTurn = true;
                _model.CurrentStatus = "Your turn";
            });

            _controller.OpponentTurn += () => BeginInvoke(() =>
            {
                _model.IsMyTurn = false;
                _model.CurrentStatus = "Opponent's turn";
            });

            _controller.MoveResult += (col, row, hit, remaining) => BeginInvoke(() =>
            {
                _awaitingMove = false;
                var p = new Point(col, row);
                _model.ApplyMoveResult(p, hit);
                _model.CurrentStatus = hit ? $"Hit! Opponent ships left: {remaining}" : $"Miss. Opponent ships left: {remaining}";
            });

            _controller.OpponentMoved += (col, row, hit) => BeginInvoke(() =>
            {
                var p = new Point(col, row);
                _model.ApplyOpponentMove(p, hit);
                _model.CurrentStatus = hit ? "Opponent hit your ship!" : "Opponent missed.";
            });

            _controller.OpponentHitByDisaster += (col, row) => BeginInvoke(() =>
            {
                var p = new Point(col, row);
                _model.ApplyOpponentHitByDisaster(p);
                Invalidate();
            });

            _controller.MaxPlayersReached += msg => BeginInvoke(() =>
            {
                MessageBox.Show(msg ?? "Server full", "Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _btnConnectLocal!.Enabled = true;
            });

            _controller.OpponentDisconnected += msg => BeginInvoke(() =>
            {
                MessageBox.Show(msg ?? "Opponent disconnected", "Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetBoards();
                _model.State = AppState.Menu;
                _model.CurrentStatus = "Opponent disconnected";
                InitStartupPanel();
                _startupPanel!.Visible = true;
            });

            _controller.GameOver += msg => BeginInvoke(() =>
            {
                _model.State = AppState.GameOver;
                MessageBox.Show(msg ?? "Game over", "Game", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetBoards();
                _model.CurrentStatus = "Game over";
                InitStartupPanel();
                _startupPanel!.Visible = true;
            });

            _controller.GameCancelled += msg => BeginInvoke(() =>
            {
                MessageBox.Show(msg ?? "Game cancelled", "Game Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetBoards();
                _model.State = AppState.Menu;
                // Show existing startup panel instead of creating a new one
                _model.CurrentStatus = "Game cancelled";
                _startupPanel!.Visible = true;
                _btnConnectLocal!.Enabled = true;
            });

            _controller.Error += msg => BeginInvoke(() =>
            {
                MessageBox.Show(msg ?? "Error", "Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
            });

            _controller.DisasterOccurred += (cells, hitsForMe, type) => BeginInvoke(() =>
            {
                _model.CurrentDisasterName = type ?? "Disaster";
                _model.IsDisasterAnimating = true;
                _ = PlayDisasterAnimationAsync(cells, hitsForMe);
            });

            _controller.DisasterFinished += () => BeginInvoke(() =>
            {
                _model.IsDisasterAnimating = false;
                _model.AnimatedCells.Clear();
                Invalidate();
            });

            _controller.StartMinePlacement += duration => BeginInvoke(() =>
            {
                _model.State = AppState.MineSelection;
                _model.CurrentStatus = $"Place your mine(s) on your board ({duration}s)";
            });

            _controller.MinesAck += count => BeginInvoke(() =>
            {
                _model.State = AppState.Waiting;
                _model.CurrentStatus = $"Placed {count} mines. Waiting for opponent...";
            });

            // ========================================
            // RECONNECTION EVENT HANDLERS
            // ========================================

            _controller.Client.PlayerNameSet += name => BeginInvoke(() =>
            {
                Console.WriteLine($"[Form1] Player name set to: {name}");
            });

            _controller.Client.GameStateRestored += gameStateObj => BeginInvoke(() =>
            {
                try
                {
                    Console.WriteLine($"[Form1] Restoring game state from reconnection");
                    Console.WriteLine($"[Form1] gameStateObj type: {gameStateObj?.GetType().Name}");

                    // Hide startup panel
                    if (_startupPanel != null)
                        _startupPanel.Visible = false;

                    // Clear any lingering animated cells from previous disasters
                    _model.AnimatedCells.Clear();

                    // Parse the game state using System.Text.Json
                    var json = System.Text.Json.JsonSerializer.Serialize(gameStateObj);
                    Console.WriteLine($"[Form1] Serialized JSON length: {json.Length}");
                    Console.WriteLine($"[Form1] JSON preview: {json.Substring(0, Math.Min(200, json.Length))}...");

                    var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    Console.WriteLine($"[Form1] Parsed JSON game state");

                    // Restore your hit cells (damage on your board from opponent)
                    // NOTE: JSON properties are camelCase from server
                    if (root.TryGetProperty("yourHitCells", out var yourHitCells))
                    {
                        _model.YourHitsByOpponent.Clear();
                        var hitCount = 0;
                        foreach (var cell in yourHitCells.EnumerateArray())
                        {
                            var x = cell.GetProperty("x").GetInt32();
                            var y = cell.GetProperty("y").GetInt32();
                            _model.YourHitsByOpponent.Add(new Point(x, y));
                            hitCount++;
                            Console.WriteLine($"[Form1] Restored damage on your board: ({x},{y})");
                        }
                        Console.WriteLine($"[Form1] Total damage on your board: {hitCount} hits");
                    }

                    // Restore opponent hit cells (successful hits YOU made on opponent's board)
                    // OpponentHitCells from server = cells where WE successfully hit the opponent
                    if (root.TryGetProperty("opponentHitCells", out var opponentHitCells))
                    {
                        _model.YourFiredHits.Clear(); // Clear successful hits on opponent
                        _model.YourFired.Clear(); // Clear all fired cells
                        var hitCount = 0;
                        foreach (var cell in opponentHitCells.EnumerateArray())
                        {
                            var x = cell.GetProperty("x").GetInt32();
                            var y = cell.GetProperty("y").GetInt32();
                            var point = new Point(x, y);
                            _model.YourFiredHits.Add(point); // Successful hits on opponent
                            _model.YourFired.Add(point); // Also add to fired cells
                            hitCount++;
                            Console.WriteLine($"[Form1] Restored successful hit on opponent: ({x},{y})");
                        }
                        Console.WriteLine($"[Form1] Total successful hits on opponent: {hitCount} hits");
                    }

                    // NOTE: Misses are NOT restored because the server doesn't track them
                    // Only successful hits are tracked and restored
                    // This means missed shots won't show up after reconnection

                    // Restore ships
                    if (root.TryGetProperty("yourShips", out var yourShips))
                    {
                        Console.WriteLine($"[Form1] Found yourShips property in JSON");
                        _model.YourShips.Clear();
                        var shipCount = 0;
                        foreach (var shipData in yourShips.EnumerateArray())
                        {
                            shipCount++;
                            var id = shipData.GetProperty("id").GetInt32();
                            var length = shipData.GetProperty("length").GetInt32();
                            var posX = shipData.GetProperty("position").GetProperty("x").GetInt32();
                            var posY = shipData.GetProperty("position").GetProperty("y").GetInt32();
                            var orientation = shipData.GetProperty("orientation").GetInt32();
                            var isPlaced = shipData.GetProperty("isPlaced").GetBoolean();

                            // Create a blocky destroyer as placeholder (since we don't have the exact ship type)
                            var ship = new BlockyClass().CreateDestroyer(length, id);
                            ship.Position = new Point(posX, posY);
                            ship.Orientation = (ShipOrientation)orientation;
                            ship.IsPlaced = isPlaced;

                            _model.YourShips.Add(ship);
                            Console.WriteLine($"[Form1] Restored ship #{shipCount}: id={id} at ({posX},{posY}), length={length}, placed={isPlaced}");
                        }
                        Console.WriteLine($"[Form1] Total ships restored: {shipCount}, YourShips.Count = {_model.YourShips.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"[Form1] WARNING: yourShips property NOT found in JSON!");
                    }

                    // Restore turn state
                    if (root.TryGetProperty("isYourTurn", out var isYourTurn))
                    {
                        _model.IsMyTurn = isYourTurn.GetBoolean();
                        Console.WriteLine($"[Form1] Restored turn state: IsMyTurn={_model.IsMyTurn}");
                    }

                    // Restore action points
                    if (root.TryGetProperty("yourActionPoints", out var actionPoints))
                    {
                        _model.ActionPoints = actionPoints.GetInt32();
                        Console.WriteLine($"[Form1] Restored action points: {_model.ActionPoints}");
                    }

                    // Restore disaster countdown
                    if (root.TryGetProperty("disasterCountdown", out var disasterCountdown))
                    {
                        _model.DisasterCountdown = disasterCountdown.GetInt32();
                        Console.WriteLine($"[Form1] Restored disaster countdown: {_model.DisasterCountdown}");
                    }

                    // Restore mines
                    if (root.TryGetProperty("yourMines", out var yourMines))
                    {
                        Console.WriteLine($"[Form1] Found yourMines property in JSON");
                        _model.YourMines.Clear();
                        var mineCount = 0;
                        var restoredCount = 0;
                        foreach (var mineData in yourMines.EnumerateArray())
                        {
                            mineCount++;
                            var posX = mineData.GetProperty("position").GetProperty("x").GetInt32();
                            var posY = mineData.GetProperty("position").GetProperty("y").GetInt32();
                            var categoryStr = mineData.GetProperty("category").GetInt32();
                            var isExploded = mineData.GetProperty("isExploded").GetBoolean();

                            // Use the factory to create the mine
                            var mine = NavalMineFactory.CreateMine(new Point(posX, posY), "LOCAL", (MineCategory)categoryStr);
                            // Note: We can't directly set IsExploded as it's private, but for display purposes
                            // exploded mines won't be shown in the client (they're already triggered)
                            if (!isExploded)
                            {
                                _model.YourMines.Add(mine);
                                restoredCount++;
                            }
                            Console.WriteLine($"[Form1] Restored mine #{mineCount} at ({posX},{posY}), category={categoryStr}, exploded={isExploded}, added={!isExploded}");
                        }
                        Console.WriteLine($"[Form1] Total mines found: {mineCount}, restored (non-exploded): {restoredCount}, YourMines.Count = {_model.YourMines.Count}");
                    }
                    else
                    {
                        Console.WriteLine($"[Form1] WARNING: yourMines property NOT found in JSON!");
                    }

                    // Check if game has actually started
                    var gameStarted = false;
                    if (root.TryGetProperty("started", out var startedProperty))
                    {
                        gameStarted = startedProperty.GetBoolean();
                        Console.WriteLine($"[Form1] Game started status: {gameStarted}");
                    }

                    // Set state based on whether game has started
                    if (gameStarted)
                    {
                        _model.State = AppState.Playing;
                        _model.CurrentStatus = _model.IsMyTurn ? "Your turn" : "Opponent's turn";
                        Console.WriteLine($"[Form1] Set state to Playing, IsMyTurn={_model.IsMyTurn}");
                    }
                    else
                    {
                        _model.State = AppState.Waiting;
                        _model.CurrentStatus = "Waiting for opponent to reconnect...";
                        Console.WriteLine($"[Form1] Set state to Waiting (game not started yet)");
                    }

                    Console.WriteLine($"[Form1] Successfully restored game state");
                    MessageBox.Show("Successfully reconnected to your saved game!", "Reconnection Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Invalidate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Form1] Error restoring game state: {ex.Message}");
                    Console.WriteLine($"[Form1] Stack trace: {ex.StackTrace}");
                    MessageBox.Show($"Error restoring game state: {ex.Message}", "Restore Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            });

            _controller.Client.ReconnectFailed += message => BeginInvoke(() =>
            {
                MessageBox.Show(message ?? "No saved game found for this player name.", "Reconnection Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                _model.CurrentStatus = "❌ Reconnection failed";
                if (_btnConnectLocal != null) _btnConnectLocal.Enabled = true;
                if (_btnReconnect != null) _btnReconnect.Enabled = true;
            });

            _controller.Client.OpponentReconnected += message => BeginInvoke(() =>
            {
                MessageBox.Show(message ?? "Your opponent has reconnected!", "Opponent Reconnected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                _model.CurrentStatus = "Opponent reconnected - game resumed!";
            });

        }
    }
}
