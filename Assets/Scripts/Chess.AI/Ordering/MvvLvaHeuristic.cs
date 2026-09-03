using Chess.AI.Evaluation;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// Most Valuable Victim, Least Valuable Attacker: prefer capturing expensive pieces with cheap
    /// ones. Also scores promotions, which gain material without capturing anything.
    /// </summary>
    /// <remarks>
    /// Pawn takes queen is almost always worth looking at first, and queen takes pawn almost
    /// never is. Weighting the victim ten times as heavily as the attacker sorts captures into
    /// that order with one subtraction and no search.
    /// </remarks>
    public sealed class MvvLvaHeuristic : IMoveScoreHeuristic
    {
        private const int VictimWeight = 10;

        public int Score(IBoard board, in Move move, int ply)
        {
            if (!move.IsTactical)
            {
                return 0;
            }

            int score = OrderingScores.Tactical;

            if (move.IsCapture)
            {
                PieceType victim = ResolveVictimType(board, move);
                PieceType attacker = board[move.From].Type;

                score += VictimWeight * EvaluationConstants.MidgameValue(victim)
                         - EvaluationConstants.MidgameValue(attacker);
            }

            if (move.IsPromotion)
            {
                score += EvaluationConstants.MidgameValue(move.PromotionPieceType);
            }

            return score;
        }

        public void OnBetaCutoff(IBoard board, in Move move, int ply, int depth)
        {
        }

        public void Reset()
        {
        }

        private static PieceType ResolveVictimType(IBoard board, in Move move)
        {
            // En passant is the one capture whose victim is not standing on the destination square.
            return move.IsEnPassant ? PieceType.Pawn : board[move.To].Type;
        }
    }
}
