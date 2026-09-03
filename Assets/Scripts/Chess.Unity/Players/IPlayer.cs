using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// A participant that supplies one move when asked.
    /// </summary>
    /// <remarks>
    /// Human and AI players are interchangeable behind this interface, which is what allows the
    /// turn loop in the controller to be written once and serve human-vs-human, human-vs-AI and
    /// AI-vs-AI without a single branch on game mode.
    /// </remarks>
    public interface IPlayer
    {
        PieceColor Color { get; }

        string DisplayName { get; }

        /// <summary>Drives UI affordances such as enabling board input and showing a thinking spinner.</summary>
        bool IsHuman { get; }

        /// <summary>
        /// Resolves with a legal move for <see cref="Color"/>. Must honour cancellation, which is
        /// raised when the game is restarted or torn down mid-turn.
        /// </summary>
        Task<Move> RequestMoveAsync(IChessGame game, CancellationToken cancellationToken);
    }
}
