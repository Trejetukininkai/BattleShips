using System;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace BattleShips.Core
{
    public class ShipPaletteRenderer
    {
        public int Cell { get; }
        public int Margin { get; }
        
        public ShipPaletteRenderer(int cell = 40, int margin = 80)
        {
            Cell = cell;
            Margin = margin;
        }

        public Rectangle GetPaletteRect(int windowWidth, int boardHeight)
        {
            // Position palette below the boards with some spacing
            int yPos = Margin + boardHeight + 20;
            return new Rectangle(10, yPos, windowWidth - 20, 80);
        }

        [SupportedOSPlatform("windows")]
        public void DrawShipPalette(Graphics g, GameModel model, Font font, int windowWidth, int boardHeight)
        {
            var paletteRect = GetPaletteRect(windowWidth, boardHeight);
            
            // Background
            using (var bgBrush = new SolidBrush(Color.FromArgb(60, 70, 90)))
            {
                g.FillRectangle(bgBrush, paletteRect);
            }
            using (var borderPen = new Pen(Color.Gray, 2))
            {
                g.DrawRectangle(borderPen, paletteRect);
            }
            
            // Title
            using (var textBrush = new SolidBrush(Color.White))
            {
                g.DrawString("Ship Palette - Drag ships to place", font, textBrush, paletteRect.X + 10, paletteRect.Y + 5);
            }
            
            // Debug: Draw ship count
            using (var debugBrush = new SolidBrush(Color.Yellow))
            {
                g.DrawString($"Ships: {model.YourShips.Count}", font, debugBrush, paletteRect.X + 300, paletteRect.Y + 5);
            }
            
            // Draw ships - simplified approach
            int startX = paletteRect.X + 15;
            int startY = paletteRect.Y + 30;
            int currentX = startX;
            
            // Standard fleet sizes
            var fleetSizes = new[] { 5, 4, 3, 3, 2 };
            
            for (int i = 0; i < fleetSizes.Length; i++)
            {
                var shipLength = fleetSizes[i];
                var isPlaced = i < model.YourShips.Count && model.YourShips[i].IsPlaced;
                
                // Ship color
                var shipColor = isPlaced ? Color.FromArgb(100, 100, 100) : Color.FromArgb(100, 200, 100);
                
                // Draw ship cells
                using (var shipBrush = new SolidBrush(shipColor))
                using (var borderPen = new Pen(Color.DarkGreen, 2))
                {
                    for (int j = 0; j < shipLength; j++)
                    {
                        var cellRect = new Rectangle(currentX + (j * 30), startY, 28, 25);
                        g.FillRectangle(shipBrush, cellRect);
                        g.DrawRectangle(borderPen, cellRect);
                    }
                }
                
                // Ship label
                using (var labelBrush = new SolidBrush(Color.White))
                {
                    var labelText = $"{shipLength}";
                    var labelX = currentX + (shipLength * 30) / 2 - 5;
                    g.DrawString(labelText, font, labelBrush, labelX, startY + 30);
                }
                
                currentX += (shipLength * 30) + 20;
            }
        }

        public Ship? HitTestShip(Point mouse, GameModel model, int windowWidth, int boardHeight)
        {
            var paletteRect = GetPaletteRect(windowWidth, boardHeight);
            if (!paletteRect.Contains(mouse)) return null;
            
            int startX = paletteRect.X + 15;
            int startY = paletteRect.Y + 30;
            int currentX = startX;
            
            var fleetSizes = new[] { 5, 4, 3, 3, 2 };
            
            for (int i = 0; i < fleetSizes.Length; i++)
            {
                var shipLength = fleetSizes[i];
                var shipWidth = shipLength * 30;
                var shipRect = new Rectangle(currentX, startY, shipWidth, 25);
                
                if (shipRect.Contains(mouse))
                {
                    // Find the ship with matching length that isn't placed
                    var availableShip = model.YourShips.FirstOrDefault(s => s.Length == shipLength && !s.IsPlaced);
                    return availableShip;
                }
                
                currentX += shipWidth + 20;
            }
            
            return null;
        }
    }
}
