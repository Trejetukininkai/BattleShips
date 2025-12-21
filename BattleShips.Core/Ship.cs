using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BattleShips.Core
{
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
        
        // PROTOTYPE PATTERN: Clone method to create a copy of the ship
        IShip Clone(int newId);
    }

    public class ShipAdapter
    {
        private readonly IShipComponent _component;

        public ShipAdapter(IShipComponent component)
        {
            _component = component;
        }

        public int Id => _component.Id;
        public int Length => _component.Length;
        public Point Position => _component.Position;
        public ShipOrientation Orientation => _component.Orientation;
        public bool IsPlaced => _component.IsPlaced;

        public List<Point> GetOccupiedCells() => _component.GetOccupiedCells();
    }

    public interface IClass
    {
        IShip CreateAircraftCarrier(int length, int id);
        IShip CreateBattleShip(int length, int id);
        IShip CreateCruiser(int length, int id);
        IShip CreateDestroyer(int length, int id);
    }

    //  ABSTRACT FACTORY BASE 
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

    //  BASE SHIP  
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
        
        // PROTOTYPE PATTERN: Clone implementation
        // Creates a deep copy of the ship with a new ID
        // Position and IsPlaced are reset to defaults
        public abstract IShip Clone(int newId);
        
        // Helper method for subclasses to clone common properties
        protected void CopyPropertiesTo(BaseShip target)
        {
            target.Class = this.Class;
            target.Orientation = this.Orientation;
            // Note: Position and IsPlaced are intentionally reset in Clone
        }
    }

    //  CONCRETE SHIPS 
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
        
        // PROTOTYPE PATTERN: Clone this AircraftCarrier
        public override IShip Clone(int newId)
        {
            var cloned = new AircraftCarrier(this.Length, newId);
            CopyPropertiesTo(cloned);
            return cloned;
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
        
        // PROTOTYPE PATTERN: Clone this BattleShip
        public override IShip Clone(int newId)
        {
            var cloned = new BattleShip(this.Length, newId);
            CopyPropertiesTo(cloned);
            return cloned;
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
        
        // PROTOTYPE PATTERN: Clone this Cruiser
        public override IShip Clone(int newId)
        {
            var cloned = new Cruiser(this.Length, newId);
            CopyPropertiesTo(cloned);
            return cloned;
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
        
        // PROTOTYPE PATTERN: Clone this Destroyer
        public override IShip Clone(int newId)
        {
            var cloned = new Destroyer(this.Length, newId);
            CopyPropertiesTo(cloned);
            return cloned;
        }
    }


    // PROTOTYPE PATTERN: Registry to store and manage ship prototypes
    public class ShipPrototypeRegistry
    {
        private readonly Dictionary<(int length, ShipClass shipClass), IShip> _prototypes = new();

        public ShipPrototypeRegistry()
        {
            InitializePrototypes();
        }

        private void InitializePrototypes()
        {
            // Create prototypes for both Blocky and Curvy classes
            var blockyFactory = new BlockyClass();
            var curvyFactory = new CurvyClass();

            // Blocky prototypes
            _prototypes[(5, ShipClass.Blocky)] = blockyFactory.CreateAircraftCarrier(5, -1);
            _prototypes[(4, ShipClass.Blocky)] = blockyFactory.CreateBattleShip(4, -1);
            _prototypes[(3, ShipClass.Blocky)] = blockyFactory.CreateCruiser(3, -1);
            _prototypes[(2, ShipClass.Blocky)] = blockyFactory.CreateDestroyer(2, -1);

            // Curvy prototypes
            _prototypes[(5, ShipClass.Curvy)] = curvyFactory.CreateAircraftCarrier(5, -1);
            _prototypes[(4, ShipClass.Curvy)] = curvyFactory.CreateBattleShip(4, -1);
            _prototypes[(3, ShipClass.Curvy)] = curvyFactory.CreateCruiser(3, -1);
            _prototypes[(2, ShipClass.Curvy)] = curvyFactory.CreateDestroyer(2, -1);
        }

        // Clone a ship from the prototype registry
        public IShip CloneShip(int length, ShipClass shipClass, int newId)
        {
            var key = (length, shipClass);
            if (!_prototypes.ContainsKey(key))
            {
                throw new ArgumentException($"No prototype found for length {length} and class {shipClass}");
            }

            return _prototypes[key].Clone(newId);
        }

        // Check if a prototype exists
        public bool HasPrototype(int length, ShipClass shipClass)
        {
            return _prototypes.ContainsKey((length, shipClass));
        }
    }

    public static class FleetConfiguration
    {
        public static readonly List<int> StandardFleet = new() { 5, 4, 3, 3, 2 };

        // Original factory-based approach (still valid)
        public static List<IShip> CreateStandardFleet()
        {
            // Randomly pick a style: 0 = Blocky, 1 = Curvy
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

        // PROTOTYPE PATTERN: New approach using prototype cloning
        public static List<IShip> CreateStandardFleetFromPrototypes(ShipClass shipClass)
        {
            var registry = new ShipPrototypeRegistry();
            var shipList = new List<IShip>();
            int index = 0;

            foreach (int length in StandardFleet)
            {
                // Clone from prototype instead of creating from factory
                IShip ship = registry.CloneShip(length, shipClass, index);
                shipList.Add(ship);
                index++;
            }

            Console.WriteLine($"[FleetConfiguration] Created fleet using Prototype pattern with {shipClass} class");
            return shipList;
        }

        // Convenience method with random class selection
        public static List<IShip> CreateStandardFleetFromPrototypes()
        {
            var rng = new Random();
            ShipClass randomClass = rng.Next(2) == 0 ? ShipClass.Blocky : ShipClass.Curvy;
            return CreateStandardFleetFromPrototypes(randomClass);
        }
    }

}
