#if UNITY_EDITOR
using System; // Enum 사용
using System.Collections.Generic; // List 사용
using System.Reflection; // private 직렬화 필드 테스트 설정
using NUnit.Framework; // EditMode 테스트
using UnityEditor; // 실제 프로젝트 에셋 로드
using UnityEngine; // ScriptableObject·Vector2Int 사용
using ProjectEta.Abilities; // Day90 Ability Framework 사용
using ProjectEta.Battle; // DamageResolver·BattleHooks·TurnManager 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용

namespace ProjectEta.Tests.EditMode
{
    public sealed class Day90AbilityFrameworkTests
    {
        private readonly List<UnityEngine.Object> _createdObjects = new List<UnityEngine.Object>();

        [SetUp]
        public void SetUp()
        {
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();
        }

        [TearDown]
        public void TearDown()
        {
            AbilityBoardRegistry.Clear();
            AbilityEffectRegistry.ResetToDefaults();

            for (int i = 0; i < _createdObjects.Count; i++)
            {
                if (_createdObjects[i] != null) UnityEngine.Object.DestroyImmediate(_createdObjects[i]);
            }

            _createdObjects.Clear();
        }

        [Test]
        public void AbilityEffectType_공통9종을정의한다()
        {
            Array values = Enum.GetValues(typeof(AbilityEffectType));

            Assert.That(values.Length, Is.EqualTo(9));
            Assert.That(Enum.IsDefined(typeof(AbilityEffectType), AbilityEffectType.Heal), Is.True);
            Assert.That(Enum.IsDefined(typeof(AbilityEffectType), AbilityEffectType.RedirectDamage), Is.True);
        }

