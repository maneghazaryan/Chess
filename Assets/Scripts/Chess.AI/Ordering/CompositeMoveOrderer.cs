using System;
using System.Collections.Generic;
using System.Linq;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// Scores every move by summing its heuristics and sorts the list best-first, with the hash
    /// move always leading.
    /// </summary>
    /// <remarks>
    /// Sorted with an insertion sort rather than <see cref="Array.Sort(Array)"/>: move lists are
    /// short (typically 30-40 entries) and already close to sorted after the first iterative
    /// deepening pass, which is precisely the case insertion sort handles best, and it avoids the
    /// comparer allocation a general sort would need.
    /// </remarks>
    public sealed class CompositeMoveOrderer : IMoveOrderer
    {
        private readonly IMoveScoreHeuristic[] _heuristics;
        private int[] _scores = new int[64];

        public CompositeMoveOrderer(params IMoveScoreHeuristic[] heuristics)
        {
            _heuristics = heuristics ?? throw new ArgumentNullException(nameof(heuristics));
        }

        public CompositeMoveOrderer(IEnumerable<IMoveScoreHeuristic> heuristics)
            : this(heuristics?.ToArray())
        {
        }

        /// <summary>The standard stack: material gain first, then killers, then history.</summary>
        public static CompositeMoveOrderer CreateStandard()
        {
            return new CompositeMoveOrderer(
                new MvvLvaHeuristic(),
                new KillerMoveHeuristic(),
                new HistoryHeuristic());
        }

        public void Order(IBoard board, MoveList moves, int ply, in Move hashMove)
        {
            int count = moves.Count;
            if (count < 2)
            {
                return;
            }

            if (_scores.Length < count)
            {
                _scores = new int[Math.Max(count, _scores.Length * 2)];
            }

            for (int i = 0; i < count; i++)
            {
                Move move = moves[i];
                _scores[i] = move == hashMove ? OrderingScores.HashMove : ScoreHeuristics(board, move, ply);
            }

            SortDescending(moves, count);
        }

        public void OnBetaCutoff(IBoard board, in Move move, int ply, int depth)
        {
            for (int i = 0; i < _heuristics.Length; i++)
            {
                _heuristics[i].OnBetaCutoff(board, move, ply, depth);
            }
        }

        public void Reset()
        {
            for (int i = 0; i < _heuristics.Length; i++)
            {
                _heuristics[i].Reset();
            }
        }

        private int ScoreHeuristics(IBoard board, in Move move, int ply)
        {
            int total = 0;

            for (int i = 0; i < _heuristics.Length; i++)
            {
                total += _heuristics[i].Score(board, move, ply);
            }

            return total;
        }

        private void SortDescending(MoveList moves, int count)
        {
            for (int i = 1; i < count; i++)
            {
                Move move = moves[i];
                int score = _scores[i];
                int j = i - 1;

                while (j >= 0 && _scores[j] < score)
                {
                    moves[j + 1] = moves[j];
                    _scores[j + 1] = _scores[j];
                    j--;
                }

                moves[j + 1] = move;
                _scores[j + 1] = score;
            }
        }
    }
}
