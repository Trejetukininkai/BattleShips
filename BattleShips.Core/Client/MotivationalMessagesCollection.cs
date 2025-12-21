using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Collection of motivational messages with factory for creating different iterators
    /// </summary>
    public class MotivationalMessagesCollection
    {
        private readonly List<string> _messages = new List<string>
        {
            "⚓ Full speed ahead, Captain!",
            "🎯 Aim true and sink 'em all!",
            "🌊 The sea favors the bold!",
            "💪 Show them who rules the waves!",
            "🔥 Burn their fleet to ashes!",
            "🎖️ Victory is within your grasp!",
            "⚔️ No mercy for the enemy!",
            "🚀 Strike fast and strike hard!",
            "🏴‍☠️ Loot and plunder awaits!",
            "⭐ You're the admiral of these waters!"
        };

        public int Count => _messages.Count;

        public string this[int index] => _messages[index];

        public IMotivationalMessageIterator CreateQueueIterator()
        {
            return new QueueIterator(this);
        }

        public IMotivationalMessageIterator CreateStackIterator()
        {
            return new StackIterator(this);
        }

        public IMotivationalMessageIterator CreateBinaryTreeIterator()
        {
            return new BinaryTreeIterator(this);
        }

        public IMotivationalMessageIterator CreateRandomIterator()
        {
            var random = new Random();
            int choice = random.Next(3);

            return choice switch
            {
                0 => CreateQueueIterator(),
                1 => CreateStackIterator(),
                2 => CreateBinaryTreeIterator(),
                _ => CreateQueueIterator()
            };
        }
    }
}
