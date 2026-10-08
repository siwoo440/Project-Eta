using System; // 기능 참조
using NUnit.Framework; // 기능 참조
using UnityEditor; // 기능 참조
using UnityEngine; // 기능 참조
using ProjectEta.Abilities; // 기능 참조
using ProjectEta.Battle; // 기능 참조
using ProjectEta.Board; // 기능 참조
using ProjectEta.Pieces; // 기능 참조
using ProjectEta.AI; // 기능 참조
using ProjectEta.Run; // 기능 참조
using ProjectEta.Cards; // 카드 제한 참조
using ProjectEta.Fusion; // 합성 제한 참조

namespace ProjectEta.Tests.EditMode // 기능 영역
{ // 범위 시작
    public sealed class Day95FiveStarIntegrationTests // 검증 동작 구성
    { // 범위 시작
        private PieceDatabase _database; // 검증 보조 구성
        [SetUp] // 테스트 지정
        public void SetUp() // 동작 검증
        { // 범위 시작
            AbilityBoardRegistry.Clear(); // 검증 동작 구성
            TemporaryDamageModifierService.Clear(); // 검증 동작 구성
            AbilityEffectRegistry.ResetToDefaults(); // 검증 동작 구성
            _database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 검증 동작 구성
        } // 범위 종료
        [TearDown] // 테스트 지정
        public void TearDown() // 동작 검증
        { // 범위 시작
            AbilityBoardRegistry.Clear(); // 검증 동작 구성
            TemporaryDamageModifierService.Clear(); // 검증 동작 구성
        } // 범위 종료
        [TestCase("siege_commander")] // 테스트 지정
        [TestCase("grand_paladin")] // 테스트 지정
        [TestCase("grand_rider")] // 테스트 지정
        [TestCase("phantom_general")] // 테스트 지정
        [TestCase("emperor")] // 테스트 지정
        [TestCase("sky_marshal")] // 테스트 지정
        [TestCase("grand_sage")] // 테스트 지정
        [TestCase("iron_regent")] // 테스트 지정
        public void FiveStar_능력연결후에도보유상한1개유지(string id) // 동작 검증
        { // 범위 시작
            var definition = _database.FindById(id); // 검증 동작 구성
            Assert.That(definition.Abilities.Length, Is.GreaterThan(0), id); // 결과 검증
            Assert.That(FusionRuleValidator.ValidateOwnedLimit(definition, 0), Is.EqualTo(FusionBlockReason.None)); // 첫 결과 허용 검증
            Assert.That(FusionRuleValidator.ValidateOwnedLimit(definition, 1), Is.EqualTo(FusionBlockReason.OwnedLimitReached)); // 중복 결과 거부 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Paladin_대각인접아군의첫피해만2감소() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var paladin = Spawn(board, "grand_paladin", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "emperor", 4, 4); // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(ally, 5), Is.EqualTo(3)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(ally, 5), Is.EqualTo(5)); // 결과 검증
            Assert.That(paladin.CurrentHp, Is.EqualTo(8)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Paladin_자신과거리2아군은보호하지않음() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var paladin = Spawn(board, "grand_paladin", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "emperor", 5, 3); // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(paladin, 3), Is.EqualTo(3)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(ally, 3), Is.EqualTo(3)); // 결과 검증
        } // 범위 종료
        [TestCase("grand_rider", 4, 6, 10)] // 테스트 지정
        [TestCase("grand_rider", 3, 4, 8)] // 테스트 지정
        [TestCase("grand_rider", 4, 8, 10)] // 테스트 지정
        [TestCase("sky_marshal", 4, 6, 11)] // 테스트 지정
        [TestCase("sky_marshal", 4, 8, 11)] // 테스트 지정
        [TestCase("sky_marshal", 4, 5, 9)] // 테스트 지정
        public void Rider_두번반복공격만피해증가(string id, int x, int y, int damage) // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var actor = Spawn(board, id, 2, 2); // 검증 동작 구성
            var target = Spawn(board, "grand_sage", x, y, false); // 검증 동작 구성
            Assert.That(CombatResolver.ResolveAttack(actor, target).DamageDealt, Is.EqualTo(damage)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Emperor_동일계열오라는중첩없이1증가() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            Spawn(board, "emperor", 3, 3); // 검증 동작 구성
            Spawn(board, "emperor", 5, 3); // 검증 동작 구성
            var ally = Spawn(board, "grand_sage", 4, 3); // 검증 동작 구성
            Assert.That(AuraResolver.GetAttack(ally), Is.EqualTo(7)); // 결과 검증
            Assert.That(ally.Definition.BaseAtk, Is.EqualTo(6)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Emperor_같은배치턴이벤트반복에도회복1회() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            Spawn(board, "emperor", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "grand_sage", 4, 3); // 검증 동작 구성
            ally.CurrentHp = 4; // 검증 동작 구성
            var hooks = new BattleHooks(); // 검증 동작 구성
            hooks.RaiseTurnStart(TurnState.DeploymentTurn, 5); // 검증 동작 구성
            hooks.RaiseTurnStart(TurnState.DeploymentTurn, 5); // 검증 동작 구성
            Assert.That(ally.CurrentHp, Is.EqualTo(5)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void IronRegent_거리2의피해2를분담하고HP1을유지() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var regent = Spawn(board, "iron_regent", 2, 2); // 검증 동작 구성
            var ally = Spawn(board, "emperor", 4, 4); // 검증 동작 구성
            regent.CurrentHp = 2; // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(ally, 5), Is.EqualTo(4)); // 결과 검증
            Assert.That(regent.CurrentHp, Is.EqualTo(1)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(ally, 3), Is.EqualTo(3)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_독선택은적에게2중첩부여() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3); // 검증 동작 구성
            var enemy = Spawn(board, "iron_regent", 5, 3, false); // 검증 동작 구성
            var ability = Find(sage, "five_sage_poison"); // 검증 동작 구성
            var result = PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(sage, enemy, board: board)); // 검증 동작 구성
            Assert.That(result.Success, Is.True); // 결과 검증
            Assert.That(enemy.FindStatus(StatusEffectType.Poison).StackCount, Is.EqualTo(2)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_피해감소는공격이아닌다음피격1회에적용() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "iron_regent", 4, 3); // 검증 동작 구성
            var enemy = Spawn(board, "grand_sage", 4, 5, false); // 검증 동작 구성
            var ability = Find(sage, "five_sage_guard"); // 검증 동작 구성
            var result = PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(sage, ally, board: board)); // 검증 동작 구성
            Assert.That(result.Success, Is.True); // 결과 검증
            Assert.That(CombatResolver.ResolveAttack(ally, enemy).DamageDealt, Is.EqualTo(7)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(ally, 4), Is.EqualTo(3)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(ally, 4), Is.EqualTo(4)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_범위밖과아군에게독거부() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "iron_regent", 4, 3); // 검증 동작 구성
            var enemy = Spawn(board, "emperor", 6, 3, false); // 검증 동작 구성
            var ability = Find(sage, "five_sage_poison"); // 검증 동작 구성
            Assert.That(PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(sage, ally, board: board)).Success, Is.False); // 결과 검증
            Assert.That(PieceAbilityService.ExecuteAbility(ability, new AbilityExecutionContext(sage, enemy, board: board)).Success, Is.False); // 결과 검증
            Assert.That(ally.HasStatus(StatusEffectType.Poison), Is.False); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void SageAI_독과보호후보모두생성() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3, false); // 검증 동작 구성
            Spawn(board, "grand_paladin", 4, 3, false); // 검증 동작 구성
            Spawn(board, "emperor", 3, 5); // 검증 동작 구성
            var candidates = EnemyAIAbilityCandidateBuilder.BuildCandidates(board, sage); // 검증 동작 구성
            Assert.That(candidates.Exists(c => c.Ability.AbilityId == "five_sage_poison"), Is.True); // 결과 검증
            Assert.That(candidates.Exists(c => c.Ability.AbilityId == "five_sage_guard"), Is.True); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Phantom_선택된형태는저장후복원하고이동시다음형태() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var phantom = Spawn(run.Board, "phantom_general", 4, 4); // 검증 동작 구성
            phantom.RestoreMovementCycleIndex(3); // 검증 동작 구성
            var restored = RunState.FromSaveData(run.ToSaveData(), _database); // 검증 동작 구성
            var piece = restored.Board.GetTile(new Vector2Int(4, 4)).OccupyingPiece; // 검증 동작 구성
            Assert.That(piece.MovementCycleIndex, Is.EqualTo(3)); // 결과 검증
            piece.BoardPosition = new Vector2Int(4, 5); // 검증 동작 구성
            Assert.That(piece.MovementCycleIndex, Is.EqualTo(4)); // 결과 검증
        } // 범위 종료
[Test] // 테스트 지정
        public void Emperor_초기배치이벤트는소비하지않고첫일반턴에왕회복() // 동작 검증
        { // 범위 시작
            var run = new RunState(1); // 검증 동작 구성
            Spawn(run.Board, "emperor", 3, 3); // 검증 동작 구성
            var king = Spawn(run.Board, "king", 4, 3); // 검증 동작 구성
            king.CurrentHp = 1; // 검증 동작 구성
            var host = new GameObject("Day95TurnController"); // 검증 동작 구성
            host.SetActive(false); // 검증 동작 구성
            var controller = host.AddComponent<BattleController>(); // 검증 동작 구성
            var fields = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 검증 동작 구성
            typeof(BattleController).GetField("_runState", fields).SetValue(controller, run); // 검증 동작 구성
            typeof(BattleController).GetField("_turnManager", fields).SetValue(controller, new TurnManager()); // 검증 동작 구성
            typeof(BattleController).GetField("_battleHooks", fields).SetValue(controller, new BattleHooks()); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                var method = typeof(BattleController).GetMethod("HandleTurnChanged", fields); // 검증 동작 구성
                method.Invoke(controller, new object[] { TurnState.DeploymentTurn, 1 }); // 검증 동작 구성
                Assert.That(king.CurrentHp, Is.EqualTo(1)); // 결과 검증
                method.Invoke(controller, new object[] { TurnState.PlayerTurn, 1 }); // 검증 동작 구성
                Assert.That(king.CurrentHp, Is.EqualTo(2)); // 결과 검증
                Assert.That(run.KingHp, Is.EqualTo(2)); // 결과 검증
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(host); // 검증 동작 구성
            } // 범위 종료
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_왕회복은런체력에도즉시반영() // 동작 검증
        { // 범위 시작
            var run = new RunState(1); // 검증 동작 구성
            var sage = Spawn(run.Board, "grand_sage", 3, 3); // 검증 동작 구성
            var king = Spawn(run.Board, "king", 4, 3); // 검증 동작 구성
            king.CurrentHp = 1; // 검증 동작 구성
            var result = PieceAbilityService.ExecuteAbility(Find(sage, "five_sage_heal"), new AbilityExecutionContext(sage, king, board: run.Board, runState: run)); // 검증 동작 구성
            Assert.That(result.Success, Is.True); // 결과 검증
            Assert.That(king.CurrentHp, Is.EqualTo(3)); // 결과 검증
            Assert.That(run.KingHp, Is.EqualTo(3)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void SageAI_자신의부상회복과보호후보도생성() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3, false); // 검증 동작 구성
            sage.CurrentHp = 2; // 검증 동작 구성
            var candidates = EnemyAIAbilityCandidateBuilder.BuildCandidates(board, sage); // 검증 동작 구성
            Assert.That(candidates.Exists(c => c.TargetPiece == sage && c.Ability.AbilityId == "five_sage_heal"), Is.True); // 결과 검증
            Assert.That(candidates.Exists(c => c.TargetPiece == sage && c.Ability.AbilityId == "five_sage_guard"), Is.True); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_미리보기는상태와행동권을변경하지않음() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var sage = Spawn(board, "grand_sage", 3, 3); // 검증 동작 구성
            var turns = new TurnManager(); // 검증 동작 구성
            turns.MarkInitialKingPlaced(); // 검증 동작 구성
            turns.TryEndDeploymentTurn(); // 검증 동작 구성
            var context = new AbilityExecutionContext(sage, sage, board: board, turnManager: turns); // 검증 동작 구성
            var ability = Find(sage, "five_sage_guard"); // 검증 동작 구성
            Assert.That(PieceAbilityService.PreviewAbility(ability, context).Success, Is.True); // 결과 검증
            Assert.That(sage.SageGuardAmount, Is.Zero); // 결과 검증
            Assert.That(turns.CanPlayerAct, Is.True); // 결과 검증
            Assert.That(PieceAbilityService.ExecuteAbility(ability, context).ConsumedAction, Is.True); // 결과 검증
            Assert.That(turns.CurrentState, Is.EqualTo(TurnState.EnemyTurn)); // 결과 검증
            Assert.That(PieceAbilityService.ExecuteAbility(ability, context).Success, Is.False); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Paladin_동일턴재통지는충전없이다음턴에충전() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            Spawn(board, "grand_paladin", 3, 3); // 검증 동작 구성
            var ally = Spawn(board, "iron_regent", 4, 3); // 검증 동작 구성
            var hooks = new BattleHooks(); // 검증 동작 구성
            hooks.RaiseTurnStart(TurnState.PlayerTurn, 2); // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(ally, 3), Is.EqualTo(1)); // 결과 검증
            hooks.RaiseTurnStart(TurnState.PlayerTurn, 2); // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(ally, 3), Is.EqualTo(3)); // 결과 검증
            hooks.RaiseTurnStart(TurnState.PlayerTurn, 3); // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(ally, 3), Is.EqualTo(1)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Rider_중간착지점이막히면돌파피해없음() // 동작 검증
        { // 범위 시작
            var board = new BoardState(); // 검증 동작 구성
            var actor = Spawn(board, "sky_marshal", 2, 2); // 검증 동작 구성
            var target = Spawn(board, "iron_regent", 4, 6, false); // 검증 동작 구성
            Spawn(board, "grand_sage", 3, 4); // 검증 동작 구성
            Assert.That(CombatResolver.ResolveAttack(actor, target).DamageDealt, Is.EqualTo(9)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Sage_보호상태는저장복원후첫피격에만감소() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var sage = Spawn(run.Board, "grand_sage", 3, 3); // 검증 동작 구성
            PieceAbilityService.ExecuteAbility(Find(sage, "five_sage_guard"), new AbilityExecutionContext(sage, sage, board: run.Board)); // 검증 동작 구성
            var restored = RunState.FromSaveData(run.ToSaveData(), _database); // 검증 동작 구성
            var target = restored.Board.GetTile(new Vector2Int(3, 3)).OccupyingPiece; // 검증 동작 구성
            Assert.That(DamageResolver.ApplyDamage(target, 3), Is.EqualTo(2)); // 결과 검증
            Assert.That(DamageResolver.ApplyDamage(target, 3), Is.EqualTo(3)); // 결과 검증
        } // 범위 종료
        [Test] // 테스트 지정
        public void Phantom_배치선택은이동후보없이적용되고전투턴에는변경금지() // 동작 검증
        { // 범위 시작
            var root = new GameObject("Day95InputRoot"); // 검증 동작 구성
            var view = root.AddComponent<BoardView>(); // 검증 동작 구성
            var input = root.AddComponent<BoardInputController>(); // 검증 동작 구성
            var run = new RunState(3); // 검증 동작 구성
            var phantom = Spawn(run.Board, "phantom_general", 4, 3); // 검증 동작 구성
            var turns = new TurnManager(); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                view.Bind(run.Board); // 검증 동작 구성
                input.Bind(run, view, turns); // 검증 동작 구성
                Assert.That(input.TrySelectPieceAt(phantom.BoardPosition), Is.True); // 결과 검증
                Assert.That(input.PendingMovement.MoveTiles, Is.Empty); // 결과 검증
                Assert.That(input.TrySelectPhantomForm(3), Is.True); // 결과 검증
                Assert.That(phantom.MovementCycleIndex, Is.EqualTo(3)); // 결과 검증
                Assert.That(input.TryMoveSelectedPieceTo(new Vector2Int(5, 3)), Is.False); // 결과 검증
                turns.MarkInitialKingPlaced(); // 검증 동작 구성
                turns.TryEndDeploymentTurn(); // 검증 동작 구성
                Assert.That(input.TrySelectPhantomForm(2), Is.False); // 결과 검증
                Assert.That(phantom.MovementCycleIndex, Is.EqualTo(3)); // 결과 검증
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(root); // 검증 동작 구성
            } // 범위 종료
        } // 범위 종료

[TestCase(false, false, BattleOutcome.Victory)] // 테스트 지정
        [TestCase(true, false, BattleOutcome.Defeat)] // 테스트 지정
        [TestCase(true, true, BattleOutcome.Defeat)] // 테스트 지정
        public void PoisonTick_마지막적과왕사망시전투종료및동시사망패배우선(bool kingPoisoned, bool enemyPoisoned, BattleOutcome expected) // 동작 검증
        { // 범위 시작
            var host = new GameObject("Day95PoisonBattle"); // 검증 동작 구성
            host.SetActive(false); // 검증 동작 구성
            var view = host.AddComponent<BoardView>(); // 검증 동작 구성
            var input = host.AddComponent<BoardInputController>(); // 검증 동작 구성
            var controller = host.AddComponent<BattleController>(); // 검증 동작 구성
            var run = new RunState(2); // 검증 동작 구성
            var turns = new TurnManager(); // 검증 동작 구성
            var hooks = new BattleHooks(); // 검증 동작 구성
            var king = Spawn(run.Board, "king", 2, 2); // 검증 동작 구성
            king.CurrentHp = 2; // 검증 동작 구성
            var enemy = Spawn(run.Board, "emperor", 6, 6, false); // 검증 동작 구성
            enemy.CurrentHp = 2; // 검증 동작 구성
            var poison = Find(new PieceRuntimeState(_database.FindById("grand_sage"), new Vector2Int(0, 0), true), "five_sage_poison").Effects[0].StatusEffect; // 검증 동작 구성
            var target = kingPoisoned ? king : enemy; // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            if (enemyPoisoned) // 조건 확인
            { // 범위 시작
                enemy.ApplyStatus(poison); // 검증 동작 구성
                enemy.ApplyStatus(poison); // 검증 동작 구성
            } // 범위 종료
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 검증 동작 구성
            typeof(BattleController).GetField("_runState", flags).SetValue(controller, run); // 검증 동작 구성
            typeof(BattleController).GetField("_turnManager", flags).SetValue(controller, turns); // 검증 동작 구성
            typeof(BattleController).GetField("_battleHooks", flags).SetValue(controller, hooks); // 검증 동작 구성
            typeof(BattleController).GetField("_boardView", flags).SetValue(controller, view); // 검증 동작 구성
            typeof(BattleController).GetField("_boardInputController", flags).SetValue(controller, input); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                typeof(BattleController).GetMethod("BindState", flags).Invoke(controller, null); // 검증 동작 구성
                var bridge = host.AddComponent<ProjectEta.Boss.LargePieceTurnEndStatusBridge>(); // 실제 씬 상태 브리지 생성
                typeof(ProjectEta.Boss.LargePieceTurnEndStatusBridge).GetField("_boardInput", flags).SetValue(bridge, input); // 실제 입력 브리지 연결
                typeof(ProjectEta.Boss.LargePieceTurnEndStatusBridge).GetMethod("TryBind", flags).Invoke(bridge, null); // 실제 턴 정산 교체 경로 재현
                hooks.RaiseTurnEnd(TurnState.EnemyTurn, 1); // 실제 턴 종료 정산
                Assert.That(turns.CurrentState, Is.EqualTo(TurnState.BattleEnded)); // 결과 검증
                Assert.That(turns.Outcome, Is.EqualTo(expected)); // 결과 검증
                if (kingPoisoned) // 조건 확인
                { // 범위 시작
                    Assert.That(run.KingHp, Is.Zero); // 결과 검증
                } // 범위 종료
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(host); // 검증 동작 구성
            } // 범위 종료
        } // 범위 종료
        [Test] // 테스트 지정
        public void RuntimeRestore_실제리소스로5성카드와독상태복원() // 동작 검증
        { // 범위 시작
            var run = new RunState(3); // 검증 동작 구성
            var sage = Spawn(run.Board, "grand_sage", 3, 3); // 검증 동작 구성
            run.Deck.AddToOwnedPool(sage.Definition); // 검증 동작 구성
            run.Hand.TryAddCard(sage.Definition); // 검증 동작 구성
            sage.ApplyStatus(Find(sage, "five_sage_poison").Effects[0].StatusEffect); // 검증 동작 구성
            sage.ApplyStatus(Find(sage, "five_sage_poison").Effects[0].StatusEffect); // 검증 동작 구성
            var data = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())); // 검증 동작 구성
            var method = typeof(RunSaveSystem).GetMethod("RestoreFromResources"); // 검증 동작 구성
            Assert.That(method, Is.Not.Null, "실제 자동 불러오기 리소스 경로"); // 결과 검증
            var restored = (RunState)method.Invoke(null, new object[] { data }); // 검증 동작 구성
            Assert.That(restored.Deck.OwnedCardPool.Count, Is.EqualTo(1)); // 결과 검증
            Assert.That(restored.Hand.Hand.Count, Is.EqualTo(1)); // 결과 검증
            Assert.That(restored.Board.GetTile(new Vector2Int(3, 3)).OccupyingPiece.FindStatus(StatusEffectType.Poison).StackCount, Is.EqualTo(2)); // 결과 검증
        } // 범위 종료

