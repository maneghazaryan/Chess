using System;

namespace Chess.Core.Primitives
{
    [Flags]
    public enum MoveFlags : ushort
    {
        None = 0,
        Quiet = 1 << 0,
        Capture = 1 << 1,
        DoublePawnPush = 1 << 2,
        EnPassant = 1 << 3,
        KingSideCastle = 1 << 4,
        QueenSideCastle = 1 << 5,
        PromoteKnight = 1 << 6,
        PromoteBishop = 1 << 7,
        PromoteRook = 1 << 8,
        PromoteQueen = 1 << 9,

        Castle = KingSideCastle | QueenSideCastle,
        Promotion = PromoteKnight | PromoteBishop | PromoteRook | PromoteQueen
    }

    public static class MoveFlagsExtensions
    {
        public static MoveFlags ToPromotionFlag(PieceType type)
        {
            switch (type)
            {
                case PieceType.Knight: return MoveFlags.PromoteKnight;
                case PieceType.Bishop: return MoveFlags.PromoteBishop;
                case PieceType.Rook: return MoveFlags.PromoteRook;
                case PieceType.Queen: return MoveFlags.PromoteQueen;
                default: return MoveFlags.None;
            }
        }

        public static PieceType ToPromotionPieceType(this MoveFlags flags)
        {
            if ((flags & MoveFlags.PromoteQueen) != 0) return PieceType.Queen;
            if ((flags & MoveFlags.PromoteRook) != 0) return PieceType.Rook;
            if ((flags & MoveFlags.PromoteBishop) != 0) return PieceType.Bishop;
            if ((flags & MoveFlags.PromoteKnight) != 0) return PieceType.Knight;
            return PieceType.None;
        }

        /// <summary>The four pieces a pawn may promote to, ordered strongest first.</summary>
        public static readonly PieceType[] PromotionPieceTypes =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };
    }
}
