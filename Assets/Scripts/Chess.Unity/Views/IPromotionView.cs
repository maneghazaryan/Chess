using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Primitives;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Asks the user which piece a promoting pawn becomes.
    /// </summary>
    public interface IPromotionView
    {
        /// <summary>
        /// Resolves with the chosen piece type, or <see cref="PieceType.None"/> if the user
        /// cancelled, in which case the controller abandons the move.
        /// </summary>
        Task<PieceType> RequestPromotionAsync(PieceColor color, CancellationToken cancellationToken);

        void Hide();
    }
}
