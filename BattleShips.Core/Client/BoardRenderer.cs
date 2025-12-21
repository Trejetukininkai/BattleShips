using BattleShips.Core.Client;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;

namespace BattleShips.Core
{
    public class BoardRenderer
    {
        public int Cell { get; }
        public int Margin { get; }
        public int TitleOffset { get; } = 40;

        // Theme properties
        public GameTheme Theme { get; private set; }

        // Derived properties for compatibility
        public Color BackgroundColor => Theme.BackgroundColor;
        public Color BoardBackgroundColor => Theme.PanelBackground;
        public Color GridLineColor => Theme.GridLineColor;
        public Color GridBorderColor => Theme.GridBorderColor;
        public Color LabelColor => Theme.LabelColor;
        public Color ShipColor => Theme.ShipColor;
        public Color MineColor => Theme.MineColor;

        private Rectangle undoButtonRect;
        private List<Rectangle> mineOptionRects = new();

        public BoardRenderer(int cell = 40, int margin = 80)
        {
            Cell = cell;
            Margin = margin;

            // Use Template Method pattern to create theme
            var builder = new DarkThemeBuilder();
            Theme = builder.BuildTheme("Default Dark Theme");

            InitializeMineOptions();
        }

        public BoardRenderer(int cell, int margin, string theme)
        {
            Cell = cell;
            Margin = margin;

            // Use Template Method pattern
            ThemeBuilder builder = theme.ToLower() switch
            {
                "light" => new LightThemeBuilder(),
                _ => new DarkThemeBuilder()
            };

            Theme = builder.BuildTheme($"{theme} Theme");
            Console.WriteLine($"[BoardRenderer] {theme} theme applied");

            InitializeMineOptions();
        }

        private void InitializeMineOptions()
        {
            int optionWidth = 80;
            int optionHeight = 40;
            int startX = Margin + Board.Size * Cell + 20;
            int startY = Margin;

            for (int i = 0; i < 4; i++)
            {
                mineOptionRects.Add(new Rectangle(startX, startY + i * (optionHeight + 10),
                    optionWidth, optionHeight));
            }
        }

        public Rectangle GetLeftBoardRect() => new Rectangle(Margin, Margin + TitleOffset,
            Board.Size * Cell, Board.Size * Cell);
        public Rectangle GetRightBoardRect() => new Rectangle(Margin * 2 + Board.Size * Cell,
            Margin + TitleOffset, Board.Size * Cell, Board.Size * Cell);

        public Point? HitTest(Point mouse, Rectangle boardRect)
        {
            if (!boardRect.Contains(mouse)) return null;
            var col = (mouse.X - boardRect.X) / Cell;
            var row = (mouse.Y - boardRect.Y) / Cell;
            if (col < 0 || row < 0 || col >= Board.Size || row >= Board.Size) return null;
            return new Point(col, row);
        }

        [SupportedOSPlatform("windows")]
        public void DrawBoards(Graphics g, GameModel model, Font font)
        {
            var left = GetLeftBoardRect();
            var right = GetRightBoardRect();

            // Draw base boards
            DrawBase(left, g);
            DrawBase(right, g);

            // Draw titles
            DrawBoardTitles(g, left, right, font);

            // Draw ships
            DrawShips(g, model, left);

            // Draw mines
            DrawMines(g, model, left);

            if (model.State == AppState.MineSelection)
            {
                DrawMineChooser(g, font, model);
                return;
            }

            DrawDraggedShipPreview(g, model, left);
            DrawOpponentShots(g, model, left);
            DrawFiredShots(g, model, right);
            DrawAnimatedDisasters(g, model, left);
            DrawUndoButton(g, font);
        }

        private void DrawBase(Rectangle origin, Graphics g)
        {
            using var labelBrush = new SolidBrush(LabelColor);
            using var thinBrush = new SolidBrush(GridLineColor);
            using var thickBrush = new SolidBrush(GridBorderColor);
            using var baseBrush = new SolidBrush(BoardBackgroundColor);

            // Labels
            for (int c = 0; c < Board.Size; c++)
            {
                var ch = (char)('A' + c);
                var x = origin.X + c * Cell + Cell / 2f;
                g.DrawString(ch.ToString(), SystemFonts.DefaultFont, labelBrush, x - 6, origin.Y - 24);
            }

            for (int r = 0; r < Board.Size; r++)
            {
                var y = origin.Y + r * Cell + Cell / 2f;
                g.DrawString((r + 1).ToString(), SystemFonts.DefaultFont, labelBrush, origin.X - 28, y - 8);
            }

            // Background
            g.FillRectangle(baseBrush, origin.X, origin.Y, origin.Width, origin.Height);

            // Thick border
            int thickness = 2;
            g.FillRectangle(thickBrush, origin.X, origin.Y, origin.Width, thickness);
            g.FillRectangle(thickBrush, origin.X, origin.Y + origin.Height - thickness, origin.Width, thickness);
            g.FillRectangle(thickBrush, origin.X, origin.Y, thickness, origin.Height);
            g.FillRectangle(thickBrush, origin.X + origin.Width - thickness, origin.Y, thickness, origin.Height);

            // Grid lines
            for (int i = 1; i < Board.Size; i++)
            {
                g.FillRectangle(thinBrush, origin.X + i * Cell, origin.Y, 1, origin.Height);
                g.FillRectangle(thinBrush, origin.X, origin.Y + i * Cell, origin.Width, 1);
            }
        }

