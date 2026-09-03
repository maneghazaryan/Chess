using System;
using System.Collections.Generic;
using Chess.Core.Game;
using Chess.Core.Primitives;

namespace Chess.Unity.Views
{
    /// <summary>
    /// Turn indicator, status banner, move list, captured material and the game control buttons.
    /// </summary>
    public interface IGameHudView
    {
        event Action RestartRequested;

        event Action UndoRequested;

        event Action MenuRequested;

        void SetActivePlayer(PieceColor color, string playerName);

        void SetStatus(GameStatus status, GameResult result);

        /// <summary>Shows or hides the "thinking" affordance while an AI player searches.</summary>
        void SetThinking(bool isThinking, string playerName);

        void AppendMove(MoveRecord record);

        void RemoveLastMove();

        void ClearMoveList();

        /// <summary>Captured pieces for both sides, so the HUD can show a material balance.</summary>
        void SetCapturedPieces(IReadOnlyList<Piece> capturedByWhite, IReadOnlyList<Piece> capturedByBlack);

        void SetUndoAvailable(bool available);

        void Show();

        void Hide();
    }
}
