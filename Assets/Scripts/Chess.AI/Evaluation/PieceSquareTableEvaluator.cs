using System.Collections.Generic;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Scores only where the pieces stand, using the midgame tables.
    /// </summary>
    /// <remarks>
    /// Separated from material so the two halves of the evaluation can be tested and weighted
    /// independently. On its own it produces an engine that develops sensibly and hangs
    /// everything, which is exactly why it is normally paired with <see cref="MaterialEvaluator"/>.
    /// </remarks>
    public sealed class PieceSquareTableEvaluator : IPositionEvaluator
    {
        public int Evaluate(IBoard board)
        {
            int whiteRelative = ScoreFor(board, PieceColor.White) - ScoreFor(board, PieceColor.Black);
            return board.SideToMove == PieceColor.White ? whiteRelative : -whiteRelative;
        }

        private static int ScoreFor(IBoard board, PieceColor color)
        {
            IReadOnlyList<Square> squares = board.GetOccupiedSquares(color);
            int total = 0;

            for (int i = 0; i < squares.Count; i++)
            {
                Square square = squares[i];
                total += PieceSquareTables.Midgame(board[square].Type, color, square);
            }

            return total;
        }
    }
}
