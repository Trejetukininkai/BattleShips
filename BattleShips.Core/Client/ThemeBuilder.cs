using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BattleShips.Core.BoardRenderer;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Abstract Template Method class for building themes
    /// </summary>
    public abstract class ThemeBuilder
    {
        // The TEMPLATE METHOD - defines the algorithm
        public GameTheme BuildTheme(string name)
        {
            var theme = new GameTheme(name);

            // Fixed sequence of steps
            SetCoreColors(theme);
            SetGridColors(theme);
            SetShipColors(theme);
            SetTextColors(theme);
            SetButtonColors(theme);
            SetGameStateColors(theme);

            ValidateTheme(theme);

            return theme;
        }

        // Abstract operations - implemented by subclasses
        protected abstract void SetCoreColors(GameTheme theme);
        protected abstract void SetGridColors(GameTheme theme);
        protected abstract void SetShipColors(GameTheme theme);
        protected abstract void SetTextColors(GameTheme theme);
        protected abstract void SetButtonColors(GameTheme theme);
        protected abstract void SetGameStateColors(GameTheme theme);

        // Hook method - can be overridden
        protected virtual void ValidateTheme(GameTheme theme)
        {
            // Default validation
            if (theme.PrimaryColor == Color.Empty)
                theme.PrimaryColor = Color.Black;
        }

        // Helper method
        protected static Color CreateColor(int r, int g, int b, int a = 255)
            => Color.FromArgb(a, r, g, b);
    }
}
