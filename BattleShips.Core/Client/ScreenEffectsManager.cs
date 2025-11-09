using System;
using System.Drawing;
using System.Runtime.Versioning;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Manages screen-level visual effects like shake and flash.
    /// Part of the rendering subsystem managed by GameRenderingFacade.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ScreenEffectsManager
    {
        private static readonly Random _random = new Random();
        private float _shakeIntensity = 0f;
        private int _flashIntensity = 0;

        /// <summary>
        /// Triggers a screen shake effect with specified intensity.
        /// </summary>
        public void TriggerShake(float intensity)
        {
            _shakeIntensity = Math.Max(_shakeIntensity, intensity);
        }

        /// <summary>
        /// Triggers a screen flash effect with specified intensity (0-255).
        /// </summary>
        public void TriggerFlash(int intensity)
        {
            _flashIntensity = Math.Max(_flashIntensity, Math.Clamp(intensity, 0, 255));
        }

        /// <summary>
        /// Applies screen shake transformation to the graphics context.
        /// Call this before drawing the frame.
        /// </summary>
        public void ApplyScreenShake(Graphics g)
        {
            if (_shakeIntensity > 0)
            {
                var shakeX = ((float)_random.NextDouble() - 0.5f) * _shakeIntensity * 2;
                var shakeY = ((float)_random.NextDouble() - 0.5f) * _shakeIntensity * 2;
                g.TranslateTransform(shakeX, shakeY);
            }
        }

        /// <summary>
        /// Applies screen flash effect by drawing a semi-transparent overlay.
        /// Call this after drawing the main content.
        /// </summary>
        public void ApplyScreenFlash(Graphics g, Size screenSize)
        {
            if (_flashIntensity > 0)
            {
                using (var flashBrush = new SolidBrush(Color.FromArgb(_flashIntensity, 255, 255, 255)))
                {
                    g.FillRectangle(flashBrush, 0, 0, screenSize.Width, screenSize.Height);
                }
            }
        }

        /// <summary>
        /// Updates effect intensities (fading over time).
        /// Call this on each animation frame.
        /// </summary>
        public void Update()
        {
            // Fade screen shake
            _shakeIntensity *= 0.9f;
            if (_shakeIntensity < 0.1f) _shakeIntensity = 0f;

            // Fade flash
            _flashIntensity = (int)(_flashIntensity * 0.95f);
            if (_flashIntensity < 5) _flashIntensity = 0;
        }

        /// <summary>
        /// Resets all effects to zero.
        /// </summary>
        public void Reset()
        {
            _shakeIntensity = 0f;
            _flashIntensity = 0;
        }

        /// <summary>
        /// Gets the current shake intensity.
        /// </summary>
        public float ShakeIntensity => _shakeIntensity;

        /// <summary>
        /// Gets the current flash intensity.
        /// </summary>
        public int FlashIntensity => _flashIntensity;
    }
}

