using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class BattleManager
    {
        private DamageChainDirector _damageDirector;
        private List<IShip> _playerShips;
        private List<IShip> _enemyShips;

        public BattleManager()
        {
            _damageDirector = new DamageChainDirector();
            _playerShips = new List<IShip>();
            _enemyShips = new List<IShip>();
        }

        public void SetShips(List<IShip> playerShips, List<IShip> enemyShips)
        {
            _playerShips = playerShips;
            _enemyShips = enemyShips;
        }

        public DamageResult PlayerAttack(int x, int y)
        {
            return _damageDirector.ProcessAttack(_enemyShips, x, y);
        }

        public DamageResult EnemyAttack(int x, int y)
        {
            return _damageDirector.ProcessAttack(_playerShips, x, y);
        }

        public void ResetBattle()
        {
            _damageDirector.Reset();
        }
    }
}
