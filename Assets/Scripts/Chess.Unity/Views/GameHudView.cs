using System;
using System.Collections.Generic;
using Chess.Core.Game;
using Chess.Core.Primitives;
using Chess.Unity.Config;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// The in-game interface: whose turn it is, the game status, the score sheet, captured
    /// material and the game control buttons.
    /// </summary>
    /// <remarks>
    /// Every serialised reference is optional. A missing label is simply not written to, so the
    /// game is playable against a bare canvas and the UI can be assembled piece by piece without
    /// ever hitting a null reference.
    /// </remarks>
    public sealed class GameHudView : MonoBehaviour, IGameHudView
    {
        [Header("Status")]
        [SerializeField] private TMP_Text _turnLabel;
        [SerializeField] private TMP_Text _statusLabel;
        [SerializeField] private GameObject _thinkingIndicator;
        [SerializeField] private TMP_Text _thinkingLabel;

        [Header("Panels")]
        [SerializeField] private GameObject _root;
        [SerializeField] private MoveListView _moveListView;
        [SerializeField] private CapturedPiecesView _whiteCapturesView;
        [SerializeField] private CapturedPiecesView _blackCapturesView;

        [Header("Buttons")]
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _menuButton;

        [Header("Game over")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private TMP_Text _gameOverLabel;

        [Header("Responsive layout")]
        [SerializeField] private RectTransform _topBar;
        [SerializeField] private RectTransform _chromePanel;
        [SerializeField] private LayoutElement _moveListLayout;

        private int _lastScreenWidth;
        private int _lastScreenHeight;
        private bool _isPortrait;

        public event Action LayoutChanged;

        public event Action RestartRequested;

        public event Action UndoRequested;

        public event Action MenuRequested;

        public void Initialise(PieceSpriteSet pieceSprites)
        {
            if (_whiteCapturesView != null)
            {
                _whiteCapturesView.Initialise(pieceSprites);
            }

            if (_blackCapturesView != null)
            {
                _blackCapturesView.Initialise(pieceSprites);
            }
        }

        public void SetActivePlayer(PieceColor color, string playerName)
        {
            if (_turnLabel != null)
            {
                _turnLabel.text = $"{color} to move  -  {playerName}";
            }
        }

        public void SetStatus(GameStatus status, GameResult result)
        {
            string description = Describe(status, result);

            if (_statusLabel != null)
            {
                _statusLabel.text = description;
            }

            bool isOver = status.IsGameOver();

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(isOver);
            }

            if (_gameOverLabel != null && isOver)
            {
                _gameOverLabel.text = description;
            }
        }

        public void SetThinking(bool isThinking, string playerName)
        {
            if (_thinkingIndicator != null)
            {
                _thinkingIndicator.SetActive(isThinking);
            }

            if (_thinkingLabel != null && isThinking)
            {
                _thinkingLabel.text = $"{playerName} is thinking...";
            }
        }

        public void AppendMove(MoveRecord record)
        {
            if (_moveListView != null)
            {
                _moveListView.Append(record);
            }
        }

        public void RemoveLastMove()
        {
            if (_moveListView != null)
            {
                _moveListView.RemoveLast();
            }
        }

        public void ClearMoveList()
        {
            if (_moveListView != null)
            {
                _moveListView.Clear();
            }
        }

        public void SetCapturedPieces(IReadOnlyList<Piece> capturedByWhite, IReadOnlyList<Piece> capturedByBlack)
        {
            if (_whiteCapturesView != null)
            {
                _whiteCapturesView.SetCapturedPieces(
                    capturedByWhite,
                    CapturedPiecesView.MaterialAdvantageInPawns(capturedByWhite, capturedByBlack, PieceColor.White));
            }

            if (_blackCapturesView != null)
            {
                _blackCapturesView.SetCapturedPieces(
                    capturedByBlack,
                    CapturedPiecesView.MaterialAdvantageInPawns(capturedByWhite, capturedByBlack, PieceColor.Black));
            }
        }

        public void SetUndoAvailable(bool available)
        {
            if (_undoButton != null)
            {
                _undoButton.interactable = available;
            }
        }

        public void Show()
        {
            SetRootActive(true);
            ApplyResponsiveLayout();
        }

        public void Hide() => SetRootActive(false);

        private void Awake()
        {
            Bind(_restartButton, () => RestartRequested?.Invoke());
            Bind(_undoButton, () => UndoRequested?.Invoke());
            Bind(_menuButton, () => MenuRequested?.Invoke());
            ApplyResponsiveLayout();
        }

        private void OnRectTransformDimensionsChange() => ApplyResponsiveLayout();

        private void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                ApplyResponsiveLayout();
            }
        }

        /// <summary>
        /// Viewport fractions occupied by the HUD, so the camera can keep the board in the leftover
        /// rectangle. Values are 0 when the HUD is hidden.
        /// </summary>
        public void GetBoardSafeInsets(out float left, out float right, out float top, out float bottom)
        {
            left = right = top = bottom = 0f;
            ApplyDeviceSafeArea(ref left, ref right, ref top, ref bottom);

            if (_root != null && !_root.activeInHierarchy)
            {
                return;
            }

            EncapsulateInset(_topBar, ref left, ref right, ref top, ref bottom);
            EncapsulateInset(_chromePanel, ref left, ref right, ref top, ref bottom);

            const float padding = 0.012f;
            if (left > 0f) left += padding;
            if (right > 0f) right += padding;
            if (top > 0f) top += padding;
            if (bottom > 0f) bottom += padding;
        }

        public void ApplyResponsiveLayout()
        {
            if (_topBar == null || _chromePanel == null)
            {
                return;
            }

            int width = Screen.width;
            int height = Screen.height;
            bool portrait = height > width;
            bool changed = width != _lastScreenWidth || height != _lastScreenHeight || portrait != _isPortrait;

            _lastScreenWidth = width;
            _lastScreenHeight = height;
            _isPortrait = portrait;

            float canvasHeight = ((RectTransform)transform).rect.height;
            if (canvasHeight <= 0f)
            {
                canvasHeight = 1080f;
            }

            const float margin = 16f;
            const float topHeight = 76f;

            if (portrait)
            {
                float bottomHeight = Mathf.Clamp(canvasHeight * 0.32f, 300f, 440f);
                UiFactory.Stretch(_topBar,
                    new Vector2(0f, 1f), Vector2.one,
                    new Vector2(margin, -topHeight), new Vector2(-margin, -margin));
                UiFactory.Stretch(_chromePanel,
                    Vector2.zero, new Vector2(1f, 0f),
                    new Vector2(margin, margin), new Vector2(-margin, bottomHeight));

                if (_moveListLayout != null)
                {
                    _moveListLayout.flexibleHeight = 1f;
                    _moveListLayout.minHeight = 80f;
                    _moveListLayout.preferredHeight = 120f;
                }
            }
            else
            {
                const float chromeWidth = 340f;
                UiFactory.Stretch(_topBar,
                    new Vector2(0f, 1f), Vector2.one,
                    new Vector2(margin, -topHeight), new Vector2(-(chromeWidth + margin), -margin));
                UiFactory.Stretch(_chromePanel,
                    new Vector2(1f, 0f), Vector2.one,
                    new Vector2(-chromeWidth, margin), new Vector2(-margin, -margin));

                if (_moveListLayout != null)
                {
                    _moveListLayout.flexibleHeight = 1f;
                    _moveListLayout.minHeight = 0f;
                    _moveListLayout.preferredHeight = -1f;
                }
            }

            if (changed)
            {
                LayoutChanged?.Invoke();
            }
        }

        private static void ApplyDeviceSafeArea(ref float left, ref float right, ref float top, ref float bottom)
        {
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            Rect safe = Screen.safeArea;
            left = Mathf.Max(left, safe.xMin / Screen.width);
            right = Mathf.Max(right, 1f - safe.xMax / Screen.width);
            bottom = Mathf.Max(bottom, safe.yMin / Screen.height);
            top = Mathf.Max(top, 1f - safe.yMax / Screen.height);
        }

        private static void EncapsulateInset(
            RectTransform rect,
            ref float left,
            ref float right,
            ref float top,
            ref float bottom)
        {
            if (rect == null || !rect.gameObject.activeInHierarchy || Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            float minX = Mathf.Min(corners[0].x, corners[2].x);
            float maxX = Mathf.Max(corners[0].x, corners[2].x);
            float minY = Mathf.Min(corners[0].y, corners[2].y);
            float maxY = Mathf.Max(corners[0].y, corners[2].y);

            float width = Screen.width;
            float height = Screen.height;

            // A panel only reserves the edge it actually sits against, so a top bar does not also
            // steal the bottom of the screen just because it has a non-zero height.
            float insetLeft = Mathf.Clamp01(maxX / width);
            float insetRight = Mathf.Clamp01(1f - minX / width);
            float insetBottom = Mathf.Clamp01(maxY / height);
            float insetTop = Mathf.Clamp01(1f - minY / height);

            bool hugsLeft = minX <= width * 0.08f && maxX < width * 0.55f;
            bool hugsRight = maxX >= width * 0.92f && minX > width * 0.45f;
            bool hugsBottom = minY <= height * 0.08f && maxY < height * 0.55f;
            bool hugsTop = maxY >= height * 0.92f && minY > height * 0.45f;

            if (hugsLeft) left = Mathf.Max(left, insetLeft);
            if (hugsRight) right = Mathf.Max(right, insetRight);
            if (hugsBottom) bottom = Mathf.Max(bottom, insetBottom);
            if (hugsTop) top = Mathf.Max(top, insetTop);
        }

        private void SetRootActive(bool active)
        {
            GameObject target = _root != null ? _root : gameObject;
            target.SetActive(active);
        }

        private static void Bind(Button button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private static string Describe(GameStatus status, GameResult result)
        {
            switch (status)
            {
                case GameStatus.Check:
                    return "Check";

                case GameStatus.Checkmate:
                    return result == GameResult.WhiteWins ? "Checkmate - White wins" : "Checkmate - Black wins";

                case GameStatus.Stalemate:
                    return "Draw - stalemate";

                case GameStatus.DrawByFiftyMoveRule:
                    return "Draw - fifty-move rule";

                case GameStatus.DrawByThreefoldRepetition:
                    return "Draw - threefold repetition";

                case GameStatus.DrawByInsufficientMaterial:
                    return "Draw - insufficient material";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Builds a complete HUD so the game is playable before a designed canvas exists.
        /// </summary>
        public static GameHudView CreateDefault(Transform parent)
        {
            RectTransform root = UiFactory.CreateRect(parent, "Hud");
            UiFactory.StretchFill(root);
            root.gameObject.SetActive(false);

            var view = root.gameObject.AddComponent<GameHudView>();
            view._root = root.gameObject;

            RectTransform top = UiFactory.CreateRect(root, "TopBar");
            view._topBar = top;
            UiFactory.Stretch(top, new Vector2(0f, 1f), Vector2.one, new Vector2(24f, -72f), new Vector2(-300f, -16f));
            UiFactory.PanelImage(top, UiFactory.Panel);
            UiFactory.Horizontal(top, 16f, 12);

            view._turnLabel = UiFactory.Label(top, "Turn", "White to move", UiFactory.SizeHeading);
            UiFactory.Size(view._turnLabel, 480f, 0f);
            view._statusLabel = UiFactory.Label(top, "Status", string.Empty, UiFactory.SizeBody);
            UiFactory.Size(view._statusLabel, 320f, 0f);

            RectTransform thinkingRect = UiFactory.CreateRect(top, "Thinking");
            UiFactory.Size(thinkingRect, 300f, 0f);
            view._thinkingIndicator = thinkingRect.gameObject;
            view._thinkingLabel = UiFactory.Label(thinkingRect, "Label", string.Empty, UiFactory.SizeBody);
            UiFactory.StretchFill((RectTransform)view._thinkingLabel.transform);
            view._thinkingIndicator.SetActive(false);

            RectTransform right = UiFactory.CreateRect(root, "ChromePanel");
            view._chromePanel = right;
            UiFactory.Stretch(right, new Vector2(1f, 0f), Vector2.one, new Vector2(-340f, 16f), new Vector2(-16f, -16f));
            UiFactory.PanelImage(right, UiFactory.Panel);
            UiFactory.Vertical(right, 10f, 12);

            TMP_Text moveHeader = UiFactory.Label(right, "MovesHeader", "Moves", UiFactory.SizeBody, TextAlignmentOptions.Center);
            UiFactory.Size(moveHeader, 0f, 34f);

            var moveList = UiFactory.CreateRect(right, "MoveList").gameObject.AddComponent<MoveListView>();
            view._moveListLayout = UiFactory.Size(moveList, 0f, 0f);
            view._moveListLayout.flexibleHeight = 1f;
            var moveScroll = UiFactory.VerticalScroll(moveList.transform, "Scroll", out RectTransform moveContent);
            UiFactory.StretchFill((RectTransform)moveScroll.transform);
            moveList.Bind(moveContent, moveScroll);
            view._moveListView = moveList;

            view._whiteCapturesView = CreateCaptures(right, "WhiteCaptures", "White took");
            view._blackCapturesView = CreateCaptures(right, "BlackCaptures", "Black took");

            RectTransform buttons = UiFactory.CreateRect(right, "Buttons");
            UiFactory.Size(buttons, 0f, 56f);
            UiFactory.Horizontal(buttons, 8f, 0);
            view._undoButton = UiFactory.TextButton(buttons, "Undo", "Undo", UiFactory.Button);
            view._restartButton = UiFactory.TextButton(buttons, "Restart", "Restart", UiFactory.Button);
            view._menuButton = UiFactory.TextButton(buttons, "Menu", "Menu", UiFactory.ButtonAccent);

            RectTransform gameOver = UiFactory.CreateRect(root, "GameOver");
            UiFactory.Stretch(gameOver, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -80f), new Vector2(280f, 80f));
            UiFactory.PanelImage(gameOver, UiFactory.PanelSolid);
            view._gameOverLabel = UiFactory.Label(gameOver, "Label", string.Empty, UiFactory.SizeHeading, TextAlignmentOptions.Center);
            UiFactory.StretchFill((RectTransform)view._gameOverLabel.transform);
            gameOver.gameObject.SetActive(false);
            view._gameOverPanel = gameOver.gameObject;

            root.gameObject.SetActive(true);
            view.ApplyResponsiveLayout();
            return view;
        }

        private static CapturedPiecesView CreateCaptures(Transform parent, string objectName, string caption)
        {
            RectTransform root = UiFactory.CreateRect(parent, objectName);
            UiFactory.Size(root, 0f, 72f);
            UiFactory.Vertical(root, 2f, 0);

            TMP_Text header = UiFactory.Label(root, "Header", caption, UiFactory.SizeCaption);
            UiFactory.Size(header, 0f, 26f);

            RectTransform row = UiFactory.CreateRect(root, "Row");
            UiFactory.Size(row, 0f, 32f);
            UiFactory.Horizontal(row, 4f, 0);

            RectTransform icons = UiFactory.CreateRect(row, "Icons");
            UiFactory.Size(icons, 0f, 32f).flexibleWidth = 1f;
            UiFactory.Horizontal(icons, 2f, 0);

            TMP_Text advantage = UiFactory.Label(row, "Advantage", string.Empty, UiFactory.SizeCaption, TextAlignmentOptions.MidlineRight);
            UiFactory.Size(advantage, 48f, 36f);

            var view = root.gameObject.AddComponent<CapturedPiecesView>();
            view.Bind(icons, advantage);
            return view;
        }
    }
}
