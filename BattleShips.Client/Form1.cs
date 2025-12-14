using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using BattleShips.Core;
using BattleShips.Core.Client;
using static BattleShips.Core.BoardRenderer;

namespace BattleShips.Client
{
    public partial class Form1 : Form
    {
        private readonly GameClientController _controller;
        private readonly GameModel _model = new GameModel();
        private readonly SFXService _sfx;
        private readonly BoardRenderer _renderer; 
        private readonly ShipPaletteRenderer _paletteRenderer = new ShipPaletteRenderer(cell: 40, margin: 80);
        private readonly BoardRendererDirector _rendererDirector = new BoardRendererDirector();
        private bool _awaitingMove = false;

        private Panel? _startupPanel;
        private Button? _btnConnectLocal;
        private Button? _btnReconnect;
        private TextBox? _txtPlayerName;
        private TextBox? _txtServerUrl;
        private Label? _lblStatus;
        private Label? _lblCountdown;
        private System.Windows.Forms.Timer? _uiTimer;
        private System.Threading.CancellationTokenSource? _disasterCts;

        public List<Rectangle> mineOptionRects = new List<Rectangle>();

        private List<Rectangle> _powerUpButtonRects = new List<Rectangle>();

        // Console interpreter fields
        private CommandInterpreter? _consoleInterpreter;
        private Panel? _consolePanel;
        private TextBox? _consoleOutput;
        private TextBox? _consoleInput;
        private Button? _toggleConsoleButton;
        private bool _consoleVisible = false;



        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;

            // Use builder to create random theme BoardRenderer
            _renderer = _rendererDirector.ConstructRandomTheme();

            // Set form background color based on theme
            BackColor = _renderer.BackgroundColor;
            ForeColor = Color.White;

            var totalWidth = 2 * _renderer.Margin + Board.Size * _renderer.Cell * 2;
            var boardHeight = Board.Size * _renderer.Cell;
            var totalHeight = 2 * _renderer.Margin + boardHeight + 150;
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

            // Initialize console interpreter
            InitializeConsole();

            WireControllerEvents();

            _controller.MineTriggered += OnMineTriggered;
            _controller.CellsHealed += OnCellsHealed;

            // Subscribe to model property changes for automatic UI updates
            _model.PropertyChanged += OnModelPropertyChanged;

            _controller.MeteorStrike += OnMeteorStrike;
        }

        private void OnMeteorStrike(List<Point> strikePoints)
        {
            Console.WriteLine($"[Form1] 🌋 Meteor Strike with {strikePoints.Count} impact points");

            // Add to animated cells for visualization
            foreach (var point in strikePoints)
            {
                _model.AnimatedCells.Add(point);
            }

            Invalidate();
        }


        private void OnMineTriggered(Guid mineId, List<Point> effectPoints, string category)
        {
            Console.WriteLine($"[Form1] Mine {mineId} triggered with {effectPoints.Count} effect points");

            // Show mine explosion animation
            foreach (var point in effectPoints)
            {
                _model.AnimatedCells.Add(point);
            }

            Invalidate();
        }

