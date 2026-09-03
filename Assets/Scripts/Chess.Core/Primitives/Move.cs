using System;

namespace Chess.Core.Primitives
{
    /// <summary>
    /// An origin square, a destination square and the flags describing what kind of move it is.
    /// Immutable and cheap to copy so it can be passed by value throughout the search.
    /// </summary>
    public readonly struct Move : IEquatable<Move>
    {
        public static readonly Move None = default;

        public Move(Square from, Square to, MoveFlags flags)
        {
            From = from;
            To = to;
            Flags = flags;
        }

        public Square From { get; }

        public Square To { get; }

        public MoveFlags Flags { get; }

        public bool IsValid => From.IsValid && To.IsValid;

        public bool IsCapture => (Flags & MoveFlags.Capture) != 0;

        public bool IsEnPassant => (Flags & MoveFlags.EnPassant) != 0;

        public bool IsCastle => (Flags & MoveFlags.Castle) != 0;

        public bool IsKingSideCastle => (Flags & MoveFlags.KingSideCastle) != 0;

        public bool IsQueenSideCastle => (Flags & MoveFlags.QueenSideCastle) != 0;

        public bool IsDoublePawnPush => (Flags & MoveFlags.DoublePawnPush) != 0;

        public bool IsPromotion => (Flags & MoveFlags.Promotion) != 0;

        public PieceType PromotionPieceType => Flags.ToPromotionPieceType();

        /// <summary>
        /// Captures and promotions change material, so they are the moves a quiescence search
        /// keeps exploring past the nominal horizon.
        /// </summary>
        public bool IsTactical => IsCapture || IsPromotion;

        public Move WithPromotion(PieceType promotionType)
        {
            MoveFlags withoutPromotion = Flags & ~MoveFlags.Promotion;
            return new Move(From, To, withoutPromotion | MoveFlagsExtensions.ToPromotionFlag(promotionType));
        }

        /// <summary>
        /// Long algebraic notation as used by UCI, for example "e2e4" or "e7e8q".
        /// Useful for logging and for expressing test fixtures compactly.
        /// </summary>
        public string ToUci()
        {
            if (!IsValid)
            {
                return "0000";
            }

            string text = $"{From}{To}";
            return IsPromotion ? text + char.ToLowerInvariant(PromotionPieceType.ToChar()) : text;
        }

        public bool Equals(Move other) => From == other.From && To == other.To && Flags == other.Flags;

        public override bool Equals(object obj) => obj is Move other && Equals(other);

        public override int GetHashCode()
        {
            return (From.Index + 1) | ((To.Index + 1) << 8) | ((int)Flags << 16);
        }

        public override string ToString() => ToUci();

        public static bool operator ==(Move left, Move right) => left.Equals(right);

        public static bool operator !=(Move left, Move right) => !left.Equals(right);
    }
}
