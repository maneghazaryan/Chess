using System;
using Chess.Unity.Players;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Mode, difficulty and side selection.
    /// </summary>
    public interface IMainMenuView
    {
        event Action<GameSetup> StartGameRequested;

        event Action QuitRequested;

        void Show();

        void Hide();
    }
}