        private void DrawBoardTitles(Graphics g, Rectangle left, Rectangle right, Font font)
        {
            using var titleFont = new Font(font.FontFamily, Math.Max(10, font.Size + 2), FontStyle.Bold);
            using var labelBrush = new SolidBrush(LabelColor);

            string leftTitle = "Your Board";
            string rightTitle = "Opponent";

            var leftSize = g.MeasureString(leftTitle, titleFont);
            var rightSize = g.MeasureString(rightTitle, titleFont);

            float titleY = left.Y - leftSize.Height - TitleOffset;
            float leftX = left.X + (left.Width - leftSize.Width) / 2f;
            float rightX = right.X + (right.Width - rightSize.Width) / 2f;

            using (var titleBg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                g.FillRectangle(titleBg, leftX - 6, titleY - 4, leftSize.Width + 12, leftSize.Height + 8);
                g.FillRectangle(titleBg, rightX - 6, titleY - 4, rightSize.Width + 12, rightSize.Height + 8);
            }

            g.DrawString(leftTitle, titleFont, labelBrush, leftX, titleY);
            g.DrawString(rightTitle, titleFont, labelBrush, rightX, titleY);
        }

        private void DrawMineChooser(Graphics g, Font font, GameModel model)
        {
            string[] mineNames = { "AntiEnemy_Restore", "AntiDisaster_Restore", "AntiEnemy_Ricochet", "AntiDisaster_Ricochet" };
            Color[] mineColors = { Color.Red, Color.Blue, Color.Green, Color.Orange };

            mineOptionRects.Clear();
            int width = 150, height = 50, spacing = 20;

            int startX = Margin * 2 + Board.Size * Cell + 20;
            int startY = Margin + TitleOffset - 30;

            for (int i = 0; i < mineNames.Length; i++)
            {
                var rect = new Rectangle(startX, startY + i * (height + spacing), width, height);
                mineOptionRects.Add(rect);

                using var brush = new SolidBrush(mineColors[i]);
                g.FillRectangle(brush, rect);
                g.DrawRectangle(Pens.White, rect);
                g.DrawString(mineNames[i], font, Brushes.White, rect.X + 10, rect.Y + 10);
            }

            g.DrawString("Choose a mine category:", font, Brushes.White, startX, startY - 30);
        }

        private void DrawShips(Graphics g, GameModel model, Rectangle boardRect)
        {
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            foreach (var ship in model.YourShips.Where(s => s.IsPlaced))
            {
                visitor.VisitShip(ship, model, boardRect);
            }
        }

        private void DrawDraggedShipPreview(Graphics g, GameModel model, Rectangle boardRect)
        {
            var previewShip = model.DraggedShip;
            if (previewShip == null || previewShip.Position.X < 0 || previewShip.Position.Y < 0)
                return;

            bool valid = model.CanPlaceShip(previewShip, previewShip.Position);
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            visitor.VisitDraggedShip(previewShip, valid, model, boardRect);
        }

        private void DrawOpponentShots(Graphics g, GameModel model, Rectangle boardRect)
        {
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            foreach (var p in model.YourHitsByOpponent)
            {
                var wasShip = model.YourShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(p));
                visitor.VisitOpponentHit(p, wasShip, model, boardRect);
            }
        }

        private void DrawFiredShots(Graphics g, GameModel model, Rectangle boardRect)
        {
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            foreach (var p in model.YourFired)
            {
                var isHit = model.YourFiredHits.Contains(p);
                visitor.VisitFiredShot(p, isHit, model, boardRect);
            }
        }

        private void DrawAnimatedDisasters(Graphics g, GameModel model, Rectangle boardRect)
        {
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            foreach (var p in model.AnimatedCells)
            {
                visitor.VisitAnimatedCell(p, model, boardRect);
            }
        }

        private void DrawMines(Graphics g, GameModel model, Rectangle boardRect)
        {
            var visitor = new RenderingVisitor(g, Cell, ShipColor, MineColor);
            foreach (var mine in model.YourMines)
            {
                visitor.VisitMine(mine, model, boardRect);
            }
        }

        private void DrawUndoButton(Graphics g, Font font)
        {
            int buttonWidth = 100;
            int buttonHeight = 30;
            undoButtonRect = new Rectangle(450, 50, buttonWidth, buttonHeight);

            using var buttonBrush = new SolidBrush(Color.FromArgb(60, 80, 100));
            using var borderPen = new Pen(Color.FromArgb(100, 150, 200), 2);
            using var textBrush = new SolidBrush(Color.White);
            using var buttonFont = new Font(font.FontFamily, 10, FontStyle.Bold);

            g.FillRectangle(buttonBrush, undoButtonRect);
            g.DrawRectangle(borderPen, undoButtonRect);

            var textSize = g.MeasureString("Undo", buttonFont);
            g.DrawString("Undo", buttonFont, textBrush,
                undoButtonRect.X + (buttonWidth - textSize.Width) / 2,
                undoButtonRect.Y + (buttonHeight - textSize.Height) / 2);
        }

        // Getters for UI interaction
        public Rectangle UndoButtonRect => undoButtonRect;
        public IReadOnlyList<Rectangle> MineOptionRects => mineOptionRects;

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
    }
}