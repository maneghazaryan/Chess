using System;
using System.Collections.Generic;
using Chess.Core.Board;
using Chess.Core.Primitives;
using Chess.Core.Rules;
using Chess.Core.Rules.Draws;

namespace Chess.Core.Game
{
    /// <summary>
    /// Classifies a position by asking two questions in order: does the side to move have a legal
    /// move, and if so, does any drawing condition apply.
    /// </summary>
    public sealed class GameStatusEvaluator : IGameStatusEvaluator
    {
        private readonly IMoveGenerator _moveGenerator;
        private readonly IAttackService _attackService;
        private readonly IReadOnlyList<IDrawRule> _drawRules;

        public GameStatusEvaluator(
            IMoveGenerator moveGenerator,
            IAttackService attackService,
            IReadOnlyList<IDrawRule> drawRules)
        {
            _moveGenerator = moveGenerator ?? throw new ArgumentNullException(nameof(moveGenerator));
            _attackService = attackService ?? throw new ArgumentNullException(nameof(attackService));
            _drawRules = drawRules ?? throw new ArgumentNullException(nameof(drawRules));
        }

        public static GameStatusEvaluator CreateStandard(IMoveGenerator moveGenerator, IAttackService attackService)
        {
            var drawRules = new IDrawRule[]
            {
                new InsufficientMaterialRule(),
                new ThreefoldRepetitionRule(),
                new FiftyMoveRule()
            };

            return new GameStatusEvaluator(moveGenerator, attackService, drawRules);
        }

        public GameStatus Evaluate(IMutableBoard board)
        {
            bool inCheck = _attackService.IsInCheck(board, board.SideToMove);

            // Having no move must be resolved before any draw rule, because checkmate ends the
            // game even on the hundredth reversible ply.
            if (!_moveGenerator.HasAnyLegalMove(board))
            {
                return inCheck ? GameStatus.Checkmate : GameStatus.Stalemate;
            }

            for (int i = 0; i < _drawRules.Count; i++)
            {
                if (_drawRules[i].IsDraw(board))
                {
                    return _drawRules[i].Status;
                }
            }

            return inCheck ? GameStatus.Check : GameStatus.InProgress;
        }

        public GameResult ToResult(GameStatus status, PieceColor sideToMove)
        {
            if (status == GameStatus.Checkmate)
            {
                // The side to move is the one that has been mated.
                return sideToMove == PieceColor.White ? GameResult.BlackWins : GameResult.WhiteWins;
            }

            return status.IsDraw() ? GameResult.Draw : GameResult.Undecided;
        }
    }
}
