using Chess.Core.Primitives;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Positional bonuses and penalties per piece per square, from Tomasz Michniewski's simplified
    /// evaluation function.
    /// </summary>
    /// <remarks>
    /// These tables stand in for chess knowledge the engine does not have. Knights are pushed
    /// towards the centre and away from the rim, bishops away from the corners, rooks onto the
    /// seventh rank and open files, and the king behind its castled pawn shelter during the
    /// middlegame but into the centre once the queens come off.
    /// <para>
    /// The literals below are written the way a board is drawn, with rank 8 on the first line and
    /// the a-file on the left. The static constructor flips them into square-index order and
    /// mirrors them for black, so nothing downstream has to think about orientation.
    /// </para>
    /// <para>
    /// Michniewski specifies only one set of tables plus a separate king endgame table. The pawn
    /// endgame table here is the usual extension: once few pieces remain, how far a pawn has
    /// advanced matters far more than which file it sits on.
    /// </para>
    /// </remarks>
    public static class PieceSquareTables
    {
        private static readonly int[] PawnMidgame =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
             5,  5, 10, 25, 25, 10,  5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5, -5,-10,  0,  0,-10, -5,  5,
             5, 10, 10,-20,-20, 10, 10,  5,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] PawnEndgame =
        {
             0,  0,  0,  0,  0,  0,  0,  0,
            80, 80, 80, 80, 80, 80, 80, 80,
            50, 50, 50, 50, 50, 50, 50, 50,
            30, 30, 30, 30, 30, 30, 30, 30,
            20, 20, 20, 20, 20, 20, 20, 20,
            10, 10, 10, 10, 10, 10, 10, 10,
            10, 10, 10, 10, 10, 10, 10, 10,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        private static readonly int[] Knight =
        {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        private static readonly int[] Bishop =
        {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        private static readonly int[] Rook =
        {
              0,  0,  0,  0,  0,  0,  0,  0,
              5, 10, 10, 10, 10, 10, 10,  5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
             -5,  0,  0,  0,  0,  0,  0, -5,
              0,  0,  0,  5,  5,  0,  0,  0
        };

        private static readonly int[] Queen =
        {
            -20,-10,-10, -5, -5,-10,-10,-20,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -10,  0,  5,  5,  5,  5,  0,-10,
             -5,  0,  5,  5,  5,  5,  0, -5,
              0,  0,  5,  5,  5,  5,  0, -5,
            -10,  5,  5,  5,  5,  5,  0,-10,
            -10,  0,  5,  0,  0,  0,  0,-10,
            -20,-10,-10, -5, -5,-10,-10,-20
        };

        private static readonly int[] KingMidgame =
        {
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -10,-20,-20,-20,-20,-20,-20,-10,
             20, 20,  0,  0,  0,  0, 20, 20,
             20, 30, 10,  0,  0, 10, 30, 20
        };

        private static readonly int[] KingEndgame =
        {
            -50,-40,-30,-20,-20,-30,-40,-50,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -50,-30,-30,-30,-30,-30,-30,-50
        };

        // Indexed [gamePhase][pieceType][color][squareIndex], resolved once at type load.
        private static readonly int[][][] MidgameLookup = BuildLookup(endgame: false);
        private static readonly int[][][] EndgameLookup = BuildLookup(endgame: true);

        public static int Midgame(PieceType type, PieceColor color, Square square) =>
            MidgameLookup[(int)type][(int)color][square.Index];

        public static int Endgame(PieceType type, PieceColor color, Square square) =>
            EndgameLookup[(int)type][(int)color][square.Index];

        private static int[][][] BuildLookup(bool endgame)
        {
            var lookup = new int[7][][];

            for (int type = 0; type < 7; type++)
            {
                int[] source = SourceTable((PieceType)type, endgame);
                lookup[type] = new[]
                {
                    Orient(source, PieceColor.White),
                    Orient(source, PieceColor.Black)
                };
            }

            return lookup;
        }

        private static int[] SourceTable(PieceType type, bool endgame)
        {
            switch (type)
            {
                case PieceType.Pawn: return endgame ? PawnEndgame : PawnMidgame;
                case PieceType.Knight: return Knight;
                case PieceType.Bishop: return Bishop;
                case PieceType.Rook: return Rook;
                case PieceType.Queen: return Queen;
                case PieceType.King: return endgame ? KingEndgame : KingMidgame;
                default: return new int[Square.Count];
            }
        }

        /// <summary>
        /// Converts a table written rank-8-first into square-index order. White reads it flipped
        /// vertically; black reads it as written, which mirrors it about the centre rank.
        /// </summary>
        private static int[] Orient(int[] source, PieceColor color)
        {
            var oriented = new int[Square.Count];

            for (int rank = 0; rank < Square.BoardSize; rank++)
            {
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    int sourceRow = color == PieceColor.White ? Square.BoardSize - 1 - rank : rank;
                    oriented[rank * Square.BoardSize + file] = source[sourceRow * Square.BoardSize + file];
                }
            }

            return oriented;
        }
    }
}
