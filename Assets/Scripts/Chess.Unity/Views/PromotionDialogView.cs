using System;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// The modal that asks which piece a promoting pawn becomes.
    /// </summary>
    /// <remarks>
    /// Exposed as an awaitable rather than a callback, so the interaction controller can express
    /// "get the move, asking about promotion if needed" as straight-line code instead of a state
    /// machine spread across two callbacks.
    /// </remarks>
    public sealed class PromotionDialogView : MonoBehaviour, IPromotionView
    {
        [Tooltip("Panel toggled on while a choice is pending. Defaults to this object.")]
        [SerializeField] private GameObject _root;

        [Tooltip("One button per promotion piece, in the order queen, rook, bishop, knight.")]
        [SerializeField] private Button[] _pieceButtons = new Button[4];

        [Tooltip("Icons on the buttons above, retinted to the promoting player's colour.")]
        [SerializeField] private Image[] _pieceIcons = new Image[4];

        [Tooltip("Optional. Cancels the promotion and returns the pawn to its square.")]
        [SerializeField] private Button _cancelButton;

        [SerializeField] private PieceSpriteSet _pieceSprites;

        private TaskCompletionSource<PieceType> _pendingChoice;
        private CancellationTokenRegistration _cancellationRegistration;

        public void Initialise(PieceSpriteSet pieceSprites)
        {
            _pieceSprites = pieceSprites != null ? pieceSprites : _pieceSprites;
        }

        public Task<PieceType> RequestPromotionAsync(PieceColor color, CancellationToken cancellationToken)
        {
            // A second request while one is open would strand the first task forever, so the
            // previous one is settled before the new one replaces it.
            Resolve(PieceType.None);

            var choice = new TaskCompletionSource<PieceType>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingChoice = choice;

            ApplyColor(color);
            SetRootActive(true);

            _cancellationRegistration = cancellationToken.Register(() =>
            {
                SetRootActive(false);
                choice.TrySetCanceled(cancellationToken);
            });

            return choice.Task;
        }

        public void Hide()
        {
            SetRootActive(false);
        }

        /// <summary>Wired to the buttons in the inspector, or bound automatically in <see cref="Awake"/>.</summary>
        public void Choose(int promotionIndex)
        {
            PieceType[] options = MoveFlagsExtensions.PromotionPieceTypes;
            if (promotionIndex < 0 || promotionIndex >= options.Length)
            {
                return;
            }

            Resolve(options[promotionIndex]);
        }

        public void Cancel() => Resolve(PieceType.None);

        private void Awake()
        {
            for (int i = 0; i < _pieceButtons.Length; i++)
            {
                if (_pieceButtons[i] == null)
                {
                    continue;
                }

                int index = i;
                _pieceButtons[i].onClick.AddListener(() => Choose(index));
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Cancel);
            }

            SetRootActive(false);
        }

        private void Resolve(PieceType choice)
        {
            if (_pendingChoice == null)
            {
                return;
            }

            SetRootActive(false);
            _cancellationRegistration.Dispose();
            _pendingChoice.TrySetResult(choice);
            _pendingChoice = null;
        }

        private void ApplyColor(PieceColor color)
        {
            if (_pieceSprites == null)
            {
                return;
            }

            PieceType[] options = MoveFlagsExtensions.PromotionPieceTypes;

            for (int i = 0; i < _pieceIcons.Length && i < options.Length; i++)
            {
                if (_pieceIcons[i] == null)
                {
                    continue;
                }

                Sprite sprite = _pieceSprites.Get(color, options[i]);
                _pieceIcons[i].sprite = sprite;
                _pieceIcons[i].enabled = sprite != null;
            }
        }

        private void SetRootActive(bool active)
        {
            GameObject target = _root != null ? _root : gameObject;
            target.SetActive(active);
        }
    }
}
