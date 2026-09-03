using System;
using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.Core.Rules
{
    /// <summary>
    /// Discards pseudo-legal moves that leave the mover's own king attacked, by playing each move
    /// and asking the question directly.
    /// </summary>
    /// <remarks>
    /// A pin-aware generator would avoid producing these moves at all and would be faster, but it
    /// is also where most engines' subtlest bugs live, especially around en passant discovered
    /// checks. Make/test/unmake is correct by construction for every case, and with a cheap
    /// <see cref="ChessBoard.UnmakeMove"/> it is affordable at the depths this engine searches.
    /// </remarks>
    public sealed class LegalityFilter : ILegalityFilter
    {
        private readonly IAttackService _attackService;

        public LegalityFilter(IAttackService attackService)
        {
            _attackService = attackService ?? throw new ArgumentNullException(nameof(attackService));
        }

        public void FilterInPlace(IMutableBoard board, MoveList moves)
        {
            int kept = 0;

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                if (!IsLegal(board, move))
                {
                    continue;
                }

                moves[kept] = move;
                kept++;
            }

            moves.Truncate(kept);
        }

        public bool IsLegal(IMutableBoard board, in Move move)
        {
            PieceColor mover = board.SideToMove;

            board.MakeMove(move);
            bool leavesKingInCheck = _attackService.IsInCheck(board, mover);
            board.UnmakeMove();

            return !leavesKingInCheck;
        }
    }
}
