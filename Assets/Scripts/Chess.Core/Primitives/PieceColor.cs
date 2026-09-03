namespace Chess.Core.Primitives
{
    public enum PieceColor : byte
    {
        White = 0,
        Black = 1
    }

    public static class PieceColorExtensions
    {
        public static PieceColor Opponent(this PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }

        /// <summary>Rank index (0-7) that a pawn of this color starts on.</summary>
        public static int PawnStartRank(this PieceColor color)
        {
            return color == PieceColor.White ? 1 : 6;
        }

        /// <summary>Rank index (0-7) that a pawn of this color promotes on.</summary>
        public static int PromotionRank(this PieceColor color)
        {
            return color == PieceColor.White ? 7 : 0;
        }

        /// <summary>Number of ranks a pawn of this color advances per push.</summary>
        public static int PawnDirection(this PieceColor color)
        {
            return color == PieceColor.White ? 1 : -1;
        }
    }
}
