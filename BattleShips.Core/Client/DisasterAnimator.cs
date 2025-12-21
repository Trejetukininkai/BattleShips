using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace BattleShips.Core
{
    public class DisasterAnimator
    {
        private readonly GameModel _model;
        private CancellationTokenSource? _cts;

        public DisasterAnimator(GameModel model)
        {
            _model = model;
        }

        public async Task PlayAsyncDisasterAnimation(List<Point> cells, List<Point>? hitsForMe)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            _model.IsDisasterAnimating = true;

            try
            {
                foreach (var cell in cells)
                {
                    token.ThrowIfCancellationRequested();
                    _model.AnimatedCells.Add(cell);
                    await Task.Delay(300, token);

                    if (hitsForMe?.Contains(cell) == true)
                        _model.ApplyOpponentMove(cell, true);

                    _model.AnimatedCells.Remove(cell);
                    await Task.Delay(120, token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[Animator] Canceled");
            }
            finally
            {
                _model.IsDisasterAnimating = false;
                _model.CurrentDisasterName = null;
                _model.AnimatedCells.Clear();
            }
        }
    }
}
