using Chess.Core.Primitives;
using UnityEngine;

namespace Chess.Unity.Views
{
    /// <summary>
    /// One of the sixty-four board squares: a tinted sprite that knows which square it is.
    /// </summary>
    /// <remarks>
    /// Created at runtime by <see cref="BoardView"/> rather than placed by hand, because
    /// sixty-four hand-positioned objects would be sixty-four chances to typo a coordinate.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SquareView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;

        public Square Square { get; private set; }

        public void Initialise(Square square, Sprite sprite, Color color, float size, int sortingOrder)
        {
            Square = square;
            name = $"Square {square}";

            EnsureRenderer();
            _renderer.sprite = sprite;
            _renderer.color = color;
            _renderer.sortingOrder = sortingOrder;

            ScaleToSquare(sprite, size);
        }

        public void SetColor(Color color)
        {
            EnsureRenderer();
            _renderer.color = color;
        }

        /// <summary>
        /// Sizes the sprite to exactly one square regardless of its import settings, so an artist
        /// changing pixels-per-unit cannot silently break the board's alignment.
        /// </summary>
        private void ScaleToSquare(Sprite sprite, float size)
        {
            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            {
                transform.localScale = Vector3.one * size;
                return;
            }

            transform.localScale = new Vector3(
                size / sprite.bounds.size.x,
                size / sprite.bounds.size.y,
                1f);
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
