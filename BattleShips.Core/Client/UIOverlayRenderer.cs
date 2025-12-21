using System;
using System.Drawing;
using System.Runtime.Versioning;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Manages UI overlay rendering (status boxes, countdown displays, etc.).
    /// Part of the rendering subsystem managed by GameRenderingFacade.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class UIOverlayRenderer
    {
        private readonly int _margin;
        private readonly BoardRenderer _boardRenderer;
        private const int Padding = 12;

        public UIOverlayRenderer(BoardRenderer boardRenderer, int margin)
        {
            _boardRenderer = boardRenderer ?? throw new ArgumentNullException(nameof(boardRenderer));
            _margin = margin;
        }

        /// <summary>
        /// Draws the status overlay box at the top-left.
        /// </summary>
        public void DrawStatusOverlay(Graphics g, string statusText)
        {
            if (string.IsNullOrEmpty(statusText)) return;

            using (var statusFont = new Font("Segoe UI", 10, FontStyle.Bold))
            {
                var statusSize = g.MeasureString(statusText, statusFont);
                var statusRect = new RectangleF(
                    _margin - Padding / 2f,
                    8 - Padding / 2f,
                    statusSize.Width + Padding,
                    statusSize.Height + Padding
                );

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

                // Text
                using (var textBrush = new SolidBrush(Color.FromArgb(220, 220, 220)))
                {
                    g.DrawString(statusText, statusFont, textBrush, statusRect.Left + Padding / 2f, statusRect.Top + Padding / 2f);
                }
            }
        }

        /// <summary>
        /// Draws the countdown overlay box at the top-center.
        /// </summary>
        public void DrawCountdownOverlay(Graphics g, string countdownText, Size clientSize)
        {
            if (string.IsNullOrEmpty(countdownText)) return;

            using (var countdownFont = new Font("Segoe UI", 11, FontStyle.Bold))
            {
                var countdownSize = g.MeasureString(countdownText, countdownFont);
                var countdownWidth = countdownSize.Width + Padding;
                var centerX = (clientSize.Width - countdownWidth) / 2f;
                var countdownRect = new RectangleF(
                    centerX,
                    8 - Padding / 2f,
                    countdownWidth,
                    countdownSize.Height + Padding
                );

                // Gradient background for countdown
                using (var bg = new SolidBrush(Color.FromArgb(180, 50, 40, 10)))
                {
                    g.FillRectangle(bg, countdownRect);
                }

                // Border (warning color)
                using (var border = new Pen(Color.FromArgb(150, 255, 193, 7), 1))
                {
                    g.DrawRectangle(border, Rectangle.Round(countdownRect));
                }

                // Text (amber/warning color)
                using (var textBrush = new SolidBrush(Color.FromArgb(255, 193, 7)))
                {
                    g.DrawString(countdownText, countdownFont, textBrush, countdownRect.Left + Padding / 2f, countdownRect.Top + Padding / 2f);
                }
            }
        }

        /// <summary>
        /// Draws a game over overlay (full screen).
        /// </summary>
        public void DrawGameOverOverlay(Graphics g, Size clientSize, string message, bool victory)
        {
            // Semi-transparent overlay
            using (var overlay = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
            {
                g.FillRectangle(overlay, 0, 0, clientSize.Width, clientSize.Height);
            }

            // Title
            using (var titleFont = new Font("Segoe UI", 36, FontStyle.Bold))
            {
                var title = victory ? "🎉 VICTORY! 🎉" : "💥 DEFEAT 💥";
                var titleSize = g.MeasureString(title, titleFont);
                var titleColor = victory ? Color.FromArgb(76, 175, 80) : Color.FromArgb(244, 67, 54);

                using (var titleBrush = new SolidBrush(titleColor))
                {
                    var titleX = (clientSize.Width - titleSize.Width) / 2;
                    var titleY = clientSize.Height / 2 - 100;
                    g.DrawString(title, titleFont, titleBrush, titleX, titleY);
                }
            }

            // Message
            if (!string.IsNullOrEmpty(message))
            {
                using (var messageFont = new Font("Segoe UI", 16, FontStyle.Regular))
                using (var messageBrush = new SolidBrush(Color.White))
                {
                    var messageSize = g.MeasureString(message, messageFont);
                    var messageX = (clientSize.Width - messageSize.Width) / 2;
                    var messageY = clientSize.Height / 2;
                    g.DrawString(message, messageFont, messageBrush, messageX, messageY);
                }
            }
        }

        /// <summary>
        /// Draws a disaster warning overlay.
        /// </summary>
        public void DrawDisasterWarning(Graphics g, Size clientSize, string disasterName)
        {
            using (var warningFont = new Font("Segoe UI", 24, FontStyle.Bold))
            {
                var warningText = $"⚠️ {disasterName} ⚠️";
                var warningSize = g.MeasureString(warningText, warningFont);

                // Background
                var warningRect = new RectangleF(
                    (clientSize.Width - warningSize.Width - 40) / 2,
                    clientSize.Height - 100,
                    warningSize.Width + 40,
                    warningSize.Height + 20
                );

                using (var bg = new SolidBrush(Color.FromArgb(200, 50, 10, 10)))
                {
                    g.FillRectangle(bg, warningRect);
                }

                using (var border = new Pen(Color.FromArgb(255, 200, 0, 0), 2))
                {
                    g.DrawRectangle(border, Rectangle.Round(warningRect));
                }

                // Text
                using (var textBrush = new SolidBrush(Color.FromArgb(255, 220, 50)))
                {
                    g.DrawString(warningText, warningFont, textBrush, warningRect.Left + 20, warningRect.Top + 10);
                }
            }
        }
    }
}

