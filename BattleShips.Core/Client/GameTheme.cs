using System.Drawing;

namespace BattleShips.Core
{
    public class GameTheme
    {
        public string Name { get; set; }
        public bool IsDarkTheme { get; set; }

        // Core Colors
        public Color BackgroundColor { get; set; }
        public Color PanelBackground { get; set; }
        public Color PrimaryColor { get; set; }
        public Color SecondaryColor { get; set; }

        // Grid Colors
        public Color GridLineColor { get; set; }
        public Color GridBorderColor { get; set; }
        public Color GridBackground { get; set; }

        // Ship Colors
        public Color ShipColor { get; set; }
        public Color ShipOutline { get; set; }
        public Color ShipHitColor { get; set; }
        public Color ShipMissColor { get; set; }
        public Color MineColor { get; set; }

        // Text Colors
        public Color TextColor { get; set; }
        public Color LabelColor { get; set; }

        // Button Colors (NEW - were missing)
        public Color ButtonColor { get; set; }
        public Color ButtonHover { get; set; }
        public Color ButtonText { get; set; }

        // Game State Colors (NEW - were missing)
        public Color HitColor { get; set; }
        public Color MissColor { get; set; }
        public Color SelectionColor { get; set; }

        public GameTheme(string name)
        {
            Name = name;
        }
    }
}