        private void OnCellsHealed(List<Point> healedCells)
        {
            Console.WriteLine($"[Form1] 🩹 Cells healed: {string.Join(", ", healedCells.Select(p => $"({p.X},{p.Y})"))}");

            // Remove these cells from hit sets
            foreach (var cell in healedCells)
            {
                _model.YourHitsByOpponent.Remove(cell);
            }

            Invalidate();
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

            // Input fields setup
            var inputWidth = 280;
            var inputHeight = 35;
            var inputSpacing = 15;
            var inputsStartY = startY + 100;

            // Player Name Label
            var lblPlayerName = new Label
            {
                Text = "Player Name:",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lblPlayerName.Location = new Point(centerX - inputWidth / 2, inputsStartY);

            // Player Name TextBox
            _txtPlayerName = new TextBox
            {
                Size = new Size(inputWidth, inputHeight),
                Location = new Point(centerX - inputWidth / 2, inputsStartY + 25),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Player1" // Default name
            };

            // Server URL Label
            var lblServerUrl = new Label
            {
                Text = "Server URL:",
                ForeColor = Color.FromArgb(200, 200, 200),
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                BackColor = Color.Transparent
            };
            lblServerUrl.Location = new Point(centerX - inputWidth / 2, inputsStartY + 70);

            // Server URL TextBox
            _txtServerUrl = new TextBox
            {
                Size = new Size(inputWidth, inputHeight),
                Location = new Point(centerX - inputWidth / 2, inputsStartY + 95),
                Font = new Font("Segoe UI", 11),
                BackColor = Color.FromArgb(45, 50, 60),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "http://localhost:5000" // Default URL
            };

            // Buttons setup
            var buttonWidth = 280;
            var buttonHeight = 50;
            var buttonSpacing = 15;
            var buttonsStartY = inputsStartY + 145;

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

            // Reconnect button
            _btnReconnect = new Button
            {
                Text = "🔄 Reconnect to Game",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219), // Modern blue
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY + buttonHeight + buttonSpacing),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnReconnect.FlatAppearance.BorderSize = 0;
            _btnReconnect.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);

