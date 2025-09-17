using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.AspNetCore.SignalR.Client;
using BattleShips.Core;

namespace BattleShips.Client
{
    public partial class Form1 : Form
    {
        private HubConnection? _conn;

        // --- grid visuals ---
        private const int Cell = 40;         // cell size in px
        private const int Margin = 40;       // left/top margin (for labels)
        private readonly HashSet<Point> _marked = new(); // clicked cells (col,row)

        public Form1()
        {
            InitializeComponent();
            DoubleBuffered = true;           // smoother drawing
            MinimumSize = new Size(2*Margin + Board.Size*Cell + 32,
                                   2*Margin + Board.Size*Cell + 72);

            Shown += async (_, __) => await ConnectAsync();
            Paint += OnPaintGrid;
            MouseClick += OnMouseClickGrid;
            Resize += (_, __) => Invalidate();
        }

        private async Task ConnectAsync()
        {
            var baseUrl = Environment.GetEnvironmentVariable("API_URL") ?? "http://localhost:5000";
            Console.WriteLine(baseUrl);
            _conn = new HubConnectionBuilder()
                .WithUrl($"{baseUrl.TrimEnd('/')}/game")
                .WithAutomaticReconnect()
                .Build();

            _conn.On<string>("Pong", s => BeginInvoke(() => Text = $"Connected · {s}"));
            await _conn.StartAsync();
            Text = "Connected to BattleShips server";
            await _conn.SendAsync("Ping", "client-hello");
        }

        // --- drawing ---
        private void OnPaintGrid(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(20, 26, 38));

            // Board rect
            var w = Board.Size * Cell;
            var h = Board.Size * Cell;

            // labels
            using var labelBrush = new SolidBrush(Color.White);
            using var thin = new Pen(Color.Gray, 1);
            using var thick = new Pen(Color.White, 2);
            using var font = new Font(Font.FontFamily, 10, FontStyle.Bold);

            // column labels A-J
            for (int c = 0; c < Board.Size; c++)
            {
                var ch = (char)('A' + c);
                var x = Margin + c * Cell + Cell / 2f;
                g.DrawString(ch.ToString(), font, labelBrush, x - 6, Margin - 24);
            }
            // row labels 1-10
            for (int r = 0; r < Board.Size; r++)
            {
                var y = Margin + r * Cell + Cell / 2f;
                g.DrawString((r + 1).ToString(), font, labelBrush, Margin - 28, y - 8);
            }

            // grid background
            g.FillRectangle(new SolidBrush(Color.FromArgb(30, 70, 120)), Margin, Margin, w, h);
            g.DrawRectangle(thick, Margin, Margin, w, h);

            // grid lines
            for (int i = 1; i < Board.Size; i++)
            {
                // vertical
                g.DrawLine(thin, Margin + i * Cell, Margin, Margin + i * Cell, Margin + h);
                // horizontal
                g.DrawLine(thin, Margin, Margin + i * Cell, Margin + w, Margin + i * Cell);
            }

            // draw marked cells (just a simple circle for now)
            foreach (var p in _marked)
            {
                var x = Margin + p.X * Cell;
                var y = Margin + p.Y * Cell;
                g.FillEllipse(Brushes.Orange, x + 8, y + 8, Cell - 16, Cell - 16);
            }
        }

        // --- input: toggle a mark on click ---
        private void OnMouseClickGrid(object? sender, MouseEventArgs e)
        {
            var col = (e.X - Margin) / Cell;
            var row = (e.Y - Margin) / Cell;
            if (col < 0 || row < 0 || col >= Board.Size || row >= Board.Size) return;

            var p = new Point(col, row);
            if (_marked.Contains(p)) _marked.Remove(p); else _marked.Add(p);
            Invalidate();
        }
    }
}
