using System;
using System.Threading;
using System.Threading.Tasks;
using Chess.AI.Evaluation;
using Chess.AI.Ordering;
using Chess.Core.Board;
using Chess.Core.Primitives;
using Chess.Core.Rules;

namespace Chess.AI.Search
{
    /// <summary>
    /// A negamax search with alpha-beta pruning, iterative deepening, a transposition table and a
    /// quiescence search at the leaves.
    /// </summary>
    /// <remarks>
    /// Negamax rather than separate maximising and minimising routines: because chess is
    /// zero-sum, the value of a position to one player is the negation of its value to the other,
    /// so one routine that negates the result of its recursive call does the work of two.
    /// <para>
    /// Alpha-beta prunes by tracking a window. Alpha is the best the side to move has already
    /// secured; beta is the best the opponent will allow. The moment a move scores at least beta,
    /// the opponent would never have permitted this position, so the remaining moves cannot
    /// matter and the loop exits. With good move ordering this cuts the effective branching
    /// factor from about 35 to about 6, which is worth roughly twice the search depth for the
    /// same time.
    /// </para>
    /// <para>
    /// This is fail-soft: it returns the best score actually found rather than clamping to the
    /// window, which gives the transposition table tighter bounds to store.
    /// </para>
    /// <para>
    /// Not thread-safe. One instance runs one search at a time; the transposition table and the
    /// ordering heuristics are shared mutable state deliberately, so that knowledge carries from
    /// one move to the next.
    /// </para>
    /// </remarks>
    public sealed class AlphaBetaSearch : IChessEngine
    {
        private readonly IMoveGenerator _moveGenerator;
        private readonly IAttackService _attackService;
        private readonly IMoveOrderer _moveOrderer;
        private readonly TranspositionTable _transpositionTable;
        private readonly QuiescenceSearch _quiescenceSearch;
        private readonly SearchDrawDetector _drawDetector = new SearchDrawDetector();

        private Move _rootBestMove;

        public AlphaBetaSearch(
            IMoveGenerator moveGenerator,
            IAttackService attackService,
            IPositionEvaluator evaluator,
            IMoveOrderer moveOrderer,
            TranspositionTable transpositionTable)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _attackService = attackService ?? throw new ArgumentNullException(nameof(attackService));
            _moveOrderer = moveOrderer ?? throw new ArgumentNullException(nameof(moveOrderer));
            _transpositionTable = transpositionTable ?? throw new ArgumentNullException(nameof(transpositionTable));

            if (evaluator == null)
            {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _quiescenceSearch = new QuiescenceSearch(moveGenerator, attackService, evaluator, moveOrderer);
        }

        /// <summary>Builds a search with the standard rule set, evaluation and ordering heuristics.</summary>
        public static AlphaBetaSearch CreateStandard(IPositionEvaluator evaluator = null)
        {
            var attackService = new AttackService();
            MoveGenerator moveGenerator = MoveGenerator.CreateStandard(attackService);

            return new AlphaBetaSearch(
                moveGenerator,
                attackService,
                evaluator ?? new TaperedEvaluator(),
                CompositeMoveOrderer.CreateStandard(),
                new TranspositionTable());
        }

        public string Name => "Alpha-Beta";

