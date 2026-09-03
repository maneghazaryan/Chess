using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// The pawn: the only piece that captures differently from how it moves, that has a
    /// conditional double step, that can capture a square it did not move to (en passant), and
    /// that changes into another piece (promotion).
    /// </summary>
    public sealed class PawnMoveGenerator : IPieceMoveGenerator
    {
        public PieceType PieceType => PieceType.Pawn;

        public void GeneratePseudoLegalMoves(IBoard board, Square from, MoveGenerationMode mode, MoveList moves)
        {
            PieceColor mover = board[from].Color;
            int forward = mover.PawnDirection();
            int promotionRank = mover.PromotionRank();

            GeneratePushes(board, from, mover, forward, promotionRank, mode, moves);
            GenerateCaptures(board, from, mover, forward, promotionRank, moves);
        }

        private static void GeneratePushes(
            IBoard board,
            Square from,
            PieceColor mover,
            int forward,
            int promotionRank,
            MoveGenerationMode mode,
            MoveList moves)
        {
            Square oneStep = from.Offset(0, forward);
            if (!oneStep.IsValid || board[oneStep].IsSome)
            {
                return;
            }

            if (oneStep.Rank == promotionRank)
            {
                // A promotion changes material even without a capture, so a quiescence search
                // still wants to see it.
                AddPromotions(from, oneStep, MoveFlags.None, moves);
                return;
            }

            if (mode != MoveGenerationMode.All)
            {
                return;
            }

            moves.Add(new Move(from, oneStep, MoveFlags.Quiet));

            if (from.Rank != mover.PawnStartRank())
            {
                return;
            }

            Square twoSteps = from.Offset(0, forward * 2);
            if (twoSteps.IsValid && board[twoSteps].IsNone)
            {
                moves.Add(new Move(from, twoSteps, MoveFlags.Quiet | MoveFlags.DoublePawnPush));
            }
        }

        private static void GenerateCaptures(
            IBoard board,
            Square from,
            PieceColor mover,
            int forward,
            int promotionRank,
            MoveList moves)
        {
            for (int fileDelta = -1; fileDelta <= 1; fileDelta += 2)
            {
                Square target = from.Offset(fileDelta, forward);
                if (!target.IsValid)
                {
                    continue;
                }

                if (target == board.EnPassantTarget && board[target].IsNone)
                {
                    moves.Add(new Move(from, target, MoveFlags.Capture | MoveFlags.EnPassant));
                    continue;
                }

                Piece occupant = board[target];
                if (occupant.IsNone || occupant.Is(mover))
                {
                    continue;
                }

                if (target.Rank == promotionRank)
                {
                    AddPromotions(from, target, MoveFlags.Capture, moves);
                }
                else
                {
                    moves.Add(new Move(from, target, MoveFlags.Capture));
                }
            }
        }

        private static void AddPromotions(Square from, Square to, MoveFlags baseFlags, MoveList moves)
        {
            PieceType[] promotionTypes = MoveFlagsExtensions.PromotionPieceTypes;
            for (int i = 0; i < promotionTypes.Length; i++)
            {
                moves.Add(new Move(from, to, baseFlags | MoveFlagsExtensions.ToPromotionFlag(promotionTypes[i])));
            }
        }
    }
}
