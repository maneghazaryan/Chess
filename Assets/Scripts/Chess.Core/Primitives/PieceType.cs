namespace Chess.Core.Primitives
{
    public enum PieceType : byte
    {
        None = 0,
        Pawn = 1,
        Knight = 2,
        Bishop = 3,
        Rook = 4,
        Queen = 5,
        King = 6
    }

    public static class PieceTypeExtensions
    {
        /// <summary>True for bishops, rooks and queens, which slide along rays until blocked.</summary>
        public static bool IsSliding(this PieceType type)
        {
            return type == PieceType.Bishop || type == PieceType.Rook || type == PieceType.Queen;
        }

        /// <summary>Upper-case letter used by FEN and SAN. Pawns have no SAN letter, so this returns 'P'.</summary>
        public static char ToChar(this PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return 'P';
                case PieceType.Knight: return 'N';
                case PieceType.Bishop: return 'B';
                case PieceType.Rook: return 'R';
                case PieceType.Queen: return 'Q';
                case PieceType.King: return 'K';
                default: return ' ';
            }
        }

        public static bool TryParse(char symbol, out PieceType type)
        {
            switch (char.ToUpperInvariant(symbol))
            {
                case 'P': type = PieceType.Pawn; return true;
                case 'N': type = PieceType.Knight; return true;
                case 'B': type = PieceType.Bishop; return true;
                case 'R': type = PieceType.Rook; return true;
                case 'Q': type = PieceType.Queen; return true;
                case 'K': type = PieceType.King; return true;
                default: type = PieceType.None; return false;
            }
        }
    }
}
