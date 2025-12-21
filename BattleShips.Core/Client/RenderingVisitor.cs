using System;
using System.Drawing;
using System.Linq;
using System.Runtime.Versioning;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// VISITOR PATTERN: Concrete visitor that renders game elements.
    /// Encapsulates all rendering logic for different game element types.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class RenderingVisitor : IGameElementVisitor
    {
        private readonly Graphics _graphics;
        private readonly int _cellSize;
        private readonly Color _shipColor;
        private readonly Color _mineColor;

        public RenderingVisitor(Graphics graphics, int cellSize, Color shipColor, Color mineColor)
        {
            _graphics = graphics ?? throw new ArgumentNullException(nameof(graphics));
            _cellSize = cellSize;
            _shipColor = shipColor;
            _mineColor = mineColor;
        }

        /// <summary>
        /// VISITOR PATTERN: Renders a ship element
        /// </summary>
        public void VisitShip(IShip ship, GameModel model, Rectangle boardRect)
        {
            using var shipBrush = new SolidBrush(_shipColor);
            using var shipBorderBrush = new SolidBrush(Color.FromArgb(200, 39, 174, 96));

            foreach (var cell in ship.GetOccupiedCells())
            {
                var x = boardRect.X + cell.X * _cellSize;
                var y = boardRect.Y + cell.Y * _cellSize;
                _graphics.FillRectangle(shipBrush, x + 4, y + 4, _cellSize - 8, _cellSize - 8);
                _graphics.FillRectangle(shipBorderBrush, x + 4, y + 4, _cellSize - 8, 2);
                _graphics.FillRectangle(shipBorderBrush, x + 4, y + _cellSize - 6, _cellSize - 8, 2);
                _graphics.FillRectangle(shipBorderBrush, x + 4, y + 4, 2, _cellSize - 8);
                _graphics.FillRectangle(shipBorderBrush, x + _cellSize - 6, y + 4, 2, _cellSize - 8);
            }
        }

        /// <summary>
        /// VISITOR PATTERN: Renders a mine element
        /// </summary>
        public void VisitMine(NavalMine mine, GameModel model, Rectangle boardRect)
        {
            using var brush = new SolidBrush(_mineColor);
            var x = boardRect.X + mine.Position.X * _cellSize;
            var y = boardRect.Y + mine.Position.Y * _cellSize;
            _graphics.FillEllipse(brush, x + 10, y + 10, _cellSize - 20, _cellSize - 20);
        }

        /// <summary>
        /// VISITOR PATTERN: Renders an opponent's hit on your board
        /// </summary>
        public void VisitOpponentHit(Point hitCell, bool isShipHit, GameModel model, Rectangle boardRect)
        {
            var x = boardRect.X + hitCell.X * _cellSize;
            var y = boardRect.Y + hitCell.Y * _cellSize;

            if (isShipHit)
            {
                using var hitBrush = new SolidBrush(Color.FromArgb(220, 231, 76, 60));
                _graphics.FillEllipse(hitBrush, x + 8, y + 8, _cellSize - 16, _cellSize - 16);
                using var hitBorder = new Pen(Color.FromArgb(255, 192, 57, 43), 2);
                _graphics.DrawEllipse(hitBorder, x + 8, y + 8, _cellSize - 16, _cellSize - 16);
            }
            else
            {
                using var missBrush = new SolidBrush(Color.FromArgb(150, 108, 122, 137));
                _graphics.FillEllipse(missBrush, x + 12, y + 12, _cellSize - 24, _cellSize - 24);
                using var missBorder = new Pen(Color.FromArgb(200, 90, 100, 110), 1);
                _graphics.DrawEllipse(missBorder, x + 12, y + 12, _cellSize - 24, _cellSize - 24);
            }
        }

        /// <summary>
        /// VISITOR PATTERN: Renders your fired shot on opponent's board
        /// </summary>
        public void VisitFiredShot(Point shotCell, bool isHit, GameModel model, Rectangle boardRect)
        {
            var x = boardRect.X + shotCell.X * _cellSize;
            var y = boardRect.Y + shotCell.Y * _cellSize;

            if (isHit)
            {
                using var hitBrush = new SolidBrush(Color.FromArgb(220, 230, 126, 34));
                _graphics.FillEllipse(hitBrush, x + 8, y + 8, _cellSize - 16, _cellSize - 16);
                using var hitBorder = new Pen(Color.FromArgb(255, 211, 84, 0), 2);
                _graphics.DrawEllipse(hitBorder, x + 8, y + 8, _cellSize - 16, _cellSize - 16);
            }
            else
            {
                using var missBrush = new SolidBrush(Color.FromArgb(120, 149, 165, 166));
                _graphics.FillEllipse(missBrush, x + 12, y + 12, _cellSize - 24, _cellSize - 24);
                using var missBorder = new Pen(Color.FromArgb(180, 127, 140, 141), 1);
                _graphics.DrawEllipse(missBorder, x + 12, y + 12, _cellSize - 24, _cellSize - 24);
            }
        }

        /// <summary>
        /// VISITOR PATTERN: Renders an animated disaster cell
        /// </summary>
        public void VisitAnimatedCell(Point cell, GameModel model, Rectangle boardRect)
        {
            if (cell.X < 0 || cell.X >= Board.Size || cell.Y < 0 || cell.Y >= Board.Size) return;

            var x = boardRect.X + cell.X * _cellSize;
            var y = boardRect.Y + cell.Y * _cellSize;
            var centerX = x + _cellSize / 2f;
            var centerY = y + _cellSize / 2f;

            var time = Environment.TickCount / 100.0f;
            var pulse = (float)(0.5 + 0.5 * Math.Sin(time * 0.8));
            var size = _cellSize * (0.3f + pulse * 0.4f);

            using (var outerBrush = new SolidBrush(Color.FromArgb((int)(120 * pulse), 255, 100, 0)))
            {
                float outerSize = size * 1.8f;
                _graphics.FillEllipse(outerBrush, centerX - outerSize / 2, centerY - outerSize / 2, outerSize, outerSize);
            }
        }

        /// <summary>
        /// VISITOR PATTERN: Renders a dragged ship preview
        /// </summary>
        public void VisitDraggedShip(IShip ship, bool isValid, GameModel model, Rectangle boardRect)
        {
            var color = isValid ? Color.FromArgb(120, 144, 238, 144) : Color.FromArgb(120, 255, 100, 100);
            using var brush = new SolidBrush(color);
            using var borderBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));

            foreach (var cell in ship.GetOccupiedCells())
            {
                if (cell.X < 0 || cell.X >= Board.Size || cell.Y < 0 || cell.Y >= Board.Size) continue;
                var x = boardRect.X + cell.X * _cellSize;
                var y = boardRect.Y + cell.Y * _cellSize;
                _graphics.FillRectangle(brush, x + 2, y + 2, _cellSize - 4, _cellSize - 4);
                _graphics.FillRectangle(borderBrush, x + 2, y + 2, _cellSize - 4, 2);
                _graphics.FillRectangle(borderBrush, x + 2, y + _cellSize - 4, _cellSize - 4, 2);
                _graphics.FillRectangle(borderBrush, x + 2, y + 2, 2, _cellSize - 4);
                _graphics.FillRectangle(borderBrush, x + _cellSize - 4, y + 2, 2, _cellSize - 4);
            }
        }
    }
}

