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
        private readonly SFXService _sfx;
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
            
            // Modern dark theme
            BackColor = Color.FromArgb(15, 20, 30);
            ForeColor = Color.White;

            var totalWidth = 2 * _renderer.Margin + Board.Size * _renderer.Cell * 2;
            var boardHeight = Board.Size * _renderer.Cell;
            var totalHeight = 2 * _renderer.Margin + boardHeight + 150; // Extra space for palette below (increased from 120 to 150)
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

            _sfx = new SFXService(_model);

            _uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _uiTimer.Tick += UiTimer_Tick;

            WireControllerEvents();

            // Subscribe to model property changes for automatic UI updates
            _model.PropertyChanged += OnModelPropertyChanged;
        }

        // ------------------------------
        //  UI Initialization
        // ------------------------------
        private void InitStartupPanel()
        {
            _startupPanel = new Panel
            {
                BackColor = Color.FromArgb(20, 25, 35),
                Size = new Size(ClientSize.Width, ClientSize.Height),
                Anchor = AnchorStyles.None
            };

            // Calculate center positions
            var centerX = ClientSize.Width / 2;
            var startY = 60;

            // Main title
            var titleLabel = new Label
            {
                Text = "⚓ BATTLESHIPS ⚓",
                ForeColor = Color.FromArgb(100, 150, 255), // Light blue
                Font = new Font("Segoe UI", 28, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var titleSize = titleLabel.PreferredSize;
            titleLabel.Location = new Point(centerX - titleSize.Width / 2, startY);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "🌊 Welcome to the ultimate naval warfare experience! 🌊",
                ForeColor = Color.FromArgb(180, 200, 220),
                Font = new Font("Segoe UI", 12, FontStyle.Italic),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            var subtitleSize = subtitleLabel.PreferredSize;
            subtitleLabel.Location = new Point(centerX - subtitleSize.Width / 2, startY + 50);

            // Buttons setup
            var buttonWidth = 280;
            var buttonHeight = 50;
            var buttonSpacing = 20;
            var buttonsStartY = startY + 120;

            // Connect to Server button
            _btnConnectLocal = new Button
            {
                Text = "🚀 Start Game",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(46, 204, 113), // Modern green
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnConnectLocal.FlatAppearance.BorderSize = 0;
            _btnConnectLocal.FlatAppearance.MouseOverBackColor = Color.FromArgb(39, 174, 96);

            // Settings button
            var btnSettings = new Button
            {
                Text = "⚙️ Settings",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 73, 94), // Modern gray
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY + buttonHeight + buttonSpacing),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSettings.FlatAppearance.BorderSize = 0;
            btnSettings.FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 62, 80);

            // Quit button
            var btnQuit = new Button
            {
                Text = "❌ Quit Game",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(231, 76, 60), // Modern red
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY + 2 * (buttonHeight + buttonSpacing)),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnQuit.FlatAppearance.BorderSize = 0;
            btnQuit.FlatAppearance.MouseOverBackColor = Color.FromArgb(192, 57, 43);

            // Status and countdown labels (moved down)
            _lblStatus = new Label
            {
                Text = "⚡ Ready to start your naval adventure",
                ForeColor = Color.FromArgb(200, 200, 200),
                AutoSize = true,
                Visible = true,
                Font = new Font("Segoe UI", 10),
                BackColor = Color.Transparent
            };
            var statusSize = _lblStatus.PreferredSize;
            _lblStatus.Location = new Point(centerX - statusSize.Width / 2, buttonsStartY + 3 * (buttonHeight + buttonSpacing) + 20);

            _lblCountdown = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(255, 193, 7), // Modern amber
                AutoSize = true,
                Visible = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            _lblCountdown.Location = new Point(centerX - 50, buttonsStartY + 3 * (buttonHeight + buttonSpacing) + 45);

            // Event handlers
            _btnConnectLocal.Click += async (_, __) =>
            {
                _btnConnectLocal.Enabled = false;
                _model.CurrentStatus = "🔄 Connecting to server...";
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
                    MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _model.CurrentStatus = "❌ Connection failed - Try again";
                    _btnConnectLocal.Enabled = true;
                }
            };

            btnSettings.Click += (_, __) =>
            {
                MessageBox.Show("⚙️ Settings panel coming soon!\n\nFuture features:\n• Sound effects\n• Graphics quality\n• Key bindings\n• Difficulty levels", 
                    "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnQuit.Click += (_, __) =>
            {
                var result = MessageBox.Show("Are you sure you want to quit BattleShips?", 
                    "Quit Game", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    Application.Exit();
                }
            };

            // Add all controls to the panel
            _startupPanel.Controls.Add(titleLabel);
            _startupPanel.Controls.Add(subtitleLabel);
            _startupPanel.Controls.Add(_btnConnectLocal);
            _startupPanel.Controls.Add(btnSettings);
            _startupPanel.Controls.Add(btnQuit);
            _startupPanel.Controls.Add(_lblStatus);
            _startupPanel.Controls.Add(_lblCountdown);
            Controls.Add(_startupPanel);
        }



        // ------------------------------
        //  Enhanced Disaster Animation System
        // ------------------------------
        private readonly Random _animRandom = new Random();
        private float _screenShakeIntensity = 0f;
        private int _flashIntensity = 0;
        private readonly List<ParticleEffect> _particles = new List<ParticleEffect>();

        private class ParticleEffect
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public float Life { get; set; } = 1.0f;
            public float MaxLife { get; set; } = 1.0f;
            public Color Color { get; set; }
            public float Size { get; set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }
        }

        private async Task PlayDisasterAnimationAsync(List<Point> cells, List<Point>? hitsForMe)
        {
            _disasterCts?.Cancel();
            _disasterCts = new System.Threading.CancellationTokenSource();
            var token = _disasterCts.Token;
            
            try
            {
                // Pre-animation flash and shake
                _flashIntensity = 255;
                _screenShakeIntensity = 8f;
                
                // Create initial particle burst
                CreateDisasterParticles(cells);
                
                var animationTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 FPS
                animationTimer.Tick += (s, e) => 
                {
                    UpdateParticles();
                    UpdateScreenEffects();
                    Invalidate();
                };
                animationTimer.Start();

                // Animate each cell with smooth timing
                for (int i = 0; i < cells.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    var cell = cells[i];
                    
                    // Add cell to animated list with wave effect
                    _model.AnimatedCells.Add(cell);
                    
                    // Create explosion particles for this cell
                    CreateExplosionParticles(cell);
                    
                    // Screen shake on impact
                    _screenShakeIntensity = Math.Max(_screenShakeIntensity, 5f);
                    _flashIntensity = Math.Max(_flashIntensity, 150);
                    
                    Invalidate();
                    
                    // Staggered timing for wave effect
                    var delay = 150 + (i * 50); // Each cell 50ms after the previous
                    await Task.Delay(delay, token);

                    // Check for hit and apply damage
                    var wasHit = hitsForMe != null && hitsForMe.Contains(cell);
                    if (wasHit) 
                    {
                        _model.ApplyOpponentMove(cell, true);
                        // Extra particles for hits
                        CreateHitParticles(cell);
                    }

                    // Keep cell visible for a bit longer
                    await Task.Delay(200, token);
                }

                // Final dramatic pause with fading effects
                await Task.Delay(800, token);
                
                animationTimer.Stop();
                animationTimer.Dispose();
            }
            catch (OperationCanceledException) { }
            finally
            {
                // Clean up animation state
                _model.IsDisasterAnimating = false;
                _model.CurrentDisasterName = null;
                _model.AnimatedCells.Clear();
                _particles.Clear();
                _screenShakeIntensity = 0f;
                _flashIntensity = 0;
                Invalidate();
            }
        }

        private void CreateDisasterParticles(List<Point> cells)
        {
            // Create ambient particles around disaster area
            foreach (var cell in cells)
            {
                var boardRect = _renderer.GetLeftBoardRect();
                var cellX = boardRect.X + cell.X * _renderer.Cell + _renderer.Cell / 2;
                var cellY = boardRect.Y + cell.Y * _renderer.Cell + _renderer.Cell / 2;

                // Create swirling particles
                for (int i = 0; i < 8; i++)
                {
                    var angle = (float)(i * Math.PI * 2 / 8);
                    var distance = 30 + _animRandom.Next(20);
                    
                    _particles.Add(new ParticleEffect
                    {
                        Position = new PointF(cellX + (float)Math.Cos(angle) * distance, 
                                            cellY + (float)Math.Sin(angle) * distance),
                        Velocity = new PointF((float)Math.Cos(angle) * 2, (float)Math.Sin(angle) * 2),
                        Life = 1.0f,
                        MaxLife = 2.0f + _animRandom.NextSingle(),
                        Color = Color.FromArgb(255, 100, 150, 255),
                        Size = 3 + _animRandom.Next(4),
                        Rotation = _animRandom.NextSingle() * (float)Math.PI * 2,
                        RotationSpeed = (_animRandom.NextSingle() - 0.5f) * 0.2f
                    });
                }
            }
        }

        private void CreateExplosionParticles(Point cell)
        {
            var boardRect = _renderer.GetLeftBoardRect();
            var cellX = boardRect.X + cell.X * _renderer.Cell + _renderer.Cell / 2;
            var cellY = boardRect.Y + cell.Y * _renderer.Cell + _renderer.Cell / 2;

            // Create explosion burst
            for (int i = 0; i < 15; i++)
            {
                var angle = _animRandom.NextSingle() * (float)Math.PI * 2;
                var speed = 3 + _animRandom.NextSingle() * 5;
                
                _particles.Add(new ParticleEffect
                {
                    Position = new PointF(cellX, cellY),
                    Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Life = 1.0f,
                    MaxLife = 1.5f,
                    Color = Color.FromArgb(255, 255, _animRandom.Next(100, 200), 0),
                    Size = 2 + _animRandom.Next(3),
                    Rotation = 0,
                    RotationSpeed = (_animRandom.NextSingle() - 0.5f) * 0.3f
                });
            }
        }

        private void CreateHitParticles(Point cell)
        {
            var boardRect = _renderer.GetLeftBoardRect();
            var cellX = boardRect.X + cell.X * _renderer.Cell + _renderer.Cell / 2;
            var cellY = boardRect.Y + cell.Y * _renderer.Cell + _renderer.Cell / 2;

            // Create red hit particles
            for (int i = 0; i < 10; i++)
            {
                var angle = _animRandom.NextSingle() * (float)Math.PI * 2;
                var speed = 2 + _animRandom.NextSingle() * 3;
                
                _particles.Add(new ParticleEffect
                {
                    Position = new PointF(cellX, cellY),
                    Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Life = 1.0f,
                    MaxLife = 2.0f,
                    Color = Color.FromArgb(255, 255, 50, 50),
                    Size = 3 + _animRandom.Next(2),
                    Rotation = 0,
                    RotationSpeed = (_animRandom.NextSingle() - 0.5f) * 0.2f
                });
            }
        }

        private void UpdateParticles()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];
                
                // Update position
                particle.Position = new PointF(
                    particle.Position.X + particle.Velocity.X,
                    particle.Position.Y + particle.Velocity.Y
                );
                
                // Update rotation
                particle.Rotation += particle.RotationSpeed;
                
                // Apply gravity and drag
                particle.Velocity = new PointF(
                    particle.Velocity.X * 0.98f,
                    particle.Velocity.Y * 0.98f + 0.1f
                );
                
                // Update life
                particle.Life -= 1.0f / 60.0f; // Assuming 60 FPS
                
                // Remove dead particles
                if (particle.Life <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        private void UpdateScreenEffects()
        {
            // Fade screen shake
            _screenShakeIntensity *= 0.9f;
            if (_screenShakeIntensity < 0.1f) _screenShakeIntensity = 0f;
            
            // Fade flash
            _flashIntensity = (int)(_flashIntensity * 0.95f);
            if (_flashIntensity < 5) _flashIntensity = 0;
        }

        private void DrawParticleEffects(Graphics g)
        {
            foreach (var particle in _particles)
            {
                var alpha = (int)(255 * (particle.Life / particle.MaxLife));
                var color = Color.FromArgb(alpha, particle.Color.R, particle.Color.G, particle.Color.B);
                
                using (var brush = new SolidBrush(color))
                {
                    var size = particle.Size * (particle.Life / particle.MaxLife);
                    var x = particle.Position.X - size / 2;
                    var y = particle.Position.Y - size / 2;
                    
                    // Draw particle as a rotated rectangle or circle
                    if (particle.Rotation != 0)
                    {
                        var oldTransform = g.Transform;
                        g.TranslateTransform(particle.Position.X, particle.Position.Y);
                        g.RotateTransform(particle.Rotation * 180f / (float)Math.PI);
                        g.FillRectangle(brush, -size/2, -size/2, size, size);
                        g.Transform = oldTransform;
                    }
                    else
                    {
                        g.FillEllipse(brush, x, y, size, size);
                    }
                }
                
                // Add glow effect for brighter particles
                if (particle.Color.R > 200 || particle.Color.G > 200)
                {
                    var glowAlpha = Math.Min(100, alpha / 2);
                    var glowColor = Color.FromArgb(glowAlpha, particle.Color.R, particle.Color.G, particle.Color.B);
                    using (var glowBrush = new SolidBrush(glowColor))
                    {
                        var glowSize = particle.Size * 2;
                        var glowX = particle.Position.X - glowSize / 2;
                        var glowY = particle.Position.Y - glowSize / 2;
                        g.FillEllipse(glowBrush, glowX, glowY, glowSize, glowSize);
                    }
                }
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
                if (_model.PlacementSecondsLeft <= 0)
                {
                    _model.State = AppState.Waiting;
                    _model.CurrentStatus = "Placement time expired. Waiting for game to start...";
                }
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

        // PropertyChanged event handler for automatic UI updates
        private void OnModelPropertyChanged(object? sender, BattleShips.Core.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_model.CurrentStatus):
                    if (_lblStatus != null)
                    {
                        _lblStatus.Text = _model.CurrentStatus;
                        Invalidate();
                    }
                    break;
                case nameof(_model.State):
                case nameof(_model.IsMyTurn):
                case nameof(_model.PlacementSecondsLeft):
                case nameof(_model.DisasterCountdown):
                    UpdateCountdownLabel();
                    Invalidate();
                    break;
                case nameof(_model.IsDisasterAnimating):
                case nameof(_model.CurrentDisasterName):
                    Invalidate();
                    break;
            }
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
            
            // Apply screen shake effect
            if (_screenShakeIntensity > 0)
            {
                var shakeX = (_animRandom.NextSingle() - 0.5f) * _screenShakeIntensity * 2;
                var shakeY = (_animRandom.NextSingle() - 0.5f) * _screenShakeIntensity * 2;
                g.TranslateTransform(shakeX, shakeY);
            }
            
            g.Clear(Color.FromArgb(15, 20, 30)); // Modern dark background
            
            // Enable anti-aliasing for smooth graphics
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var font = new Font("Segoe UI", 10, FontStyle.Bold);
            
            _renderer.DrawBoards(g, _model, font);
            
            // Draw ship palette below boards if in placement mode
            if (_model.State == AppState.Placement)
            {
                var boardHeight = Board.Size * _renderer.Cell;
                _paletteRenderer.DrawShipPalette(g, _model, font, ClientSize.Width, boardHeight);
            }
            
            // Draw particle effects
            DrawParticleEffects(g);
            
            // Apply screen flash effect
            if (_flashIntensity > 0)
            {
                using (var flashBrush = new SolidBrush(Color.FromArgb(_flashIntensity, 255, 255, 255)))
                {
                    g.FillRectangle(flashBrush, 0, 0, ClientSize.Width, ClientSize.Height);
                }
            }

            const int pad = 12;
            var statusText = _lblStatus?.Text ?? "";
            var countdownText = _lblCountdown?.Text ?? "";

            // Modern status box with rounded corners effect
            using (var statusFont = new Font("Segoe UI", 10, FontStyle.Bold))
            {
                var statusSize = g.MeasureString(statusText, statusFont);
                var statusRect = new RectangleF(_renderer.Margin - pad / 2f, 8 - pad / 2f, statusSize.Width + pad, statusSize.Height + pad);
                
                // Gradient background for status
                using (var bg = new SolidBrush(Color.FromArgb(180, 25, 35, 50)))
                {
                    g.FillRectangle(bg, statusRect);
                }
                // Border
                using (var border = new Pen(Color.FromArgb(100, 100, 150, 200), 1))
                {
                    g.DrawRectangle(border, Rectangle.Round(statusRect));
                }
                
                using (var textBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
                {
                    g.DrawString(statusText, statusFont, textBrush, statusRect.Left + pad / 2f, statusRect.Top + pad / 2f);
                }
            }

            // Modern countdown box
            if (!string.IsNullOrEmpty(countdownText))
            {
                using (var countdownFont = new Font("Segoe UI", 11, FontStyle.Bold))
                {
                    var countdownSize = g.MeasureString(countdownText, countdownFont);
                    var countdownWidth = countdownSize.Width + pad;
                    var centerX = (ClientSize.Width - countdownWidth) / 2f;
                    var countdownRect = new RectangleF(centerX, 8 - pad / 2f, countdownWidth, countdownSize.Height + pad);
                    
                    // Gradient background for countdown
                    using (var bg2 = new SolidBrush(Color.FromArgb(180, 50, 40, 10)))
                    {
                        g.FillRectangle(bg2, countdownRect);
                    }
                    // Border
                    using (var border = new Pen(Color.FromArgb(150, 255, 193, 7), 2))
                    {
                        g.DrawRectangle(border, Rectangle.Round(countdownRect));
                    }
                    
                    using (var textBrush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                    {
                        g.DrawString(countdownText, countdownFont, textBrush, countdownRect.Left + pad / 2f, countdownRect.Top + pad / 2f);
                    }
                }
            }

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
                    _sfx.PlayShipPlacedSound();

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
                            _model.CurrentStatus = "Placement submitted. Waiting for opponent...";
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
                        _model.CurrentStatus = $"Placement: place ships ({placedCount}/{totalCount})";
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
                        _model.CurrentStatus = "Move sent...";
                        await _controller.MakeMove(hitRight.Value.X, hitRight.Value.Y);
                        _model.YourFired.Add(hitRight.Value);
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
