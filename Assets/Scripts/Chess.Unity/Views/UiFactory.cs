using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Shared runtime UI construction used by the default view builders and the editor setup.
    /// </summary>
    public static class UiFactory
    {
        public static readonly Color Panel = new Color(0.14f, 0.11f, 0.09f, 0.92f);
        public static readonly Color PanelSolid = new Color(0.16f, 0.13f, 0.10f, 0.97f);
        public static readonly Color Button = new Color(0.42f, 0.30f, 0.18f, 1f);
        public static readonly Color ButtonAccent = new Color(0.62f, 0.42f, 0.18f, 1f);
        public static readonly Color Cream = new Color(0.96f, 0.91f, 0.80f, 1f);
        public static readonly Color Muted = new Color(0.82f, 0.76f, 0.66f, 1f);

        public static RectTransform CreateRect(Transform parent, string objectName)
        {
            var host = new GameObject(objectName, typeof(RectTransform));
            host.transform.SetParent(parent, false);
            return (RectTransform)host.transform;
        }

        public static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        public static void StretchFill(RectTransform rect) =>
            Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        public static Image PanelImage(RectTransform rect, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static TMP_Text Label(Transform parent, string objectName, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft)
        {
            RectTransform rect = CreateRect(parent, objectName);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = Cream;
            label.alignment = align;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        public static Button TextButton(Transform parent, string objectName, string caption, Color color)
        {
            RectTransform rect = CreateRect(parent, objectName);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.18f);
            button.colors = colors;

            TMP_Text label = Label(rect, "Label", caption, 22f, TextAlignmentOptions.Center);
            StretchFill((RectTransform)label.transform);
            return button;
        }

        public static Toggle TextToggle(Transform parent, string objectName, string caption)
        {
            RectTransform rect = CreateRect(parent, objectName);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Button;

            var toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = image;
            toggle.isOn = false;

            TMP_Text label = Label(rect, "Label", caption, 20f, TextAlignmentOptions.Center);
            StretchFill((RectTransform)label.transform);

            toggle.onValueChanged.AddListener(on =>
            {
                image.color = on ? ButtonAccent : Button;
            });

            return toggle;
        }

        public static LayoutElement Size(Component component, float width, float height)
        {
            var element = component.gameObject.AddComponent<LayoutElement>();
            if (width > 0f)
            {
                element.preferredWidth = width;
                element.minWidth = width;
            }

            if (height > 0f)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }

            return element;
        }

        public static HorizontalLayoutGroup Horizontal(RectTransform rect, float spacing, int padding)
        {
            var group = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static VerticalLayoutGroup Vertical(RectTransform rect, float spacing, int padding)
        {
            var group = rect.gameObject.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = new RectOffset(padding, padding, padding, padding);
            group.childAlignment = TextAnchor.UpperCenter;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static ScrollRect VerticalScroll(Transform parent, string objectName, out RectTransform content)
        {
            RectTransform root = CreateRect(parent, objectName);
            PanelImage(root, new Color(0.10f, 0.08f, 0.06f, 0.55f));

            var scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;

            RectTransform viewport = CreateRect(root, "Viewport");
            StretchFill(viewport);
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = viewport;

            content = CreateRect(viewport, "Content");
            Stretch(content, new Vector2(0f, 1f), Vector2.one, Vector2.zero, Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Vertical(content, 2f, 6);

            scroll.content = content;
            return scroll;
        }

        public static TMP_Dropdown Dropdown(Transform parent, string objectName)
        {
            RectTransform root = CreateRect(parent, objectName);
            var background = root.gameObject.AddComponent<Image>();
            background.color = Button;

            var dropdown = root.gameObject.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = background;

            TMP_Text caption = Label(root, "Label", "Medium", 20f, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)caption.transform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-28f, 0f));

            TMP_Text arrow = Label(root, "Arrow", "▾", 18f, TextAlignmentOptions.Center);
            Stretch((RectTransform)arrow.transform, new Vector2(1f, 0f), Vector2.one, new Vector2(-28f, 0f), Vector2.zero);

            RectTransform template = CreateRect(root, "Template");
            Stretch(template, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -160f), new Vector2(0f, 0f));
            template.pivot = new Vector2(0.5f, 1f);
            PanelImage(template, PanelSolid);
            template.gameObject.SetActive(false);

            var scroll = template.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            RectTransform viewport = CreateRect(template, "Viewport");
            StretchFill(viewport);
            viewport.gameObject.AddComponent<Image>().color = Color.white;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            RectTransform content = CreateRect(viewport, "Content");
            Stretch(content, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, -32f), Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);
            var contentFit = content.gameObject.AddComponent<ContentSizeFitter>();
            contentFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Vertical(content, 0f, 0);

            RectTransform item = CreateRect(content, "Item");
            item.anchorMin = new Vector2(0f, 0.5f);
            item.anchorMax = new Vector2(1f, 0.5f);
            item.sizeDelta = new Vector2(0f, 32f);
            Size(item, 0f, 32f);
            var toggle = item.gameObject.AddComponent<Toggle>();

            RectTransform itemBackground = CreateRect(item, "Item Background");
            StretchFill(itemBackground);
            toggle.targetGraphic = PanelImage(itemBackground, Button);

            RectTransform checkmark = CreateRect(item, "Item Checkmark");
            Stretch(checkmark, Vector2.zero, new Vector2(0f, 1f), new Vector2(6f, 6f), new Vector2(22f, -6f));
            var checkImage = PanelImage(checkmark, Cream);
            toggle.graphic = checkImage;

            TMP_Text itemLabel = Label(item, "Item Label", "Option", 18f, TextAlignmentOptions.MidlineLeft);
            Stretch((RectTransform)itemLabel.transform, Vector2.zero, Vector2.one, new Vector2(28f, 0f), new Vector2(-8f, 0f));

            scroll.viewport = viewport;
            scroll.content = content;
            dropdown.template = template;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            return dropdown;
        }

        public static Canvas OverlayCanvas(string objectName)
        {
            var host = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = host.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = host.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }
    }
}
