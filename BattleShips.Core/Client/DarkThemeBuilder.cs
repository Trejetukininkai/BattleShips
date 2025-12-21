using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    public sealed class DarkThemeBuilder : ThemeBuilder
    {
        protected override void SetCoreColors(GameTheme theme)
        {
            theme.BackgroundColor = CreateColor(15, 20, 30);
            theme.PanelBackground = CreateColor(25, 35, 50);
            theme.PrimaryColor = CreateColor(0, 120, 215);
            theme.SecondaryColor = CreateColor(40, 40, 40);
            theme.IsDarkTheme = true;
        }

        protected override void SetGridColors(GameTheme theme)
        {
            theme.GridLineColor = CreateColor(60, 80, 100);
            theme.GridBorderColor = CreateColor(100, 150, 200);
            theme.GridBackground = CreateColor(25, 25, 25);
        }

        protected override void SetShipColors(GameTheme theme)
        {
            theme.ShipColor = CreateColor(180, 46, 204, 113);
            theme.ShipOutline = CreateColor(0, 90, 180);
            theme.ShipHitColor = CreateColor(220, 53, 69);
            theme.ShipMissColor = CreateColor(100, 100, 100);
            theme.MineColor = CreateColor(180, 200, 200, 0);
        }

        protected override void SetTextColors(GameTheme theme)
        {
            theme.TextColor = CreateColor(200, 220, 240);
            theme.LabelColor = CreateColor(200, 220, 240);
        }

        protected override void SetButtonColors(GameTheme theme)
        {
            // Button colors for dark theme
            theme.ButtonColor = CreateColor(45, 45, 45);
            theme.ButtonHover = CreateColor(65, 65, 65);
            theme.ButtonText = Color.White;
        }

        protected override void SetGameStateColors(GameTheme theme)
        {
            theme.HitColor = CreateColor(220, 53, 69);
            theme.MissColor = CreateColor(100, 100, 100);
            theme.SelectionColor = CreateColor(0, 120, 215, 80);
        }
    }
}
