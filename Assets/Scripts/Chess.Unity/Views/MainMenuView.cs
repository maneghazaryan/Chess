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

        [Tooltip("Populated at runtime with Easy, Medium and Hard.")]
        [SerializeField] private TMP_Dropdown _difficultyDropdown;

        [Header("Side")]
        [Tooltip("Shown only when a human is facing the computer.")]
        [SerializeField] private GameObject _sidePanel;

        [SerializeField] private Toggle _playAsWhiteToggle;
        [SerializeField] private Toggle _playAsBlackToggle;

        [Header("Actions")]
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _quitButton;

        private GameMode _mode = GameMode.HumanVsHuman;

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
            Bind(_startButton, StartGame);
            Bind(_quitButton, () => QuitRequested?.Invoke());

            PopulateDifficultyDropdown();
            RefreshOptionVisibility();
        }

        private GameSetup BuildSetup()
        {
            var difficulty = (AiDifficulty)Mathf.Clamp(
                _difficultyDropdown != null ? _difficultyDropdown.value : (int)AiDifficulty.Medium,
                0,
                Enum.GetValues(typeof(AiDifficulty)).Length - 1);

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
            _difficultyDropdown.value = (int)AiDifficulty.Medium;
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
    }
}
