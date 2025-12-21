using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    /// <summary>
    /// Binary tree traversal iterator - processes messages in tree order
    /// </summary>
    public class BinaryTreeIterator : IMotivationalMessageIterator
    {
        private readonly MotivationalMessagesCollection _collection;
        private readonly List<int> _traversalOrder = new List<int>();
        private int _traversalIndex;

        public string Current
        {
            get
            {
                if (IsDone || _traversalIndex >= _traversalOrder.Count)
                    return "No more messages!";

                return _collection[_traversalOrder[_traversalIndex]];
            }
        }

        public bool IsDone => _traversalIndex >= _traversalOrder.Count;

        public BinaryTreeIterator(MotivationalMessagesCollection collection)
        {
            _collection = collection;
            _traversalIndex = 0;
            BuildTreeTraversal();
        }

        /// <summary>
        /// Builds in-order traversal of binary tree representation
        /// </summary>
        private void BuildTreeTraversal()
        {
            if (_collection.Count == 0) return;

            // Represent list as a binary tree:
            // Index 0 = root
            // Left child = 2*i + 1
            // Right child = 2*i + 2

            // Perform in-order traversal recursively
            TraverseInOrder(0);
        }

        private void TraverseInOrder(int nodeIndex)
        {
            if (nodeIndex >= _collection.Count) return;

            // Left subtree
            int leftChild = 2 * nodeIndex + 1;
            TraverseInOrder(leftChild);

            // Current node
            _traversalOrder.Add(nodeIndex);

            // Right subtree
            int rightChild = 2 * nodeIndex + 2;
            TraverseInOrder(rightChild);
        }

        public bool Next()
        {
            if (IsDone) return false;

            _traversalIndex++;
            return !IsDone;
        }

        public void Reset()
        {
            _traversalIndex = 0;
        }

        /// <summary>
        /// Gets the current traversal order (for debugging)
        /// </summary>
        public string GetTraversalOrder()
        {
            var indices = string.Join(" → ", _traversalOrder);
            var messages = string.Join(" → ", _traversalOrder.Select(i => $"[{i}]").ToArray());
            return $"Tree traversal: {messages} (Indices: {indices})";
        }
    }
}
