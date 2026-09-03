using Chess.Core.Game;
using UnityEngine;

namespace Chess.Unity.Audio
{
    /// <summary>
    /// Plays the sound that matches what just happened on the board.
    /// </summary>
    /// <remarks>
    /// The choice of clip is a small piece of presentation logic and lives here so that neither
    /// the model nor the controller has to know a sound exists. Clips are optional; an unassigned
    /// one is simply silent.
    /// </remarks>
    public sealed class ChessAudioService : MonoBehaviour, IChessAudioService
    {
        [SerializeField] private AudioSource _source;

        [Header("Clips")]
        [SerializeField] private AudioClip _move;
        [SerializeField] private AudioClip _capture;
        [SerializeField] private AudioClip _castle;
        [SerializeField] private AudioClip _check;
        [SerializeField] private AudioClip _promotion;
        [SerializeField] private AudioClip _gameEnd;
        [SerializeField] private AudioClip _illegalMove;

        [SerializeField, Range(0f, 1f)] private float _volume = 0.8f;

        public void PlayMove(MoveRecord record)
        {
            // Ordered by how much the player needs to notice it: a check matters more than the
            // capture that delivered it, which matters more than the move itself.
            if (record.ResultingStatus == GameStatus.Check || record.ResultingStatus == GameStatus.Checkmate)
            {
                Play(_check);
                return;
            }

            if (record.Move.IsPromotion)
            {
                Play(_promotion != null ? _promotion : _move);
                return;
            }

            if (record.Move.IsCastle)
            {
                Play(_castle != null ? _castle : _move);
                return;
            }

            Play(record.IsCapture ? _capture : _move);
        }

        public void PlayGameEnd(GameResult result) => Play(_gameEnd);

        public void PlayIllegalMove() => Play(_illegalMove);

        private void Awake()
        {
            if (_source == null)
            {
                _source = GetComponent<AudioSource>();
            }

            if (_source == null)
            {
                _source = gameObject.AddComponent<AudioSource>();
                _source.playOnAwake = false;
            }
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && _source != null)
            {
                _source.PlayOneShot(clip, _volume);
            }
        }
    }
}