            // Settings button
            var btnSettings = new Button
            {
                Text = "⚙️ Settings",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 73, 94), // Modern gray
                FlatStyle = FlatStyle.Flat,
                Size = new Size(buttonWidth, buttonHeight),
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY + 2 * (buttonHeight + buttonSpacing)),
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
                Location = new Point(centerX - buttonWidth / 2, buttonsStartY + 3 * (buttonHeight + buttonSpacing)),
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
            _lblStatus.Location = new Point(centerX - statusSize.Width / 2, buttonsStartY + 4 * (buttonHeight + buttonSpacing) + 20);

            _lblCountdown = new Label
            {
                Text = "",
                ForeColor = Color.FromArgb(255, 193, 7), // Modern amber
                AutoSize = true,
                Visible = true,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            _lblCountdown.Location = new Point(centerX - 50, buttonsStartY + 4 * (buttonHeight + buttonSpacing) + 45);

            // Event handlers
            _btnConnectLocal.Click += async (_, __) =>
            {
                _btnConnectLocal.Enabled = false;
                _btnReconnect!.Enabled = false;
                _model.CurrentStatus = "🔄 Connecting to server...";

                var playerName = _txtPlayerName?.Text ?? "Player1";
                var serverUrl = _txtServerUrl?.Text ?? "http://localhost:5000";

                try
                {
                    await _controller.ConnectAsync(serverUrl);

                    // Set player name after connecting
                    await _controller.Client.SetPlayerName(playerName);

                    ResetBoards();
                    _model.State = AppState.Waiting;
                    _startupPanel!.Visible = false;
                    Text = $"Connected to BattleShips server as {playerName}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to connect: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _model.CurrentStatus = "❌ Connection failed - Try again";
                    _btnConnectLocal.Enabled = true;
                    _btnReconnect!.Enabled = true;
                }
            };

            _btnReconnect.Click += async (_, __) =>
            {
                _btnConnectLocal!.Enabled = false;
                _btnReconnect.Enabled = false;
                _model.CurrentStatus = "🔄 Reconnecting to saved game...";

                var playerName = _txtPlayerName?.Text ?? "Player1";
                var serverUrl = _txtServerUrl?.Text ?? "http://localhost:5000";

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    MessageBox.Show("Please enter your player name to reconnect.", "Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _btnConnectLocal!.Enabled = true;
                    _btnReconnect.Enabled = true;
                    return;
                }

                try
                {
                    await _controller.ConnectAsync(serverUrl);

                    // Attempt to reconnect to saved game
                    await _controller.Client.ReconnectToGame(playerName);

                    // The reconnection response will be handled by the GameStateRestored event
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to reconnect: {ex.Message}", "Reconnection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _model.CurrentStatus = "❌ Reconnection failed - Try again";
                    _btnConnectLocal!.Enabled = true;
                    _btnReconnect.Enabled = true;
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
            _startupPanel.Controls.Add(lblPlayerName);
            _startupPanel.Controls.Add(_txtPlayerName);
            _startupPanel.Controls.Add(lblServerUrl);
            _startupPanel.Controls.Add(_txtServerUrl);
            _startupPanel.Controls.Add(_btnConnectLocal);
            _startupPanel.Controls.Add(_btnReconnect);
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

        private void UpdateUIForStateChange()
        {
            // Hide startup panel when state changes from Menu
            if (_model.State != AppState.Menu && _startupPanel != null && _startupPanel.Visible)
            {
                _startupPanel.Visible = false;
                Text = "BattleShips - Connected";
            }

            // Show startup panel when returning to menu
            if (_model.State == AppState.Menu && _startupPanel != null && !_startupPanel.Visible)
            {
                _startupPanel.Visible = true;
                Text = "BattleShips";
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
                        if (_lblStatus.InvokeRequired)
                        {
                            _lblStatus.Invoke(() =>
                            {
                                _lblStatus.Text = _model.CurrentStatus;
                                Invalidate();
                            });
                        }
                        else
                        {
                            _lblStatus.Text = _model.CurrentStatus;
                            Invalidate();
                        }
                    }
                    break;

                case nameof(_model.State):
                    if (InvokeRequired)
                    {
                        Invoke(() =>
                        {
                            UpdateCountdownLabel();
                            UpdateUIForStateChange();
                            Invalidate();
                        });
                    }
                    else
                    {
                        UpdateCountdownLabel();
                        UpdateUIForStateChange();
                        Invalidate();
                    }
                    break;

                case nameof(_model.IsMyTurn):
                case nameof(_model.PlacementSecondsLeft):
                case nameof(_model.DisasterCountdown):
                    if (InvokeRequired)
                    {
                        Invoke(() => { UpdateCountdownLabel(); Invalidate(); });
                    }
                    else
                    {
                        UpdateCountdownLabel();
                        Invalidate();
                    }
                    break;

                case nameof(_model.IsDisasterAnimating):
                case nameof(_model.CurrentDisasterName):
                    if (InvokeRequired)
                        Invoke(Invalidate);
                    else
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

            g.Clear(_renderer.BackgroundColor);

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

            using var powerUpFont = new Font("Segoe UI", 10, FontStyle.Bold);
            DrawPowerUpUI(g, powerUpFont);
        }

        private void OnMouseDown(object? sender, MouseEventArgs e)
        {
            if (_model.State != AppState.Placement) return;

            // --- Undo button click detection ---
            if (_renderer.UndoButtonRect.Contains(e.Location))
            {
                _model.UndoLastShipPlacement();
                Invalidate(); // Redraw board after undo
                return; // Stop other click handling
            }

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
            var mouse = new Point(e.X, e.Y);

            if (_model.State == AppState.MineSelection)
            {
                // Check if user clicked a mine option
                var selectedCategory = _renderer.HitTestMineOption(mouse);
                if (selectedCategory.HasValue)
                {
                    _model.SelectedMineCategory = selectedCategory.Value;
                    _model.CurrentStatus = $"Selected {selectedCategory.Value}. Click your board to place it.";
                    return; 
                }

                // Check if user clicked the left board with a mine category selected
                var leftCell = _renderer.HitTest(mouse, _renderer.GetLeftBoardRect());
                if (leftCell.HasValue && _model.SelectedMineCategory.HasValue)
                {
                    // Check if this cell already has a mine
                    if (_model.YourMines.Any(m => m.Position == leftCell.Value))
                    {
                        _model.CurrentStatus = "Cell already has a mine! Choose another location.";
                        _model.SelectedMineCategory = null; // ✅ Reset selection
                        return;
                    }

                    // Check if cell has a ship
                    if (_model.YourShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(leftCell.Value)))
                    {
                        _model.CurrentStatus = "Cannot place mine on a ship! Choose another location.";
                        _model.SelectedMineCategory = null; // ✅ Reset selection
                        return;
                    }

                    // Place the mine on the model
                    _model.PlaceMine(leftCell.Value);
                    _model.CurrentStatus = $"Mine placed at {GetCellName(leftCell.Value)}. " +
                                          $"Placed {_model.YourMines.Count}/3 mines.";

                    // Auto-complete after placing 3 mines
                    if (_model.YourMines.Count >= 3)
                    {
                        _model.CurrentStatus = "All mines placed. Sending to server...";

                        if (_controller.IsConnected)
                        {
                            try
                            {
                                // Convert to simple types for SignalR
                                var minePositions = _model.YourMines.Select(m => m.Position).ToList();
                                var mineCategories = _model.YourMines.Select(m => m.Category.ToString()).ToList();

                                Console.WriteLine($"[Client] 📢 Sending {minePositions.Count} mines to server...");
                                foreach (var pos in minePositions)
                                {
                                    Console.WriteLine($"[Client] Mine at ({pos.X},{pos.Y})");
                                }

                                await _controller.PlaceMines(minePositions, mineCategories);
                                Console.WriteLine($"[Client] ✅ Mines sent to server successfully");
                                _model.CurrentStatus = "Mines submitted. Waiting for opponent...";
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Client] ❌ Failed to send mines: {ex.Message}");
                                MessageBox.Show($"Failed to send mines: {ex.Message}", "Error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                                _model.CurrentStatus = "Failed to send mines. Try again.";
                            }
                        }
                    }
                    else
                    {
                        // Reset selection for next mine placement
                        _model.SelectedMineCategory = null;
                        _model.CurrentStatus = "Choose another mine type to place.";
                    }

                    Invalidate(); // Refresh the display
                    return;
                }
            }

            for (int i = 0; i < _powerUpButtonRects.Count; i++)
            {
                if (_powerUpButtonRects[i].Contains(e.Location))
                {
                    var powerUp = _model.AvailablePowerUps[i];
                    if (_model.CanActivatePowerUp(powerUp))
                    {
                        _model.ActivatePowerUp(powerUp, _controller);
                    }
                    return;
                }
            }


            var rightRect = _renderer.GetRightBoardRect();
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

        private string GetCellName(Point cell)
        {
            var colChar = (char)('A' + cell.X);
            return $"{colChar}{cell.Y + 1}";
        }

        private async void DebugGameState()
        {
            if (_controller.IsConnected)
            {
                try
                {
                    await _controller.DebugGameState();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Debug failed: {ex.Message}");
                }
            }
        }

        private async void TestSignalRConnection()
        {
            if (_controller.IsConnected)
            {
                try
                {
                    Console.WriteLine($"[Client] Testing SignalR connection...");
                    await _controller.TestConnection("Hello from client");
                    Console.WriteLine($"[Client] SignalR test sent");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Client] SignalR test failed: {ex.Message}");
                }
            }
        }

        // Call it from a button or key press
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F12) // Press F12 to debug
            {
                DebugGameState();
            }
            base.OnKeyDown(e);
        }


        private void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // Toggle console with ~ key (tilde/grave accent)
            if (e.KeyCode == Keys.Oemtilde)
            {
                ToggleConsole();
                e.Handled = true;
                return;
            }

            if (_model.DraggedShip != null && (e.KeyCode == Keys.R || e.KeyCode == Keys.Space))
            {
                _model.DraggedShip.Rotate();
                Invalidate();
                e.Handled = true;
            }
        }

        public MineCategory? HitTestMineOption(Point mouse)
        {
            for (int i = 0; i < mineOptionRects.Count; i++)
            {
                if (mineOptionRects[i].Contains(mouse))
                {
                    return i switch
                    {
                        0 => MineCategory.AntiEnemy_Restore,
                        1 => MineCategory.AntiDisaster_Restore,
                        2 => MineCategory.AntiEnemy_Ricochet,
                        3 => MineCategory.AntiDisaster_Ricochet,
                        _ => null
                    };
                }
            }
            return null;
        }

        /*private void InitializePowerUpUI()
        {
            _powerUpButtonRects.Clear();
            int buttonWidth = 120;
            int buttonHeight = 40;
            int startX = _renderer.Margin;
            int startY = 100; // Position below status

            foreach (var powerUp in _model.AvailablePowerUps)
            {
                _powerUpButtonRects.Add(new Rectangle(startX, startY, buttonWidth, buttonHeight));
                startY += buttonHeight + 5;
            }
        }*/

        private void DrawPowerUpUI(Graphics g, Font font)
        {
            if (_powerUpButtonRects.Count != _model.AvailablePowerUps.Count)
            {
                InitializePowerUpButtons();
            }

            // Draw AP counter centered between boards
            using var apFont = new Font(font.FontFamily, 14, FontStyle.Bold);
            using var apBrush = new SolidBrush(Color.Gold);

            int centerX = _renderer.Margin + Board.Size * _renderer.Cell + (_renderer.Margin / 2);
            int uiStartY = _renderer.Margin + 30;

            var apTextSize = g.MeasureString(_model.ActionPointsText, apFont);
            g.DrawString(_model.ActionPointsText, apFont, apBrush, centerX - apTextSize.Width / 2, uiStartY);

            // Use smaller font for buttons
            using var buttonFont = new Font(font.FontFamily, 8, FontStyle.Regular);

            // Draw powerup buttons
            for (int i = 0; i < _model.AvailablePowerUps.Count; i++)
            {
                if (i >= _powerUpButtonRects.Count) continue;

                var powerUp = _model.AvailablePowerUps[i];
                var rect = _powerUpButtonRects[i];

                bool canActivate = _model.CanActivatePowerUp(powerUp);
                var buttonColor = canActivate ? Color.FromArgb(70, 170, 70) : Color.FromArgb(100, 100, 100);
                var textColor = canActivate ? Color.White : Color.LightGray;

                using var buttonBrush = new SolidBrush(buttonColor);
                using var textBrush = new SolidBrush(textColor);

                g.FillRectangle(buttonBrush, rect);

                if (canActivate)
                    g.DrawRectangle(Pens.Lime, rect);
                else
                    g.DrawRectangle(Pens.Gray, rect);

                var shortText = GetShortPowerUpText(powerUp.Name, powerUp.Cost);
                var textSize = g.MeasureString(shortText, buttonFont);
                g.DrawString(shortText, buttonFont, textBrush,
                    rect.X + (rect.Width - textSize.Width) / 2,
                    rect.Y + (rect.Height - textSize.Height) / 2);
            }
        }

        private string GetShortPowerUpText(string powerUpName, int cost)
        {
            return powerUpName switch
            {
                "InitiateDisaster" => $"Disaster\n{cost}",
                "MiniNuke" => $"Nuke\n{cost}",
                "Repair" => $"Repair\n{cost}",
                _ => $"{powerUpName}\n{cost}"
            };
        }

        private void InitializePowerUpButtons()
        {
            _powerUpButtonRects.Clear();

            // Much smaller buttons
            int buttonWidth = 60;  // Reduced width
            int buttonHeight = 30; // Reduced height
            int centerX = _renderer.Margin + Board.Size * _renderer.Cell + (_renderer.Margin / 2);
            int startY = _renderer.Margin + 60; // Closer to AP counter
            int spacing = 6; // Tighter spacing

            for (int i = 0; i < _model.AvailablePowerUps.Count; i++)
            {
                var rect = new Rectangle(
                    centerX - buttonWidth / 2,
                    startY + i * (buttonHeight + spacing),
                    buttonWidth,
                    buttonHeight
                );
                _powerUpButtonRects.Add(rect);
            }
        }

        // ------------------------------
        //  Console Interpreter
        // ------------------------------
        private void InitializeConsole()
        {
            // Create interpreter
            var context = new CommandContext(_model, _controller.Client);
            _consoleInterpreter = new CommandInterpreter(context);

            // Create toggle button (top-right corner)
            _toggleConsoleButton = new Button
            {
                Text = "Console (~)",
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(50, 50, 50),
                ForeColor = Color.Lime,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Consolas", 9F, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _toggleConsoleButton.Click += (s, e) => ToggleConsole();
            Controls.Add(_toggleConsoleButton);

            // Position button on resize
            Resize += (s, e) =>
            {
                if (_toggleConsoleButton != null)
                {
                    _toggleConsoleButton.Location = new Point(ClientSize.Width - 110, 10);
                }
                if (_consolePanel != null)
                {
                    _consolePanel.Location = new Point(10, ClientSize.Height - 310);
                }
            };

            // Create console panel (initially hidden)
            _consolePanel = new Panel
            {
                Size = new Size(600, 300),
                Location = new Point(10, ClientSize.Height - 310),
                BackColor = Color.FromArgb(20, 20, 20),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            // Console output (read-only multiline textbox)
            _consoleOutput = new TextBox
            {
                Size = new Size(580, 240),
                Location = new Point(10, 10),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9F),
                Text = "=== BattleShips Console Ready ===\r\nType 'help' for available commands\r\nPress ~ to toggle console\r\n"
            };
            _consolePanel.Controls.Add(_consoleOutput);

            // Console input
            _consoleInput = new TextBox
            {
                Size = new Size(580, 25),
                Location = new Point(10, 260),
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = new Font("Consolas", 10F)
            };
            _consoleInput.KeyDown += ConsoleInput_KeyDown;
            _consolePanel.Controls.Add(_consoleInput);

            Controls.Add(_consolePanel);

            // Position button initially
            _toggleConsoleButton.Location = new Point(ClientSize.Width - 110, 10);
        }

        private void ToggleConsole()
        {
            _consoleVisible = !_consoleVisible;
            if (_consolePanel != null)
            {
                _consolePanel.Visible = _consoleVisible;
                _consolePanel.BringToFront();
                if (_consoleVisible && _consoleInput != null)
                {
                    _consoleInput.Focus();
                }
            }
        }

        private void ConsoleInput_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && _consoleInput != null && _consoleInterpreter != null)
            {
                e.SuppressKeyPress = true; // Prevent beep sound

                string command = _consoleInput.Text.Trim();
                if (string.IsNullOrEmpty(command))
                    return;

                // Execute command
                string result = _consoleInterpreter.Execute(command);

                // Display in output
                if (_consoleOutput != null)
                {
                    _consoleOutput.AppendText($"\r\n> {command}\r\n");
                    // Convert \n to \r\n for proper Windows TextBox display
                    string formattedResult = result.Replace("\n", "\r\n");
                    _consoleOutput.AppendText(formattedResult + "\r\n");

                    // Auto-scroll to bottom
                    _consoleOutput.SelectionStart = _consoleOutput.Text.Length;
                    _consoleOutput.ScrollToCaret();
                }

                // Clear input
                _consoleInput.Clear();

                // Refresh game display
                Invalidate();
            }
            else if (e.KeyCode == Keys.Up && _consoleInterpreter != null)
            {
                // History navigation
                var previousCmd = _consoleInterpreter.GetPreviousCommand();
                if (previousCmd != null && _consoleInput != null)
                {
                    _consoleInput.Text = previousCmd;
                    _consoleInput.SelectionStart = _consoleInput.Text.Length;
                }
            }
            else if (e.KeyCode == Keys.Down && _consoleInterpreter != null)
            {
                var nextCmd = _consoleInterpreter.GetNextCommand();
                if (_consoleInput != null)
                {
                    _consoleInput.Text = nextCmd ?? "";
                    _consoleInput.SelectionStart = _consoleInput.Text.Length;
                }
            }
            else if (e.KeyCode == Keys.Escape)
            {
                // Close console on Escape
                ToggleConsole();
            }
        }
    }
}