[Test] // 테스트 지정
        public void StatusBridge_재연결이후에도독은턴당한번만정산() // 동작 검증
        { // 범위 시작
            var root = new GameObject("Day95BridgeRebind"); // 검증 동작 구성
            root.SetActive(false); // 검증 동작 구성
            var view = root.AddComponent<BoardView>(); // 검증 동작 구성
            var input = root.AddComponent<BoardInputController>(); // 검증 동작 구성
            var bridge = root.AddComponent<ProjectEta.Boss.LargePieceTurnEndStatusBridge>(); // 검증 동작 구성
            var run = new RunState(3); // 검증 동작 구성
            var hooks = new BattleHooks(); // 검증 동작 구성
            var turns = new TurnManager(); // 검증 동작 구성
            var target = Spawn(run.Board, "emperor", 4, 4, false); // 검증 동작 구성
            target.CurrentHp = 6; // 검증 동작 구성
            var sage = new PieceRuntimeState(_database.FindById("grand_sage"), Vector2Int.zero, true); // 검증 동작 구성
            var poison = Find(sage, "five_sage_poison").Effects[0].StatusEffect; // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance; // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                view.Bind(run.Board); // 검증 동작 구성
                input.Bind(run, view, turns, hooks); // 검증 동작 구성
                typeof(ProjectEta.Boss.LargePieceTurnEndStatusBridge).GetField("_boardInput", flags).SetValue(bridge, input); // 검증 동작 구성
                typeof(ProjectEta.Boss.LargePieceTurnEndStatusBridge).GetMethod("TryBind", flags).Invoke(bridge, null); // 검증 동작 구성
                input.Bind(run, view, turns, hooks); // 검증 동작 구성
                hooks.RaiseTurnEnd(TurnState.EnemyTurn, 1); // 검증 동작 구성
                Assert.That(target.CurrentHp, Is.EqualTo(4)); // 결과 검증
                Assert.That(target.FindStatus(StatusEffectType.Poison).RemainingTurns, Is.EqualTo(2)); // 결과 검증
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(root); // 검증 동작 구성
            } // 범위 종료
        } // 범위 종료
        [TestCase(6, 4)] // 테스트 지정
        [TestCase(2, 0)] // 테스트 지정
        public void StatusTick_2x2중복정산없이사망시전체점유제거(int hp, int remaining) // 동작 검증
        { // 범위 시작
            var root = new GameObject("Day95LargePoison"); // 검증 동작 구성
            root.SetActive(false); // 검증 동작 구성
            var view = root.AddComponent<BoardView>(); // 검증 동작 구성
            var input = root.AddComponent<BoardInputController>(); // 검증 동작 구성
            var run = new RunState(3); // 검증 동작 구성
            var target = Spawn(run.Board, "emperor", 4, 4, false); // 검증 동작 구성
            run.Board.TryOccupyArea(target.BoardPosition, new Vector2Int(2, 2), target); // 검증 동작 구성
            target.CurrentHp = hp; // 검증 동작 구성
            var sage = new PieceRuntimeState(_database.FindById("grand_sage"), Vector2Int.zero, true); // 검증 동작 구성
            var poison = Find(sage, "five_sage_poison").Effects[0].StatusEffect; // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            target.ApplyStatus(poison); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                view.Bind(run.Board); // 검증 동작 구성
                input.Bind(run, view, new TurnManager(), new BattleHooks()); // 검증 동작 구성
                input.ApplyTurnEndStatusEffects(); // 검증 동작 구성
                Assert.That(target.CurrentHp, Is.EqualTo(remaining)); // 결과 검증
                if (remaining == 0) // 조건 확인
                { // 범위 시작
                    Assert.That(run.Board.GetTile(new Vector2Int(5, 5)).OccupyingPiece, Is.Null); // 결과 검증
                } // 범위 종료
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                UnityEngine.Object.DestroyImmediate(root); // 검증 동작 구성
            } // 범위 종료
        } // 범위 종료

