using System;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;
using ProjectEta.Run;

namespace ProjectEta.AI
{
    public static class EnemyAIActionExecutor
    {
        public static bool TryExecute(
            AIActionCandidate action,
            RunState runState,
            TurnManager turnManager,
            BattleHooks battleHooks,
            BoardView boardView,
            out CombatResult combatResult)
        {
            combatResult = null;

            if (action == null ||
                runState == null ||
                turnManager == null)
            {
                return false;
            }

            if (turnManager.CurrentState != TurnState.EnemyTurn)
            {
                return false;
            }

            if (action.Actor == null ||
                action.Actor.IsPlayerPiece ||
                action.Actor.IsDead)
            {
                return false;
            }

            BoardState board = runState.Board;
            TileState originTile =
                board.GetTile(action.Actor.BoardPosition);

            if (originTile == null ||
                originTile.OccupyingPiece != action.Actor)
            {
                return false;
            }

            if (action.ActionType == AIActionType.Ability)
            {
                if (action.Ability == null) return false;

                var abilityContext = new AbilityExecutionContext(
                    action.Actor,
                    action.TargetPiece,
                    action.Target,
                    board,
                    runState,
                    battleHooks,
                    turnManager);

                AbilityExecutionResult preview =
                    PieceAbilityService.PreviewAbility(
                        action.Ability,
                        abilityContext);

                if (!preview.Success) return false;

                AbilityExecutionResult result =
                    PieceAbilityService.ExecuteAbility(
                        action.Ability,
                        abilityContext);

                if (!result.Success) return false;

                Debug.Log(
                    $"Enemy AI Ability: {action.Actor.Definition.DisplayName} " +
                    $"-> {action.Ability.DisplayName} / Target={action.Target} / Score={action.Score}");

                CompleteEnemyTurn(turnManager, battleHooks);
                return true;
            }

            MovementResult legalMovement =
                MovementResolver.GetReachableTiles(
                    action.Actor,
                    board);

            if (action.ActionType == AIActionType.Move)
            {
                if (!legalMovement.MoveTiles.Contains(action.Target))
                {
                    return false;
                }

                TileState destinationTile =
                    board.GetTile(action.Target);

                if (destinationTile == null ||
                    destinationTile.IsOccupied ||
                    destinationTile.IsBlockedByObstacle)
                {
                    return false;
                }

                MovePiece(
                    action.Actor,
                    action.Target,
                    board,
                    boardView,
                    battleHooks);

                Debug.Log(
                    $"Enemy AI 이동: {action.Actor.Definition.DisplayName} " +
                    $"{action.Origin} -> {action.Target} / Score={action.Score}");

                CompleteEnemyTurn(turnManager, battleHooks);
                return true;
            }

            if (action.ActionType == AIActionType.Attack)
            {
                if (!legalMovement.AttackTiles.Contains(action.Target))
                {
                    return false;
                }

                TileState targetTile =
                    board.GetTile(action.Target);

                PieceRuntimeState defender =
                    targetTile?.OccupyingPiece;

                if (defender == null ||
                    !defender.IsPlayerPiece ||
                    defender.IsDead)
                {
                    return false;
                }

                battleHooks?.RaiseBeforeAttack(
                    action.Actor,
                    defender);

                combatResult = CombatResolver.ResolveAttack(
                    action.Actor,
                    defender,
                    battleHooks);

                if (combatResult.DefenderDied)
                {
                    RemovePlayerPiece(
                        defender,
                        runState,
                        board,
                        boardView);

                    if (CombatMovementPolicy
                        .ShouldOccupyDefenderTileAfterKill(
                            action.Actor.Definition))
                    {
                        MovePiece(
                            action.Actor,
                            action.Target,
                            board,
                            boardView,
                            battleHooks);
                    }
                }

                battleHooks?.RaiseAfterAttack(combatResult);

                SynchronizeKingAndDefeat(
                    defender,
                    runState,
                    turnManager);

                Debug.Log(
                    $"Enemy AI 공격: {action.Actor.Definition.DisplayName} " +
                    $"-> {defender.Definition.DisplayName} / " +
                    $"Damage={combatResult.DamageDealt}, HP={defender.CurrentHp}, Score={action.Score}");

                if (turnManager.CurrentState == TurnState.EnemyTurn)
                {
                    CompleteEnemyTurn(
                        turnManager,
                        battleHooks);
                }

                return true;
            }

            return false;
        }

