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
            // Position palette below the boards with more spacing
            int yPos = Margin + boardHeight + 50; // Increased from 20 to 50
            return new Rectangle(10, yPos, windowWidth - 20, 80);
        }

        [SupportedOSPlatform("windows")]
        public void DrawShipPalette(Graphics g, GameModel model, Font font, int windowWidth, int boardHeight)
        {
            var paletteRect = GetPaletteRect(windowWidth, boardHeight);
            
            // Modern gradient-like background
            using (var bgBrush = new SolidBrush(Color.FromArgb(30, 40, 55)))
            {
                g.FillRectangle(bgBrush, paletteRect);
            }
            
            // Modern border with glow effect
            using (var borderPen = new Pen(Color.FromArgb(100, 150, 200), 2))
            {
                g.DrawRectangle(borderPen, paletteRect);
            }
            
            // Inner glow effect
            var innerRect = new Rectangle(paletteRect.X + 1, paletteRect.Y + 1, paletteRect.Width - 2, paletteRect.Height - 2);
            using (var innerBorderPen = new Pen(Color.FromArgb(50, 200, 220, 240), 1))
            {
                g.DrawRectangle(innerBorderPen, innerRect);
            }
            
            // Modern title with better typography
            using (var titleFont = new Font("Segoe UI", 11, FontStyle.Bold))
            using (var textBrush = new SolidBrush(Color.FromArgb(220, 230, 240)))
            {
                g.DrawString("🚢 Ship Fleet - Drag to Deploy", titleFont, textBrush, paletteRect.X + 15, paletteRect.Y + 8);
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
                
                // Modern ship colors with gradients
                var shipColor = isPlaced ? 
                    Color.FromArgb(120, 70, 70, 70) :     // Dimmed gray for placed
                    Color.FromArgb(180, 46, 204, 113);    // Bright emerald for available
                
                var borderColor = isPlaced ? 
                    Color.FromArgb(150, 50, 50, 50) :     // Dark gray border for placed
                    Color.FromArgb(220, 39, 174, 96);     // Bright emerald border for available
                
                // Draw ship cells with modern styling
                using (var shipBrush = new SolidBrush(shipColor))
                using (var borderPen = new Pen(borderColor, 2))
                {
                    for (int j = 0; j < shipLength; j++)
                    {
                        var cellRect = new Rectangle(currentX + (j * 30), startY, 28, 25);
                        g.FillRectangle(shipBrush, cellRect);
                        g.DrawRectangle(borderPen, cellRect);
                        
                        // Add subtle inner highlight for depth
                        if (!isPlaced)
                        {
                            var highlightRect = new Rectangle(cellRect.X + 2, cellRect.Y + 2, cellRect.Width - 4, 2);
                            using (var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255)))
                            {
                                g.FillRectangle(highlightBrush, highlightRect);
                            }
                        }
                    }
                }
                
                // Modern ship label with better typography
                using (var labelFont = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(isPlaced ? Color.FromArgb(120, 120, 120) : Color.FromArgb(220, 230, 240)))
                {
                    var labelText = $"×{shipLength}";
                    var labelSize = g.MeasureString(labelText, labelFont);
                    var labelX = currentX + (shipLength * 30) / 2 - labelSize.Width / 2;
                    g.DrawString(labelText, labelFont, labelBrush, labelX, startY + 30);
                }
                
                currentX += (shipLength * 30) + 25; // Increased spacing
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