[Test] // 테스트 지정
        public void BattleBootstrap_씬전환후능력UI재생성과중복방지() // 동작 검증
        { // 범위 시작
            var before = new System.Collections.Generic.HashSet<GameObject>(UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)); // 검증 동작 구성
            var method = typeof(ProjectEta.SceneFlow.SceneRuntimeBootstrap).GetMethod("EnsurePresentationRuntime", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic); // 검증 동작 구성
            try // 검증 동작 구성
            { // 범위 시작
                method.Invoke(null, null); // 검증 동작 구성
                var first = UnityEngine.Object.FindFirstObjectByType<ProjectEta.UI.PieceAbilityOverlayUI>(); // 검증 동작 구성
                Assert.That(first, Is.Not.Null, "Battle 진입 시 능력 UI 설치 누락"); // 결과 검증
                method.Invoke(null, null); // 검증 동작 구성
                Assert.That(UnityEngine.Object.FindObjectsByType<ProjectEta.UI.PieceAbilityOverlayUI>(FindObjectsSortMode.None).Length, Is.EqualTo(1)); // 결과 검증
                UnityEngine.Object.DestroyImmediate(first.gameObject); // 검증 동작 구성
                method.Invoke(null, null); // 검증 동작 구성
                Assert.That(UnityEngine.Object.FindFirstObjectByType<ProjectEta.UI.PieceAbilityOverlayUI>(), Is.Not.Null, "씬 전환 후 재설치 누락"); // 결과 검증
            } // 범위 종료
            finally // 검증 동작 구성
            { // 범위 시작
                foreach (var item in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)) // 대상 순회
                { // 범위 시작
                    if (item != null && !before.Contains(item)) // 조건 확인
                    { // 범위 시작
                        UnityEngine.Object.DestroyImmediate(item); // 검증 동작 구성
                    } // 범위 종료
                } // 범위 종료
            } // 범위 종료
        } // 범위 종료

        private PieceRuntimeState Spawn(BoardState board, string id, int x, int y, bool player = true) // 검증 보조 구성
        { // 범위 시작
            var piece = new PieceRuntimeState(_database.FindById(id), new Vector2Int(x, y), player); // 검증 동작 구성
            board.GetTile(piece.BoardPosition).OccupyingPiece = piece; // 검증 동작 구성
            return piece; // 결과 반환
        } // 범위 종료
        private static PieceAbilityDefinition Find(PieceRuntimeState piece, string id) // 검증 보조 구성
        { // 범위 시작
            var ability = Array.Find(piece.Definition.Abilities, item => item != null && item.AbilityId == id); // 검증 동작 구성
            Assert.That(ability, Is.Not.Null, id); // 결과 검증
            return ability; // 결과 반환
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
