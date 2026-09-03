using Chess.Core.Game;

namespace Chess.Unity.Audio
{
    public interface IChessAudioService
    {
        /// <summary>Picks the appropriate clip from the move itself: castle, capture, check or plain move.</summary>
        void PlayMove(MoveRecord record);

        void PlayGameEnd(GameResult result);

        void PlayIllegalMove();
    }
}
