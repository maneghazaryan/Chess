using Chess.Core.Primitives;

namespace Chess.Unity.Players
{
    /// <summary>
    /// Builds the pair of players for a match. Game mode is consumed here and nowhere else, so no
    /// downstream code needs to know whether an opponent is human.
    /// </summary>
    public interface IPlayerFactory
    {
        IPlayer Create(PieceColor color, in GameSetup setup);
    }
}
