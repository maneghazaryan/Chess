using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// One row of the score sheet: a move number and the pair of half-moves that share it.
    /// </summary>
    public sealed class MoveListEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text _moveNumberLabel;
        [SerializeField] private TMP_Text _whiteMoveLabel;
        [SerializeField] private TMP_Text _blackMoveLabel;

        public void SetContent(int moveNumber, string whiteMove, string blackMove)
        {
            SetText(_moveNumberLabel, $"{moveNumber}.");
            SetText(_whiteMoveLabel, whiteMove);
            SetText(_blackMoveLabel, blackMove);
        }

        /// <summary>
        /// Builds a usable row from scratch when no prefab has been assigned, so the move list is
        /// legible before the UI art exists. Replace by assigning a designed prefab.
        /// </summary>
        public static MoveListEntry CreateFallback(RectTransform parent)
        {
            var host = new GameObject("MoveRow", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var layout = host.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var entry = host.AddComponent<MoveListEntry>();
            entry._moveNumberLabel = CreateLabel(host.transform, "Number", 56f);
            entry._whiteMoveLabel = CreateLabel(host.transform, "White", 110f);
            entry._blackMoveLabel = CreateLabel(host.transform, "Black", 110f);
            return entry;
        }

        private static TMP_Text CreateLabel(Transform parent, string labelName, float width)
        {
            var host = new GameObject(labelName, typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var label = host.AddComponent<TextMeshProUGUI>();
            label.fontSize = UiFactory.SizeCaption;
            label.color = UiFactory.Cream;

            var element = host.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = 36f;

            return label;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value;
            }
        }
    }
}
