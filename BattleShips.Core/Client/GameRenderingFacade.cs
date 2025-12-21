using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Versioning;

namespace BattleShips.Core.Client
{
    [SupportedOSPlatform("windows")]
    public class GameRenderingFacade
    {
        private readonly BoardRenderer _boardRenderer;
        private readonly ShipPaletteRenderer _paletteRenderer;
        private readonly ScreenEffectsManager _screenEffects;
        private readonly ParticleEffectsManager _particleEffects;
        private readonly UIOverlayRenderer _uiOverlay;

        public GameRenderingFacade(BoardRenderer boardRenderer, int cellSize = 40, int margin = 80)
        {
            _boardRenderer = boardRenderer ?? throw new ArgumentNullException(nameof(boardRenderer));
            _paletteRenderer = new ShipPaletteRenderer(cellSize, margin);
            _screenEffects = new ScreenEffectsManager();
            _particleEffects = new ParticleEffectsManager();
            _uiOverlay = new UIOverlayRenderer(boardRenderer, margin);
        }

        /// <summary>
        /// FACADE MAIN METHOD: Renders the entire game frame.
        /// Coordinates all rendering subsystems in the correct order.
        /// </summary>
        public void RenderFrame(Graphics g, GameModel model, Size clientSize, string statusText, string countdownText)
        {
            // 1. Apply pre-render effects (screen shake)
            _screenEffects.ApplyScreenShake(g);

            // 2. Clear and setup graphics
            g.Clear(_boardRenderer.BackgroundColor);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            using var font = new Font("Segoe UI", 10, FontStyle.Bold);

            // 3. Render game boards
            _boardRenderer.DrawBoards(g, model, font);

            // 4. Render ship palette (if in placement mode)
            if (model.State == AppState.Placement)
            {
                var boardHeight = Board.Size * _boardRenderer.Cell;
                _paletteRenderer.DrawShipPalette(g, model, font, clientSize.Width, boardHeight);
            }

            // 5. Render particle effects
            _particleEffects.DrawParticles(g);

            // 6. Apply post-render effects (screen flash)
            _screenEffects.ApplyScreenFlash(g, clientSize);

            // 7. Render UI overlays
            _uiOverlay.DrawStatusOverlay(g, statusText);
            _uiOverlay.DrawCountdownOverlay(g, countdownText, clientSize);
        }

        /// <summary>
        /// Triggers screen shake effect with specified intensity.
        /// </summary>
        public void TriggerScreenShake(float intensity = 8f)
        {
            _screenEffects.TriggerShake(intensity);
        }

        /// <summary>
        /// Triggers screen flash effect with specified intensity.
        /// </summary>
        public void TriggerScreenFlash(int intensity = 255)
        {
            _screenEffects.TriggerFlash(intensity);
        }

        /// <summary>
        /// Creates explosion particles at the specified board cell.
        /// </summary>
        public void CreateExplosionAt(Point cell, Rectangle boardRect, int cellSize)
        {
            _particleEffects.CreateExplosion(cell, boardRect, cellSize);
        }

        /// <summary>
        /// Creates disaster-related particles (for multiple cells).
        /// </summary>
        public void CreateDisasterParticles(List<Point> cells, Rectangle boardRect, int cellSize)
        {
            _particleEffects.CreateDisasterBurst(cells, boardRect, cellSize);
        }

        /// <summary>
        /// Updates all active effects (particles, shake, flash).
        /// Should be called on each animation frame (e.g., timer tick).
        /// </summary>
        public void UpdateEffects()
        {
            _particleEffects.Update();
            _screenEffects.Update();
        }

        /// <summary>
        /// Clears all active particles and resets effects.
        /// </summary>
        public void ClearAllEffects()
        {
            _particleEffects.Clear();
            _screenEffects.Reset();
        }

        /// <summary>
        /// Gets the ship palette's hit test result for mouse interaction.
        /// </summary>
        public IShip? HitTestShipPalette(Point mouse, GameModel model, int windowWidth, int boardHeight)
        {
            return _paletteRenderer.HitTestShip(mouse, model, windowWidth, boardHeight);
        }

        /// <summary>
        /// Gets the board renderer's cell hit test for mouse interaction.
        /// </summary>
        public Point? HitTestBoard(Point mouse, Rectangle boardRect)
        {
            return _boardRenderer.HitTest(mouse, boardRect);
        }

        /// <summary>
        /// Gets the left board rectangle.
        /// </summary>
        public Rectangle GetLeftBoardRect() => _boardRenderer.GetLeftBoardRect();

        /// <summary>
        /// Gets the right board rectangle.
        /// </summary>
        public Rectangle GetRightBoardRect() => _boardRenderer.GetRightBoardRect();

        /// <summary>
        /// Gets the background color from the renderer (for Form background).
        /// </summary>
        public Color BackgroundColor => _boardRenderer.BackgroundColor;

        /// <summary>
        /// Gets the cell size from the renderer.
        /// </summary>
        public int CellSize => _boardRenderer.Cell;

        /// <summary>
        /// Gets the margin from the renderer.
        /// </summary>
        public int Margin => _boardRenderer.Margin;
    }
}

