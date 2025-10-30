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
        }
    }
}