        [Test]
        public void AbilityRegistry_90일차3종Executor만기본등록하고중복을차단한다()
        {
            Assert.That(AbilityEffectRegistry.RegisteredCount, Is.EqualTo(3));
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.Heal), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.ModifyDamage), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.RedirectDamage), Is.True);
            Assert.That(AbilityEffectRegistry.IsImplemented(AbilityEffectType.ApplyStatus), Is.False);

            Assert.That(AbilityEffectRegistry.Register(new HealAbilityExecutor()), Is.False);
            Assert.That(AbilityEffectRegistry.RegisteredCount, Is.EqualTo(3));
        }

        [Test]
        public void ExistingPieceDefinitions_Ability가없어도빈배열로안전하게동작한다()
        {
            PieceDatabase database =
                AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset");

            Assert.That(database, Is.Not.Null);
            PieceDefinition pawn = database.FindById("pawn");
            Assert.That(pawn, Is.Not.Null);
            Assert.That(pawn.Abilities, Is.Not.Null);
            Assert.That(pawn.Abilities.Length, Is.EqualTo(0));
        }

        [Test]
        public void PieceDefinition_복수Ability와Ability당복수Effect를보유할수있다()
        {
            PieceAbilityDefinition first = CreateAbility(
                "multi_a",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 2),
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: 1));

            PieceAbilityDefinition second = CreateAbility(
                "multi_b",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 1));

            PieceDefinition definition = CreatePiece("ability_holder", 10, 2, first, second);

            Assert.That(definition.Abilities.Length, Is.EqualTo(2));
            Assert.That(definition.Abilities[0].Effects.Length, Is.EqualTo(2));
            Assert.That(definition.Abilities[1].Effects.Length, Is.EqualTo(1));
        }

        [Test]
        public void Heal_Preview는상태를변경하지않고실행은최대HP에서멈춘다()
        {
            PieceAbilityDefinition heal = CreateAbility(
                "heal_test",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 5));

            PieceDefinition definition = CreatePiece("healer", 10, 1, heal);
            var piece = new PieceRuntimeState(definition, new Vector2Int(1, 1), true);
            piece.CurrentHp = 8;

            var context = new AbilityExecutionContext(piece, piece);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(heal, context);

            Assert.That(preview.Success, Is.True);
            Assert.That(preview.Amount, Is.EqualTo(2));
            Assert.That(piece.CurrentHp, Is.EqualTo(8));

            AbilityExecutionResult executed = PieceAbilityService.ExecuteAbility(heal, context);

            Assert.That(executed.Success, Is.True);
            Assert.That(executed.Amount, Is.EqualTo(2));
            Assert.That(piece.CurrentHp, Is.EqualTo(10));
        }

        [Test]
        public void ActiveHeal_PlayerActionCost를한번만소비한다()
        {
            PieceAbilityDefinition heal = CreateAbility(
                "active_heal",
                AbilityTrigger.Active,
                AbilityActionCost.PlayerAction,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 2));

            PieceDefinition definition = CreatePiece("active_healer", 10, 1, heal);
            var piece = new PieceRuntimeState(definition, new Vector2Int(1, 1), true);
            piece.CurrentHp = 5;
            var turn = new TurnManager();

            turn.MarkInitialKingPlaced();
            Assert.That(turn.TryEndDeploymentTurn(), Is.True);
            Assert.That(turn.CanPlayerAct, Is.True);

            AbilityExecutionResult result = PieceAbilityService.ExecuteAbility(
                heal,
                new AbilityExecutionContext(piece, piece, turnManager: turn));

            Assert.That(result.Success, Is.True);
            Assert.That(result.ConsumedAction, Is.True);
            Assert.That(piece.CurrentHp, Is.EqualTo(7));
            Assert.That(turn.CurrentState, Is.EqualTo(TurnState.EnemyTurn));
        }

        [Test]
        public void UnimplementedEffect_실행전에안전하게실패해앞Effect도적용하지않는다()
        {
            PieceAbilityDefinition mixed = CreateAbility(
                "mixed_unimplemented",
                AbilityTrigger.Active,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.Heal, amount: 3),
                new AbilityEffectData(AbilityEffectType.ApplyStatus, durationTurns: 2));

            PieceDefinition definition = CreatePiece("mixed_piece", 10, 1, mixed);
            var piece = new PieceRuntimeState(definition, new Vector2Int(1, 1), true);
            piece.CurrentHp = 4;

            AbilityExecutionResult result =
                PieceAbilityService.ExecuteAbility(mixed, new AbilityExecutionContext(piece, piece));

            Assert.That(result.Success, Is.False);
            Assert.That(piece.CurrentHp, Is.EqualTo(4));
            StringAssert.Contains("아직 구현되지 않았습니다", result.FailureReason);
        }

        [Test]
        public void ModifyDamage_공격자증가와피격자감소를순서대로적용한다()
        {
            PieceAbilityDefinition attackBoost = CreateAbility(
                "attack_boost",
                AbilityTrigger.BeforeDamage,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: 3));

            PieceAbilityDefinition defense = CreateAbility(
                "damage_reduce",
                AbilityTrigger.BeforeDamage,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: -2));

            PieceRuntimeState attacker = new PieceRuntimeState(
                CreatePiece("attacker", 10, 5, attackBoost),
                new Vector2Int(4, 3),
                true);

            PieceRuntimeState defender = new PieceRuntimeState(
                CreatePiece("defender", 10, 1, defense),
                new Vector2Int(4, 4),
                false);

            int applied = DamageResolver.ApplyDamage(defender, 5, attacker);

            Assert.That(applied, Is.EqualTo(6)); // 5 + 3 - 2
            Assert.That(defender.CurrentHp, Is.EqualTo(4));
        }

        [Test]
        public void ModifyDamage_감소효과는일반피해를최소1로유지한다()
        {
            PieceAbilityDefinition defense = CreateAbility(
                "strong_reduce",
                AbilityTrigger.BeforeDamage,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: -99));

            PieceRuntimeState attacker =
                new PieceRuntimeState(CreatePiece("source", 10, 1), new Vector2Int(1, 1), true);
            PieceRuntimeState defender =
                new PieceRuntimeState(CreatePiece("tank", 10, 1, defense), new Vector2Int(1, 2), false);

            int applied = DamageResolver.ApplyDamage(defender, 4, attacker);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(defender.CurrentHp, Is.EqualTo(9));
        }

        [Test]
        public void RedirectDamage_인접보호자가대신피해를받는다()
        {
            BoardState board = new BoardState();

            PieceAbilityDefinition redirect = CreateAbility(
                "redirect",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 1));

            PieceRuntimeState protectedPiece =
                new PieceRuntimeState(CreatePiece("protected", 10, 1), new Vector2Int(4, 4), true);
            PieceRuntimeState protector =
                new PieceRuntimeState(CreatePiece("protector", 10, 1, redirect), new Vector2Int(3, 4), true);
            PieceRuntimeState attacker =
                new PieceRuntimeState(CreatePiece("enemy", 10, 1), new Vector2Int(4, 5), false);

            board.GetTile(protectedPiece.BoardPosition).OccupyingPiece = protectedPiece;
            board.GetTile(protector.BoardPosition).OccupyingPiece = protector;
            board.GetTile(attacker.BoardPosition).OccupyingPiece = attacker;

            int applied = DamageResolver.ApplyDamage(protectedPiece, 3, attacker);

            Assert.That(applied, Is.EqualTo(3));
            Assert.That(protectedPiece.CurrentHp, Is.EqualTo(10));
            Assert.That(protector.CurrentHp, Is.EqualTo(7));
        }

        [Test]
        public void RedirectDamage_동일거리후보는좌표순으로항상같은보호자를선택한다()
        {
            BoardState board = new BoardState();

            PieceAbilityDefinition redirect = CreateAbility(
                "redirect_priority",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 1));

            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target", 10, 1), new Vector2Int(4, 4), true);
            PieceRuntimeState left =
                new PieceRuntimeState(CreatePiece("left", 10, 1, redirect), new Vector2Int(3, 4), true);
            PieceRuntimeState right =
                new PieceRuntimeState(CreatePiece("right", 10, 1, redirect), new Vector2Int(5, 4), true);
            PieceRuntimeState attacker =
                new PieceRuntimeState(CreatePiece("enemy2", 10, 1), new Vector2Int(4, 5), false);

            board.GetTile(target.BoardPosition).OccupyingPiece = target;
            board.GetTile(left.BoardPosition).OccupyingPiece = left;
            board.GetTile(right.BoardPosition).OccupyingPiece = right;
            board.GetTile(attacker.BoardPosition).OccupyingPiece = attacker;

            DamageResolver.ApplyDamage(target, 2, attacker);

            Assert.That(target.CurrentHp, Is.EqualTo(10));
            Assert.That(left.CurrentHp, Is.EqualTo(8));
            Assert.That(right.CurrentHp, Is.EqualTo(10));
        }

        [Test]
        public void RedirectDamage_보호자자신의ModifyDamage를Redirect후적용한다()
        {
            BoardState board = new BoardState();

            PieceAbilityDefinition redirect = CreateAbility(
                "redirect_with_defense",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 1));

            PieceAbilityDefinition defense = CreateAbility(
                "redirect_defense",
                AbilityTrigger.BeforeDamage,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.ModifyDamage, amount: -2));

            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("protected2", 10, 1), new Vector2Int(4, 4), true);
            PieceRuntimeState protector =
                new PieceRuntimeState(CreatePiece("guardian", 10, 1, redirect, defense), new Vector2Int(3, 4), true);
            PieceRuntimeState attacker =
                new PieceRuntimeState(CreatePiece("enemy3", 10, 1), new Vector2Int(4, 5), false);

            board.GetTile(target.BoardPosition).OccupyingPiece = target;
            board.GetTile(protector.BoardPosition).OccupyingPiece = protector;
            board.GetTile(attacker.BoardPosition).OccupyingPiece = attacker;

            int applied = DamageResolver.ApplyDamage(target, 3, attacker);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(target.CurrentHp, Is.EqualTo(10));
            Assert.That(protector.CurrentHp, Is.EqualTo(9));
        }

        [Test]
        public void RedirectDamage_한피해Context에서두번Redirect할수없다()
        {
            PieceAbilityDefinition redirect = CreateAbility(
                "redirect_once",
                AbilityTrigger.Passive,
                AbilityActionCost.None,
                new AbilityEffectData(AbilityEffectType.RedirectDamage, radius: 2));

            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("target_once", 10, 1), new Vector2Int(4, 4), true);
            PieceRuntimeState first =
                new PieceRuntimeState(CreatePiece("first_guard", 10, 1, redirect), new Vector2Int(3, 4), true);
            PieceRuntimeState second =
                new PieceRuntimeState(CreatePiece("second_guard", 10, 1, redirect), new Vector2Int(2, 4), true);

            var damage = new DamageContext(target, null, 5);
            Assert.That(AbilityEffectRegistry.TryGet(AbilityEffectType.RedirectDamage, out IAbilityEffectExecutor executor), Is.True);

            AbilityExecutionResult firstResult = executor.Execute(
                redirect.Effects[0],
                new AbilityExecutionContext(first, target, damageContext: damage));

            AbilityExecutionResult secondResult = executor.Execute(
                redirect.Effects[0],
                new AbilityExecutionContext(second, first, damageContext: damage));

            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.False);
            Assert.That(damage.Target, Is.SameAs(first));
            Assert.That(damage.RedirectCount, Is.EqualTo(1));
        }

        [Test]
        public void ExistingBeforeDamageHook_Ability처리후에도피해를0으로차단할수있다()
        {
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("hook_target", 10, 1), new Vector2Int(1, 1), true);
            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("hook_source", 10, 1), new Vector2Int(1, 2), false);
            var hooks = new BattleHooks();

            hooks.BeforeDamage += context => context.Amount = 0;

            int applied = DamageResolver.ApplyDamage(target, 5, source, hooks);

            Assert.That(applied, Is.EqualTo(0));
            Assert.That(target.CurrentHp, Is.EqualTo(10));
        }

        [Test]
        public void Ability없는기물은기존DamageResolver결과를그대로유지한다()
        {
            PieceRuntimeState target =
                new PieceRuntimeState(CreatePiece("plain_target", 10, 1), new Vector2Int(1, 1), true);
            PieceRuntimeState source =
                new PieceRuntimeState(CreatePiece("plain_source", 10, 1), new Vector2Int(1, 2), false);

            int applied = DamageResolver.ApplyDamage(target, 4, source);

            Assert.That(applied, Is.EqualTo(4));
            Assert.That(target.CurrentHp, Is.EqualTo(6));
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
