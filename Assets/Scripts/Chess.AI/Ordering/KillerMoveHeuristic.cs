using System;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// Remembers the quiet moves that recently caused a cutoff at each ply and tries them early
    /// when the same ply is reached again.
    /// </summary>
    /// <remarks>
    /// Sibling nodes at the same ply usually share a refutation. If advancing a pawn to fork two
    /// pieces refutes one of the opponent's tries, it very likely refutes the next one too, so
    /// remembering it per ply is far cheaper than rediscovering it. Two slots per ply is the
    /// standard compromise: enough to hold a second idea, few enough to stay meaningful.
    /// </remarks>
    public sealed class KillerMoveHeuristic : IMoveScoreHeuristic
    {
        private const int SlotsPerPly = 2;

        private Move[,] _killers;

        public KillerMoveHeuristic(int maxPly = 128)
        {
            _killers = new Move[Math.Max(1, maxPly), SlotsPerPly];
        }

        public int Score(IBoard board, in Move move, int ply)
        {
            // Captures are already ordered by material gain, so recording them here would add
            // nothing and would push genuinely informative quiet moves out of the slots.
            if (move.IsTactical || ply >= _killers.GetLength(0))
            {
                return 0;
            }

            if (_killers[ply, 0] == move)
            {
                return OrderingScores.PrimaryKiller;
            }

            if (_killers[ply, 1] == move)
            {
                return OrderingScores.SecondaryKiller;
            }

            return 0;
        }

        public void OnBetaCutoff(IBoard board, in Move move, int ply, int depth)
        {
            if (move.IsTactical)
            {
                return;
            }

            EnsureCapacity(ply);

            if (_killers[ply, 0] == move)
            {
                return;
            }

            _killers[ply, 1] = _killers[ply, 0];
            _killers[ply, 0] = move;
        }

        public void Reset() => Array.Clear(_killers, 0, _killers.Length);

        private void EnsureCapacity(int ply)
        {
            int currentPlies = _killers.GetLength(0);
            if (ply < currentPlies)
            {
                return;
            }

            var grown = new Move[Math.Max(ply + 1, currentPlies * 2), SlotsPerPly];
            Array.Copy(_killers, grown, _killers.Length);
            _killers = grown;
        }
    }
}
