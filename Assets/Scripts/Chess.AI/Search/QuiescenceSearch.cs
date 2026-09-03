using System;
using Chess.AI.Evaluation;
using Chess.AI.Ordering;
using Chess.Core.Board;
using Chess.Core.Primitives;
using Chess.Core.Rules;

namespace Chess.AI.Search
{
    /// <summary>
    /// Extends the search past its nominal depth until the position is quiet, so that leaves are
    /// only evaluated when no capture is hanging.
    /// </summary>
    /// <remarks>
    /// This exists to cure the horizon effect. A fixed-depth search that happens to stop right
    /// after "queen takes pawn" scores the position a pawn up, never seeing that the queen is
    /// recaptured on the very next ply. Evaluating only quiet positions removes that entire class
    /// of blunder, and it is the difference between an engine that plays chess and one that
    /// throws pieces away.
    /// <para>
    /// The search has no depth limit of its own; it terminates because captures run out. Two
    /// things keep that from exploding: standing pat, which prunes lines the side to move would
    /// simply decline, and delta pruning, which skips captures too small to rescue a lost
    /// position.
    /// </para>
    /// </remarks>
    public sealed class QuiescenceSearch
    {
        /// <summary>
        /// A capture is not worth searching if even winning that piece for free, plus a margin for
        /// positional compensation, would leave the score below alpha.
        /// </summary>
        private const int DeltaPruningMargin = 200;

        /// <summary>A backstop against pathological capture sequences; normal lines end long before this.</summary>
        private const int MaxQuiescencePly = 96;

        private readonly IMoveGenerator _moveGenerator;
        private readonly IAttackService _attackService;
        private readonly IPositionEvaluator _evaluator;
        private readonly IMoveOrderer _moveOrderer;

        public QuiescenceSearch(
            IMoveGenerator moveGenerator,
            IAttackService attackService,
            IPositionEvaluator evaluator,
            IMoveOrderer moveOrderer)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _attackService = attackService ?? throw new ArgumentNullException(nameof(attackService));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _moveOrderer = moveOrderer ?? throw new ArgumentNullException(nameof(moveOrderer));
        }

        public int Search(SearchContext context, int ply, int alpha, int beta)
        {
            if (context.ShouldAbort())
            {
                return 0;
            }

            context.CountNode();

            IMutableBoard board = context.Board;
            bool inCheck = _attackService.IsInCheck(board, board.SideToMove);

            int standPat;
            if (inCheck)
            {
                // Standing pat while in check would let the side to move "pass", which is not a
                // legal option and would hide forced mates. Every evasion must be searched.
                standPat = -EvaluationConstants.Infinity;
            }
            else
            {
                standPat = _evaluator.Evaluate(board);

                if (standPat >= beta)
                {
                    return standPat;
                }

                if (standPat > alpha)
                {
                    alpha = standPat;
                }
            }

            if (ply >= MaxQuiescencePly)
            {
                return inCheck ? _evaluator.Evaluate(board) : standPat;
            }

            MoveList moves = context.Pool.Rent(ply);
            _moveGenerator.GenerateLegalMoves(
                board,
                moves,
                inCheck ? MoveGenerationMode.All : MoveGenerationMode.TacticalOnly);

            if (moves.Count == 0)
            {
                // No evasion while in check is mate; no capture in a quiet position just means the
                // position is already quiet, so the stand-pat score stands.
                return inCheck ? -EvaluationConstants.MateScore + ply : standPat;
            }

            _moveOrderer.Order(board, moves, ply, Move.None);

            int best = standPat;

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];

                if (!inCheck && ShouldSkipByDelta(board, move, standPat, alpha))
                {
                    continue;
                }

                board.MakeMove(move);
                int score = -Search(context, ply + 1, -beta, -alpha);
                board.UnmakeMove();

                if (context.IsAborted)
                {
                    return 0;
                }

                if (score > best)
                {
                    best = score;
                }

                if (best > alpha)
                {
                    alpha = best;
                }

                if (best >= beta)
                {
                    break;
                }
            }

            return best;
        }

        private static bool ShouldSkipByDelta(IBoard board, in Move move, int standPat, int alpha)
        {
            // Promotions swing material by far more than the margin allows for, so they are never
            // pruned this way.
            if (move.IsPromotion || !move.IsCapture)
            {
                return false;
            }

            PieceType victim = move.IsEnPassant ? PieceType.Pawn : board[move.To].Type;
            int optimisticGain = EvaluationConstants.MidgameValue(victim) + DeltaPruningMargin;

            return standPat + optimisticGain < alpha;
        }
    }
}
