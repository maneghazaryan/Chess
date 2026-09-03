using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Core.Rules.Draws
{
    /// <summary>
    /// A draw when neither side has the material to deliver mate by any sequence of legal moves.
    /// </summary>
    /// <remarks>
    /// FIDE's automatic version covers exactly four material configurations: king versus king,
    /// king and bishop versus king, king and knight versus king, and king and bishop versus king
    /// and bishop with both bishops on the same colour squares. Positions such as king and two
    /// knights versus king are excluded because mate remains possible, if not forceable.
    /// </remarks>
    public sealed class InsufficientMaterialRule : IDrawRule
    {
        public GameStatus Status => GameStatus.DrawByInsufficientMaterial;

        public bool IsDraw(IBoard board)
        {
            if (HasMatingMaterial(board, PieceColor.White) || HasMatingMaterial(board, PieceColor.Black))
            {
                return false;
            }

            int whiteMinors = CountMinorPieces(board, PieceColor.White);
            int blackMinors = CountMinorPieces(board, PieceColor.Black);

            if (whiteMinors + blackMinors <= 1)
            {
                return true;
            }

            return whiteMinors == 1
                   && blackMinors == 1
                   && IsBishopVersusSameColouredBishop(board);
        }

        private static bool HasMatingMaterial(IBoard board, PieceColor color)
        {
            return board.CountPieces(color, PieceType.Pawn) > 0
                   || board.CountPieces(color, PieceType.Rook) > 0
                   || board.CountPieces(color, PieceType.Queen) > 0;
        }

        private static int CountMinorPieces(IBoard board, PieceColor color)
        {
            return board.CountPieces(color, PieceType.Knight) + board.CountPieces(color, PieceType.Bishop);
        }

        private static bool IsBishopVersusSameColouredBishop(IBoard board)
        {
            if (board.CountPieces(PieceColor.White, PieceType.Bishop) != 1 ||
                board.CountPieces(PieceColor.Black, PieceType.Bishop) != 1)
            {
                return false;
            }

            Square whiteBishop = FindFirst(board, PieceColor.White, PieceType.Bishop);
            Square blackBishop = FindFirst(board, PieceColor.Black, PieceType.Bishop);

            return whiteBishop.IsValid
                   && blackBishop.IsValid
                   && whiteBishop.IsLight == blackBishop.IsLight;
        }

        private static Square FindFirst(IBoard board, PieceColor color, PieceType type)
        {
            System.Collections.Generic.IReadOnlyList<Square> squares = board.GetOccupiedSquares(color);
            for (int i = 0; i < squares.Count; i++)
            {
                if (board[squares[i]].Type == type)
                {
                    return squares[i];
                }
            }

            return Square.None;
        }
    }
}
