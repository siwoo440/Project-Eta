#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day92BoardAbilityTests
    {
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            TileBlockService.ClearAll();
            MovementRangeModifierService.ClearAll();
            TemporarySummonService.ClearAll();
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            TileBlockService.ClearAll();
            MovementRangeModifierService.ClearAll();
            TemporarySummonService.ClearAll();
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();

            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null) UnityEngine.Object.DestroyImmediate(_created[i]);
            }

            _created.Clear();
        }

        [Test]
        public void Registry_92일차완료후공통Effect9종을모두구현한다()
        {
            Assert.That(AbilityEffectRegistry.RegisteredCount, Is.EqualTo(9));

            foreach (AbilityEffectType type in Enum.GetValues(typeof(AbilityEffectType)))
            {
                Assert.That(
                    AbilityEffectRegistry.IsImplemented(type),
                    Is.True,
                    $"{type} Executor가 등록되어야 합니다.");
            }
        }

        [Test]
        public void ModifyMoveRange_Preview는상태를변경하지않고추가후보를계산한다()
        {
            var board = new BoardState();
            PieceAbilityDefinition ability = CreateAbility(
                "move_preview",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyMoveRange, amount: 2));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("move_piece", 10, 1, PieceMovementType.King, ability), new Vector2Int(4, 4), true);
            board.GetTile(piece.BoardPosition).OccupyingPiece = piece;

            var context = new AbilityExecutionContext(piece, board: board);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(ability, context);

            Assert.That(preview.Success, Is.True);
            Assert.That(MovementRangeModifierService.ActiveCount, Is.EqualTo(0));

            MovementResult before = MovementResolver.GetReachableTiles(piece, board);
            Assert.That(before.MoveTiles.Contains(new Vector2Int(6, 4)), Is.False);
        }

        [Test]
        public void ModifyMoveRange_Execute후추가이동후보를제공한다()
        {
            var board = new BoardState();
            PieceAbilityDefinition ability = CreateAbility(
                "move_execute",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyMoveRange, amount: 2));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("move_piece2", 10, 1, PieceMovementType.King, ability), new Vector2Int(4, 4), true);
            board.GetTile(piece.BoardPosition).OccupyingPiece = piece;

            PieceAbilityService.ExecuteAbility(
                ability,
                new AbilityExecutionContext(piece, board: board));

            MovementResult after = MovementResolver.GetReachableTiles(piece, board);

            Assert.That(MovementRangeModifierService.ActiveCount, Is.EqualTo(1));
            Assert.That(after.MoveTiles.Contains(new Vector2Int(6, 4)), Is.True);
            Assert.That(after.MoveTiles.Contains(new Vector2Int(6, 6)), Is.True);
        }

        [Test]
        public void ModifyMoveRange_속박상태에서는추가Move를만들지않는다()
        {
            var board = new BoardState();
            StatusEffectDefinition root = CreateStatus(StatusEffectType.Root);

            PieceAbilityDefinition passive = CreateAbility(
                "move_passive",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyMoveRange, amount: 2));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("rooted", 10, 1, PieceMovementType.King, passive), new Vector2Int(4, 4), true);
            board.GetTile(piece.BoardPosition).OccupyingPiece = piece;
            piece.ApplyStatus(root);

            MovementResult result = MovementResolver.GetReachableTiles(piece, board);

            Assert.That(piece.CanMove, Is.False);
            Assert.That(result.MoveTiles, Is.Empty);
        }

        [Test]
        public void ModifyMoveRange_장애물과봉쇄칸뒤로진행하지않는다()
        {
            var board = new BoardState();

            PieceAbilityDefinition passive = CreateAbility(
                "directional_move",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(
                    AbilityEffectType.ModifyMoveRange,
                    amount: 3,
                    vector: new Vector2Int(1, 0)));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("directional", 10, 1, PieceMovementType.King, passive), new Vector2Int(2, 2), true);
            board.GetTile(piece.BoardPosition).OccupyingPiece = piece;
            board.GetTile(new Vector2Int(3, 2)).SetObstacle(true, false);

            MovementResult obstacleResult = MovementResolver.GetReachableTiles(piece, board);

            Assert.That(obstacleResult.MoveTiles.Contains(new Vector2Int(3, 2)), Is.False);
            Assert.That(obstacleResult.MoveTiles.Contains(new Vector2Int(4, 2)), Is.False);

            board.GetTile(new Vector2Int(3, 2)).SetObstacle(false);
            TileBlockService.TryBlock(board, new Vector2Int(3, 2), piece, null, 1);

            MovementResult blockedResult = MovementResolver.GetReachableTiles(piece, board);

            Assert.That(blockedResult.MoveTiles.Contains(new Vector2Int(3, 2)), Is.False);
            Assert.That(blockedResult.MoveTiles.Contains(new Vector2Int(4, 2)), Is.False);
        }

        [Test]
        public void BlockTile_Preview는Board를바꾸지않고Execute에서만봉쇄한다()
        {
            var board = new BoardState();
            PieceAbilityDefinition ability = CreateAbility(
                "block",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.BlockTile, durationTurns: 1));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("blocker", 10, 1, PieceMovementType.King, ability), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int target = new Vector2Int(2, 1);
            var context = new AbilityExecutionContext(owner, targetPosition: target, board: board);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(ability, context);
            Assert.That(preview.Success, Is.True);
            Assert.That(board.GetTile(target).IsBlockedByAbility, Is.False);

            AbilityExecutionResult executed = PieceAbilityService.ExecuteAbility(ability, context);
            Assert.That(executed.Success, Is.True);
            Assert.That(board.GetTile(target).IsBlockedByAbility, Is.True);
            Assert.That(board.GetTile(target).IsBlockedByObstacle, Is.True);
        }

        [Test]
        public void BlockTile_점유칸과장애물칸은봉쇄할수없다()
        {
            var board = new BoardState();
            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("owner", 10, 1, PieceMovementType.King), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int occupied = new Vector2Int(2, 1);
            board.GetTile(occupied).OccupyingPiece =
                new PieceRuntimeState(CreatePiece("ally", 10, 1, PieceMovementType.King), occupied, true);

            Assert.That(TileBlockService.CanBlock(board, occupied), Is.False);

            Vector2Int obstacle = new Vector2Int(3, 1);
            board.GetTile(obstacle).SetObstacle(true, true);

            Assert.That(TileBlockService.CanBlock(board, obstacle), Is.False);
        }

        [Test]
        public void BlockTile_다음PlayerTurn시작에기본봉쇄가해제된다()
        {
            var board = new BoardState();
            var turn = new TurnManager();

            turn.MarkInitialKingPlaced();
            Assert.That(turn.TryEndDeploymentTurn(), Is.True);
            Assert.That(turn.CurrentState, Is.EqualTo(TurnState.PlayerTurn));
            Assert.That(turn.TurnNumber, Is.EqualTo(1));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("block_owner", 10, 1, PieceMovementType.King), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int target = new Vector2Int(2, 1);
            Assert.That(TileBlockService.TryBlock(board, target, owner, turn, 1), Is.True);
            Assert.That(board.GetTile(target).IsBlockedByAbility, Is.True);

            Assert.That(turn.TryCompletePlayerAction(), Is.True);
            Assert.That(turn.CompleteEnemyTurn(), Is.True);

            Assert.That(turn.CurrentState, Is.EqualTo(TurnState.PlayerTurn));
            Assert.That(turn.TurnNumber, Is.EqualTo(2));
            Assert.That(board.GetTile(target).IsBlockedByAbility, Is.False);
        }

        [Test]
        public void BlockTile_슬라이드기물의이동경로를차단한다()
        {
            var board = new BoardState();
            PieceRuntimeState rook =
                new PieceRuntimeState(CreatePiece("rook_test", 10, 1, PieceMovementType.Rook), new Vector2Int(0, 0), true);
            board.GetTile(rook.BoardPosition).OccupyingPiece = rook;

            Assert.That(TileBlockService.TryBlock(board, new Vector2Int(0, 2), rook, null, 1), Is.True);

            MovementResult result = MovementResolver.GetReachableTiles(rook, board);

            Assert.That(result.MoveTiles.Contains(new Vector2Int(0, 1)), Is.True);
            Assert.That(result.MoveTiles.Contains(new Vector2Int(0, 2)), Is.False);
            Assert.That(result.MoveTiles.Contains(new Vector2Int(0, 3)), Is.False);
        }

        [Test]
        public void DestroyObstacle_Preview는상태를유지하고파괴가능장애물만실행한다()
        {
            var board = new BoardState();
            PieceAbilityDefinition ability = CreateAbility(
                "destroy",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.DestroyObstacle));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("breaker", 10, 1, PieceMovementType.King, ability), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int target = new Vector2Int(2, 1);
            board.GetTile(target).SetObstacle(true, true);

            var context = new AbilityExecutionContext(owner, targetPosition: target, board: board);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(ability, context);
            Assert.That(preview.Success, Is.True);
            Assert.That(board.GetTile(target).HasObstacle, Is.True);

            AbilityExecutionResult executed = PieceAbilityService.ExecuteAbility(ability, context);
            Assert.That(executed.Success, Is.True);
            Assert.That(board.GetTile(target).HasObstacle, Is.False);
        }

        [Test]
        public void DestroyObstacle_파괴불가장애물과빈칸은실패한다()
        {
            var board = new BoardState();
            PieceAbilityDefinition ability = CreateAbility(
                "destroy_fail",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.DestroyObstacle));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("breaker2", 10, 1, PieceMovementType.King, ability), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int solid = new Vector2Int(2, 1);
            board.GetTile(solid).SetObstacle(true, false);

            Assert.That(
                PieceAbilityService.ExecuteAbility(
                    ability,
                    new AbilityExecutionContext(owner, targetPosition: solid, board: board)).Success,
                Is.False);

            Vector2Int empty = new Vector2Int(3, 1);

            Assert.That(
                PieceAbilityService.ExecuteAbility(
                    ability,
                    new AbilityExecutionContext(owner, targetPosition: empty, board: board)).Success,
                Is.False);
        }

        [Test]
        public void DestroyObstacle_파괴후MovementResolver가즉시새경로를계산한다()
        {
            var board = new BoardState();

            PieceAbilityDefinition destroy = CreateAbility(
                "destroy_route",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.DestroyObstacle));

            PieceRuntimeState rook =
                new PieceRuntimeState(CreatePiece("route_rook", 10, 1, PieceMovementType.Rook, destroy), new Vector2Int(0, 0), true);
            board.GetTile(rook.BoardPosition).OccupyingPiece = rook;

            Vector2Int obstacle = new Vector2Int(0, 1);
            board.GetTile(obstacle).SetObstacle(true, true);

            MovementResult before = MovementResolver.GetReachableTiles(rook, board);
            Assert.That(before.MoveTiles.Contains(new Vector2Int(0, 2)), Is.False);

            AbilityExecutionResult result = PieceAbilityService.ExecuteAbility(
                destroy,
                new AbilityExecutionContext(rook, targetPosition: obstacle, board: board));

            Assert.That(result.Success, Is.True);

            MovementResult after = MovementResolver.GetReachableTiles(rook, board);
            Assert.That(after.MoveTiles.Contains(new Vector2Int(0, 1)), Is.True);
            Assert.That(after.MoveTiles.Contains(new Vector2Int(0, 2)), Is.True);
        }

        private PieceDefinition CreatePiece(
            string id,
            int hp,
            int atk,
            PieceMovementType movementType,
            params PieceAbilityDefinition[] abilities)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>();
            SetField(definition, "_pieceId", id);
            SetField(definition, "_displayName", id);
            SetField(definition, "_baseHp", hp);
            SetField(definition, "_baseAtk", atk);
            SetField(definition, "_movementType", movementType);
            SetField(definition, "_occupancySize", Vector2Int.one);
            SetField(definition, "_abilities", abilities ?? Array.Empty<PieceAbilityDefinition>());
            _created.Add(definition);
            return definition;
        }

        private PieceAbilityDefinition CreateAbility(
            string id,
            AbilityTrigger trigger,
            AbilityActionCost actionCost,
            params AbilityEffectData[] effects)
        {
            PieceAbilityDefinition ability = ScriptableObject.CreateInstance<PieceAbilityDefinition>();
            SetField(ability, "_abilityId", id);
            SetField(ability, "_displayName", id);
            SetField(ability, "_trigger", trigger);
            SetField(ability, "_actionCost", actionCost);
            SetField(ability, "_effects", effects ?? Array.Empty<AbilityEffectData>());
            _created.Add(ability);
            return ability;
        }

        private StatusEffectDefinition CreateStatus(StatusEffectType type)
        {
            StatusEffectDefinition definition = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            SetField(definition, "_statusType", type);
            SetField(definition, "_displayName", type.ToString());
            SetField(definition, "_stackMode", StatusStackMode.RefreshDuration);
            SetField(definition, "_maxStacks", 1);
            SetField(definition, "_defaultDurationTurns", 1);
            _created.Add(definition);
            return definition;
        }

        private static void SetField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
#endif
