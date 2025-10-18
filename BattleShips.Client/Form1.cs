using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShips.Core;

namespace BattleShips.Client
{
    public partial class Form1 : Form
    {
        private readonly GameClientController _controller;
        private readonly GameModel _model = new GameModel();
        private readonly BoardRenderer _renderer = new BoardRenderer(cell: 40, margin: 80);

        private bool _awaitingMove = false;

        private Panel? _startupPanel;
        private Button? _btnConnectLocal;
        private Label? _lblStatus;
        private Label? _lblCountdown;
        private System.Windows.Forms.Timer? _uiTimer;
        private System.Threading.CancellationTokenSource? _disasterCts;

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;

            var totalWidth = 2 * _renderer.Margin + Board.Size * _renderer.Cell * 2;
            var totalHeight = 2 * _renderer.Margin + Board.Size * _renderer.Cell + 80;
            MinimumSize = new Size(totalWidth, totalHeight);

            var rawClient = new GameClient();
            _controller = new GameClientController(rawClient, _model);

            InitStartupPanel();

            Paint += OnPaintGrid;
            MouseClick += OnMouseClickGrid;
            Resize += (_, __) => Invalidate();

            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += UiTimer_Tick;

            WireControllerEvents();
        }

        // ------------------------------
        //  UI Initialization
        // ------------------------------
        private void InitStartupPanel()
        {
            _startupPanel = new Panel
            {
                BackColor = Color.FromArgb(30, 34, 44),
                Size = new Size(ClientSize.Width, ClientSize.Height),
                Anchor = AnchorStyles.None
            };

            _btnConnectLocal = new Button
            {
                Text = "Connect to localhost",
                ForeColor = Color.White,
                Size = new Size(200, 32),
                Location = new Point(50, 30)
            };

            _lblStatus = new Label
            {
                Text = "Not connected",
                ForeColor = Color.White,
                Location = new Point(50, 72),
                AutoSize = true,
                Visible = true
            };

            _lblCountdown = new Label
            {
                Text = "",
                ForeColor = Color.Yellow,
                Location = new Point(50, 100),
                AutoSize = true,
                Visible = true
            };

            _btnConnectLocal.Click += async (_, __) =>
            {
                _btnConnectLocal.Enabled = false;
                _lblStatus.Text = "Connecting...";
                try
                {
                    await _controller.ConnectAsync("http://localhost:5000");
                    ResetBoards();
                    _model.State = AppState.Waiting;
                    _startupPanel!.Visible = false;
                    Text = "Connected to BattleShips server";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect: {ex.Message}", "Connect error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _lblStatus.Text = "Not connected";
                    _btnConnectLocal.Enabled = true;
                }
            };

            _startupPanel.Controls.Add(_btnConnectLocal);
            _startupPanel.Controls.Add(_lblStatus);
            _startupPanel.Controls.Add(_lblCountdown);
            Controls.Add(_startupPanel);
        }

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

