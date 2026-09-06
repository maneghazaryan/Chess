using System.Collections.Generic;
using System.Text;
using Chess.Core.Game;
using Chess.Core.Primitives;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// The running score sheet, one row per full move: "1. e4 e5".
    /// </summary>
    /// <remarks>
    /// Rows are pooled and reused rather than instantiated per move. The view is scrolled to the
    /// bottom only when a move is appended, so a player who has scrolled back to review an earlier
    /// position is not yanked forward on every reply.
    /// </remarks>
    public sealed class MoveListView : MonoBehaviour
    {
        [Tooltip("Vertical layout group that rows are parented to.")]
        [SerializeField] private RectTransform _rowContainer;

        [Tooltip("Prefab carrying a MoveListEntry. A plain fallback row is built if this is empty.")]
        [SerializeField] private MoveListEntry _rowPrefab;

        [Tooltip("Optional. Scrolled to the bottom whenever a move is appended.")]
        [SerializeField] private ScrollRect _scrollRect;

        private readonly List<MoveListEntry> _rows = new List<MoveListEntry>();
        private readonly List<MoveRecord> _records = new List<MoveRecord>();

        public void Append(MoveRecord record)
        {
            _records.Add(record);
            Rebuild();
            ScrollToBottom();
        }

        public void RemoveLast()
        {
            if (_records.Count == 0)
            {
                return;
            }

            _records.RemoveAt(_records.Count - 1);
            Rebuild();
        }

        public void Clear()
        {
            _records.Clear();
            Rebuild();
        }

        internal void Bind(RectTransform rowContainer, ScrollRect scrollRect)
        {
            _rowContainer = rowContainer;
            _scrollRect = scrollRect;
        }

        /// <summary>The whole game as PGN-style movetext, for copying out or logging.</summary>
        public string ToMoveText()
        {
            var builder = new StringBuilder(_records.Count * 8);

            for (int i = 0; i < _records.Count; i++)
            {
                if (_records[i].MovingColor == PieceColor.White)
                {
                    builder.Append(_records[i].FullMoveNumber).Append(". ");
                }

                builder.Append(_records[i].StandardNotation).Append(' ');
            }

            return builder.ToString().TrimEnd();
        }

        private void Rebuild()
        {
            int rowCount = (_records.Count + 1) / 2;

            for (int row = 0; row < rowCount; row++)
            {
                int whiteIndex = row * 2;
                int blackIndex = whiteIndex + 1;

                RentRow(row).SetContent(
                    _records[whiteIndex].FullMoveNumber,
                    _records[whiteIndex].StandardNotation,
                    blackIndex < _records.Count ? _records[blackIndex].StandardNotation : string.Empty);
            }

            for (int row = rowCount; row < _rows.Count; row++)
            {
                _rows[row].gameObject.SetActive(false);
            }
        }

        private MoveListEntry RentRow(int index)
        {
            while (_rows.Count <= index)
            {
                _rows.Add(_rowPrefab != null
                    ? Instantiate(_rowPrefab, _rowContainer)
                    : MoveListEntry.CreateFallback(_rowContainer));
            }

            _rows[index].gameObject.SetActive(true);
            return _rows[index];
        }

        private void ScrollToBottom()
        {
            if (_scrollRect == null)
            {
                return;
            }

            // The layout group has not run yet on the frame a row is added, so the scroll position
            // would be computed against a stale content height without this.
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
