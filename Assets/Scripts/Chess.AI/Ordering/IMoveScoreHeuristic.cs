using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// One reason to try a move early, expressed as a score.
    /// </summary>
    /// <remarks>
    /// Heuristics are summed rather than consulted in sequence, so each occupies its own band of
    /// the score range (see <see cref="OrderingScores"/>) and a new one can be added without
    /// disturbing the relative order the existing ones produce.
    /// </remarks>
    public interface IMoveScoreHeuristic
    {
        int Score(IBoard board, in Move move, int ply);

        /// <summary>
        /// Notifies the heuristic that this move caused a beta cutoff, which is the signal that
        /// killer and history heuristics learn from.
        /// </summary>
        void OnBetaCutoff(IBoard board, in Move move, int ply, int depth);

        void Reset();
    }

    /// <summary>
    /// The score bands each heuristic occupies. Keeping them an order of magnitude apart means a
    /// captures score always outranks any history score, no matter how well trained.
    /// </summary>
    public static class OrderingScores
    {
        public const int HashMove = 10_000_000;
        public const int Tactical = 1_000_000;
        public const int PrimaryKiller = 900_000;
        public const int SecondaryKiller = 800_000;
        public const int MaxHistory = 700_000;
    }
}
