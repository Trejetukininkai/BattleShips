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
        private readonly GameClientController _controller;
        private readonly GameModel _model = new GameModel();
        private readonly BoardRenderer _renderer = new BoardRenderer(cell: 40, margin: 80);
        private readonly ShipPaletteRenderer _paletteRenderer = new ShipPaletteRenderer(cell: 40, margin: 80);

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
            var boardHeight = Board.Size * _renderer.Cell;
            var totalHeight = 2 * _renderer.Margin + boardHeight + 120; // Extra space for palette below
            MinimumSize = new Size(totalWidth, totalHeight);

            var rawClient = new GameClient();
            _controller = new GameClientController(rawClient, _model);

            // Initialize fleet
            _model.YourShips.AddRange(FleetConfiguration.CreateStandardFleet());

            InitStartupPanel();

            Paint += OnPaintGrid;
            MouseClick += OnMouseClickGrid;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseMove += OnMouseMove;
            KeyDown += OnKeyDown;
            Resize += (_, __) => Invalidate();
            
            // Enable key events
            KeyPreview = true;

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
            // Re-initialize fleet after reset
            _model.YourShips.AddRange(FleetConfiguration.CreateStandardFleet());
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
            
            // Draw ship palette below boards if in placement mode
            if (_model.State == AppState.Placement)
            {
                var boardHeight = Board.Size * _renderer.Cell;
                _paletteRenderer.DrawShipPalette(g, _model, font, ClientSize.Width, boardHeight);
            }

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

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (_model.State != AppState.Placement) return;
            
            var mouse = new Point(e.X, e.Y);
            
            // Check if clicking on ship palette
            var boardHeight = Board.Size * _renderer.Cell;
            var ship = _paletteRenderer.HitTestShip(mouse, _model, ClientSize.Width, boardHeight);
            if (ship != null && !ship.IsPlaced)
            {
                _model.DraggedShip = ship;
                _model.DragOffset = Point.Empty;
                Cursor = Cursors.Hand;
            }
            else if (e.Button == MouseButtons.Right && _model.DraggedShip != null)
            {
                // Right-click to rotate ship
                _model.DraggedShip.Rotate();
                Invalidate();
            }
        }

        private void OnMouseMove(object? sender, MouseEventArgs e)
        {
            if (_model.DraggedShip != null)
            {
                var leftRect = _renderer.GetLeftBoardRect();
                var boardPos = _renderer.HitTest(new Point(e.X, e.Y), leftRect);
                
                // Store the current mouse position for preview drawing
                _model.DraggedShip.Position = boardPos ?? new Point(-1, -1);
                
                if (boardPos != null && _model.CanPlaceShip(_model.DraggedShip, boardPos.Value))
                {
                    Cursor = Cursors.Default;
                }
                else
                {
                    Cursor = Cursors.No;
                }
                
                Invalidate();
            }
        }

        private void OnMouseUp(object? sender, MouseEventArgs e)
        {
            if (_model.DraggedShip != null)
            {
                var leftRect = _renderer.GetLeftBoardRect();
                var boardPos = _renderer.HitTest(new Point(e.X, e.Y), leftRect);
                
                if (boardPos != null && _model.CanPlaceShip(_model.DraggedShip, boardPos.Value))
                {
                    _model.PlaceShip(_model.DraggedShip, boardPos.Value);
                    
                    // Check if all ships are placed
                    var allPlaced = _model.YourShips.All(s => s.IsPlaced);
                    if (allPlaced && _controller.IsConnected)
                    {
                        try
                        {
                            var shipCells = _model.GetAllShipCells();
                            _controller.PlaceShips(shipCells);
                            _model.State = AppState.Waiting;
                            _uiTimer?.Stop();
                            _model.PlacementSecondsLeft = 0;
                            UpdateCountdownLabel();
                            _lblStatus!.Text = "Placement submitted. Waiting for opponent...";
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to send placement: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        var placedCount = _model.YourShips.Count(s => s.IsPlaced);
                        var totalCount = _model.YourShips.Count;
                        _lblStatus!.Text = $"Placement: place ships ({placedCount}/{totalCount})";
                    }
                }
                
                // Reset the ship position if it wasn't placed
                if (!_model.DraggedShip.IsPlaced)
                {
                    _model.DraggedShip.Position = Point.Empty;
                }
                
                _model.DraggedShip = null;
                Cursor = Cursors.Default;
                Invalidate();
            }
        }

        private async void OnMouseClickGrid(object? sender, MouseEventArgs e)
        {
            var rightRect = _renderer.GetRightBoardRect();
            var mouse = new Point(e.X, e.Y);

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

        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (_model.DraggedShip != null && (e.KeyCode == Keys.R || e.KeyCode == Keys.Space))
            {
                _model.DraggedShip.Rotate();
                Invalidate();
                e.Handled = true;
            }
        }
    }
}
