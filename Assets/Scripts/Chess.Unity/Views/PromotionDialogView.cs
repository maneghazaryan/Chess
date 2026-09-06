using System;
using System.Threading;
using System.Threading.Tasks;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using TMPro;
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

        /// <summary>
        /// Builds a complete promotion dialog so the game is playable before a designed canvas exists.
        /// </summary>
        public static PromotionDialogView CreateDefault(Transform parent)
        {
            RectTransform overlay = UiFactory.CreateRect(parent, "PromotionDialog");
            UiFactory.StretchFill(overlay);
            overlay.gameObject.SetActive(false);
            UiFactory.PanelImage(overlay, new Color(0.05f, 0.04f, 0.03f, 0.55f));

            var view = overlay.gameObject.AddComponent<PromotionDialogView>();
            view._root = overlay.gameObject;

            RectTransform card = UiFactory.CreateRect(overlay, "Card");
            UiFactory.Stretch(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260f, -140f), new Vector2(260f, 140f));
            UiFactory.PanelImage(card, UiFactory.PanelSolid);
            UiFactory.Vertical(card, 12f, 16);

            TMP_Text title = UiFactory.Label(card, "Title", "Promote pawn", UiFactory.SizeHeading, TextAlignmentOptions.Center);
            UiFactory.Size(title, 0f, 44f);

            RectTransform pieces = UiFactory.CreateRect(card, "Pieces");
            UiFactory.Size(pieces, 0f, 88f);
            UiFactory.Horizontal(pieces, 10f, 0);

            view._pieceButtons = new Button[4];
            view._pieceIcons = new Image[4];
            string[] captions = { "Queen", "Rook", "Bishop", "Knight" };

            for (int i = 0; i < captions.Length; i++)
            {
                Button button = UiFactory.TextButton(pieces, captions[i], string.Empty, UiFactory.Button);
                UiFactory.Size(button, 0f, 88f).flexibleWidth = 1f;

                RectTransform iconRect = UiFactory.CreateRect(button.transform, "Icon");
                UiFactory.Stretch(iconRect, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), Vector2.zero, Vector2.zero);
                var icon = iconRect.gameObject.AddComponent<Image>();
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                view._pieceButtons[i] = button;
                view._pieceIcons[i] = icon;
            }

            view._cancelButton = UiFactory.TextButton(card, "Cancel", "Cancel", UiFactory.Button);
            UiFactory.Size(view._cancelButton, 0f, 40f);
            overlay.gameObject.SetActive(true);
            return view;
        }
    }
}
