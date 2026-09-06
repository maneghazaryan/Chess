using System;
using Chess.AI;
using Chess.Core.Primitives;
using Chess.Unity.Players;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Mode, difficulty and side selection.
    /// </summary>
    /// <remarks>
    /// Collects the three choices into a single <see cref="GameSetup"/> and raises it as one
    /// event, so nothing downstream ever sees a half-configured match. Options that do not apply
    /// to the selected mode are hidden rather than disabled, since difficulty is meaningless in a
    /// two-player game.
    /// </remarks>
    public sealed class MainMenuView : MonoBehaviour, IMainMenuView
    {
        [Tooltip("Panel toggled by Show and Hide. Defaults to this object.")]
        [SerializeField] private GameObject _root;

        [Header("Mode")]
        [SerializeField] private Button _humanVsHumanButton;
        [SerializeField] private Button _humanVsAiButton;
        [SerializeField] private Button _aiVsAiButton;
        [SerializeField] private TMP_Text _selectedModeLabel;

        [Header("AI options")]
        [Tooltip("Shown only for modes that involve the computer.")]
        [SerializeField] private GameObject _aiOptionsPanel;

        [Tooltip("Optional. Used when difficulty is presented as a dropdown instead of buttons.")]
        [SerializeField] private TMP_Dropdown _difficultyDropdown;

        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _mediumButton;
        [SerializeField] private Button _hardButton;

        [Header("Side")]
        [Tooltip("Shown only when a human is facing the computer.")]
        [SerializeField] private GameObject _sidePanel;

        [SerializeField] private Toggle _playAsWhiteToggle;
        [SerializeField] private Toggle _playAsBlackToggle;

        [Header("Actions")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        private GameMode _mode = GameMode.HumanVsHuman;
        private AiDifficulty _difficulty = AiDifficulty.Medium;

        public event Action<GameSetup> StartGameRequested;

        public event Action QuitRequested;

        public void Show()
        {
            SetRootActive(true);
            RefreshOptionVisibility();
        }

        public void Hide() => SetRootActive(false);

        public void SelectMode(GameMode mode)
        {
            _mode = mode;
            RefreshOptionVisibility();
        }

        public void StartGame() => StartGameRequested?.Invoke(BuildSetup());

        private void Awake()
        {
            Bind(_humanVsHumanButton, () => SelectMode(GameMode.HumanVsHuman));
            Bind(_humanVsAiButton, () => SelectMode(GameMode.HumanVsAi));
            Bind(_aiVsAiButton, () => SelectMode(GameMode.AiVsAi));
            Bind(_easyButton, () => SelectDifficulty(AiDifficulty.Easy));
            Bind(_mediumButton, () => SelectDifficulty(AiDifficulty.Medium));
            Bind(_hardButton, () => SelectDifficulty(AiDifficulty.Hard));
            Bind(_startButton, StartGame);
            Bind(_quitButton, () => QuitRequested?.Invoke());

            PopulateDifficultyDropdown();
            RefreshDifficultyButtons();
            RefreshOptionVisibility();
        }

        public void SelectDifficulty(AiDifficulty difficulty)
        {
            _difficulty = difficulty;

            if (_difficultyDropdown != null)
            {
                _difficultyDropdown.SetValueWithoutNotify((int)difficulty);
                _difficultyDropdown.RefreshShownValue();
            }

            RefreshDifficultyButtons();
        }

        private GameSetup BuildSetup()
        {
            AiDifficulty difficulty = _difficulty;
            if (_easyButton == null && _difficultyDropdown != null)
            {
                difficulty = (AiDifficulty)Mathf.Clamp(
                    _difficultyDropdown.value,
                    0,
                    Enum.GetValues(typeof(AiDifficulty)).Length - 1);
            }

            PieceColor humanColor = _playAsBlackToggle != null && _playAsBlackToggle.isOn
                ? PieceColor.Black
                : PieceColor.White;

            return new GameSetup(_mode, difficulty, humanColor);
        }

        private void PopulateDifficultyDropdown()
        {
            if (_difficultyDropdown == null)
            {
                return;
            }

            _difficultyDropdown.ClearOptions();
            _difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>(
                Enum.GetNames(typeof(AiDifficulty))));
            _difficultyDropdown.SetValueWithoutNotify((int)_difficulty);
            _difficultyDropdown.RefreshShownValue();
            _difficultyDropdown.onValueChanged.AddListener(index =>
                SelectDifficulty((AiDifficulty)Mathf.Clamp(index, 0, 2)));
        }

        private void RefreshDifficultyButtons()
        {
            TintDifficultyButton(_easyButton, _difficulty == AiDifficulty.Easy);
            TintDifficultyButton(_mediumButton, _difficulty == AiDifficulty.Medium);
            TintDifficultyButton(_hardButton, _difficulty == AiDifficulty.Hard);
        }

        private static void TintDifficultyButton(Button button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.color = selected ? UiFactory.ButtonAccent : UiFactory.Button;
            }
        }

        private void RefreshOptionVisibility()
        {
            bool involvesAi = _mode != GameMode.HumanVsHuman;
            bool humanChoosesSide = _mode == GameMode.HumanVsAi;

            if (_aiOptionsPanel != null)
            {
                _aiOptionsPanel.SetActive(involvesAi);
            }

            if (_sidePanel != null)
            {
                _sidePanel.SetActive(humanChoosesSide);
            }

            if (_selectedModeLabel != null)
            {
                _selectedModeLabel.text = Describe(_mode);
            }

            if (_playAsWhiteToggle != null && _playAsBlackToggle != null &&
                !_playAsWhiteToggle.isOn && !_playAsBlackToggle.isOn)
            {
                _playAsWhiteToggle.isOn = true;
            }
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

        private static string Describe(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.HumanVsHuman: return "Two players";
                case GameMode.HumanVsAi: return "Player vs computer";
                case GameMode.AiVsAi: return "Computer vs computer";
                default: return mode.ToString();
            }
        }

        /// <summary>
        /// Builds a complete menu so the game is playable before a designed canvas exists.
        /// </summary>
        public static MainMenuView CreateDefault(Transform parent)
        {
            RectTransform overlay = UiFactory.CreateRect(parent, "MainMenu");
            UiFactory.StretchFill(overlay);
            overlay.gameObject.SetActive(false);
            UiFactory.PanelImage(overlay, new Color(0.05f, 0.04f, 0.03f, 0.72f));

            var view = overlay.gameObject.AddComponent<MainMenuView>();
            view._root = overlay.gameObject;

            RectTransform card = UiFactory.CreateRect(overlay, "Card");
            UiFactory.Stretch(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-280f, -360f), new Vector2(280f, 360f));
            UiFactory.PanelImage(card, UiFactory.PanelSolid);
            UiFactory.Vertical(card, 14f, 28);

            TMP_Text title = UiFactory.Label(card, "Title", "Chess", UiFactory.SizeTitle, TextAlignmentOptions.Center);
            UiFactory.Size(title, 0f, 72f);

            view._selectedModeLabel = UiFactory.Label(card, "ModeLabel", "Two players", UiFactory.SizeBody, TextAlignmentOptions.Center);
            UiFactory.Size(view._selectedModeLabel, 0f, 36f);

            view._humanVsHumanButton = UiFactory.TextButton(card, "TwoPlayers", "Two players", UiFactory.Button);
            UiFactory.Size(view._humanVsHumanButton, 0f, 56f);
            view._humanVsAiButton = UiFactory.TextButton(card, "VsComputer", "Player vs computer", UiFactory.Button);
            UiFactory.Size(view._humanVsAiButton, 0f, 56f);
            view._aiVsAiButton = UiFactory.TextButton(card, "ComputerMatch", "Computer vs computer", UiFactory.Button);
            UiFactory.Size(view._aiVsAiButton, 0f, 56f);

            RectTransform aiOptions = UiFactory.CreateRect(card, "AiOptions");
            UiFactory.Size(aiOptions, 0f, 104f);
            UiFactory.Vertical(aiOptions, 8f, 0);
            TMP_Text difficultyLabel = UiFactory.Label(aiOptions, "DifficultyLabel", "Difficulty", UiFactory.SizeCaption, TextAlignmentOptions.Center);
            UiFactory.Size(difficultyLabel, 0f, 30f);

            RectTransform difficultyRow = UiFactory.CreateRect(aiOptions, "Difficulty");
            UiFactory.Size(difficultyRow, 0f, 56f);
            UiFactory.Horizontal(difficultyRow, 8f, 0);
            view._easyButton = UiFactory.TextButton(difficultyRow, "Easy", "Easy", UiFactory.Button);
            view._mediumButton = UiFactory.TextButton(difficultyRow, "Medium", "Medium", UiFactory.ButtonAccent);
            view._hardButton = UiFactory.TextButton(difficultyRow, "Hard", "Hard", UiFactory.Button);
            UiFactory.Size(view._easyButton, 0f, 56f).flexibleWidth = 1f;
            UiFactory.Size(view._mediumButton, 0f, 56f).flexibleWidth = 1f;
            UiFactory.Size(view._hardButton, 0f, 56f).flexibleWidth = 1f;
            view._aiOptionsPanel = aiOptions.gameObject;

            RectTransform side = UiFactory.CreateRect(card, "Side");
            UiFactory.Size(side, 0f, 56f);
            UiFactory.Horizontal(side, 10f, 0);
            var group = side.gameObject.AddComponent<ToggleGroup>();
            view._playAsWhiteToggle = UiFactory.TextToggle(side, "PlayWhite", "Play White");
            view._playAsWhiteToggle.group = group;
            view._playAsWhiteToggle.isOn = true;
            view._playAsBlackToggle = UiFactory.TextToggle(side, "PlayBlack", "Play Black");
            view._playAsBlackToggle.group = group;
            UiFactory.Size(view._playAsWhiteToggle, 0f, 56f).flexibleWidth = 1f;
            UiFactory.Size(view._playAsBlackToggle, 0f, 56f).flexibleWidth = 1f;
            view._sidePanel = side.gameObject;

            view._startButton = UiFactory.TextButton(card, "Start", "Start game", UiFactory.ButtonAccent);
            UiFactory.Size(view._startButton, 0f, 64f);
            view._quitButton = UiFactory.TextButton(card, "Quit", "Quit", UiFactory.Button);
            UiFactory.Size(view._quitButton, 0f, 52f);

            overlay.gameObject.SetActive(true);
            return view;
        }
    }
}
