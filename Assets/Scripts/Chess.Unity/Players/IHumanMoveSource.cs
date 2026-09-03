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

        /// <summary>Starts accepting input for the given colour.</summary>
        void BeginTurn(PieceColor color);

        /// <summary>Stops accepting input and clears any partial selection.</summary>
        void EndTurn();
    }
}
