using System;
using System.Drawing;
using System.Runtime.Versioning;
using BattleShips.Core; // for Board.Size
using System.Windows.Forms;

namespace BattleShips.Core
{
    public class BoardRenderer
    {
        public int Cell { get; }
        public int Margin { get; }
        public int TitleOffset { get; }
        public BoardRenderer(int cell = 40, int margin = 80)
        {
            Cell = cell;
            Margin = margin;
            TitleOffset = (int)(Margin / 2);
        }

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
        [SupportedOSPlatform("windows")] // to supress warnings
        public void DrawBoards(Graphics g, GameModel model, Font font)
        {
            var w = Board.Size * Cell;
            var h = Board.Size * Cell;

            // brushes/pens (use brushes instead of Pen for cross-platform compatibility)
            using var labelBrush = new SolidBrush(Color.White);
            using var thinBrush = new SolidBrush(Color.Gray);
            using var thickBrush = new SolidBrush(Color.White);
            using var baseBrush = new SolidBrush(Color.FromArgb(30, 70, 120));

            var left = GetLeftBoardRect();
            var right = GetRightBoardRect();

            void DrawBase(Rectangle origin)
            {
                // labels
                for (int c = 0; c < Board.Size; c++)
                {
                    var ch = (char)('A' + c);
                    var x = origin.X + c * Cell + Cell / 2f;
                    g.DrawString(ch.ToString(), font, labelBrush, x - 6, origin.Y - 24);
                }

                for (int r = 0; r < Board.Size; r++)
                {
                    var y = origin.Y + r * Cell + Cell / 2f;
                    g.DrawString((r + 1).ToString(), font, labelBrush, origin.X - 28, y - 8);
                }

                // background
                g.FillRectangle(baseBrush, origin.X, origin.Y, origin.Width, origin.Height);

                // draw thick border by filling rectangles (thickness = 2)
                int thickness = 2;
                g.FillRectangle(thickBrush, origin.X, origin.Y, origin.Width, thickness); // top
                g.FillRectangle(thickBrush, origin.X, origin.Y + origin.Height - thickness, origin.Width, thickness); // bottom
                g.FillRectangle(thickBrush, origin.X, origin.Y, thickness, origin.Height); // left
                g.FillRectangle(thickBrush, origin.X + origin.Width - thickness, origin.Y, thickness, origin.Height); // right

                // grid lines (1px) using thin brush
                for (int i = 1; i < Board.Size; i++)
                {
                    g.FillRectangle(thinBrush, origin.X + i * Cell, origin.Y, 1, origin.Height); // vertical
                    g.FillRectangle(thinBrush, origin.X, origin.Y + i * Cell, origin.Width, 1); // horizontal
                }
            }

            // draw both boards
            DrawBase(left);
            DrawBase(right);

            // draw titles above each board
            using var titleFont = new Font(font.FontFamily, Math.Max(10, font.Size + 2), FontStyle.Bold);
            var leftTitle = "Your Board";
            var rightTitle = "Opponent";
            var leftSize = g.MeasureString(leftTitle, titleFont);
            var rightSize = g.MeasureString(rightTitle, titleFont);
            var titleY = left.Y - leftSize.Height - TitleOffset;
            var leftTitleX = left.X + (left.Width - leftSize.Width) / 2f;
            var rightTitleX = right.X + (right.Width - rightSize.Width) / 2f;
            // subtle background for titles for readability
            using (var titleBg = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                g.FillRectangle(titleBg, leftTitleX - 6, titleY - 4, leftSize.Width + 12, leftSize.Height + 8);
                g.FillRectangle(titleBg, rightTitleX - 6, titleY - 4, rightSize.Width + 12, rightSize.Height + 8);
            }
            g.DrawString(leftTitle, titleFont, labelBrush, leftTitleX, titleY);
            g.DrawString(rightTitle, titleFont, labelBrush, rightTitleX, titleY);

            // draw your ships on left
            using (var shipBrush = new SolidBrush(Color.FromArgb(180, 144, 238, 144))) // Light green with transparency
            using (var shipBorderBrush = new SolidBrush(Color.FromArgb(120, 34, 139, 34))) // Dark green for borders
            {
                foreach (var ship in model.YourShips.Where(s => s.IsPlaced))
                {
                    var cells = ship.GetOccupiedCells();
                    foreach (var cell in cells)
                    {
                        var x = left.X + cell.X * Cell;
                        var y = left.Y + cell.Y * Cell;
                        
                        // Draw ship rectangle with border
                        g.FillRectangle(shipBrush, x + 4, y + 4, Cell - 8, Cell - 8);
                        g.FillRectangle(shipBorderBrush, x + 4, y + 4, Cell - 8, 2); // top border
                        g.FillRectangle(shipBorderBrush, x + 4, y + Cell - 6, Cell - 8, 2); // bottom border
                        g.FillRectangle(shipBorderBrush, x + 4, y + 4, 2, Cell - 8); // left border
                        g.FillRectangle(shipBorderBrush, x + Cell - 6, y + 4, 2, Cell - 8); // right border
                    }
                }
            }

            // Draw dragged ship preview
            if (model.DraggedShip != null && model.DraggedShip.Position.X >= 0 && model.DraggedShip.Position.Y >= 0)
            {
                var previewShip = model.DraggedShip;
                var isValidPlacement = model.CanPlaceShip(previewShip, previewShip.Position);
                
                // Choose color based on validity
                var previewColor = isValidPlacement ? 
                    Color.FromArgb(120, 144, 238, 144) : // Semi-transparent green for valid
                    Color.FromArgb(120, 255, 100, 100);   // Semi-transparent red for invalid
                
                using (var previewBrush = new SolidBrush(previewColor))
                using (var previewBorderBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255))) // White border
                {
                    var cells = previewShip.GetOccupiedCells();
                    foreach (var cell in cells)
                    {
                        // Only draw if within board bounds
                        if (cell.X >= 0 && cell.X < Board.Size && cell.Y >= 0 && cell.Y < Board.Size)
                        {
                            var x = left.X + cell.X * Cell;
                            var y = left.Y + cell.Y * Cell;
                            
                            // Draw preview ship rectangle with border
                            g.FillRectangle(previewBrush, x + 2, y + 2, Cell - 4, Cell - 4);
                            g.FillRectangle(previewBorderBrush, x + 2, y + 2, Cell - 4, 2); // top border
                            g.FillRectangle(previewBorderBrush, x + 2, y + Cell - 4, Cell - 4, 2); // bottom border
                            g.FillRectangle(previewBorderBrush, x + 2, y + 2, 2, Cell - 4); // left border
                            g.FillRectangle(previewBorderBrush, x + Cell - 4, y + 2, 2, Cell - 4); // right border
                        }
                    }
                }
            }

