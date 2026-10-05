#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Cards;
using ProjectEta.Pieces;
using ProjectEta.Run;

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day91StatusAuraSummonTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            TemporarySummonService.ClearAll();
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            TemporarySummonService.ClearAll();
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();

            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null) UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void Registry_91일차완료후6개Effect를구현한다()
        {
            Assert.That(AbilityEffectRegistry.RegisteredCount, Is.EqualTo(6));
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.ApplyStatus), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.Aura), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.Summon), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyMoveRange), Is.False);
        }

        [TestCase(StatusEffectType.Poison)]
        [TestCase(StatusEffectType.Burn)]
        [TestCase(StatusEffectType.Stun)]
        [TestCase(StatusEffectType.Root)]
        public void ApplyStatus_기존4종상태효과를공통Ability로적용한다(StatusEffectType statusType)
        {
            StatusEffectDefinition status = CreateStatus(statusType);
            PieceAbilityDefinition ability = CreateAbility(
                "status_" + statusType,
                AbilityTrigger.Active,
                new AbilityEffectData(
                    AbilityEffectType.ApplyStatus,
                    statusEffect: status));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("caster", 10, 1, ability), new Vector2Int(1, 1), true);
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target_" + statusType, 10, 1), new Vector2Int(1, 2), false);

            AbilityExecutionResult preview =
                PieceAbilityService.PreviewAbility(ability, new AbilityExecutionContext(owner, target));

            Assert.That(preview.Success, Is.True);
            Assert.That(target.HasStatus(statusType), Is.False);

            AbilityExecutionResult executed =
                PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(owner, target));

            Assert.That(executed.Success, Is.True);
            Assert.That(target.HasStatus(statusType), Is.True);

            if (statusType == StatusEffectType.Stun)
            {
                Assert.That(target.CanMove, Is.False);
                Assert.That(target.CanAttack, Is.False);
            }

            if (statusType == StatusEffectType.Root)
            {
                Assert.That(target.CanMove, Is.False);
                Assert.That(target.CanAttack, Is.True);
            }
        }

        [Test]
        public void ApplyStatus_면역대상에는실행전부터실패한다()
        {
            StatusEffectDefinition poison = CreateStatus(StatusEffectType.Poison);
            PieceAbilityDefinition ability = CreateAbility(
                "poison",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.ApplyStatus, statusEffect: poison));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("caster2", 10, 1, ability), Vector2Int.zero, true);
            PieceRuntimeState immune =
                new PieceRuntimeState(
                    CreatePiece("immune", 10, 1, ability: null, immunity: StatusEffectType.Poison),
                    Vector2Int.one,
                    false);

            AbilityExecutionResult result =
                PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(owner, immune));

            Assert.That(result.Success, Is.False);
            Assert.That(immune.HasStatus(StatusEffectType.Poison), Is.False);
        }

        [Test]
        public void ApplyStatus_독은중첩하고화상은지속시간만갱신한다()
        {
            StatusEffectDefinition poison = CreateStatus(
                StatusEffectType.Poison,
                StatusStackMode.StacksAdd,
                maxStacks: 3,
                duration: 3);

            StatusEffectDefinition burn = CreateStatus(
                StatusEffectType.Burn,
                StatusStackMode.RefreshDuration,
                maxStacks: 1,
                duration: 3);

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("caster3", 10, 1), Vector2Int.zero, true);
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target3", 10, 1), Vector2Int.one, false);

            PieceAbilityDefinition poisonAbility = CreateAbility(
                "poison_stack",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.ApplyStatus, statusEffect: poison));
            PieceAbilityDefinition burnAbility = CreateAbility(
                "burn_refresh",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.ApplyStatus, statusEffect: burn));

            PieceAbilityService.ExecuteAbility(poisonAbility, new AbilityExecutionContext(owner, target));
            PieceAbilityService.ExecuteAbility(poisonAbility, new AbilityExecutionContext(owner, target));
            Assert.That(target.FindStatus(StatusEffectType.Poison).StackCount, Is.EqualTo(2));

            PieceAbilityService.ExecuteAbility(burnAbility, new AbilityExecutionContext(owner, target));
            target.FindStatus(StatusEffectType.Burn).Tick();
            PieceAbilityService.ExecuteAbility(burnAbility, new AbilityExecutionContext(owner, target));

            Assert.That(target.FindStatus(StatusEffectType.Burn).StackCount, Is.EqualTo(1));
            Assert.That(target.FindStatus(StatusEffectType.Burn).RemainingTurns, Is.EqualTo(3));
        }

        [Test]
        public void Aura_범위안같은진영공격력을동적으로증가시킨다()
        {
            var board = new BoardState();

            PieceAbilityDefinition aura = CreateAbility(
                "attack_aura",
                AbilityTrigger.Passive,
                new AbilityEffectData(
                    AbilityEffectType.Aura,
                    amount: 2,
                    radius: 1,
                    auraGroupId: "attack_power"));

            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("aura_source", 10, 1, aura), new Vector2Int(4, 4), true);
            PieceRuntimeState ally =
                new PieceRuntimeState(CreatePiece("ally", 10, 4), new Vector2Int(5, 5), true);
            PieceRuntimeState enemy =
                new PieceRuntimeState(CreatePiece("enemy", 10, 4), new Vector2Int(4, 5), false);

            board.GetTile(source.BoardPosition).OccupyingPiece = source;
            board.GetTile(ally.BoardPosition).OccupyingPiece = ally;
            board.GetTile(enemy.BoardPosition).OccupyingPiece = enemy;

            Assert.That(AuraResolver.GetAttack(ally, board), Is.EqualTo(6));
            Assert.That(AuraResolver.GetAttack(enemy, board), Is.EqualTo(4));
            Assert.That(ally.Definition.BaseAtk, Is.EqualTo(4));
        }

        [Test]
        public void Aura_같은Group은최고값만다른Group은합산한다()
        {
            var board = new BoardState();

            PieceAbilityDefinition weak = CreateAbility(
                "weak",
                AbilityTrigger.Passive,
                new AbilityEffectData(AbilityEffectType.Aura, amount: 1, radius: 2, auraGroupId: "attack"));
            PieceAbilityDefinition strong = CreateAbility(
                "strong",
                AbilityTrigger.Passive,
                new AbilityEffectData(AbilityEffectType.Aura, amount: 3, radius: 2, auraGroupId: "attack"));
            PieceAbilityDefinition other = CreateAbility(
                "other",
                AbilityTrigger.Passive,
                new AbilityEffectData(AbilityEffectType.Aura, amount: 2, radius: 2, auraGroupId: "command"));

            PieceRuntimeState a =
                new PieceRuntimeState(CreatePiece("a", 10, 1, weak), new Vector2Int(3, 4), true);
            PieceRuntimeState b =
                new PieceRuntimeState(CreatePiece("b", 10, 1, strong), new Vector2Int(4, 3), true);
            PieceRuntimeState c =
                new PieceRuntimeState(CreatePiece("c", 10, 1, other), new Vector2Int(5, 4), true);
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target_aura", 10, 4), new Vector2Int(4, 4), true);

            board.GetTile(a.BoardPosition).OccupyingPiece = a;
            board.GetTile(b.BoardPosition).OccupyingPiece = b;
            board.GetTile(c.BoardPosition).OccupyingPiece = c;
            board.GetTile(target.BoardPosition).OccupyingPiece = target;

            Assert.That(AuraResolver.GetAttackModifier(target, board), Is.EqualTo(5)); // attack +3, command +2
            Assert.That(AuraResolver.GetAttack(target, board), Is.EqualTo(9));
        }

        [Test]
        public void Aura_범위이탈과Source사망시즉시효과가사라진다()
        {
            var board = new BoardState();

            PieceAbilityDefinition aura = CreateAbility(
                "range_aura",
                AbilityTrigger.Passive,
                new AbilityEffectData(AbilityEffectType.Aura, amount: 2, radius: 1, auraGroupId: "attack"));

            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("source_aura", 10, 1, aura), new Vector2Int(4, 4), true);
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target_aura2", 10, 4), new Vector2Int(5, 4), true);

            board.GetTile(source.BoardPosition).OccupyingPiece = source;
            board.GetTile(target.BoardPosition).OccupyingPiece = target;

            Assert.That(AuraResolver.GetAttack(target, board), Is.EqualTo(6));

            board.GetTile(target.BoardPosition).OccupyingPiece = null;
            target.BoardPosition = new Vector2Int(7, 7);
            board.GetTile(target.BoardPosition).OccupyingPiece = target;
            Assert.That(AuraResolver.GetAttack(target, board), Is.EqualTo(4));

            board.GetTile(target.BoardPosition).OccupyingPiece = null;
            target.BoardPosition = new Vector2Int(5, 4);
            board.GetTile(target.BoardPosition).OccupyingPiece = target;
            source.CurrentHp = 0;

            Assert.That(AuraResolver.GetAttack(target, board), Is.EqualTo(4));
        }

        [Test]
        public void CombatResolver_실제공격피해에Aura보정을사용한다()
        {
            var board = new BoardState();

            PieceAbilityDefinition aura = CreateAbility(
                "combat_aura",
                AbilityTrigger.Passive,
                new AbilityEffectData(AbilityEffectType.Aura, amount: 2, radius: 1, auraGroupId: "attack"));

            PieceRuntimeState auraSource =
                new PieceRuntimeState(CreatePiece("aura_combat", 10, 1, aura), new Vector2Int(3, 4), true);
            PieceRuntimeState attacker =
                new PieceRuntimeState(CreatePiece("attacker", 10, 4), new Vector2Int(4, 4), true);
            PieceRuntimeState defender =
                new PieceRuntimeState(CreatePiece("defender", 10, 1), new Vector2Int(4, 5), false);

            board.GetTile(auraSource.BoardPosition).OccupyingPiece = auraSource;
            board.GetTile(attacker.BoardPosition).OccupyingPiece = attacker;
            board.GetTile(defender.BoardPosition).OccupyingPiece = defender;

            CombatResult result = CombatResolver.ResolveAttack(attacker, defender);

            Assert.That(result.DamageDealt, Is.EqualTo(6));
            Assert.That(defender.CurrentHp, Is.EqualTo(4));
        }

        [Test]
        public void Summon_Preview는Board를변경하지않고Execute만임시기물을배치한다()
        {
            var run = new RunState(3);
            var turn = new TurnManager();

            PieceDefinition summonTemplate = CreatePiece("summon_template", 3, 1);
            PieceAbilityDefinition summon = CreateAbility(
                "summon",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.Summon, summonPiece: summonTemplate));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("summoner", 10, 1, summon), new Vector2Int(1, 1), true);
            run.Board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int target = new Vector2Int(2, 1);
            var context = new AbilityExecutionContext(
                owner,
                targetPosition: target,
                board: run.Board,
                runState: run,
                turnManager: turn);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(summon, context);

            Assert.That(preview.Success, Is.True);
            Assert.That(run.Board.GetTile(target).OccupyingPiece, Is.Null);

            AbilityExecutionResult executed = PieceAbilityService.ExecuteAbility(summon, context);

            Assert.That(executed.Success, Is.True);
            Assert.That(executed.ActualTarget, Is.Not.Null);
            Assert.That(executed.ActualTarget.IsTemporarySummon, Is.True);
            Assert.That(executed.ActualTarget.Definition.IsRuntimeTemporarySummonDefinition, Is.True);
            Assert.That(executed.ActualTarget.Definition.PieceId, Is.Empty);
            Assert.That(run.Board.GetTile(target).OccupyingPiece, Is.SameAs(executed.ActualTarget));
        }

        [Test]
        public void Summon_점유칸과장애물칸에서는실행전실패한다()
        {
            var board = new BoardState();
            PieceDefinition template = CreatePiece("summon_blocked", 3, 1);
            PieceAbilityDefinition summon = CreateAbility(
                "summon_blocked_ability",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.Summon, summonPiece: template));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("summoner2", 10, 1, summon), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int occupied = new Vector2Int(2, 1);
            board.GetTile(occupied).OccupyingPiece =
                new PieceRuntimeState(CreatePiece("occupier", 3, 1), occupied, true);

            Assert.That(
                PieceAbilityService.PreviewAbility(
                    summon,
                    new AbilityExecutionContext(owner, targetPosition: occupied, board: board)).Success,
                Is.False);

            Vector2Int obstacle = new Vector2Int(3, 1);
            board.GetTile(obstacle).IsBlockedByObstacle = true;

            Assert.That(
                PieceAbilityService.PreviewAbility(
                    summon,
                    new AbilityExecutionContext(owner, targetPosition: obstacle, board: board)).Success,
                Is.False);
        }

        [Test]
        public void TemporarySummon_카드Pool과DeadPile에들어가지않는다()
        {
            var run = new RunState(3);
            PieceDefinition template = CreatePiece("pool_summon", 3, 1);
            PieceAbilityDefinition summon = CreateAbility(
                "pool_summon_ability",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.Summon, summonPiece: template));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("summoner3", 10, 1, summon), new Vector2Int(1, 1), true);
            run.Board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            AbilityExecutionResult result = PieceAbilityService.ExecuteAbility(
                summon,
                new AbilityExecutionContext(
                    owner,
                    targetPosition: new Vector2Int(2, 1),
                    board: run.Board,
                    runState: run));

            PieceDefinition runtimeDefinition = result.ActualTarget.Definition;

            run.Deck.AddToOwnedPool(runtimeDefinition);
            run.Deck.AddToDrawPile(runtimeDefinition);
            run.Deck.MoveToDeadPile(runtimeDefinition);

            Assert.That(run.Deck.OwnedCardPool, Is.Empty);
            Assert.That(run.Deck.DrawPile, Is.Empty);
            Assert.That(run.Deck.DeadCardPile, Is.Empty);
        }

        [Test]
        public void TemporarySummon_전투종료시Board에서제거된다()
        {
            var board = new BoardState();
            var turn = new TurnManager();

            PieceDefinition template = CreatePiece("cleanup_summon", 3, 1);
            PieceAbilityDefinition summon = CreateAbility(
                "cleanup_summon_ability",
                AbilityTrigger.Active,
                new AbilityEffectData(AbilityEffectType.Summon, summonPiece: template));

            PieceRuntimeState owner =
                new PieceRuntimeState(CreatePiece("summoner4", 10, 1, summon), new Vector2Int(1, 1), true);
            board.GetTile(owner.BoardPosition).OccupyingPiece = owner;

            Vector2Int target = new Vector2Int(2, 1);
            PieceAbilityService.ExecuteAbility(
                summon,
                new AbilityExecutionContext(
                    owner,
                    targetPosition: target,
                    board: board,
                    turnManager: turn));

            Assert.That(board.GetTile(target).OccupyingPiece, Is.Not.Null);
            Assert.That(TemporarySummonService.ActiveCount, Is.EqualTo(1));

            turn.EndBattle(BattleOutcome.Victory);

            Assert.That(board.GetTile(target).OccupyingPiece, Is.Null);
            Assert.That(TemporarySummonService.ActiveCount, Is.EqualTo(0));
        }

        private StatusEffectDefinition CreateStatus(
            StatusEffectType type,
            StatusStackMode stackMode = StatusStackMode.RefreshDuration,
            int maxStacks = 1,
            int duration = 3)
        {
            StatusEffectDefinition definition = ScriptableObject.CreateInstance<StatusEffectDefinition>();
            SetField(definition, "_statusType", type);
            SetField(definition, "_displayName", type.ToString());
            SetField(definition, "_stackMode", stackMode);
            SetField(definition, "_maxStacks", maxStacks);
            SetField(definition, "_defaultDurationTurns", duration);
            SetField(definition, "_tickDamagePerStack", type == StatusEffectType.Poison || type == StatusEffectType.Burn ? 1 : 0);
            _createdObjects.Add(definition);
            return definition;
        }

        private PieceDefinition CreatePiece(
            string pieceId,
            int hp,
            int atk,
            PieceAbilityDefinition ability = null,
            StatusEffectType immunity = StatusEffectType.None)
        {
            return CreatePiece(
                pieceId,
                hp,
                atk,
                ability != null ? new[] { ability } : Array.Empty<PieceAbilityDefinition>(),
                immunity);
        }

        private PieceDefinition CreatePiece(
            string pieceId,
            int hp,
            int atk,
            PieceAbilityDefinition[] abilities,
            StatusEffectType immunity = StatusEffectType.None)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>();
            SetField(definition, "_pieceId", pieceId);
            SetField(definition, "_displayName", pieceId);
            SetField(definition, "_baseHp", hp);
            SetField(definition, "_baseAtk", atk);
            SetField(definition, "_occupancySize", Vector2Int.one);
            SetField(definition, "_immuneStatusTags", immunity);
            SetField(definition, "_abilities", abilities ?? Array.Empty<PieceAbilityDefinition>());
            _createdObjects.Add(definition);
            return definition;
        }

        private PieceAbilityDefinition CreateAbility(
            string id,
            AbilityTrigger trigger,
            params AbilityEffectData[] effects)
        {
            PieceAbilityDefinition ability = ScriptableObject.CreateInstance<PieceAbilityDefinition>();
            SetField(ability, "_abilityId", id);
            SetField(ability, "_displayName", id);
            SetField(ability, "_trigger", trigger);
            SetField(ability, "_actionCost", AbilityActionCost.None);
            SetField(ability, "_effects", effects ?? Array.Empty<AbilityEffectData>());
            _createdObjects.Add(ability);
            return ability;
        }

        private static void SetField<TValue>(object target, string fieldName, TValue value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
#endif
