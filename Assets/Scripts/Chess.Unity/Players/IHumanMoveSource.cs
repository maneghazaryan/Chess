using System;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// Where a human player's moves come from.
    /// </summary>
    /// <remarks>
    /// Interposing this between <see cref="HumanPlayer"/> and the board interaction lets the
    /// player be tested without a scene, and would let moves arrive from somewhere else entirely
    /// (a text entry field, a network peer) without changing the turn loop.
    /// </remarks>
    public interface IHumanMoveSource
    {
        /// <summary>Raised with a fully resolved legal move, promotion choice included.</summary>
        event Action<Move> MoveChosen;

        /// <summary>
        /// Starts accepting input for the given colour. Returns a session id so a cancelled turn
        /// can end itself without disabling a newer one that has already begun.
        /// </summary>
        int BeginTurn(PieceColor color);

        /// <summary>Stops accepting input for <paramref name="turnSession"/>, if it is still current.</summary>
        void EndTurn(int turnSession);
    }
}
