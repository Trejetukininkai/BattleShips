using BattleShips.Core;
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
        public int TitleOffset { get; } = 40; // set default


        public BoardRenderer(int cell = 40, int margin = 80)
        {
            Cell = cell;
            Margin = margin;

            // Example: 4 mine options, 60x40 pixels each, spaced vertically
            int optionWidth = 80;
            int optionHeight = 40;
            int startX = Margin + Board.Size * Cell + 20; // right of left board
            int startY = Margin;

            for (int i = 0; i < 4; i++)
            {
                mineOptionRects.Add(new Rectangle(startX, startY + i * (optionHeight + 10), optionWidth, optionHeight));
            }
        }

        private Rectangle undoButtonRect;
        public Rectangle UndoButtonRect => undoButtonRect;

        public Rectangle GetLeftBoardRect() => new Rectangle(Margin, Margin + TitleOffset, Board.Size * Cell, Board.Size * Cell);
        public Rectangle GetRightBoardRect() => new Rectangle(Margin * 2 + Board.Size * Cell, Margin + TitleOffset, Board.Size * Cell, Board.Size * Cell);

        public Point? HitTest(Point mouse, Rectangle boardRect)
        {
            if (!boardRect.Contains(mouse)) return null;
            var col = (mouse.X - boardRect.X) / Cell;
            var row = (mouse.Y - boardRect.Y) / Cell;
            if (col < 0 || row < 0 || col >= Board.Size || row >= Board.Size) return null;
            return new Point(col, row);
        }

        // Rectangles for mine chooser options
        private List<Rectangle> mineOptionRects = new();

        [SupportedOSPlatform("windows")]
        public void DrawBoards(Graphics g, GameModel model, Font font)
        {
            var left = GetLeftBoardRect();
            var right = GetRightBoardRect();

            // --- Draw base boards ---
            DrawBase(left, g);
            DrawBase(right, g);

            // Draw titles
            DrawBoardTitles(g, left, right, font);

            // --- Mine selection overlay ---
            if (model.State == AppState.MineSelection)
            {
                DrawMineChooser(g, font, model);
                return; // skip other game elements until selection done
            }

            // Draw ships
            DrawShips(g, model, left);

            // Draw dragged ship preview
            DrawDraggedShipPreview(g, model, left);

            // Opponent hits on your board
            DrawOpponentShots(g, model, left);

            // Your shots on opponent board
            DrawFiredShots(g, model, right);

            // Animated disaster effects
            DrawAnimatedDisasters(g, model, left);

            // Draw mines
            DrawMines(g, model, left);

            // Undo button
            DrawUndoButton(g, font);
        }

        private void DrawBase(Rectangle origin, Graphics g)
        {
            using var labelBrush = new SolidBrush(Color.FromArgb(200, 220, 240));
            using var thinBrush = new SolidBrush(Color.FromArgb(60, 80, 100));
            using var thickBrush = new SolidBrush(Color.FromArgb(100, 150, 200));
            using var baseBrush = new SolidBrush(Color.FromArgb(25, 35, 50));

            // labels
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

            // background
            g.FillRectangle(baseBrush, origin.X, origin.Y, origin.Width, origin.Height);

            // thick border
            int thickness = 2;
            g.FillRectangle(thickBrush, origin.X, origin.Y, origin.Width, thickness); // top
            g.FillRectangle(thickBrush, origin.X, origin.Y + origin.Height - thickness, origin.Width, thickness); // bottom
            g.FillRectangle(thickBrush, origin.X, origin.Y, thickness, origin.Height); // left
            g.FillRectangle(thickBrush, origin.X + origin.Width - thickness, origin.Y, thickness, origin.Height); // right

            // grid lines
            for (int i = 1; i < Board.Size; i++)
            {
                g.FillRectangle(thinBrush, origin.X + i * Cell, origin.Y, 1, origin.Height);
                g.FillRectangle(thinBrush, origin.X, origin.Y + i * Cell, origin.Width, 1);
            }
        }

        private void DrawBoardTitles(Graphics g, Rectangle left, Rectangle right, Font font)
        {
            using var titleFont = new Font(font.FontFamily, Math.Max(10, font.Size + 2), FontStyle.Bold);
            using var labelBrush = new SolidBrush(Color.FromArgb(200, 220, 240));

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

            int startX = Margin * 2 + Board.Size * Cell + 20; // Right of left board
            int startY = Margin + TitleOffset - 30; // Above the boards

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
            using var shipBrush = new SolidBrush(Color.FromArgb(180, 46, 204, 113));
            using var shipBorderBrush = new SolidBrush(Color.FromArgb(200, 39, 174, 96));

            foreach (var ship in model.YourShips.Where(s => s.IsPlaced))
            {
                foreach (var cell in ship.GetOccupiedCells())
                {
                    var x = boardRect.X + cell.X * Cell;
                    var y = boardRect.Y + cell.Y * Cell;
                    g.FillRectangle(shipBrush, x + 4, y + 4, Cell - 8, Cell - 8);
                    g.FillRectangle(shipBorderBrush, x + 4, y + 4, Cell - 8, 2);
                    g.FillRectangle(shipBorderBrush, x + 4, y + Cell - 6, Cell - 8, 2);
                    g.FillRectangle(shipBorderBrush, x + 4, y + 4, 2, Cell - 8);
                    g.FillRectangle(shipBorderBrush, x + Cell - 6, y + 4, 2, Cell - 8);
                }
            }
        }

        private void DrawDraggedShipPreview(Graphics g, GameModel model, Rectangle boardRect)
        {
            var previewShip = model.DraggedShip;
            if (previewShip == null) return;
            if (previewShip.Position.X < 0 || previewShip.Position.Y < 0) return;

            bool valid = model.CanPlaceShip(previewShip, previewShip.Position);
            var color = valid ? Color.FromArgb(120, 144, 238, 144) : Color.FromArgb(120, 255, 100, 100);

            using var brush = new SolidBrush(color);
            using var borderBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));

            foreach (var cell in previewShip.GetOccupiedCells())
            {
                if (cell.X < 0 || cell.X >= Board.Size || cell.Y < 0 || cell.Y >= Board.Size) continue;
                var x = boardRect.X + cell.X * Cell;
                var y = boardRect.Y + cell.Y * Cell;
                g.FillRectangle(brush, x + 2, y + 2, Cell - 4, Cell - 4);
                g.FillRectangle(borderBrush, x + 2, y + 2, Cell - 4, 2);
                g.FillRectangle(borderBrush, x + 2, y + Cell - 4, Cell - 4, 2);
                g.FillRectangle(borderBrush, x + 2, y + 2, 2, Cell - 4);
                g.FillRectangle(borderBrush, x + Cell - 4, y + 2, 2, Cell - 4);
            }
        }

        private void DrawOpponentShots(Graphics g, GameModel model, Rectangle boardRect)
        {
            foreach (var p in model.YourHitsByOpponent)
            {
                var x = boardRect.X + p.X * Cell;
                var y = boardRect.Y + p.Y * Cell;
                var wasShip = model.YourShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(p));

                if (wasShip)
                {
                    using var hitBrush = new SolidBrush(Color.FromArgb(220, 231, 76, 60));
                    g.FillEllipse(hitBrush, x + 8, y + 8, Cell - 16, Cell - 16);
                    using var hitBorder = new Pen(Color.FromArgb(255, 192, 57, 43), 2);
                    g.DrawEllipse(hitBorder, x + 8, y + 8, Cell - 16, Cell - 16);
                }
                else
                {
                    using var missBrush = new SolidBrush(Color.FromArgb(150, 108, 122, 137));
                    g.FillEllipse(missBrush, x + 12, y + 12, Cell - 24, Cell - 24);
                    using var missBorder = new Pen(Color.FromArgb(200, 90, 100, 110), 1);
                    g.DrawEllipse(missBorder, x + 12, y + 12, Cell - 24, Cell - 24);
                }
            }
        }

        private void DrawFiredShots(Graphics g, GameModel model, Rectangle boardRect)
        {
            foreach (var p in model.YourFired)
            {
                var x = boardRect.X + p.X * Cell;
                var y = boardRect.Y + p.Y * Cell;
                var isHit = model.YourFiredHits.Contains(p);

                if (isHit)
                {
                    using var hitBrush = new SolidBrush(Color.FromArgb(220, 230, 126, 34));
                    g.FillEllipse(hitBrush, x + 8, y + 8, Cell - 16, Cell - 16);
                    using var hitBorder = new Pen(Color.FromArgb(255, 211, 84, 0), 2);
                    g.DrawEllipse(hitBorder, x + 8, y + 8, Cell - 16, Cell - 16);
                }
                else
                {
                    using var missBrush = new SolidBrush(Color.FromArgb(120, 149, 165, 166));
                    g.FillEllipse(missBrush, x + 12, y + 12, Cell - 24, Cell - 24);
                    using var missBorder = new Pen(Color.FromArgb(180, 127, 140, 141), 1);
                    g.DrawEllipse(missBorder, x + 12, y + 12, Cell - 24, Cell - 24);
                }
            }
        }

        private void DrawAnimatedDisasters(Graphics g, GameModel model, Rectangle boardRect)
        {
            foreach (var p in model.AnimatedCells)
            {
                if (p.X < 0 || p.X >= Board.Size || p.Y < 0 || p.Y >= Board.Size) continue;
                var x = boardRect.X + p.X * Cell;
                var y = boardRect.Y + p.Y * Cell;
                var centerX = x + Cell / 2f;
                var centerY = y + Cell / 2f;

                var time = Environment.TickCount / 100.0f;
                var pulse = (float)(0.5 + 0.5 * Math.Sin(time * 0.8));
                var size = Cell * (0.3f + pulse * 0.4f);

                using (var outerBrush = new SolidBrush(Color.FromArgb((int)(120 * pulse), 255, 100, 0)))
                {
                    float outerSize = size * 1.8f;
                    g.FillEllipse(outerBrush, centerX - outerSize / 2, centerY - outerSize / 2, outerSize, outerSize);
                }
            }
        }

        private void DrawMines(Graphics g, GameModel model, Rectangle boardRect)
        {
            foreach (var mine in model.YourMines)
            {
                var x = boardRect.X + mine.Position.X * Cell;
                var y = boardRect.Y + mine.Position.Y * Cell;
                using var brush = new SolidBrush(Color.FromArgb(180, 200, 200, 0));
                g.FillEllipse(brush, x + 10, y + 10, Cell - 20, Cell - 20);
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

        // Expose mine chooser rectangles for click handling
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
