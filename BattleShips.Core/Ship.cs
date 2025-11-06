using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
    // ===== ENUMS =====
    public enum ShipOrientation
    {
        Horizontal,
        Vertical
    }

    public enum ShipClass
    {
        Blocky,
        Curvy
    }

    // ===== INTERFACES =====
    public interface IShip
    {
        int Length { get; }
        int Id { get; }
        Point Position { get; set; }
        ShipOrientation Orientation { get; set; }
        bool IsPlaced { get; set; }
        List<Point> GetOccupiedCells();
        void Rotate();
        bool IsValidPosition(int boardSize);
    }

    public interface IClass
    {
        IShip CreateAircraftCarrier(int length, int id);
        IShip CreateBattleShip(int length, int id);
        IShip CreateCruiser(int length, int id);
        IShip CreateDestroyer(int length, int id);
    }

    // ===== ABSTRACT FACTORY BASE =====
    public abstract class ShipFactoryBase : IClass
    {
        protected ShipClass ShipClassType { get; }

        protected ShipFactoryBase(ShipClass shipClassType)
        {
            ShipClassType = shipClassType;
        }

        public virtual IShip CreateAircraftCarrier(int length, int id)
            => SetClass(new AircraftCarrier(length, id));

        public virtual IShip CreateBattleShip(int length, int id)
            => SetClass(new BattleShip(length, id));

        public virtual IShip CreateCruiser(int length, int id)
            => SetClass(new Cruiser(length, id));

        public virtual IShip CreateDestroyer(int length, int id)
            => SetClass(new Destroyer(length, id));

        protected IShip SetClass(BaseShip ship)
        {
            ship.Class = ShipClassType;
            return ship;
        }
    }

    public class CurvyClass : ShipFactoryBase
    {
        public CurvyClass() : base(ShipClass.Curvy) { }
    }

    public class BlockyClass : ShipFactoryBase
    {
        public BlockyClass() : base(ShipClass.Blocky) { }
    }

    // ===== BASE SHIP IMPLEMENTATION =====
    public abstract class BaseShip : IShip
    {
        public int Length { get; protected set; }
        public int Id { get; }
        public Point Position { get; set; }
        public ShipOrientation Orientation { get; set; }
        public bool IsPlaced { get; set; }
        public ShipClass Class { get; set; }

        protected BaseShip(int length, int id)
        {
            Length = length;
            Id = id;
            Position = Point.Empty;
            Orientation = ShipOrientation.Horizontal;
            IsPlaced = false;
        }

        public void Rotate()
        {
            Orientation = Orientation == ShipOrientation.Horizontal
                ? ShipOrientation.Vertical
                : ShipOrientation.Horizontal;
        }

        public bool IsValidPosition(int boardSize)
        {
            return GetOccupiedCells().All(cell =>
                cell.X >= 0 && cell.X < boardSize &&
                cell.Y >= 0 && cell.Y < boardSize);
        }

        public abstract List<Point> GetOccupiedCells();
    }

    // ===== CONCRETE SHIPS =====
    public class AircraftCarrier : BaseShip
    {
        public AircraftCarrier(int length, int id) : base(length, id) { }

        public override List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            for (int i = 0; i < Length; i++)
            {
                if (Class == ShipClass.Blocky)
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i, Position.Y)
                        : new Point(Position.X, Position.Y + i));
                }
                else // Curvy
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i / 2, Position.Y + i - i / 2)
                        : new Point(Position.X + i - i / 2, Position.Y + i / 2));
                }
            }
            return cells;
        }
    }

    public class BattleShip : BaseShip
    {
        public BattleShip(int length, int id) : base(length, id) { }

        public override List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            for (int i = 0; i < Length; i++)
            {
                if (Class == ShipClass.Blocky)
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i, Position.Y)
                        : new Point(Position.X, Position.Y + i));
                }
                else
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i / 2, Position.Y + i - i / 2)
                        : new Point(Position.X + i - i / 2, Position.Y + i / 2));
                }
            }
            return cells;
        }
    }

    public class Cruiser : BaseShip
    {
        public Cruiser(int length, int id) : base(length, id) { }

        public override List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            for (int i = 0; i < Length; i++)
            {
                if (Class == ShipClass.Blocky)
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i, Position.Y)
                        : new Point(Position.X, Position.Y + i));
                }
                else
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i / 2, Position.Y + i - i / 2)
                        : new Point(Position.X + i - i / 2, Position.Y + i / 2));
                }
            }
            return cells;
        }
    }

    public class Destroyer : BaseShip
    {
        public Destroyer(int length, int id) : base(length, id) { }

        public override List<Point> GetOccupiedCells()
        {
            var cells = new List<Point>();
            for (int i = 0; i < Length; i++)
            {
                if (Class == ShipClass.Blocky)
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i, Position.Y)
                        : new Point(Position.X, Position.Y + i));
                }
                else
                {
                    cells.Add(Orientation == ShipOrientation.Horizontal
                        ? new Point(Position.X + i / 2, Position.Y + i - i / 2)
                        : new Point(Position.X + i - i / 2, Position.Y + i / 2));
                }
            }
            return cells;
        }
    }

    // ===== CLIENT / CONFIGURATION =====
    public static class FleetConfiguration
    {
        public static readonly List<int> StandardFleet = new() { 5, 4, 3, 3, 2 };

        public static List<IShip> CreateStandardFleet()
        {
            // 🎲 Randomly pick a style: 0 = Blocky, 1 = Curvy
            var rng = new Random();
            ShipClass randomClass = rng.Next(2) == 0 ? ShipClass.Blocky : ShipClass.Curvy;

            IClass factory = randomClass == ShipClass.Curvy
                ? new CurvyClass()
                : new BlockyClass();

            var shipList = new List<IShip>();
            int index = 0;

            foreach (int length in StandardFleet)
            {
                IShip ship = length switch
                {
                    5 => factory.CreateAircraftCarrier(length, index),
                    4 => factory.CreateBattleShip(length, index),
                    3 => factory.CreateCruiser(length, index),
                    2 => factory.CreateDestroyer(length, index),
                    _ => throw new ArgumentException($"Invalid ship length {length}")
                };

                shipList.Add(ship);
                index++;
            }

            return shipList;
        }
    }

}
