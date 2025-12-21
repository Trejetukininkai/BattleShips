using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core.Client
{
    public interface IMotivationalMessageIterator
    {
        string Current { get; }
        bool IsDone { get; }
        bool Next();
        void Reset();
    }
}
