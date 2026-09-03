using System;

namespace Chess.Core.Primitives
{
    /// <summary>
    /// A coloured chess piece packed into a single byte: bits 0-2 hold the <see cref="PieceType"/>,
    /// bit 3 holds the <see cref="PieceColor"/>. <c>default(Piece)</c> is the empty square.
    /// </summary>
    public readonly struct Piece : IEquatable<Piece>
    {
        private const int TypeMask = 0b0111;
        private const int ColorBit = 0b1000;

        private readonly byte _value;

        private Piece(byte value) => _value = value;

        public static readonly Piece None = new Piece(0);

        public static readonly Piece WhitePawn = Create(PieceColor.White, PieceType.Pawn);
        public static readonly Piece WhiteKnight = Create(PieceColor.White, PieceType.Knight);
        public static readonly Piece WhiteBishop = Create(PieceColor.White, PieceType.Bishop);
        public static readonly Piece WhiteRook = Create(PieceColor.White, PieceType.Rook);
        public static readonly Piece WhiteQueen = Create(PieceColor.White, PieceType.Queen);
        public static readonly Piece WhiteKing = Create(PieceColor.White, PieceType.King);

        public static readonly Piece BlackPawn = Create(PieceColor.Black, PieceType.Pawn);
        public static readonly Piece BlackKnight = Create(PieceColor.Black, PieceType.Knight);
        public static readonly Piece BlackBishop = Create(PieceColor.Black, PieceType.Bishop);
        public static readonly Piece BlackRook = Create(PieceColor.Black, PieceType.Rook);
        public static readonly Piece BlackQueen = Create(PieceColor.Black, PieceType.Queen);
        public static readonly Piece BlackKing = Create(PieceColor.Black, PieceType.King);

        public static Piece Create(PieceColor color, PieceType type)
        {
            if (type == PieceType.None)
            {
                return None;
            }

            int value = (int)type;
            if (color == PieceColor.Black)
            {
                value |= ColorBit;
            }

            return new Piece((byte)value);
        }

        public PieceType Type => (PieceType)(_value & TypeMask);

        public PieceColor Color => (_value & ColorBit) != 0 ? PieceColor.Black : PieceColor.White;

        public bool IsNone => (_value & TypeMask) == 0;

        public bool IsSome => (_value & TypeMask) != 0;

        public bool Is(PieceColor color) => IsSome && Color == color;

        public bool Is(PieceColor color, PieceType type) => Type == type && Color == color;

        /// <summary>FEN symbol: upper case for white, lower case for black, '.' for an empty square.</summary>
        public char ToFenChar()
        {
            if (IsNone)
            {
                return '.';
            }

            char symbol = Type.ToChar();
            return Color == PieceColor.White ? symbol : char.ToLowerInvariant(symbol);
        }

        public static bool TryParseFenChar(char symbol, out Piece piece)
        {
            if (!PieceTypeExtensions.TryParse(symbol, out PieceType type))
            {
                piece = None;
                return false;
            }

            PieceColor color = char.IsUpper(symbol) ? PieceColor.White : PieceColor.Black;
            piece = Create(color, type);
            return true;
        }

        public bool Equals(Piece other) => _value == other._value;

        public override bool Equals(object obj) => obj is Piece other && Equals(other);

        public override int GetHashCode() => _value;

        public override string ToString() => IsNone ? "-" : ToFenChar().ToString();

        public static bool operator ==(Piece left, Piece right) => left._value == right._value;

        public static bool operator !=(Piece left, Piece right) => left._value != right._value;
    }
}
