using System;
using System.Drawing;
using System.Windows.Forms;

namespace BattleShips.Client
{
    public static class StartupUiBuilder
    {
        public static (Panel panel, Button connectBtn, Label status, Label countdown) Build(
            Action onConnectClick)
        {
            var panel = new Panel
            {
                BackColor = Color.FromArgb(30, 34, 44),
                Dock = DockStyle.Fill
            };

            var btn = new Button
            {
                Text = "Connect to localhost",
                ForeColor = Color.White,
                Size = new Size(200, 32),
                Location = new Point(50, 30)
            };
            btn.Click += (_, __) => onConnectClick();

            var status = new Label
            {
                Text = "Not connected",
                ForeColor = Color.White,
                Location = new Point(50, 72),
                AutoSize = true
            };

            var countdown = new Label
            {
                Text = "",
                ForeColor = Color.Yellow,
                Location = new Point(50, 100),
                AutoSize = true
            };

            panel.Controls.Add(btn);
            panel.Controls.Add(status);
            panel.Controls.Add(countdown);
            return (panel, btn, status, countdown);
        }
    }
}
