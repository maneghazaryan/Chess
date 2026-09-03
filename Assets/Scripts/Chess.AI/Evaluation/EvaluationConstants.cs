using Chess.Core.Primitives;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// Score scale and material weights. Everything is in centipawns, so a pawn is worth 100.
    /// </summary>
    public static class EvaluationConstants
    {
        /// <summary>Comfortably beyond any reachable evaluation, used as the initial alpha-beta window.</summary>
        public const int Infinity = 1_000_000;

        /// <summary>
        /// Score for being checkmated. Kept far below <see cref="Infinity"/> so that
        /// <c>MateScore + ply</c> can encode distance to mate without colliding with the window.
        /// </summary>
        public const int MateScore = 100_000;

        /// <summary>Any score this close to mate is a mate score rather than a normal evaluation.</summary>
        public const int MateThreshold = MateScore - 1000;

        public const int DrawScore = 0;

        /// <summary>
        /// Michniewski's simplified values. Bishops are rated ten centipawns above knights, which
        /// is what makes the engine keep the bishop pair without any explicit rule for it.
        /// </summary>
        private static readonly int[] MidgameValues =
        {
            0,      // None
            100,    // Pawn
            320,    // Knight
            330,    // Bishop
            500,    // Rook
            900,    // Queen
            0       // King: never captured, so counting it would only add a constant
        };

        /// <summary>
        /// Endgame weights. Pawns gain value as promotion becomes realistic; minor pieces lose a
        /// little as the board opens up.
        /// </summary>
        private static readonly int[] EndgameValues =
        {
            0,      // None
            120,    // Pawn
            310,    // Knight
            330,    // Bishop
            520,    // Rook
            930,    // Queen
            0       // King
        };

        public static int MidgameValue(PieceType type) => MidgameValues[(int)type];

        public static int EndgameValue(PieceType type) => EndgameValues[(int)type];

        /// <summary>
        /// Per-piece contribution to the game phase, following Fruit's scheme. Pawns contribute
        /// nothing because a position with only pawns is unambiguously an endgame.
        /// </summary>
        private static readonly int[] PhaseWeights =
        {
            0,  // None
            0,  // Pawn
            1,  // Knight
            1,  // Bishop
            2,  // Rook
            4,  // Queen
            0   // King
        };

        /// <summary>Phase weight of a full starting array: four minors, four rooks, two queens.</summary>
        public const int TotalPhase = 24;

        /// <summary>Phase is expressed on a 0-256 scale so interpolation is a shift rather than a divide.</summary>
        public const int PhaseScale = 256;

        public static int PhaseWeight(PieceType type) => PhaseWeights[(int)type];
    }
}
