using System;
using System.Globalization;
using System.Text;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Notation
{
    /// <summary>
    /// Reads and writes Forsyth-Edwards Notation, the standard one-line description of a position.
    /// </summary>
    /// <remarks>
    /// Beyond loading the start position, FEN is what makes the rules testable: a perft or
    /// edge-case test can state its position in a single string instead of a page of setup code.
    /// </remarks>
    public static class FenSerializer
    {
        public const string StartPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public static ChessBoard Parse(string fen)
        {
            var board = new ChessBoard();
            Populate(board, fen);
            return board;
        }

        public static void Populate(ChessBoard board, string fen)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (string.IsNullOrWhiteSpace(fen))
            {
                throw new ArgumentException("FEN must not be empty.", nameof(fen));
            }

            string[] fields = fen.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 4)
            {
                throw new FormatException($"FEN needs at least four fields but had {fields.Length}: '{fen}'.");
            }

            board.Clear();

            ParsePiecePlacement(board, fields[0]);

            PieceColor sideToMove = ParseSideToMove(fields[1]);
            CastlingRights castlingRights = ParseCastlingRights(fields[2]);
            Square enPassant = ParseEnPassant(fields[3]);
            int halfMoveClock = fields.Length > 4 ? ParseInt(fields[4], 0) : 0;
            int fullMoveNumber = fields.Length > 5 ? ParseInt(fields[5], 1) : 1;

            board.CompleteSetup(sideToMove, castlingRights, enPassant, halfMoveClock, fullMoveNumber);
        }

        public static string Serialize(IBoard board)
        {
            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            var builder = new StringBuilder(80);

            for (int rank = Square.BoardSize - 1; rank >= 0; rank--)
            {
                int emptyRun = 0;

                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Piece piece = board[Square.FromFileRank(file, rank)];

                    if (piece.IsNone)
                    {
                        emptyRun++;
                        continue;
                    }

                    if (emptyRun > 0)
                    {
                        builder.Append(emptyRun);
                        emptyRun = 0;
                    }

                    builder.Append(piece.ToFenChar());
                }

                if (emptyRun > 0)
                {
                    builder.Append(emptyRun);
                }

                if (rank > 0)
                {
                    builder.Append('/');
                }
            }

            builder.Append(board.SideToMove == PieceColor.White ? " w " : " b ");
            builder.Append(SerializeCastlingRights(board.CastlingRights));
            builder.Append(' ');
            builder.Append(board.EnPassantTarget.IsValid ? board.EnPassantTarget.ToString() : "-");
            builder.Append(' ');
            builder.Append(board.HalfMoveClock.ToString(CultureInfo.InvariantCulture));
            builder.Append(' ');
            builder.Append(board.FullMoveNumber.ToString(CultureInfo.InvariantCulture));

            return builder.ToString();
        }

        private static void ParsePiecePlacement(ChessBoard board, string placement)
        {
            int rank = Square.BoardSize - 1;
            int file = 0;

            foreach (char symbol in placement)
            {
                if (symbol == '/')
                {
                    rank--;
                    file = 0;
                    continue;
                }

                if (char.IsDigit(symbol))
                {
                    file += symbol - '0';
                    continue;
                }

                if (!Piece.TryParseFenChar(symbol, out Piece piece))
                {
                    throw new FormatException($"'{symbol}' is not a valid FEN piece symbol.");
                }

                Square square = Square.FromFileRank(file, rank);
                if (!square.IsValid)
                {
                    throw new FormatException($"FEN piece placement describes a square off the board: '{placement}'.");
                }

                board.SetupPiece(square, piece);
                file++;
            }
        }

        private static PieceColor ParseSideToMove(string field)
        {
            switch (field)
            {
                case "w": return PieceColor.White;
                case "b": return PieceColor.Black;
                default: throw new FormatException($"'{field}' is not a valid side to move.");
            }
        }

        private static CastlingRights ParseCastlingRights(string field)
        {
            if (field == "-")
            {
                return CastlingRights.None;
            }

            CastlingRights rights = CastlingRights.None;

            foreach (char symbol in field)
            {
                switch (symbol)
                {
                    case 'K': rights |= CastlingRights.WhiteKingSide; break;
                    case 'Q': rights |= CastlingRights.WhiteQueenSide; break;
                    case 'k': rights |= CastlingRights.BlackKingSide; break;
                    case 'q': rights |= CastlingRights.BlackQueenSide; break;
                    default: throw new FormatException($"'{symbol}' is not a valid castling right.");
                }
            }

            return rights;
        }

        private static Square ParseEnPassant(string field)
        {
            if (field == "-")
            {
                return Square.None;
            }

            if (!Square.TryParse(field, out Square square))
            {
                throw new FormatException($"'{field}' is not a valid en passant square.");
            }

            return square;
        }

        private static int ParseInt(string field, int fallback)
        {
            return int.TryParse(field, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                ? value
                : fallback;
        }

        private static string SerializeCastlingRights(CastlingRights rights)
        {
            if (rights == CastlingRights.None)
            {
                return "-";
            }

            var builder = new StringBuilder(4);
            if (rights.Has(CastlingRights.WhiteKingSide)) builder.Append('K');
            if (rights.Has(CastlingRights.WhiteQueenSide)) builder.Append('Q');
            if (rights.Has(CastlingRights.BlackKingSide)) builder.Append('k');
            if (rights.Has(CastlingRights.BlackQueenSide)) builder.Append('q');
            return builder.ToString();
        }
    }
}