        private static void MovePiece(
            PieceRuntimeState piece,
            Vector2Int destination,
            BoardState board,
            BoardView boardView,
            BattleHooks battleHooks)
        {
            Vector2Int origin = piece.BoardPosition;

            battleHooks?.RaiseBeforeMove(
                piece,
                origin,
                destination);

            TileState originTile =
                board.GetTile(origin);

            if (originTile != null &&
                originTile.OccupyingPiece == piece)
            {
                originTile.OccupyingPiece = null;
            }

            piece.BoardPosition = destination;

            TileState destinationTile =
                board.GetTile(destination);

            if (destinationTile != null)
            {
                destinationTile.OccupyingPiece = piece;
            }

            if (boardView != null)
            {
                PieceView pieceView =
                    FindPieceView(piece);

                if (pieceView != null)
                {
                    pieceView.MoveTo(
                        destination,
                        boardView.TileSize);
                }
            }

            battleHooks?.RaiseAfterMove(
                piece,
                origin,
                destination);
        }

        private static void RemovePlayerPiece(
            PieceRuntimeState defender,
            RunState runState,
            BoardState board,
            BoardView boardView)
        {
            TileState tile =
                board.GetTile(defender.BoardPosition);

            if (tile != null &&
                tile.OccupyingPiece == defender)
            {
                tile.OccupyingPiece = null;
            }

            if (boardView != null)
            {
                PieceView pieceView =
                    FindPieceView(defender);

                if (pieceView != null)
                {
                    pieceView.PlayDeathTogglingThenDestroy(
                        () => DestroyObject(
                            pieceView.gameObject));
                }
            }

            runState.Deck.MoveToDeadPile(
                defender.Definition);

            RefreshDeckPanelIfPresent();
        }

        private static void SynchronizeKingAndDefeat(
            PieceRuntimeState defender,
            RunState runState,
            TurnManager turnManager)
        {
            if (!IsKing(defender)) return;

            runState.KingHp = defender.CurrentHp;

            if (runState.IsDefeated)
            {
                turnManager.EndBattle(
                    BattleOutcome.Defeat);
            }
        }

        private static void CompleteEnemyTurn(
            TurnManager turnManager,
            BattleHooks battleHooks)
        {
            if (turnManager.CurrentState != TurnState.EnemyTurn)
            {
                return;
            }

            if (turnManager.CompleteEnemyTurn())
            {
                battleHooks?.RaiseTurnEnd(
                    turnManager.CurrentState,
                    turnManager.TurnNumber);
            }
        }

        private static PieceView FindPieceView(
            PieceRuntimeState state)
        {
            PieceView[] views =
                UnityEngine.Object.FindObjectsByType<PieceView>(
                    FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null &&
                    views[i].RuntimeState == state)
                {
                    return views[i];
                }
            }

            return null;
        }

        private static void RefreshDeckPanelIfPresent()
        {
            BattleController battleController =
                UnityEngine.Object.FindFirstObjectByType<BattleController>();

            BoardInputController boardInput =
                UnityEngine.Object.FindFirstObjectByType<BoardInputController>();

            if (battleController?.DeckPanelUI != null &&
                boardInput != null)
            {
                battleController.DeckPanelUI.Bind(
                    boardInput);
            }
        }

        private static void DestroyObject(
            UnityEngine.Object target)
        {
            if (target == null) return;

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static bool IsKing(
            PieceRuntimeState piece)
        {
            if (piece?.Definition == null) return false;

            if (string.Equals(
                    piece.Definition.PieceId,
                    "king",
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return piece.Definition.MovementType ==
                   PieceMovementType.King;
        }
    }
}
