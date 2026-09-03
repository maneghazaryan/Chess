using System;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// The king's eight steps, plus castling.
    /// </summary>
    /// <remarks>
    /// Castling is generated here rather than filtered later because its legality conditions are
    /// not about the king ending up in check: the king may not start in check, and may not pass
    /// through an attacked square, neither of which the generic legality filter would catch.
    /// </remarks>
    public sealed class KingMoveGenerator : StepMoveGeneratorBase
    {
        private readonly IAttackService _attackService;

        public KingMoveGenerator(IAttackService attackService)
        {
            _attackService = attackService ?? throw new ArgumentNullException(nameof(attackService));
        }

        public override PieceType PieceType => PieceType.King;

        protected override Direction[] Steps => Directions.All;

        public override void GeneratePseudoLegalMoves(IBoard board, Square from, MoveGenerationMode mode, MoveList moves)
        {
            base.GeneratePseudoLegalMoves(board, from, mode, moves);

            if (mode != MoveGenerationMode.All)
            {
                return;
            }

            PieceColor mover = board[from].Color;
            if (from != ChessBoard.GetKingStartSquare(mover))
            {
                return;
            }

            TryAddCastle(board, from, mover, kingSide: true, moves);
            TryAddCastle(board, from, mover, kingSide: false, moves);
        }

        private void TryAddCastle(IBoard board, Square kingSquare, PieceColor mover, bool kingSide, MoveList moves)
        {
            CastlingRights required = kingSide
                ? CastlingRightsExtensions.KingSideFor(mover)
                : CastlingRightsExtensions.QueenSideFor(mover);

            if (!board.CastlingRights.Has(required))
            {
                return;
            }

            ChessBoard.GetCastlingRookSquares(mover, kingSide, out Square rookFrom, out _);
            if (!board[rookFrom].Is(mover, PieceType.Rook))
            {
                return;
            }

            Square kingTo = ChessBoard.GetCastlingKingDestination(mover, kingSide);
            int step = kingSide ? 1 : -1;

            // Every square strictly between king and rook must be empty. On the queen's side that
            // includes the b-file square, which the king never visits.
            for (Square square = kingSquare.Offset(step, 0); square != rookFrom; square = square.Offset(step, 0))
            {
                if (board[square].IsSome)
                {
                    return;
                }
            }

            // The king may not be in check, pass through check, or land in check. The landing
            // square is re-verified here rather than left to the legality filter so that all three
            // conditions read together.
            PieceColor opponent = mover.Opponent();
            for (Square square = kingSquare; ; square = square.Offset(step, 0))
            {
                if (_attackService.IsSquareAttacked(board, square, opponent))
                {
                    return;
                }

                if (square == kingTo)
                {
                    break;
                }
            }

            MoveFlags flag = kingSide ? MoveFlags.KingSideCastle : MoveFlags.QueenSideCastle;
            moves.Add(new Move(kingSquare, kingTo, flag));
        }
    }
}