            // opponent shots on your board
            foreach (var p in model.YourHitsByOpponent)
            {
                var x = left.X + p.X * Cell;
                var y = left.Y + p.Y * Cell;
                
                // Check if this hit was on a ship
                var wasShip = model.YourShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(p));
                g.FillEllipse(wasShip ? Brushes.Red : Brushes.Gray, x + 10, y + 10, Cell - 20, Cell - 20);
            }

            // your fired shots on opponent board
            foreach (var p in model.YourFired)
            {
                var x = right.X + p.X * Cell;
                var y = right.Y + p.Y * Cell;
                var isHit = model.YourFiredHits.Contains(p);
                g.FillEllipse(isHit ? Brushes.Red : Brushes.Gray, x + 10, y + 10, Cell - 20, Cell - 20);
            }

            // animated impact overlay (draw on top of boards) - semi-transparent yellow ring
            using (var animBrush = new SolidBrush(Color.FromArgb(180, 255, 200, 0)))
            using (var animPen = new Pen(Color.FromArgb(220, 255, 140, 0), 3))
            {
                foreach (var p in model.AnimatedCells)
                {
                    // determine whether cell is on left or right board (we animate impacts on both boards if present)
                    var lx = left.X + p.X * Cell;
                    var ly = left.Y + p.Y * Cell;
                    var rx = right.X + p.X * Cell;
                    var ry = right.Y + p.Y * Cell;

                    // draw small filled circle + ring on left board
                    if (p.X >= 0 && p.Y >= 0 && p.X < Board.Size && p.Y < Board.Size)
                    {
                        // we draw on left board by default (disaster is applied to the board coordinates)
                        g.FillEllipse(animBrush, lx + 6, ly + 6, Cell - 12, Cell - 12);
                        g.DrawEllipse(animPen, lx + 6, ly + 6, Cell - 12, Cell - 12);
                    }
                }
            }

            // status & countdown
            // const int pad = 8;
            // var statusText = ""; // caller will draw actual text above/beside if desired
        }
    }
}