using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// FIFO (First-In-First-Out) iterator - processes messages in order
    /// </summary>
    public class QueueIterator : IMotivationalMessageIterator
    {
        private readonly MotivationalMessagesCollection _collection;
        private int _currentIndex;

        public string Current => IsDone ? "No more messages!" : _collection[_currentIndex];
        public bool IsDone => _currentIndex >= _collection.Count;

        public QueueIterator(MotivationalMessagesCollection collection)
        {
            _collection = collection;
            _currentIndex = 0;
        }

        public bool Next()
        {
            if (IsDone) return false;

            _currentIndex++;
            return !IsDone;
        }

        public void Reset()
        {
            _currentIndex = 0;
        }
    }
}
