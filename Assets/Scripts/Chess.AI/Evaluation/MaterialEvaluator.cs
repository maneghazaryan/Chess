using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Counts material and nothing else.
    /// </summary>
    /// <remarks>
    /// Useful on its own as the weakest difficulty setting, where an opponent that only understands
    /// "do not lose pieces" is a reasonable beginner sparring partner, and as a baseline the
    /// stronger evaluators can be compared against.
    /// </remarks>
    public sealed class MaterialEvaluator : IPositionEvaluator
    {
        public int Evaluate(IBoard board)
        {
            int white = CountMaterial(board, PieceColor.White);
            int black = CountMaterial(board, PieceColor.Black);
            int whiteRelative = white - black;

            return board.SideToMove == PieceColor.White ? whiteRelative : -whiteRelative;
        }

        private static int CountMaterial(IBoard board, PieceColor color)
        {
            int total = 0;

            for (PieceType type = PieceType.Pawn; type <= PieceType.Queen; type++)
            {
                total += board.CountPieces(color, type) * EvaluationConstants.MidgameValue(type);
            }

            return total;
        }
    }
}