        // ------------------------------
        //  Disaster Animation
        // ------------------------------
        private async Task PlayDisasterAnimationAsync(List<Point> cells, List<Point>? hitsForMe)
        {
            _disasterCts?.Cancel();
            _disasterCts = new System.Threading.CancellationTokenSource();
            var token = _disasterCts.Token;
            try
            {
                foreach (var cell in cells)
                {
                    token.ThrowIfCancellationRequested();
                    _model.AnimatedCells.Add(cell);
                    Invalidate();
                    await Task.Delay(300, token);

                    var wasHit = hitsForMe != null && hitsForMe.Contains(cell);
                    if (wasHit) _model.ApplyOpponentMove(cell, true);

                    _model.AnimatedCells.Remove(cell);
                    Invalidate();
                    await Task.Delay(120, token);
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _model.IsDisasterAnimating = false;
                _model.CurrentDisasterName = null;
                _model.AnimatedCells.Clear();
                Invalidate();
            }
        }

        // ------------------------------
        //  UI Timer / Helpers
        // ------------------------------
        private void UiTimer_Tick(object? sender, EventArgs e)
        {
            if (_model.State == AppState.Placement && _model.PlacementSecondsLeft > 0)
            {
                _model.PlacementSecondsLeft--;
                UpdateCountdownLabel();
                if (_model.PlacementSecondsLeft <= 0)
                {
                    _model.State = AppState.Waiting;
                    _lblStatus!.Text = "Placement time expired. Waiting for game to start...";
                }
                Invalidate();
            }
        }

        private void UpdateCountdownLabel()
        {
            if (_lblCountdown == null) return;

            if (_model.State == AppState.Placement)
            {
                _lblCountdown.Text = _model.PlacementSecondsLeft > 0 ? $"Placement: {_model.PlacementSecondsLeft}s" : "";
                return;
            }

            if (_model.State == AppState.Playing)
            {
                _lblCountdown.Text = _model.DisasterCountdown > 0
                    ? $"Disaster in {_model.DisasterCountdown} turns"
                    : (_model.DisasterCountdown == 0 ? "Disaster imminent!" : "");
                return;
            }

            _lblCountdown.Text = "";
        }

        private void ResetBoards()
        {
            _model.Reset();
            _uiTimer?.Stop();
            UpdateCountdownLabel();
            Invalidate();
        }

        // ------------------------------
        //  Drawing & Input
        // ------------------------------
        private void OnPaintGrid(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(20, 26, 38));

            using var font = new Font(Font.FontFamily, 10, FontStyle.Bold);
            _renderer.DrawBoards(g, _model, font);

            const int pad = 8;
            var statusText = _lblStatus?.Text ?? "";
            var countdownText = _lblCountdown?.Text ?? "";

            var statusSize = g.MeasureString(statusText, font);
            var statusRect = new RectangleF(_renderer.Margin - pad / 2f, 8 - pad / 2f, statusSize.Width + pad, statusSize.Height + pad);
            using (var bg = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
                g.FillRectangle(bg, statusRect);
            g.DrawString(statusText, font, Brushes.White, statusRect.Left + pad / 2f, statusRect.Top + pad / 2f);

            var countdownSize = g.MeasureString(countdownText, font);
            var countdownWidth = countdownSize.Width + pad;
            var centerX = (ClientSize.Width - countdownWidth) / 2f;
            var countdownRect = new RectangleF(centerX, 8 - pad / 2f, countdownWidth, countdownSize.Height + pad);
            using (var bg2 = new SolidBrush(Color.FromArgb(140, 30, 30, 0)))
                g.FillRectangle(bg2, countdownRect);
            g.DrawString(countdownText, font, Brushes.Yellow, countdownRect.Left + pad / 2f, countdownRect.Top + pad / 2f);

            if (_model.IsDisasterAnimating && !string.IsNullOrEmpty(_model.CurrentDisasterName))
            {
                using var bigFont = new Font(Font.FontFamily, 14, FontStyle.Bold);
                var txt = _model.CurrentDisasterName!;
                var size = g.MeasureString(txt, bigFont);
                var rect = new RectangleF((ClientSize.Width - size.Width) / 2f - 8, 40, size.Width + 16, size.Height + 8);
                using var bg = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
                g.FillRectangle(bg, rect);
                g.DrawString(txt, bigFont, Brushes.Orange, rect.Left + 8, rect.Top + 4);
            }
        }

        private async void OnMouseClickGrid(object? sender, MouseEventArgs e)
        {
            var leftRect = _renderer.GetLeftBoardRect();
            var rightRect = _renderer.GetRightBoardRect();
            var mouse = new Point(e.X, e.Y);

            var hitLeft = _renderer.HitTest(mouse, leftRect);
            if (hitLeft != null && _model.State == AppState.Placement)
            {
                var ok = _model.ToggleShip(hitLeft.Value, 10);
                if (!ok)
                {
                    MessageBox.Show("Max 10 ships placed", "Placement", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                _lblStatus!.Text = $"Placement: place 10 ships ({_model.YourShips.Count}/10)";
                Invalidate();

                if (_model.YourShips.Count == 10 && _controller.IsConnected)
                {
                    try
                    {
                        await _controller.PlaceShips(_model.YourShips.ToList());
                        _model.State = AppState.Waiting;
                        _uiTimer?.Stop();
                        _model.PlacementSecondsLeft = 0;
                        UpdateCountdownLabel();
                        _lblStatus!.Text = "Placement submitted. Waiting for opponent...";
                        Invalidate();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to send placement: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return;
            }

            var hitRight = _renderer.HitTest(mouse, rightRect);
            if (hitRight != null)
            {
                if (_model.IsDisasterAnimating)
                {
                    MessageBox.Show("Cannot make moves while disaster is happening", "Wait", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (_model.State != AppState.Playing || !_model.IsMyTurn) 
                {
                    MessageBox.Show("Not your turn", "Wait", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                if (_awaitingMove) return;

                if (_controller.IsConnected)
                {
                    try
                    {
                        _awaitingMove = true;
                        _model.IsMyTurn = false;
                        _lblStatus!.Text = "Move sent...";
                        await _controller.MakeMove(hitRight.Value.X, hitRight.Value.Y);
                        _model.YourFired.Add(hitRight.Value);
                        Invalidate();
                    }
                    catch (Exception ex)
                    {
                        _awaitingMove = false;
                        _model.IsMyTurn = true;
                        Console.WriteLine("Send failed: " + ex.Message);
                    }
                }
            }
        }
    }
}
