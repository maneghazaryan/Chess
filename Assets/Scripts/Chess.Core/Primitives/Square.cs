using System;

namespace Chess.Core.Primitives
{
    /// <summary>
    /// A board square identified by a 0-63 index where 0 is a1 and 63 is h8
    /// (index = rank * 8 + file).
    /// </summary>
    /// <remarks>
    /// The index is stored biased by one so that <c>default(Square)</c> is <see cref="None"/>
    /// rather than a1, which removes a whole class of "forgot to initialise" bugs.
    /// </remarks>
    public readonly struct Square : IEquatable<Square>
    {
        public const int Count = 64;
        public const int BoardSize = 8;

        private readonly byte _biasedIndex;

        private Square(byte biasedIndex) => _biasedIndex = biasedIndex;

        public static readonly Square None = new Square(0);

        public bool IsValid => _biasedIndex != 0;

        /// <summary>0-63, or -1 when this is <see cref="None"/>.</summary>
        public int Index => _biasedIndex - 1;

        /// <summary>0-7, where 0 is the a-file.</summary>
        public int File => Index & 7;

        /// <summary>0-7, where 0 is rank 1.</summary>
        public int Rank => Index >> 3;

        /// <summary>True when the square is light-coloured (h1, a8 and their diagonals).</summary>
        public bool IsLight => ((File + Rank) & 1) != 0;

        public static Square FromIndex(int index)
        {
            return IsIndexOnBoard(index) ? new Square((byte)(index + 1)) : None;
        }

        public static Square FromFileRank(int file, int rank)
        {
            if (!IsCoordinateOnBoard(file) || !IsCoordinateOnBoard(rank))
            {
                return None;
            }

            return new Square((byte)(rank * BoardSize + file + 1));
        }

        /// <summary>
        /// Offsets by whole files and ranks. Returns <see cref="None"/> when the result leaves the
        /// board, which is what makes the mailbox generators safe without edge-guard tables.
        /// </summary>
        public Square Offset(int fileDelta, int rankDelta)
        {
            return IsValid ? FromFileRank(File + fileDelta, Rank + rankDelta) : None;
        }

        public static bool IsIndexOnBoard(int index) => index >= 0 && index < Count;

        public static bool IsCoordinateOnBoard(int coordinate) => coordinate >= 0 && coordinate < BoardSize;

        public static bool TryParse(string text, out Square square)
        {
            square = None;
            if (string.IsNullOrEmpty(text) || text.Length != 2)
            {
                return false;
            }

            int file = char.ToLowerInvariant(text[0]) - 'a';
            int rank = text[1] - '1';
            square = FromFileRank(file, rank);
            return square.IsValid;
        }

        public bool Equals(Square other) => _biasedIndex == other._biasedIndex;

        public override bool Equals(object obj) => obj is Square other && Equals(other);

        public override int GetHashCode() => _biasedIndex;

        public override string ToString()
        {
            return IsValid ? $"{(char)('a' + File)}{(char)('1' + Rank)}" : "-";
        }

        public static bool operator ==(Square left, Square right) => left._biasedIndex == right._biasedIndex;

        public static bool operator !=(Square left, Square right) => left._biasedIndex != right._biasedIndex;
    }
}
