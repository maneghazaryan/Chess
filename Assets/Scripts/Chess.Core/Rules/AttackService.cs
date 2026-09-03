using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Determines whether a square is attacked, by looking outward from that square rather than
    /// by enumerating every enemy move.
    /// </summary>
    /// <remarks>
    /// The trick is that attacks are symmetric for the purposes of detection: to find out whether
    /// a knight attacks square S, look at the knight-move squares around S and see whether any
    /// holds an enemy knight. That turns an O(moves) question into a handful of array reads, which
    /// matters because check detection runs on every generated move.
    /// </remarks>
    public sealed class AttackService : IAttackService
    {
        public bool IsInCheck(IBoard board, PieceColor color)
        {
            Square kingSquare = board.GetKingSquare(color);
            return kingSquare.IsValid && IsSquareAttacked(board, kingSquare, color.Opponent());
        }

        public bool IsSquareAttacked(IBoard board, Square square, PieceColor byColor)
        {
            if (!square.IsValid)
            {
                return false;
            }

            return IsAttackedByPawn(board, square, byColor)
                   || IsAttackedByStepper(board, square, byColor, PieceType.Knight, Directions.Knight)
                   || IsAttackedByStepper(board, square, byColor, PieceType.King, Directions.All)
                   || IsAttackedBySlider(board, square, byColor, Directions.Orthogonal, PieceType.Rook)
                   || IsAttackedBySlider(board, square, byColor, Directions.Diagonal, PieceType.Bishop);
        }

        private static bool IsAttackedByPawn(IBoard board, Square square, PieceColor byColor)
        {
            // An enemy pawn attacking this square sits one rank *behind* it from the pawn's own
            // point of view, so step against the attacker's direction of travel.
            int rankDelta = -byColor.PawnDirection();

            for (int fileDelta = -1; fileDelta <= 1; fileDelta += 2)
            {
                Square origin = square.Offset(fileDelta, rankDelta);
                if (origin.IsValid && board[origin].Is(byColor, PieceType.Pawn))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAttackedByStepper(
            IBoard board,
            Square square,
            PieceColor byColor,
            PieceType pieceType,
            Direction[] steps)
        {
            for (int i = 0; i < steps.Length; i++)
            {
                Square origin = square.Offset(steps[i].FileDelta, steps[i].RankDelta);
                if (origin.IsValid && board[origin].Is(byColor, pieceType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAttackedBySlider(
            IBoard board,
            Square square,
            PieceColor byColor,
            Direction[] rays,
            PieceType lineSlider)
        {
            for (int i = 0; i < rays.Length; i++)
            {
                Direction ray = rays[i];
                Square current = square.Offset(ray.FileDelta, ray.RankDelta);

                while (current.IsValid)
                {
                    Piece occupant = board[current];
                    if (occupant.IsSome)
                    {
                        if (occupant.Is(byColor) &&
                            (occupant.Type == lineSlider || occupant.Type == PieceType.Queen))
                        {
                            return true;
                        }

                        break;
                    }

                    current = current.Offset(ray.FileDelta, ray.RankDelta);
                }
            }

            return false;
        }
    }
}
