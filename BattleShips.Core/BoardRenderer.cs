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
            foreach (var p in model.YourShips)
            {
                var x = left.X + p.X * Cell;
                var y = left.Y + p.Y * Cell;
                g.FillEllipse(Brushes.LightGreen, x + 8, y + 8, Cell - 16, Cell - 16);
            }

            // opponent shots on your board
            foreach (var p in model.YourHitsByOpponent)
            {
                var x = left.X + p.X * Cell;
                var y = left.Y + p.Y * Cell;
                var wasShip = model.YourShips.Contains(p);
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

            // status & countdown
            // const int pad = 8;
            // var statusText = ""; // caller will draw actual text above/beside if desired
        }
    }
}