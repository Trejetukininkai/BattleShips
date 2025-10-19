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

            // Modern color scheme
            using var labelBrush = new SolidBrush(Color.FromArgb(200, 220, 240));
            using var thinBrush = new SolidBrush(Color.FromArgb(60, 80, 100));
            using var thickBrush = new SolidBrush(Color.FromArgb(100, 150, 200));
            using var baseBrush = new SolidBrush(Color.FromArgb(25, 35, 50));

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

            // draw your ships on left - modern green theme
            using (var shipBrush = new SolidBrush(Color.FromArgb(180, 46, 204, 113))) // Modern emerald green
            using (var shipBorderBrush = new SolidBrush(Color.FromArgb(200, 39, 174, 96))) // Darker emerald border
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

            // opponent shots on your board - modern styling
            foreach (var p in model.YourHitsByOpponent)
            {
                var x = left.X + p.X * Cell;
                var y = left.Y + p.Y * Cell;
                
                // Check if this hit was on a ship
                var wasShip = model.YourShips.Any(ship => ship.IsPlaced && ship.GetOccupiedCells().Contains(p));
                
                if (wasShip)
                {
                    // Modern hit marker - red with glow effect
                    using (var hitBrush = new SolidBrush(Color.FromArgb(220, 231, 76, 60)))
                    {
                        g.FillEllipse(hitBrush, x + 8, y + 8, Cell - 16, Cell - 16);
                    }
                    using (var hitBorder = new Pen(Color.FromArgb(255, 192, 57, 43), 2))
                    {
                        g.DrawEllipse(hitBorder, x + 8, y + 8, Cell - 16, Cell - 16);
                    }
                }
                else
                {
                    // Modern miss marker - blue-gray
                    using (var missBrush = new SolidBrush(Color.FromArgb(150, 108, 122, 137)))
                    {
                        g.FillEllipse(missBrush, x + 12, y + 12, Cell - 24, Cell - 24);
                    }
                    using (var missBorder = new Pen(Color.FromArgb(200, 90, 100, 110), 1))
                    {
                        g.DrawEllipse(missBorder, x + 12, y + 12, Cell - 24, Cell - 24);
                    }
                }
            }

            // your fired shots on opponent board - modern styling
            foreach (var p in model.YourFired)
            {
                var x = right.X + p.X * Cell;
                var y = right.Y + p.Y * Cell;
                var isHit = model.YourFiredHits.Contains(p);
                
                if (isHit)
                {
                    // Modern hit marker - orange-red
                    using (var hitBrush = new SolidBrush(Color.FromArgb(220, 230, 126, 34)))
                    {
                        g.FillEllipse(hitBrush, x + 8, y + 8, Cell - 16, Cell - 16);
                    }
                    using (var hitBorder = new Pen(Color.FromArgb(255, 211, 84, 0), 2))
                    {
                        g.DrawEllipse(hitBorder, x + 8, y + 8, Cell - 16, Cell - 16);
                    }
                }
                else
                {
                    // Modern miss marker - subtle gray
                    using (var missBrush = new SolidBrush(Color.FromArgb(120, 149, 165, 166)))
                    {
                        g.FillEllipse(missBrush, x + 12, y + 12, Cell - 24, Cell - 24);
                    }
                    using (var missBorder = new Pen(Color.FromArgb(180, 127, 140, 141), 1))
                    {
                        g.DrawEllipse(missBorder, x + 12, y + 12, Cell - 24, Cell - 24);
                    }
                }
            }

            // Enhanced animated disaster effects (draw on top of boards)
            foreach (var p in model.AnimatedCells)
            {
                if (p.X >= 0 && p.Y >= 0 && p.X < Board.Size && p.Y < Board.Size)
                {
                    var x = left.X + p.X * Cell;
                    var y = left.Y + p.Y * Cell;
                    var centerX = x + Cell / 2f;
                    var centerY = y + Cell / 2f;
                    
                    // Pulsing energy effect
                    var time = Environment.TickCount / 100.0f;
                    var pulse = (float)(0.5 + 0.5 * Math.Sin(time * 0.8));
                    var size = Cell * (0.3f + pulse * 0.4f);
                    
                    // Outer glow ring
                    using (var outerBrush = new SolidBrush(Color.FromArgb((int)(120 * pulse), 255, 100, 0)))
                    {
                        var outerSize = size * 1.8f;
                        g.FillEllipse(outerBrush, centerX - outerSize/2, centerY - outerSize/2, outerSize, outerSize);
                    }
                    
                    // Middle energy ring
                    using (var middleBrush = new SolidBrush(Color.FromArgb((int)(180 * pulse), 255, 200, 50)))
                    {
                        var middleSize = size * 1.3f;
                        g.FillEllipse(middleBrush, centerX - middleSize/2, centerY - middleSize/2, middleSize, middleSize);
                    }
                    
                    // Inner core
                    using (var coreBrush = new SolidBrush(Color.FromArgb((int)(220 + 35 * pulse), 255, 255, 200)))
                    {
                        g.FillEllipse(coreBrush, centerX - size/2, centerY - size/2, size, size);
                    }
                    
                    // Rotating energy spikes
                    using (var spikePen = new Pen(Color.FromArgb((int)(200 * pulse), 255, 150, 0), 3))
                    {
                        var spikeLength = Cell * 0.6f;
                        for (int i = 0; i < 8; i++)
                        {
                            var angle = time * 0.5f + i * Math.PI / 4;
                            var startX = centerX + (float)Math.Cos(angle) * (size * 0.4f);
                            var startY = centerY + (float)Math.Sin(angle) * (size * 0.4f);
                            var endX = centerX + (float)Math.Cos(angle) * spikeLength;
                            var endY = centerY + (float)Math.Sin(angle) * spikeLength;
                            
                            g.DrawLine(spikePen, startX, startY, endX, endY);
                        }
                    }
                    
                    // Electric arcs
                    using (var arcPen = new Pen(Color.FromArgb((int)(150 * pulse), 100, 200, 255), 2))
                    {
                        var random = new Random(p.X * 1000 + p.Y); // Consistent randomness per cell
                        for (int i = 0; i < 4; i++)
                        {
                            var angle1 = random.NextSingle() * (float)Math.PI * 2;
                            var angle2 = angle1 + (random.NextSingle() - 0.5f) * (float)Math.PI;
                            var radius = size * 0.3f;
                            
                            var x1 = centerX + (float)Math.Cos(angle1) * radius;
                            var y1 = centerY + (float)Math.Sin(angle1) * radius;
                            var x2 = centerX + (float)Math.Cos(angle2) * radius;
                            var y2 = centerY + (float)Math.Sin(angle2) * radius;
                            
                            // Jagged arc effect
                            var midX = (x1 + x2) / 2 + (random.NextSingle() - 0.5f) * 10;
                            var midY = (y1 + y2) / 2 + (random.NextSingle() - 0.5f) * 10;
                            
                            g.DrawLine(arcPen, x1, y1, midX, midY);
                            g.DrawLine(arcPen, midX, midY, x2, y2);
                        }
                    }
                }
            }

            // status & countdown
            // const int pad = 8;
            // var statusText = ""; // caller will draw actual text above/beside if desired
        }
    }
}