using UnityEngine;

namespace Chess.Unity.Config
{
    /// <summary>
    /// The board's colours, highlight sprites and layout metrics.
    /// </summary>
    /// <remarks>
    /// Squares are tinted rather than textured, so a whole board palette is two colour fields and
    /// no art at all. Only the highlight overlays need sprites.
    /// </remarks>
    [CreateAssetMenu(fileName = "BoardTheme", menuName = "Chess/Board Theme")]
    public sealed class BoardTheme : ScriptableObject
    {
        [Header("Square colours")]
        [SerializeField] private Color _lightSquare = new Color(0.93f, 0.85f, 0.71f);
        [SerializeField] private Color _darkSquare = new Color(0.71f, 0.53f, 0.39f);

        [Header("Highlight colours")]
        [SerializeField] private Color _selection = new Color(1f, 0.85f, 0.25f, 0.55f);
        [SerializeField] private Color _legalMove = new Color(0.15f, 0.15f, 0.15f, 0.35f);
        [SerializeField] private Color _captureTarget = new Color(0.85f, 0.25f, 0.2f, 0.55f);
        [SerializeField] private Color _lastMove = new Color(0.95f, 0.9f, 0.35f, 0.35f);
        [SerializeField] private Color _check = new Color(0.9f, 0.15f, 0.1f, 0.7f);

        [Header("Sprites")]
        [Tooltip("A plain white square. Tinted per square, so one sprite serves the whole board.")]
        [SerializeField] private Sprite _squareSprite;

        [Tooltip("A filled circle, drawn on empty squares a selected piece may move to.")]
        [SerializeField] private Sprite _legalMoveDotSprite;

        [Tooltip("A hollow ring, drawn around opponent pieces that may be captured.")]
        [SerializeField] private Sprite _captureRingSprite;

        [Tooltip("A square outline or full square, used for selection, last move and check.")]
        [SerializeField] private Sprite _squareHighlightSprite;

        [Header("Layout")]
        [Tooltip("World-space edge length of a single square.")]
        [SerializeField, Min(0.01f)] private float _squareSize = 1f;

        [Tooltip("Seconds a piece takes to slide from one square to another.")]
        [SerializeField, Min(0f)] private float _moveAnimationDuration = 0.18f;

        [Tooltip("Eases the slide so pieces do not start and stop abruptly.")]
        [SerializeField]
        private AnimationCurve _moveAnimationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Color LightSquare => _lightSquare;

        public Color DarkSquare => _darkSquare;

        public Color Selection => _selection;

        public Color LegalMove => _legalMove;

        public Color CaptureTarget => _captureTarget;

        public Color LastMove => _lastMove;

        public Color Check => _check;

        public Sprite SquareSprite => _squareSprite;

        public Sprite LegalMoveDotSprite => _legalMoveDotSprite;

        public Sprite CaptureRingSprite => _captureRingSprite;

        public Sprite SquareHighlightSprite => _squareHighlightSprite;

        public float SquareSize => _squareSize;

        public float MoveAnimationDuration => _moveAnimationDuration;

        public AnimationCurve MoveAnimationCurve => _moveAnimationCurve;

        public Color ColorForSquare(bool isLight) => isLight ? _lightSquare : _darkSquare;
    }
}
