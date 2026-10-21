using System; // Seed와 비교
using System.Collections; // 매개변수 검사 목록
using System.Collections.Generic; // 실제 카드와 완료 이력
using System.Linq; // 검증용 목록 조회
using NUnit.Framework; // 의미 있는 동작 검사
using UnityEditor; // 실제 데이터베이스 로드
using UnityEngine; // JSON과 실제 에셋
using ProjectEta.Run; // 이벤트 실행
using ProjectEta.Pieces; // 카드 정의
using ProjectEta.Fusion; // 실제 합성 경로
using ProjectEta.Meta; // 고정 해금·정보 저장
using ProjectEta.Battle; // 실제 첫 승리 흐름

namespace ProjectEta.Tests.EditMode // 102일차 통합 규칙 검사
{ // 영역 시작
    public sealed class Day102EventTests // 35종 선택지·저장·손익 검사
    { // 타입 시작
        private PieceDatabase _database; // 실제 전체 기물
        private FusionRecipeDatabase _recipes; // 실제 합성 경로
        private MetaProgressState _meta; // 디스크와 분리된 영구 상태
        private readonly List<RunState> _runs = new List<RunState>(); // 검사 생성 런 정리
        [SetUp] // 실제 에셋 연결
        public void SetUp() // 독립 상태
        { // 메서드 시작
            _database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 플레이어 풀
            _recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 실제 레시피
            _meta = new MetaProgressState(); // 기존 디스크 저장 보호
            Assert.That(_database, Is.Not.Null); // 필수 에셋 확인
            Assert.That(_recipes, Is.Not.Null); // 필수 레시피 확인
        } // 메서드 종료
        [TearDown] // 검사 자원 정리
        public void TearDown() // 원본 에셋 보호
        { // 메서드 시작
            var clones = new HashSet<PieceDefinition>(); // 중복 정리 방지
            foreach (RunState run in _runs) // 검사 런
            { // 반복 시작
                foreach (PieceDefinition card in run.Deck.OwnedCardPool) // 남은 강화 복제
                { // 반복 시작
                    if (card != null && !AssetDatabase.Contains(card)) clones.Add(card); // 원본 에셋 제외
                } // 반복 종료
            } // 반복 종료
            foreach (PieceDefinition card in clones) UnityEngine.Object.DestroyImmediate(card); // 검사 복제만 정리
            _runs.Clear(); // 이전 상태 참조 해제
            RunEconomyService.ResetForTests(); // 경제 검사 상태 해제
        } // 메서드 종료
        public static IEnumerable Choices() // 기획 105개 선택지
        { // 메서드 시작
            foreach (RunEventDefinition definition in RunEventCatalog.All) // 35종 데이터
            { // 반복 시작
                foreach (RunEventChoice choice in definition.choices) yield return new TestCaseData(definition.id, choice.id); // 각 선택지를 독립 검사
            } // 반복 종료
        } // 메서드 종료
        [TestCaseSource(nameof(Choices))] // 실제 105개 선택 실행
        public void EveryPlannedChoice_AppliesExactEffectsOnce(string eventId, string choiceId) // 비용·효과·중복 차단
        { // 메서드 시작
            RunEventDefinition definition = RunEventCatalog.Find(eventId); // 기획 정의
            RunState run = CreateRun(definition.phase == 0 ? 2 : definition.phase, "choice"); // 유효 등장 페이즈
            RunEventSession session = ForceEvent(run, eventId); // 원하는 이벤트 진입
            RunEventChoice choice = session.definition.choices.Single(item => item.id == choiceId); // 실제 고정 선택지
            RunEconomyState economy = RunEconomyService.Restore(run, 200); // 모든 기획 비용 충족
            RunContentUnlockSnapshot unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 디스크 없는 해금
            List<PieceDefinition> targets = RunEventCardRules.Targets(run, choice); // 실제 선택 대상
            PieceDefinition target = choice.NeedsTarget ? targets[0] : null; // 해당 효과 대상
            Assert.That(RunEventService.UnavailableReason(run, economy, session, choice, 3, _database.Definitions, unlocks, _recipes.Recipes, target), Is.Empty); // 조건 충족 확인
            int countBefore = run.Deck.OwnedCardPool.Count; // 카드 손익 기준
            Assert.That(RunEventService.TryCommit(run, economy, session, choice, target, 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out RunEventRecord result), Is.True); // 실제 효과 실행
            Assert.That(economy.Currency, Is.EqualTo(200 - choice.goldCost + choice.goldGain)); // 데이터와 비용·수입 일치
            Assert.That(run.KingHp, Is.EqualTo(Math.Min(3, 2 - choice.kingCost + choice.kingHeal))); // 왕 HP 효과 일치
            Assert.That(result.hpUpgrade, Is.EqualTo(choice.hpUpgrade)); // HP만 강화하는 경우 포함
            Assert.That(result.atkUpgrade, Is.EqualTo(choice.atkUpgrade)); // ATK만 강화하는 경우 포함
            int expectedCount = countBefore + (choice.cardOperation == RunEventCardOperation.Gain || choice.cardOperation == RunEventCardOperation.LinkedGain ? 1 : 0) - (choice.cardOperation == RunEventCardOperation.Remove ? 1 : 0); // 카드 변화 계산
            Assert.That(run.Deck.OwnedCardPool.Count, Is.EqualTo(expectedCount)); // 제거·교환·획득 일치
            if (!string.IsNullOrEmpty(result.addedCardId)) Assert.That(_database.FindById(result.addedCardId).Grade, Is.EqualTo(PieceGrade.OneStar)); // 높은 등급 직접 지급 금지
            if (choice.cardOperation == RunEventCardOperation.Exchange) Assert.That(result.addedCardId, Is.Not.EqualTo(target.PieceId)); // 자기 자신 교환 금지
            if (choice.hpUpgrade > 0 || choice.atkUpgrade > 0) // 실제 복제 검사
            { // 조건 시작
                PieceDefinition upgraded = run.Deck.OwnedCardPool.Last(); // 추가된 강화 카드
                Assert.That(upgraded.BaseHp, Is.EqualTo(target.BaseHp + choice.hpUpgrade)); // 실제 강화 HP
                Assert.That(upgraded.BaseAtk, Is.EqualTo(target.BaseAtk + choice.atkUpgrade)); // 실제 강화 ATK
                Assert.That(upgraded.PieceId, Is.EqualTo(target.PieceId)); // 보유 상한 식별 유지
            } // 조건 종료
            Assert.That(RunEventService.TryCommit(run, economy, session, choice, target, 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out _), Is.False); // 같은 프레임 재클릭 차단
            Assert.That(run.BalanceData.eventHistory102.Count, Is.EqualTo(1)); // 손익 이력 한 번
            Assert.That(economy.Currency, Is.EqualTo(result.goldAfter)); // 중복 비용·보상 없음
        } // 메서드 종료
        [Test] // 등장 풀 구성 검사
        public void Catalog_HasTenCommonAndFivePerPhaseWith105Choices() // 실제 기획 구조
        { // 메서드 시작
            Assert.That(RunEventCatalog.All.Count, Is.EqualTo(35)); // 정의 개수
            Assert.That(RunEventCatalog.All.Count(item => item.phase == 0), Is.EqualTo(10)); // 공통 열 종
            Assert.That(RunEventCatalog.All.Sum(item => item.choices.Length), Is.EqualTo(105)); // 전체 선택지
            for (int phase = 1; phase <= 5; phase++) Assert.That(RunEventCatalog.All.Count(item => item.phase == phase), Is.EqualTo(5)); // 전용 페이즈 제한
        } // 메서드 종료
        [Test] // 극단 상태 선택 막힘 검사
        public void EveryEvent_LeavesAnAvailableChoiceWithNoGoldAndNoCards() // 안전한 선택 유지
        { // 메서드 시작
            foreach (RunEventDefinition definition in RunEventCatalog.All) // 전체 이벤트
            { // 반복 시작
                RunState run = CreateRun(definition.phase == 0 ? 1 : definition.phase, definition.id); // 이벤트별 독립 상태
                foreach (PieceDefinition card in run.Deck.OwnedCardPool.ToArray()) run.Deck.RemoveFromOwnedPool(card); // 빈 덱 재현
                run.KingHp = 3; // 회복 불필요
                RunEventSession session = ForceEvent(run, definition.id); // 실제 선택지
                RunEconomyState economy = RunEconomyService.Restore(run, 0); // 비용 지불 불가
                var unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 기본 해금
                Assert.That(session.definition.choices.Any(choice => string.IsNullOrEmpty(RunEventService.UnavailableReason(run, economy, session, choice, 3, _database.Definitions, unlocks, _recipes.Recipes))), Is.True, definition.id); // 진행 가능한 선택 유지
            } // 반복 종료
        } // 메서드 종료
        [Test] // 같은 런 재등장 검사
        public void CompletedEventIds_AreExcludedUntilPhasePoolIsExhausted() // 완료 풀 고갈 처리
        { // 메서드 시작
            RunState run = CreateRun(1, "first"); // 공통과 1페이즈
            var ids = new HashSet<string>(); // 완료 ID
            for (int index = 0; index < 15; index++) // 해당 페이즈 전체 후보
            { // 반복 시작
                MoveToEvent(run, 1, "visit_" + index); // 서로 다른 노드
                RunEventSession session = RunEventService.GetOrCreate(run); // 실제 추첨
                Assert.That(session.definition.phase == 0 || session.definition.phase == 1, Is.True); // 다른 페이즈 누출 금지
                Assert.That(ids.Add(session.definition.id), Is.True); // 중복 추첨 금지
                RunEventChoice leave = session.definition.choices[2]; // 기획 C 선택
                Assert.That(RunEventService.TryCommit(run, RunEconomyService.GetOrCreate(run), session, leave, null, 3, _database.Definitions, RunContentUnlockSnapshot.Capture(run.RunId, _meta), _recipes.Recipes, _meta, out _), Is.True); // 완료 등록
            } // 반복 종료
            MoveToEvent(run, 1, "exhausted"); // 다음 이벤트 진입
            Assert.That(RunEventService.GetOrCreate(run).definition.id, Is.EqualTo("EXHAUSTED")); // 보상 재활용 대신 안전 통과
        } // 메서드 종료
        [Test] // 스냅샷·자동 이어하기 검사
        public void EventCheckpoint_RestoresSelectionResultAndRejectsRepayment() // 실제 저장 구조
        { // 메서드 시작
            RunState run = CreateRun(3, "save"); // 이벤트 안전 지점
            RunEventSession session = ForceEvent(run, "EV13"); // 기획 회복·위험
            RunEventChoice choice = session.definition.choices[1]; // HP 비용·Gold 보상
            var unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 실제 해금 스냅샷
            var economy = RunEconomyService.Restore(run, 100); // 저장 재화
            RunSaveData pending = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())); // 선택 전 저장
            Assert.That(RunSaveSystem.IsContinueDataValid(pending), Is.True); // 실제 이어하기 유효성
            RunState pendingCopy = RunState.FromSaveData(pending, _database); // 미완료 선택 복원
            _runs.Add(pendingCopy); // 복원 자원 정리
            Assert.That(RunEventService.GetOrCreate(pendingCopy).definition.id, Is.EqualTo("EV13")); // 동일 이벤트 유지
            Assert.That(RunEventService.TryCommit(run, economy, session, choice, null, 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out RunEventRecord result), Is.True); // 실제 선택 완료
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), _database); // 결과와 손익 복원
            _runs.Add(restored); // 복원 자원 정리
            RunEventSession copy = RunEventService.GetOrCreate(restored); // 결과 화면 진입
            Assert.That(copy.resolved, Is.True); // 완료 상태 유지
            Assert.That(copy.result.goldAfter, Is.EqualTo(result.goldAfter)); // 손익 유지
            Assert.That(RunEventService.TryCommit(restored, RunEconomyService.GetOrCreate(restored), copy, copy.definition.choices[1], null, 3, _database.Definitions, RunContentUnlockSnapshot.Capture(restored.RunId, _meta), _recipes.Recipes, _meta, out _), Is.False); // 불러오기 후 재지급 금지
        } // 메서드 종료
        [Test] // 카드 제한 검사
        public void Rewards_RespectUnlocksDeadCopiesAndOneStarOnly() // 실제 전체 로스터 필터
        { // 메서드 시작
            RunState run = CreateRun(1, "limits"); // 독립 런
            var unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 기본 고정 해금
            PieceDefinition card = RunEventCardRules.Rewards(run, _database.Definitions, unlocks, _recipes.Recipes, false)[0]; // 실제 지급 카드
            while (CardRewardRules.CountOwnedCopies(run.Deck.OwnedCardPool, card.PieceId) < 3) run.Deck.AddToOwnedPool(card); // 정상 보유 상한
            run.Deck.MoveToDeadPile(card); // 사망 한 장 분리
            List<PieceDefinition> candidates = RunEventCardRules.Rewards(run, _database.Definitions, unlocks, _recipes.Recipes, false); // 실제 후보 재검사
            Assert.That(candidates.All(item => item.Grade == PieceGrade.OneStar && item.PieceId != card.PieceId && MetaContentAvailabilityService.IsPieceAvailable(item, unlocks)), Is.True); // 잠김·고등급·합산 세 장 제외
        } // 메서드 종료
        [Test] // 무효 입력과 원자성 검사
        public void InvalidTargetInsufficientGoldAndLethalCosts_LeaveStateUntouched() // 실패 시 비용 보존
        { // 메서드 시작
            RunState run = CreateRun(2, "reject"); // 독립 상태
            RunEventSession session = ForceEvent(run, "EV02"); // 유료 강화
            RunEventChoice choice = session.definition.choices[0]; // HP만 강화
            var unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 기본 해금
            var economy = RunEconomyService.Restore(run, 24); // 한 Gold 부족
            string before = JsonUtility.ToJson(run.ToSaveData()); // 실제 상태 기준
            Assert.That(RunEventService.TryCommit(run, economy, session, choice, run.Deck.OwnedCardPool[0], 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out _), Is.False); // 부족 비용 차단
            Assert.That(JsonUtility.ToJson(run.ToSaveData()), Is.EqualTo(before)); // 자원·카드·손익 보존
            economy.RestoreCurrency(100); // 비용 충족
            Assert.That(RunEventService.TryCommit(run, economy, session, choice, null, 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out _), Is.False); // 대상 확정 전 비용 없음
            Assert.That(economy.Currency, Is.EqualTo(100)); // 취소와 같은 자원 상태
            session = ForceEvent(run, "EV05"); // 위험 재화 선택
            run.KingHp = 1; // 즉사 조건
            Assert.That(RunEventService.TryCommit(run, economy, session, session.definition.choices[0], null, 3, _database.Definitions, unlocks, _recipes.Recipes, _meta, out _), Is.False); // HP 비용 즉사 금지
            Assert.That(run.KingHp, Is.EqualTo(1)); // HP 보존
        } // 메서드 종료
        [Test] // 영구 후보 정보 검사
        public void RepeatedInformation_KeepsGoldAndSuppressesNewDiscovery() // 열람과 기계 효과 분리
        { // 메서드 시작
            for (int index = 0; index < 2; index++) // 서로 다른 런의 같은 정보
            { // 반복 시작
                RunState run = CreateRun(1, "info"); // 새 런
                RunEventSession session = ForceEvent(run, "EV07"); // 교대 일지
                var economy = RunEconomyService.Restore(run, 100); // 재화 기준
                Assert.That(RunEventService.TryCommit(run, economy, session, session.definition.choices[0], null, 3, _database.Definitions, RunContentUnlockSnapshot.Capture(run.RunId, _meta), _recipes.Recipes, _meta, out RunEventRecord result), Is.True); // 정보와 보상 적용
                Assert.That(result.newInformation, Is.EqualTo(index == 0)); // 첫 알림만 허용
                Assert.That(economy.Currency, Is.EqualTo(105)); // 기본 효과는 항상 유지
                _meta = MetaProgressSaveService.Deserialize(MetaProgressSaveService.Serialize(_meta)); // 영구 저장 JSON 복원
                Assert.That(_meta.HasEventInformation("EV07"), Is.True); // 후보 열람 유지
            } // 반복 종료
        } // 메서드 종료
        private RunState CreateRun(int phase, string suffix) // 실제 카드·노드 상태 준비
        { // 메서드 시작
            var run = new RunState(2); // 비용·회복 가능한 왕 HP
            _runs.Add(run); // 복제 정리 책임
            var unlocks = RunContentUnlockSnapshot.Capture(run.RunId, _meta); // 디스크 없는 고정 해금
            List<PieceDefinition> cards = RunEventCardRules.Rewards(run, _database.Definitions, unlocks, _recipes.Recipes, false); // 실제 기본 1성
            Assert.That(cards.Count, Is.GreaterThanOrEqualTo(2)); // 교환 가능한 실제 데이터
            run.Deck.AddToOwnedPool(cards[0]); // 첫 대상
            run.Deck.AddToOwnedPool(cards[1]); // 다른 종류 대상
            run.FusionDiscovery.Merge(_recipes.Recipes.Where(item => item != null).Select(item => item.RecipeId)); // 연결 보상 검사 전제
            MoveToEvent(run, phase, suffix); // 실제 이벤트 노드
            return run; // 시험 런
        } // 메서드 종료
        private static void MoveToEvent(RunState run, int phase, string suffix) // 유효한 이벤트 체크포인트
        { // 메서드 시작
            run.CurrentRound = 4; // 이벤트 깊이
            var current = new StageNode("phase_" + phase + "_before_" + suffix, new Vector2Int(4, 2), 3, "stage_3_battle"); // 이전 노드
            var next = new StageNode("phase_" + phase + "_event_" + suffix, new Vector2Int(4, 3), 4, "stage_4_event"); // 진입 이벤트
            var connections = new List<string>(); // 합법 연결 목록
            connections.Add(next.NodeId); // 도착 노드 연결
            current.SetNextNodeIds(connections); // 실제 합법 연결
            var nodes = new List<StageNode>(); // 다음 노드 목록
            nodes.Add(next); // 이벤트 등록
            run.RouteMap.Configure(3, current, nodes); // 지도 구성
            Assert.That(run.RouteMap.TryMoveKingTo(next.NodeId), Is.True); // 실제 노드 선택
            run.Flow.EnterEvent(); // 이벤트 흐름
        } // 메서드 종료
        private static RunEventSession ForceEvent(RunState run, string id) // 개별 기획 효과 검사
        { // 메서드 시작
            RunEventSession session = RunEventService.GetOrCreate(run); // 실제 세션 생성
            session.definition = RunEventCatalog.Find(id).Copy(); // 정확한 선택지 대상으로 교체
            return session; // 같은 저장 세션 유지
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
