using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    public sealed class LightThemeBuilder : ThemeBuilder
    {
        protected override void SetCoreColors(GameTheme theme)
        {
            theme.BackgroundColor = CreateColor(240, 240, 245);
            theme.PanelBackground = CreateColor(220, 230, 240);
            theme.PrimaryColor = CreateColor(13, 110, 253);
            theme.SecondaryColor = CreateColor(240, 240, 240);
            theme.IsDarkTheme = false;
        }

        protected override void SetGridColors(GameTheme theme)
        {
            theme.GridLineColor = CreateColor(180, 190, 200);
            theme.GridBorderColor = CreateColor(120, 140, 180);
            theme.GridBackground = Color.White;
        }

        protected override void SetShipColors(GameTheme theme)
        {
            theme.ShipColor = CreateColor(180, 30, 130, 80);
            theme.ShipOutline = CreateColor(10, 88, 202);
            theme.ShipHitColor = CreateColor(220, 53, 69);
            theme.ShipMissColor = CreateColor(173, 181, 189);
            theme.MineColor = CreateColor(180, 180, 160, 0);
        }

        protected override void SetTextColors(GameTheme theme)
        {
            theme.TextColor = CreateColor(40, 40, 60);
            theme.LabelColor = CreateColor(40, 40, 60);
        }

        protected override void SetButtonColors(GameTheme theme)
        {
            theme.ButtonColor = CreateColor(248, 249, 250);
            theme.ButtonHover = CreateColor(233, 236, 239);
            theme.ButtonText = CreateColor(33, 37, 41);
        }

        protected override void SetGameStateColors(GameTheme theme)
        {
            theme.HitColor = CreateColor(220, 53, 69);
            theme.MissColor = CreateColor(173, 181, 189);
            theme.SelectionColor = CreateColor(13, 110, 253, 40);
        }
    }
}
