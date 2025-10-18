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
                _lblStatus!.Text = msg ?? "Waiting for opponent...";
                Invalidate();
            });

            _controller.StartPlacement += secs => BeginInvoke(() =>
            {
                _model.State = AppState.Placement;
                _model.PlacementSecondsLeft = secs;
                _lblStatus!.Text = $"Placement: place 10 ships ({_model.YourShips.Count}/10)";
                UpdateCountdownLabel();
                _uiTimer!.Start();
                Invalidate();
            });

            _controller.PlacementAck += count => BeginInvoke(() =>
            {
                _model.State = AppState.Waiting;
                _uiTimer?.Stop();
                _model.PlacementSecondsLeft = 0;
                UpdateCountdownLabel();
                _lblStatus!.Text = $"Placed {count} ships. Waiting for opponent...";
                Invalidate();
            });

            _controller.GameStarted += youStart => BeginInvoke(() =>
            {
                _model.State = AppState.Playing;
                _model.IsMyTurn = youStart;
                _lblStatus!.Text = youStart ? "Your turn" : "Opponent's turn";
                _uiTimer?.Stop();
                _model.PlacementSecondsLeft = 0;
                UpdateCountdownLabel();
                Invalidate();
            });

            _controller.YourTurn += () => BeginInvoke(() =>
            {
                _awaitingMove = false;
                _model.IsMyTurn = true;
                _lblStatus!.Text = "Your turn";
                Invalidate();
            });

            _controller.OpponentTurn += () => BeginInvoke(() =>
            {
                _model.IsMyTurn = false;
                _lblStatus!.Text = "Opponent's turn";
                Invalidate();
            });

            _controller.MoveResult += (col, row, hit, remaining) => BeginInvoke(() =>
            {
                _awaitingMove = false;
                var p = new Point(col, row);
                _model.ApplyMoveResult(p, hit);
                _lblStatus!.Text = hit ? $"Hit! Opponent ships left: {remaining}" : $"Miss. Opponent ships left: {remaining}";
                Invalidate();
            });

            _controller.OpponentMoved += (col, row, hit) => BeginInvoke(() =>
            {
                var p = new Point(col, row);
                _model.ApplyOpponentMove(p, hit);
                _lblStatus!.Text = hit ? "Opponent hit your ship!" : "Opponent missed.";
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
                InitStartupPanel();
                _lblStatus!.Text = "Opponent disconnected";
                _startupPanel!.Visible = true;
            });

            _controller.GameOver += msg => BeginInvoke(() =>
            {
                _model.State = AppState.GameOver;
                MessageBox.Show(msg ?? "Game over", "Game", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ResetBoards();
                InitStartupPanel();
                _lblStatus!.Text = "Game over";
                _startupPanel!.Visible = true;
            });

            _controller.GameCancelled += msg => BeginInvoke(() =>
            {
                MessageBox.Show(msg ?? "Game cancelled", "Game Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ResetBoards();
                _model.State = AppState.Menu;
                // Show existing startup panel instead of creating a new one
                _lblStatus!.Text = "Game cancelled";
                _startupPanel!.Visible = true;
                _btnConnectLocal!.Enabled = true;
                Invalidate(); // Ensure UI repaints with the updated state
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
