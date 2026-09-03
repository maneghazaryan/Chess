using System.Collections;
using Chess.Core.Primitives;
using UnityEngine;

namespace Chess.Unity.Views
{
    /// <summary>
    /// A single piece sprite, able to slide from one square to another.
    /// </summary>
    /// <remarks>
    /// Holds no game state beyond which piece it is showing and which square it occupies. It never
    /// asks whether a move is legal; it is told to move and it moves.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PieceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        private Coroutine _activeAnimation;

        public Piece Piece { get; private set; }

        public Square Square { get; private set; }

        public bool IsAnimating => _activeAnimation != null;

        public void Initialise(Piece piece, Square square, Sprite sprite, Vector3 localPosition, float size, int sortingOrder)
        {
            Piece = piece;
            Square = square;
            name = $"{piece.Color} {piece.Type} {square}";

            EnsureRenderer();
            _renderer.sprite = sprite;
            _renderer.sortingOrder = sortingOrder;
            _renderer.color = Color.white;

            ScaleToSquare(sprite, size);
            transform.localPosition = localPosition;
        }

        /// <summary>Repoints an existing view at a different piece, used when a pawn promotes.</summary>
        public void SetPiece(Piece piece, Sprite sprite, float size)
        {
            Piece = piece;
            name = $"{piece.Color} {piece.Type} {Square}";

            EnsureRenderer();
            _renderer.sprite = sprite;
            ScaleToSquare(sprite, size);
        }

        public void SnapTo(Square square, Vector3 localPosition)
        {
            StopActiveAnimation();
            Square = square;
            transform.localPosition = localPosition;
        }

        public IEnumerator AnimateTo(Square square, Vector3 localPosition, float duration, AnimationCurve curve)
        {
            StopActiveAnimation();
            Square = square;

            if (duration <= 0f)
            {
                transform.localPosition = localPosition;
                yield break;
            }

            _activeAnimation = StartCoroutine(SlideTo(localPosition, duration, curve));
            yield return _activeAnimation;
        }

        public void SetSortingOrder(int order)
        {
            EnsureRenderer();
            _renderer.sortingOrder = order;
        }

        private IEnumerator SlideTo(Vector3 target, float duration, AnimationCurve curve)
        {
            Vector3 start = transform.localPosition;
            float elapsed = 0f;

            // Unscaled time, so pausing the game or slowing it for an effect does not leave a
            // piece stranded between squares.
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.localPosition = Vector3.LerpUnclamped(start, target, curve?.Evaluate(t) ?? t);
                yield return null;
            }

            transform.localPosition = target;
            _activeAnimation = null;
        }

        private void StopActiveAnimation()
        {
            if (_activeAnimation == null)
            {
                return;
            }

            StopCoroutine(_activeAnimation);
            _activeAnimation = null;
        }

        private void ScaleToSquare(Sprite sprite, float size)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            {
                transform.localScale = Vector3.one * size;
                return;
            }

            float uniform = size / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            transform.localScale = new Vector3(uniform, uniform, 1f);
        }

        private void EnsureRenderer()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Reset() => _renderer = GetComponent<SpriteRenderer>();
    }
}
