using Chess.Core.Primitives;

namespace Chess.Core.Board
{
    /// <summary>
    /// Zobrist hashing: a position's hash is the XOR of one random 64-bit key per piece-on-square,
    /// plus keys for side to move, castling rights and the en passant file.
    /// </summary>
    /// <remarks>
    /// Because XOR is its own inverse, a move only has to XOR out what it removed and XOR in what
    /// it added, making the hash O(1) per move. That is what makes both the transposition table
    /// and repetition detection affordable inside the search.
    /// <para>
    /// Keys come from a fixed-seed SplitMix64 generator rather than <c>System.Random</c> so that
    /// hashes are identical across runs and platforms, which keeps failures reproducible.
    /// </para>
    /// </remarks>
    public static class Zobrist
    {
        private const ulong Seed = 0x9E3779B97F4A7C15UL;

        private static readonly ulong[,,] PieceSquareKeys = new ulong[2, 7, Square.Count];
        private static readonly ulong[] CastlingKeys = new ulong[16];
        private static readonly ulong[] EnPassantFileKeys = new ulong[Square.BoardSize];

        public static readonly ulong SideToMoveKey;

        static Zobrist()
        {
            ulong state = Seed;

            for (int color = 0; color < 2; color++)
            {
                for (int type = 1; type < 7; type++)
                {
                    for (int square = 0; square < Square.Count; square++)
                    {
                        PieceSquareKeys[color, type, square] = NextRandom(ref state);
                    }
                }
            }

            for (int i = 0; i < CastlingKeys.Length; i++)
            {
                CastlingKeys[i] = NextRandom(ref state);
            }

            for (int i = 0; i < EnPassantFileKeys.Length; i++)
            {
                EnPassantFileKeys[i] = NextRandom(ref state);
            }

            SideToMoveKey = NextRandom(ref state);
        }

        public static ulong PieceSquare(Piece piece, Square square)
        {
            if (piece.IsNone || !square.IsValid)
            {
                return 0UL;
            }

            return PieceSquareKeys[(int)piece.Color, (int)piece.Type, square.Index];
        }

        public static ulong Castling(CastlingRights rights) => CastlingKeys[(int)rights & 0xF];

        public static ulong EnPassant(Square target) =>
            target.IsValid ? EnPassantFileKeys[target.File] : 0UL;

        /// <summary>SplitMix64, chosen for having no weak seeds and passing the usual randomness batteries.</summary>
        private static ulong NextRandom(ref ulong state)
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
