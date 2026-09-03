using System;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// Scores quiet moves by how often the same origin-destination pair has caused a cutoff
    /// anywhere in the tree.
    /// </summary>
    /// <remarks>
    /// Where killers are sharply ply-local, history is the search's long-term memory: it notices
    /// that, in this position type, a particular rook lift or knight manoeuvre keeps working.
    /// Cutoffs are credited <c>depth squared</c> so that a refutation found deep in the tree,
    /// which cost far more to find, counts for more than a shallow one.
    /// </remarks>
    public sealed class HistoryHeuristic : IMoveScoreHeuristic
    {
        /// <summary>
        /// When any entry reaches this, the whole table is halved. That both keeps history from
        /// overflowing its score band and lets the table forget moves that stopped working.
        /// </summary>
        private const int AgeingThreshold = OrderingScores.MaxHistory;

        private readonly int[,,] _scores = new int[2, Square.Count, Square.Count];

        public int Score(IBoard board, in Move move, int ply)
        {
            if (move.IsTactical)
            {
                return 0;
            }

            return _scores[(int)board.SideToMove, move.From.Index, move.To.Index];
        }

        public void OnBetaCutoff(IBoard board, in Move move, int ply, int depth)
        {
            if (move.IsTactical)
            {
                return;
            }

            int color = (int)board.SideToMove;
            int updated = _scores[color, move.From.Index, move.To.Index] + depth * depth;

            if (updated >= AgeingThreshold)
            {
                Halve();
                updated /= 2;
            }

            _scores[color, move.From.Index, move.To.Index] = updated;
        }

        public void Reset() => Array.Clear(_scores, 0, _scores.Length);

        private void Halve()
        {
            for (int color = 0; color < 2; color++)
            {
                for (int from = 0; from < Square.Count; from++)
                {
                    for (int to = 0; to < Square.Count; to++)
                    {
                        _scores[color, from, to] /= 2;
                    }
                }
            }
        }
    }
}
