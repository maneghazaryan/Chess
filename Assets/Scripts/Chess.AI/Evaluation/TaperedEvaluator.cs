using System.Collections.Generic;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Evaluation
{
    /// <summary>
    /// The full evaluation: material and piece placement, scored twice, once with middlegame
    /// weights and once with endgame weights, then blended by how much material is left.
    /// </summary>
    /// <remarks>
    /// Tapering exists to remove a discontinuity. With a single set of weights the engine's idea
    /// of a good king square flips the instant some arbitrary threshold is crossed, and it will
    /// happily trade into an endgame it then misevaluates. Interpolating on a 0-256 phase scale
    /// makes that transition smooth, so the engine starts walking its king towards the centre
    /// gradually as the queens and rooks come off.
    /// <para>
    /// Both halves are accumulated in a single pass over the occupied squares, because evaluation
    /// runs at every leaf of the search and is the second-hottest code path after move generation.
    /// </para>
    /// </remarks>
    public sealed class TaperedEvaluator : IPositionEvaluator
    {
        public int Evaluate(IBoard board)
        {
            int midgame = 0;
            int endgame = 0;
            int phaseRemaining = EvaluationConstants.TotalPhase;

            Accumulate(board, PieceColor.White, ref midgame, ref endgame, ref phaseRemaining, sign: 1);
            Accumulate(board, PieceColor.Black, ref midgame, ref endgame, ref phaseRemaining, sign: -1);

            int phase = ToPhaseScale(phaseRemaining);
            int whiteRelative =
                (midgame * (EvaluationConstants.PhaseScale - phase) + endgame * phase)
                / EvaluationConstants.PhaseScale;

            return board.SideToMove == PieceColor.White ? whiteRelative : -whiteRelative;
        }

        private static void Accumulate(
            IBoard board,
            PieceColor color,
            ref int midgame,
            ref int endgame,
            ref int phaseRemaining,
            int sign)
        {
            IReadOnlyList<Square> squares = board.GetOccupiedSquares(color);

            for (int i = 0; i < squares.Count; i++)
            {
                Square square = squares[i];
                PieceType type = board[square].Type;

                midgame += sign * (EvaluationConstants.MidgameValue(type)
                                   + PieceSquareTables.Midgame(type, color, square));

                endgame += sign * (EvaluationConstants.EndgameValue(type)
                                   + PieceSquareTables.Endgame(type, color, square));

                phaseRemaining -= EvaluationConstants.PhaseWeight(type);
            }
        }

        /// <summary>
        /// Maps remaining non-pawn material to 0 (full starting material, pure middlegame) through
        /// 256 (bare kings, pure endgame). Clamped because promotions can put more material on the
        /// board than the game started with.
        /// </summary>
        private static int ToPhaseScale(int phaseRemaining)
        {
            if (phaseRemaining < 0)
            {
                phaseRemaining = 0;
            }

            return (phaseRemaining * EvaluationConstants.PhaseScale
                    + EvaluationConstants.TotalPhase / 2)
                   / EvaluationConstants.TotalPhase;
        }
    }
}
