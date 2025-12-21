using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.Versioning;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Manages particle effects like explosions, impacts, and visual debris.
    /// Part of the rendering subsystem managed by GameRenderingFacade.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class ParticleEffectsManager
    {
        private readonly List<Particle> _particles = new List<Particle>();
        private static readonly Random _random = new Random();

        /// <summary>
        /// Represents a single particle in the effect system.
        /// </summary>
        private class Particle
        {
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public float Life { get; set; } = 1.0f;
            public float MaxLife { get; set; } = 1.0f;
            public Color Color { get; set; }
            public float Size { get; set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }
        }

        /// <summary>
        /// Creates an explosion effect at a specific board cell.
        /// </summary>
        public void CreateExplosion(Point cell, Rectangle boardRect, int cellSize)
        {
            var cellX = boardRect.X + cell.X * cellSize + cellSize / 2;
            var cellY = boardRect.Y + cell.Y * cellSize + cellSize / 2;

            // Create red hit particles
            for (int i = 0; i < 10; i++)
            {
                var angle = (float)(_random.NextDouble() * Math.PI * 2);
                var speed = 2 + (float)_random.NextDouble() * 3;

                _particles.Add(new Particle
                {
                    Position = new PointF(cellX, cellY),
                    Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                    Life = 1.0f,
                    MaxLife = 2.0f,
                    Color = Color.FromArgb(255, 255, 50, 50),
                    Size = 3 + _random.Next(2),
                    Rotation = 0,
                    RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 0.2f
                });
            }
        }

        /// <summary>
        /// Creates a burst of particles for disaster effects (multiple cells).
        /// </summary>
        public void CreateDisasterBurst(List<Point> cells, Rectangle boardRect, int cellSize)
        {
            foreach (var cell in cells)
            {
                var cellX = boardRect.X + cell.X * cellSize + cellSize / 2;
                var cellY = boardRect.Y + cell.Y * cellSize + cellSize / 2;

                // Create yellow/orange disaster particles
                for (int i = 0; i < 15; i++)
                {
                    var angle = (float)(_random.NextDouble() * Math.PI * 2);
                    var speed = 3 + (float)_random.NextDouble() * 4;

                    _particles.Add(new Particle
                    {
                        Position = new PointF(cellX, cellY),
                        Velocity = new PointF((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed),
                        Life = 1.0f,
                        MaxLife = 2.5f,
                        Color = Color.FromArgb(255, 255, 200, 50),
                        Size = 4 + _random.Next(3),
                        Rotation = 0,
                        RotationSpeed = ((float)_random.NextDouble() - 0.5f) * 0.3f
                    });
                }
            }
        }

        /// <summary>
        /// Updates all particles (position, life, etc.).
        /// Call this on each animation frame.
        /// </summary>
        public void Update()
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                var particle = _particles[i];

                // Update position
                particle.Position = new PointF(
                    particle.Position.X + particle.Velocity.X,
                    particle.Position.Y + particle.Velocity.Y
                );

                // Update rotation
                particle.Rotation += particle.RotationSpeed;

                // Apply gravity and drag
                particle.Velocity = new PointF(
                    particle.Velocity.X * 0.98f,
                    particle.Velocity.Y * 0.98f + 0.1f
                );

                // Update life
                particle.Life -= 1.0f / 60.0f; // Assuming 60 FPS

                // Remove dead particles
                if (particle.Life <= 0)
                {
                    _particles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Draws all active particles.
        /// </summary>
        public void DrawParticles(Graphics g)
        {
            foreach (var particle in _particles)
            {
                var alpha = (int)(255 * (particle.Life / particle.MaxLife));
                var color = Color.FromArgb(alpha, particle.Color.R, particle.Color.G, particle.Color.B);

                using (var brush = new SolidBrush(color))
                {
                    var size = particle.Size * (particle.Life / particle.MaxLife);
                    var x = particle.Position.X - size / 2;
                    var y = particle.Position.Y - size / 2;

                    // Draw particle as a rotated rectangle or circle
                    if (particle.Rotation != 0)
                    {
                        var oldTransform = g.Transform;
                        g.TranslateTransform(particle.Position.X, particle.Position.Y);
                        g.RotateTransform(particle.Rotation * 180f / (float)Math.PI);
                        g.FillRectangle(brush, -size / 2, -size / 2, size, size);
                        g.Transform = oldTransform;
                    }
                    else
                    {
                        g.FillEllipse(brush, x, y, size, size);
                    }
                }

                // Add glow effect for brighter particles
                if (particle.Color.R > 200 || particle.Color.G > 200)
                {
                    var glowAlpha = Math.Min(100, alpha / 2);
                    var glowColor = Color.FromArgb(glowAlpha, particle.Color.R, particle.Color.G, particle.Color.B);
                    using (var glowBrush = new SolidBrush(glowColor))
                    {
                        var glowSize = particle.Size * 2;
                        var glowX = particle.Position.X - glowSize / 2;
                        var glowY = particle.Position.Y - glowSize / 2;
                        g.FillEllipse(glowBrush, glowX, glowY, glowSize, glowSize);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all particles.
        /// </summary>
        public void Clear()
        {
            _particles.Clear();
        }

        /// <summary>
        /// Gets the current number of active particles.
        /// </summary>
        public int ParticleCount => _particles.Count;
    }
}

