using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using System.Reflection; // PieceDefinition private 필드 주입에 사용하는 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleHooks, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementRuleData를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // 39일차 페이즈·텔레그래프·범위 공격 타입을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day39BossPhaseAndTelegraphTests // 39일차 최종 보스 2페이즈와 텔레그래프 핵심 규칙을 검증하는 테스트 모음
    {
        [Test] // 최대 HP의 절반 이하에서만 Phase 2로 전환되는지 검증
        public void PhaseRules_EnterPhase2AtHalfHp()
        {
            Assert.IsFalse(BossPhaseRules.ShouldEnterPhase2(16, 30)); // 30의 절반보다 높은 16 HP는 아직 Phase 1
            Assert.IsTrue(BossPhaseRules.ShouldEnterPhase2(15, 30)); // 정확히 50%인 15 HP부터 Phase 2
            Assert.IsTrue(BossPhaseRules.ShouldEnterPhase2(1, 30)); // 그 이하 HP도 Phase 2 조건 유지
            Assert.IsFalse(BossPhaseRules.ShouldEnterPhase2(0, 30)); // 이미 사망한 보스는 새 페이즈로 전환하지 않음
        }

        [Test] // 같은 보스 런타임 상태에서 Phase 2 진입 이벤트가 한 번만 성공하는지 검증
        public void PhaseRuntime_TransitionsOnlyOnce()
        {
            var state = new BossPhaseRuntimeState(); // 새 보스는 Phase 1 상태로 시작

            Assert.AreEqual(BossPhase.Phase1, state.Phase); // 초기 페이즈 확인
            Assert.IsTrue(state.TryEnterPhase2(15, 30)); // 첫 50% 도달은 전환 성공
            Assert.AreEqual(BossPhase.Phase2, state.Phase); // 실제 Phase 2 상태 확인
            Assert.IsFalse(state.TryEnterPhase2(10, 30)); // 이후 HP가 더 내려가도 중복 전환 금지
        }

        [Test] // Phase 2 증원 호출 플래그가 한 번만 소비되는지 검증
        public void PhaseRuntime_ReinforcementRequestIsOneShot()
        {
            var state = new BossPhaseRuntimeState(); // 새 런타임 페이즈 상태 생성
            Assert.IsTrue(state.TryEnterPhase2(15, 30)); // Phase 2 진입

            Assert.IsTrue(state.TryMarkReinforcementCalled()); // 첫 증원 호출만 성공
            Assert.IsFalse(state.TryMarkReinforcementCalled()); // 같은 보스에서 두 번째 호출은 거부
        }

        [Test] // 보스 주변 Slam 텔레그래프가 2x2 몸체를 제외한 정확한 외곽 링을 만드는지 검증
        public void SlamRing_BuildsTwelveCellsAroundInteriorTwoByTwoBoss()
        {
            var board = new BoardState(); // 빈 10x10 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 4)); // 경계에서 떨어진 중앙에 2x2 보스 배치

            List<Vector2Int> cells = BossPatternLibrary.BuildSlamRing(board, boss); // 주변 강타 위험 칸 계산

            Assert.AreEqual(12, cells.Count); // 2x2를 둘러싼 4x4 외곽 링은 16-4 = 12칸
            Assert.IsFalse(cells.Contains(new Vector2Int(4, 4))); // 보스 좌하단 점유 칸 제외
            Assert.IsFalse(cells.Contains(new Vector2Int(5, 5))); // 보스 우상단 점유 칸 제외
            Assert.IsTrue(cells.Contains(new Vector2Int(3, 4))); // 왼쪽 인접 칸 포함
            Assert.IsTrue(cells.Contains(new Vector2Int(6, 5))); // 오른쪽 인접 칸 포함
            Assert.IsTrue(cells.Contains(new Vector2Int(3, 3))); // 모서리 대각 위험 칸도 포함
        }

        [Test] // King이 오른쪽에 있을 때 두 칸 폭의 직선 텔레그래프가 오른쪽 방향으로 생성되는지 검증
        public void KingLane_BuildsTwoWideLaneTowardKing()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(3, 5)); // 2x2 보스 배치
            var king = Place(board, CreateDefinition("king", Vector2Int.one, PieceMovementType.King, PieceCategory.Basic, 3, 1), new Vector2Int(9, 5), true); // 보스 오른쪽에 플레이어 King 배치

            List<Vector2Int> cells = BossPatternLibrary.BuildKingLane(board, boss, king, 3); // King 방향 3칸 길이 직선 위험 칸 계산

            Assert.AreEqual(6, cells.Count); // 2칸 폭 x 3칸 길이 = 6칸
            Assert.IsTrue(cells.Contains(new Vector2Int(5, 5))); // 보스 바로 오른쪽 아래 줄 포함
            Assert.IsTrue(cells.Contains(new Vector2Int(7, 6))); // 가장 먼 오른쪽 위 줄 포함
            Assert.IsFalse(cells.Contains(new Vector2Int(2, 5))); // 반대 방향 칸은 제외
            Assert.IsFalse(cells.Contains(boss.BoardPosition)); // 보스 점유 칸은 제외
        }

        [Test] // 텔레그래프 상태를 저장하는 것 자체로는 플레이어 HP가 줄지 않는지 검증
        public void PendingTelegraph_DoesNotApplyDamageUntilExecution()
        {
            var board = new BoardState(); // 빈 보드 생성
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 4)); // 보스 배치
            var player = Place(board, CreateDefinition("pawn", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 10, 1), new Vector2Int(3, 4), true); // Slam 위험 칸에 플레이어 배치
            var phaseState = new BossPhaseRuntimeState(); // 보스 페이즈 런타임 상태 생성
            phaseState.TryEnterPhase2(15, 30); // Phase 2 진입
            int beforeHp = player.CurrentHp; // 예고 전 HP 저장

            var telegraph = new BossTelegraphState(boss, BossPatternType.SlamRing, "주변 강타", BossPatternLibrary.BuildSlamRing(board, boss), 5); // 다음 EnemyTurn 실행 예정 텔레그래프 생성
            phaseState.SetPendingTelegraph(telegraph); // 예고 상태만 저장

            Assert.AreEqual(beforeHp, player.CurrentHp); // 예고 시점에는 실제 피해가 없어야 함
            Assert.AreSame(telegraph, phaseState.PendingTelegraph); // 실행 대기 텔레그래프가 그대로 보관돼야 함
        }

        [Test] // 예고된 범위 공격 실행 시 위험 칸에 남아 있는 플레이어만 기존 CombatResolver 피해를 받는지 검증
        public void AreaAttack_HitsOnlyPlayersStillInsideTelegraphedCells()
        {
            var runState = new RunState(3); // 테스트용 런 상태 생성
            var board = runState.Board; // 실제 보드 참조
            var boss = CreateAndPlaceBoss(board, new Vector2Int(4, 6)); // 적 2x2 보스 배치
            var inside = Place(board, CreateDefinition("inside", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 10, 1), new Vector2Int(3, 6), true); // 위험 칸 안 플레이어
            var outside = Place(board, CreateDefinition("outside", Vector2Int.one, PieceMovementType.Pawn, PieceCategory.Basic, 10, 1), new Vector2Int(1, 1), true); // 위험 칸 밖 플레이어
            var turnManager = CreateEnemyTurnManager(); // 실제 EnemyTurn 상태 생성
            var hooks = new BattleHooks(); // 기존 전투 훅 버스 생성
            int insideBefore = inside.CurrentHp; // 안쪽 대상 공격 전 HP
            int outsideBefore = outside.CurrentHp; // 바깥 대상 공격 전 HP
            var cells = BossPatternLibrary.BuildSlamRing(board, boss); // 실제 예고와 동일한 위험 칸 계산

            bool executed = BossActionExecutor.TryExecuteTelegraphedAreaAttack(boss, cells, runState, turnManager, hooks, null, out int hitCount); // 예고 공격 실행

            Assert.IsTrue(executed); // 범위 행동 자체는 정상 실행
            Assert.AreEqual(1, hitCount); // 위험 칸에 남은 플레이어 한 기만 적중
            Assert.AreEqual(insideBefore - boss.Definition.BaseAtk, inside.CurrentHp); // 안쪽 대상은 보스 BaseAtk만큼 피해
            Assert.AreEqual(outsideBefore, outside.CurrentHp); // 바깥 대상은 피해 없음
            Assert.AreNotEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 범위 공격 후 적 턴 종료
        }

        [Test] // 두 패턴이 번갈아 선택되어 Phase 2가 단일 패턴 반복이 되지 않는지 검증
        public void PhaseRuntime_AlternatesSlamAndKingLanePatterns()
        {
            var state = new BossPhaseRuntimeState(); // Phase 상태 생성
            state.TryEnterPhase2(15, 30); // Phase 2 진입

            Assert.AreEqual(BossPatternType.SlamRing, state.ConsumeNextPatternType()); // 첫 패턴은 주변 강타
            Assert.AreEqual(BossPatternType.KingLane, state.ConsumeNextPatternType()); // 다음 패턴은 King 방향 직선
            Assert.AreEqual(BossPatternType.SlamRing, state.ConsumeNextPatternType()); // 세 번째는 다시 주변 강타
        }

        [Test] // 보스 상태 UI 문구가 페이즈와 예고 패턴을 명확히 표시하는지 검증
        public void BossPhaseStatusUI_BuildsReadablePhase2TelegraphText()
        {
            string text = BossPhaseStatusUI.BuildDisplayText(BossPhase.Phase2, "주변 강타", true); // Phase 2 진입 직후 증원 발생 상태 문구 생성

            StringAssert.Contains("BOSS PHASE 2", text); // 페이즈 표시 포함
            StringAssert.Contains("주변 강타", text); // 현재 예고 패턴 포함
            StringAssert.Contains("증원", text); // 전환 시 증원 안내 포함
        }

        private static PieceRuntimeState CreateAndPlaceBoss(BoardState board, Vector2Int anchor) // 2x2 프로토타입 보스를 만들고 네 칸에 점유시키는 테스트 도우미
        {
            var definition = CreateDefinition("prototype_boss_37", new Vector2Int(2, 2), PieceMovementType.Custom, PieceCategory.Boss, 30, 4); // HP30/ATK4 보스 정의 생성
            var boss = new PieceRuntimeState(definition, anchor, false); // 적 보스 런타임 생성
            Assert.IsTrue(board.TryOccupyArea(anchor, new Vector2Int(2, 2), boss)); // 네 칸 전체 점유
            return boss; // 생성 보스 반환
        }

        private static PieceRuntimeState Place(BoardState board, PieceDefinition definition, Vector2Int position, bool isPlayerPiece) // 일반 테스트 기물 배치 도우미
        {
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 런타임 기물 생성
            board.GetTile(position).OccupyingPiece = piece; // 한 칸 점유 등록
            return piece; // 생성 기물 반환
        }

        private static TurnManager CreateEnemyTurnManager() // 기존 공개 API로 EnemyTurn까지 진행한 TurnManager를 만드는 도우미
        {
            var manager = new TurnManager(); // 시작 배치 상태 생성
            manager.MarkInitialKingPlaced(); // 시작 King 조건 충족
            Assert.IsTrue(manager.TryEndDeploymentTurn()); // PlayerTurn 진입
            Assert.IsTrue(manager.TryCompletePlayerAction()); // EnemyTurn 진입
            Assert.AreEqual(TurnState.EnemyTurn, manager.CurrentState); // 실제 적 턴 확인
            return manager; // 준비된 매니저 반환
        }

        private static PieceDefinition CreateDefinition(string pieceId, Vector2Int occupancySize, PieceMovementType movementType, PieceCategory category, int hp, int atk) // 테스트용 PieceDefinition 생성 도우미
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 정의 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetPrivateField(definition, "_category", category); // 분류 설정
            SetPrivateField(definition, "_grade", PieceGrade.FiveStar); // 테스트 편의를 위해 5성 설정
            SetPrivateField(definition, "_movementType", movementType); // 이동 타입 설정
            SetPrivateField(definition, "_roleTags", PieceRoleTag.Tanker | PieceRoleTag.Attacker); // 역할 태그 설정
            SetPrivateField(definition, "_immuneStatusTags", StatusEffectType.None); // 테스트에서는 상태 면역 없음
            SetPrivateField(definition, "_movementRules", Array.Empty<MovementRuleData>()); // 이동 규칙 데이터 비움
            SetPrivateField(definition, "_baseHp", hp); // HP 설정
            SetPrivateField(definition, "_baseAtk", atk); // ATK 설정
            SetPrivateField(definition, "_occupancySize", occupancySize); // 점유 크기 설정
            return definition; // 완성 정의 반환
        }

        private static void SetPrivateField<T>(PieceDefinition target, string fieldName, T value) // PieceDefinition private 직렬화 필드 주입 도우미
        {
            var field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 필드 탐색
            Assert.IsNotNull(field, $"PieceDefinition.{fieldName} 필드를 찾을 수 있어야 합니다."); // 구조 변경 시 명확한 실패
            field.SetValue(target, value); // 테스트 값 적용
        }
    }
}