        public Task<SearchResult> FindBestMoveAsync(
            IBoard position,
            SearchLimits limits,
            CancellationToken cancellationToken)
        {
            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            // The working copy is taken here, on the calling thread, so the background task never
            // touches the position the rest of the application is reading.
            IMutableBoard workingCopy = position.CreateWorkingCopy();

            return Task.Run(() => Search(workingCopy, limits, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Runs the search synchronously on the caller's thread. Exposed for tests and for
        /// callers that already have a background thread.
        /// </summary>
        public SearchResult FindBestMove(IBoard position, SearchLimits limits, CancellationToken cancellationToken = default)
        {
            return Search(position.CreateWorkingCopy(), limits, cancellationToken);
        }

        public void ClearLearnedState()
        {
            _transpositionTable.Clear();
            _moveOrderer.Reset();
        }

        private SearchResult Search(IMutableBoard board, SearchLimits limits, CancellationToken cancellationToken)
        {
            var context = new SearchContext(board, limits.MaxTime, cancellationToken);

            Move bestMove = Move.None;
            int bestScore = 0;
            int depthReached = 0;

            // Iterative deepening. Re-searching from depth one looks wasteful, but the exponential
            // shape of the tree means the shallow passes cost almost nothing, and each one leaves
            // behind transposition entries that order the next pass so well that the deeper search
            // finishes faster than it would have alone. It also guarantees a usable move is always
            // available the moment the clock runs out.
            for (int depth = 1; depth <= limits.MaxDepth; depth++)
            {
                _rootBestMove = Move.None;

                int score = Negamax(
                    context,
                    depth,
                    ply: 0,
                    alpha: -EvaluationConstants.Infinity,
                    beta: EvaluationConstants.Infinity);

                if (context.IsAborted)
                {
                    break;
                }

                bestMove = _rootBestMove;
                bestScore = score;
                depthReached = depth;

                // A forced mate has been proven; searching deeper cannot improve on it.
                if (Math.Abs(score) > EvaluationConstants.MateThreshold)
                {
                    break;
                }
            }

            if (!bestMove.IsValid)
            {
                bestMove = FindAnyLegalMove(board);
            }

            return new SearchResult(bestMove, bestScore, depthReached, context.Nodes, context.Elapsed);
        }

        private int Negamax(SearchContext context, int depth, int ply, int alpha, int beta)
        {
            if (context.ShouldAbort())
            {
                return 0;
            }

            IMutableBoard board = context.Board;
            bool isRoot = ply == 0;

            if (!isRoot && _drawDetector.IsDraw(board))
            {
                return EvaluationConstants.DrawScore;
            }

            int originalAlpha = alpha;

            bool hasEntry = _transpositionTable.TryProbe(
                board.ZobristKey, depth, ply, alpha, beta, out int cachedScore, out Move hashMove);

            // The root must always produce a move, so a cutoff there is skipped even when the
            // table could supply the score.
            if (hasEntry && !isRoot)
            {
                return cachedScore;
            }

            if (depth <= 0)
            {
                return _quiescenceSearch.Search(context, ply, alpha, beta);
            }

            context.CountNode();

            MoveList moves = context.Pool.Rent(ply);
            _moveGenerator.GenerateLegalMoves(board, moves);

            if (moves.Count == 0)
            {
                // Mate scores carry the distance from the root, so a mate in three outranks a mate
                // in five and the engine plays the quickest win rather than dawdling.
                return _attackService.IsInCheck(board, board.SideToMove)
                    ? -EvaluationConstants.MateScore + ply
                    : EvaluationConstants.DrawScore;
            }

            _moveOrderer.Order(board, moves, ply, hashMove);

            int best = -EvaluationConstants.Infinity;
            Move bestMove = Move.None;

            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];

                board.MakeMove(move);
                int score = -Negamax(context, depth - 1, ply + 1, -beta, -alpha);
                board.UnmakeMove();

                if (context.IsAborted)
                {
                    return 0;
                }

                if (score > best)
                {
                    best = score;
                    bestMove = move;

                    if (isRoot)
                    {
                        _rootBestMove = move;
                    }
                }

                if (best > alpha)
                {
                    alpha = best;
                }

                if (best >= beta)
                {
                    _moveOrderer.OnBetaCutoff(board, move, ply, depth);
                    break;
                }
            }

            _transpositionTable.Store(board.ZobristKey, depth, ply, best, originalAlpha, beta, bestMove);

            return best;
        }

        /// <summary>
        /// A fallback for the case where the very first iteration was cancelled before completing
        /// a single root move. Returning something legal beats returning nothing.
        /// </summary>
        private Move FindAnyLegalMove(IMutableBoard board)
        {
            var moves = new MoveList();
            _moveGenerator.GenerateLegalMoves(board, moves);
            return moves.Count > 0 ? moves[0] : Move.None;
        }
    }
}
