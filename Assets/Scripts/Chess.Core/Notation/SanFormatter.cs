using System.Collections.Generic;
using System.Text;
using Chess.Core.Board;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Core.Notation
{
    /// <summary>
    /// Formats moves in Standard Algebraic Notation, the notation used in the move list.
    /// </summary>
    /// <remarks>
    /// SAN names a move by its destination and adds only as much about the origin as is needed to
    /// make it unambiguous, so formatting one move requires knowing every legal move in the
    /// position. It is therefore split in two: <see cref="Format"/> runs before the move is
    /// played, while it still has that context, and <see cref="AppendStatusSuffix"/> runs after,
    /// once the resulting check or mate is known.
    /// </remarks>
    public static class SanFormatter
    {
        public const string KingSideCastle = "O-O";
        public const string QueenSideCastle = "O-O-O";

        /// <summary>
        /// Formats <paramref name="move"/> against the position it is played in. Both
        /// <paramref name="board"/> and <paramref name="legalMoves"/> must describe the position
        /// before the move is made.
        /// </summary>
        public static string Format(IBoard board, in Move move, IReadOnlyList<Move> legalMoves)
        {
            if (move.IsKingSideCastle)
            {
                return KingSideCastle;
            }

            if (move.IsQueenSideCastle)
            {
                return QueenSideCastle;
            }

            Piece moving = board[move.From];
            var builder = new StringBuilder(8);

            if (moving.Type == PieceType.Pawn)
            {
                AppendPawnMove(builder, move);
            }
            else
            {
                builder.Append(moving.Type.ToChar());
                builder.Append(ResolveDisambiguation(board, move, moving, legalMoves));

                if (move.IsCapture)
                {
                    builder.Append('x');
                }

                builder.Append(move.To);
            }

            if (move.IsPromotion)
            {
                builder.Append('=');
                builder.Append(move.PromotionPieceType.ToChar());
            }

            return builder.ToString();
        }

        public static string AppendStatusSuffix(string san, GameStatus status)
        {
            switch (status)
            {
                case GameStatus.Checkmate: return san + "#";
                case GameStatus.Check: return san + "+";
                default: return san;
            }
        }

        private static void AppendPawnMove(StringBuilder builder, in Move move)
        {
            // A capturing pawn is identified by the file it came from, as in "exd5".
            if (move.IsCapture)
            {
                builder.Append((char)('a' + move.From.File));
                builder.Append('x');
            }

            builder.Append(move.To);
        }

        private static string ResolveDisambiguation(
            IBoard board,
            in Move move,
            Piece moving,
            IReadOnlyList<Move> legalMoves)
        {
            bool anyRival = false;
            bool sharesFile = false;
            bool sharesRank = false;

            for (int i = 0; i < legalMoves.Count; i++)
            {
                Move candidate = legalMoves[i];

                if (candidate.To != move.To || candidate.From == move.From)
                {
                    continue;
                }

                Piece other = board[candidate.From];
                if (other.Type != moving.Type || other.Color != moving.Color)
                {
                    continue;
                }

                anyRival = true;
                sharesFile |= candidate.From.File == move.From.File;
                sharesRank |= candidate.From.Rank == move.From.Rank;
            }

            if (!anyRival)
            {
                return string.Empty;
            }

            // The file alone suffices unless another candidate shares it; then the rank; and only
            // when both are shared does the full square get spelled out.
            if (!sharesFile)
            {
                return ((char)('a' + move.From.File)).ToString();
            }

            if (!sharesRank)
            {
                return ((char)('1' + move.From.Rank)).ToString();
            }

            return move.From.ToString();
        }
    }
}
