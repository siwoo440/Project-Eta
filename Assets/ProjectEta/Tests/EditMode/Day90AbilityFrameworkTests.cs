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
    public sealed class Day90AbilityFrameworkTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            AbilityBoardRegistry.Clear();
            TemporarySummonService.ClearAll();
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
        public void AbilityEffectType_공통9종을유지한다()
        {
            Assert.That(Enum.GetValues(typeof(AbilityEffectType)).Length, Is.EqualTo(9));
        }

        [Test]
        public void Day90CoreExecutors_후속일차Registry확장후에도유지된다()
        {
            Assert.That(AbilityEffectRegistry.RegisteredCount, Is.GreaterThanOrEqualTo(3));
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.Heal), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyDamage), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.RedirectDamage), Is.True);
            Assert.That(AbilityEffectRegistry.Register(new HealAbilityExecutor()), Is.False);
        }

        [Test]
        public void 아직미구현된Effect는복합Ability전체를실행전에차단한다()
        {
            PieceAbilityDefinition ability = CreateAbility(
                "future_effect",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 2),
                new AbilityEffectData(AbilityEffectType.ModifyMoveRange, amount: 1));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("future", 10, 1, ability), Vector2Int.zero, true);
            piece.CurrentHp = 5;

            AbilityExecutionResult result =
                PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(piece, piece));

            Assert.That(result.Success, Is.False);
            Assert.That(piece.CurrentHp, Is.EqualTo(5));
        }

        [Test]
        public void Heal_Preview는상태를변경하지않고최대HP에서멈춘다()
        {
            PieceAbilityDefinition ability = CreateAbility(
                "heal",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 5));

            PieceRuntimeState piece =
                new PieceRuntimeState(CreatePiece("heal_piece", 10, 1, ability), Vector2Int.zero, true);
            piece.CurrentHp = 8;

            AbilityExecutionResult preview =
                PieceAbilityService.PreviewAbility(ability, new AbilityExecutionContext(piece, piece));

            Assert.That(preview.Amount, Is.EqualTo(2));
            Assert.That(piece.CurrentHp, Is.EqualTo(8));

            PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(piece, piece));
            Assert.That(piece.CurrentHp, Is.EqualTo(10));
        }

        [Test]
        public void ModifyDamage_최소피해1과기존BeforeDamage0차단을함께유지한다()
        {
            PieceAbilityDefinition defense = CreateAbility(
                "defense",
                AbilityTrigger.BeforeDamage,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: -99));

            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target", 10, 1, defense), new Vector2Int(1, 1), true);
            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("source", 10, 1), new Vector2Int(1, 2), false);

            Assert.That(DamageResolver.ApplyDamage(target, 4, source), Is.EqualTo(1));

            target.CurrentHp = 10;
            var hooks = new BattleHooks();
            hooks.BeforeDamage += context => context.Amount = 0;

            Assert.That(DamageResolver.ApplyDamage(target, 4, source, hooks), Is.EqualTo(0));
            Assert.That(target.CurrentHp, Is.EqualTo(10));
        }

        [Test]
        public void RedirectDamage_보호자에게피해를전환한다()
        {
            var board = new BoardState();

            PieceAbilityDefinition redirect = CreateAbility(
                "redirect",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 1));

            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target_r", 10, 1), new Vector2Int(4, 4), true);
            PieceRuntimeState protector =
                new PieceRuntimeState(CreatePiece("protector", 10, 1, redirect), new Vector2Int(3, 4), true);
            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("enemy", 10, 1), new Vector2Int(4, 5), false);

            board.GetTile(target.BoardPosition).OccupyingPiece = target;
            board.GetTile(protector.BoardPosition).OccupyingPiece = protector;
            board.GetTile(source.BoardPosition).OccupyingPiece = source;

            DamageResolver.ApplyDamage(target, 3, source);

            Assert.That(target.CurrentHp, Is.EqualTo(10));
            Assert.That(protector.CurrentHp, Is.EqualTo(7));
        }

        private PieceDefinition CreatePiece(
            string pieceId,
            int hp,
            int atk,
            params PieceAbilityDefinition[] abilities)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>();
            SetField(definition, "_pieceId", pieceId);
            SetField(definition, "_displayName", pieceId);
            SetField(definition, "_baseHp", hp);
            SetField(definition, "_baseAtk", atk);
            SetField(definition, "_occupancySize", Vector2Int.one);
            SetField(definition, "_abilities", abilities ?? Array.Empty<PieceAbilityDefinition>());
            _createdObjects.Add(definition);
            return definition;
        }

        private PieceAbilityDefinition CreateAbility(
            string abilityId,
            AbilityTrigger trigger,
            AbilityActionCost actionCost,
            params AbilityEffectData[] effects)
        {
            PieceAbilityDefinition ability = ScriptableObject.CreateInstance<PieceAbilityDefinition>();
            SetField(ability, "_abilityId", abilityId);
            SetField(ability, "_displayName", abilityId);
            SetField(ability, "_trigger", trigger);
            SetField(ability, "_actionCost", actionCost);
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
