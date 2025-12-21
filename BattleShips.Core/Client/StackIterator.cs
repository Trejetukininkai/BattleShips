using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// LIFO (Last-In-First-Out) iterator - processes messages in reverse order
    /// </summary>
    public class StackIterator : IMotivationalMessageIterator
    {
        private readonly MotivationalMessagesCollection _collection;
        private int _currentIndex;

        public string Current => IsDone ? "No more messages!" : _collection[_currentIndex];
        public bool IsDone => _currentIndex < 0;

        public StackIterator(MotivationalMessagesCollection collection)
        {
            _collection = collection;
            _currentIndex = _collection.Count - 1; // Start from last
        }

        public bool Next()
        {
            if (IsDone) return false;

            _currentIndex--;
            return !IsDone;
        }

        public void Reset()
        {
            _currentIndex = _collection.Count - 1;
        }
    }
}
