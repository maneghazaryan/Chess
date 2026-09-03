using Chess.Core.Board;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Scores a quiet position in centipawns.
    /// </summary>
    /// <remarks>
    /// The score is always from the point of view of <see cref="IBoard.SideToMove"/>, which is
    /// what lets the negamax search simply negate it on the way back up the tree.
    /// </remarks>
    public interface IPositionEvaluator
    {
        int Evaluate(IBoard board);
    }
}
