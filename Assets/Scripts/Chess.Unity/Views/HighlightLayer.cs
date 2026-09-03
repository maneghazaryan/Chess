using System.Collections.Generic;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using UnityEngine;

namespace Chess.Unity.Views
{
    /// <summary>
    /// What a highlight is telling the player.
    /// </summary>
    public enum HighlightKind
    {
        Selection,
        LegalMove,
        CaptureTarget,
        LastMove,
        Check
    }

    /// <summary>
    /// Draws the overlays that tell the player what is selected, where it can go and whether a
    /// king is in check.
    /// </summary>
    /// <remarks>
    /// Highlights change several times per interaction, so the sprites are pooled and deactivated
    /// rather than destroyed and recreated.
    /// <para>
    /// They fall into two lifetimes. Selection and legal-move markers are transient: every click
    /// clears them. Last-move and check markers are persistent: they must survive the player
    /// picking up and putting down a piece, and are only cleared when the position itself changes.
    /// </para>
    /// </remarks>
    public sealed class HighlightLayer : MonoBehaviour
    {
        private struct ActiveHighlight
        {
            public SpriteRenderer Renderer;
            public Square Square;
            public HighlightKind Kind;
        }

        [SerializeField] private BoardTheme _theme;
        [SerializeField] private int _sortingOrder = 5;

        private readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        private readonly List<ActiveHighlight> _transient = new List<ActiveHighlight>();
        private readonly List<ActiveHighlight> _persistent = new List<ActiveHighlight>();

        private BoardGeometry _geometry;

        public void Initialise(BoardTheme theme, BoardGeometry geometry)
        {
            _theme = theme;
            _geometry = geometry;
        }

        public void SetGeometry(BoardGeometry geometry)
        {
            _geometry = geometry;
            Reposition(_transient);
            Reposition(_persistent);
        }

        /// <summary>Adds a marker that survives selection changes and is only removed by <see cref="ClearAll"/>.</summary>
        public void ShowPersistent(Square square, HighlightKind kind) => Show(square, kind, _persistent);

        /// <summary>Adds a marker that <see cref="ClearTransient"/> removes on the next click.</summary>
        public void ShowTransient(Square square, HighlightKind kind) => Show(square, kind, _transient);

        public void ClearTransient() => Release(_transient);

        public void ClearAll()
        {
            Release(_transient);
            Release(_persistent);
        }

        private void Show(Square square, HighlightKind kind, List<ActiveHighlight> tracker)
        {
            if (!square.IsValid || _theme == null)
            {
                return;
            }

            SpriteRenderer marker = Rent();
            marker.sprite = SpriteFor(kind);
            marker.color = ColorFor(kind);

            // The legal-move dot sits above the other overlays so it stays readable on top of a
            // last-move highlight.
            marker.sortingOrder = _sortingOrder + (kind == HighlightKind.LegalMove ? 1 : 0);

            Transform markerTransform = marker.transform;
            markerTransform.localPosition = _geometry.ToLocalPosition(square);
            markerTransform.localScale = ScaleFor(kind, marker.sprite);

            marker.gameObject.name = $"Highlight {kind} {square}";
            marker.gameObject.SetActive(true);

            tracker.Add(new ActiveHighlight { Renderer = marker, Square = square, Kind = kind });
        }

        private Sprite SpriteFor(HighlightKind kind)
        {
            switch (kind)
            {
                case HighlightKind.LegalMove: return _theme.LegalMoveDotSprite;
                case HighlightKind.CaptureTarget: return _theme.CaptureRingSprite;
                default: return _theme.SquareHighlightSprite;
            }
        }

        private Color ColorFor(HighlightKind kind)
        {
            switch (kind)
            {
                case HighlightKind.Selection: return _theme.Selection;
                case HighlightKind.LegalMove: return _theme.LegalMove;
                case HighlightKind.CaptureTarget: return _theme.CaptureTarget;
                case HighlightKind.LastMove: return _theme.LastMove;
                case HighlightKind.Check: return _theme.Check;
                default: return Color.white;
            }
        }

        private Vector3 ScaleFor(HighlightKind kind, Sprite sprite)
        {
            // The legal-move dot is drawn small so it reads as a marker on an empty square rather
            // than as something occupying it.
            float fraction = kind == HighlightKind.LegalMove ? 0.32f : 1f;
            float target = _geometry.SquareSize * fraction;

            if (sprite == null || sprite.bounds.size.x <= 0f || sprite.bounds.size.y <= 0f)
            {
                return Vector3.one * target;
            }

            return new Vector3(target / sprite.bounds.size.x, target / sprite.bounds.size.y, 1f);
        }

        private SpriteRenderer Rent()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (!_pool[i].gameObject.activeSelf)
                {
                    return _pool[i];
                }
            }

            var instance = new GameObject("Highlight", typeof(SpriteRenderer));
            instance.transform.SetParent(transform, false);
            instance.SetActive(false);

            var created = instance.GetComponent<SpriteRenderer>();
            _pool.Add(created);
            return created;
        }

        private static void Release(List<ActiveHighlight> tracker)
        {
            for (int i = 0; i < tracker.Count; i++)
            {
                tracker[i].Renderer.gameObject.SetActive(false);
            }

            tracker.Clear();
        }

        private void Reposition(List<ActiveHighlight> tracker)
        {
            // Flipping the board would otherwise leave every marker on the square it used to occupy.
            for (int i = 0; i < tracker.Count; i++)
            {
                ActiveHighlight highlight = tracker[i];
                highlight.Renderer.transform.localPosition = _geometry.ToLocalPosition(highlight.Square);
                highlight.Renderer.transform.localScale = ScaleFor(highlight.Kind, highlight.Renderer.sprite);
            }
        }
    }
}
