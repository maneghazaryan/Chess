using System.Collections.Generic;
using Chess.AI.Evaluation;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Shows the pieces one side has captured, plus the resulting material advantage.
    /// </summary>
    /// <remarks>
    /// Captured pieces are sorted by value so the row reads consistently rather than in the
    /// accidental order of the game, and only the leading side's advantage is labelled, which is
    /// the convention players expect from online boards.
    /// </remarks>
    public sealed class CapturedPiecesView : MonoBehaviour
    {
        [Tooltip("Layout group the captured piece icons are parented to.")]
        [SerializeField] private RectTransform _iconContainer;

        [Tooltip("Optional label showing the material advantage, for example \"+3\".")]
        [SerializeField] private TMP_Text _advantageLabel;

        [SerializeField] private PieceSpriteSet _pieceSprites;
        [SerializeField, Min(8f)] private float _iconSize = 28f;

        private readonly List<Image> _icons = new List<Image>();
        private readonly List<Piece> _sorted = new List<Piece>();

        public void Initialise(PieceSpriteSet pieceSprites)
        {
            _pieceSprites = pieceSprites != null ? pieceSprites : _pieceSprites;
        }

        internal void Bind(RectTransform iconContainer, TMP_Text advantageLabel)
        {
            _iconContainer = iconContainer;
            _advantageLabel = advantageLabel;
        }

        public void SetCapturedPieces(IReadOnlyList<Piece> captured, int materialAdvantage)
        {
            SortByValueDescending(captured);

            for (int i = 0; i < _sorted.Count; i++)
            {
                Image icon = RentIcon(i);
                icon.sprite = _pieceSprites != null ? _pieceSprites.Get(_sorted[i]) : null;
                icon.enabled = icon.sprite != null;
                icon.gameObject.SetActive(true);
            }

            for (int i = _sorted.Count; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
            }

            if (_advantageLabel != null)
            {
                _advantageLabel.text = materialAdvantage > 0 ? $"+{materialAdvantage}" : string.Empty;
            }
        }

        /// <summary>Material advantage in pawns, positive when <paramref name="color"/> is ahead.</summary>
        public static int MaterialAdvantageInPawns(
            IReadOnlyList<Piece> capturedByWhite,
            IReadOnlyList<Piece> capturedByBlack,
            PieceColor color)
        {
            int whiteGain = SumValues(capturedByWhite);
            int blackGain = SumValues(capturedByBlack);
            int difference = color == PieceColor.White ? whiteGain - blackGain : blackGain - whiteGain;

            return difference / EvaluationConstants.MidgameValue(PieceType.Pawn);
        }

        private static int SumValues(IReadOnlyList<Piece> pieces)
        {
            int total = 0;

            for (int i = 0; i < pieces.Count; i++)
            {
                total += EvaluationConstants.MidgameValue(pieces[i].Type);
            }

            return total;
        }

        private void SortByValueDescending(IReadOnlyList<Piece> captured)
        {
            _sorted.Clear();
            for (int i = 0; i < captured.Count; i++)
            {
                _sorted.Add(captured[i]);
            }

            _sorted.Sort((left, right) =>
                EvaluationConstants.MidgameValue(right.Type).CompareTo(EvaluationConstants.MidgameValue(left.Type)));
        }

        private Image RentIcon(int index)
        {
            while (_icons.Count <= index)
            {
                var host = new GameObject("CapturedPiece", typeof(RectTransform));
                host.transform.SetParent(_iconContainer != null ? _iconContainer : transform, false);

                var image = host.AddComponent<Image>();
                image.preserveAspect = true;

                var element = host.AddComponent<LayoutElement>();
                element.preferredWidth = _iconSize;
                element.preferredHeight = _iconSize;

                _icons.Add(image);
            }

            return _icons[index];
        }
    }
}
