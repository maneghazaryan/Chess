using Chess.Core.Board;
using Chess.Core.Primitives;

namespace Chess.AI.Ordering
{
    /// <summary>
    /// Sorts a move list so the most promising moves are searched first.
    /// </summary>
    /// <remarks>
    /// Alpha-beta only prunes once it has found a good move, so ordering is the single largest
    /// lever on search speed: the same search with good ordering visits roughly the square root
    /// of the nodes it would visit with bad ordering.
    /// </remarks>
    public interface IMoveOrderer
    {
        /// <summary>
        /// Reorders <paramref name="moves"/> in place.
        /// </summary>
        /// <param name="hashMove">
        /// The best move previously found for this position, from the transposition table or a
        /// shallower iteration. Tried first when valid.
        /// </param>
        /// <param name="ply">Distance from the root, used to index ply-local heuristics such as killer moves.</param>
        void Order(IBoard board, MoveList moves, int ply, in Move hashMove);

        /// <summary>Called on a beta cutoff so the orderer can learn which quiet moves refute positions.</summary>
        void OnBetaCutoff(IBoard board, in Move move, int ply, int depth);

        /// <summary>Clears any state carried between searches.</summary>
        void Reset();
    }
}
