using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // JsonUtility·Vector2Int·ScriptableObject 사용
using ProjectEta.King; // KingArchetype·KingRunStateService 사용
using ProjectEta.Meta; // 메타 보상 Claim 저장 검증 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // 런 세이브·경로·경제 상태 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day51RunPersistenceTests
    {
        [SetUp]
        public void SetUp()
        {
            RunEconomyService.ResetForTests(); // 테스트 간 런 경제 상태 분리
        }

        [Test]
        public void RouteMap_SaveRestore_PreservesSeedCurrentNodeAndSelectedPath()
        {
            var route = new RouteMapState(); // 원본 경로 지도 생성
            var current = new StageNode("depth_3_resolved", new Vector2Int(4, 2), 3, "ResolvedStage"); // 현재 노드 생성
            var target = new StageNode("depth_4_0_battle", new Vector2Int(5, 3), 4, "stage_4_battle"); // 다음 선택 노드 생성
            current.SetNextNodeIds(new[] { target.NodeId }); // 현재 노드와 다음 노드 연결
            route.Configure(3, current, new[] { target }); // 테스트 경로 구성
            Assert.IsTrue(route.TryMoveKingTo(target.NodeId)); // 실제 경로 선택 기록

            RouteMapSaveData data = route.ToSaveData(); // 경로 지도 저장 DTO 생성
            var restored = new RouteMapState(); // 복원 대상 경로 지도 생성
            restored.Restore(data); // 저장 DTO 기반 복원

            Assert.AreEqual(route.MapSeed, restored.MapSeed); // 맵 시드 유지 검증
            Assert.AreEqual(target.NodeId, restored.CurrentNodeId); // 현재 노드 유지 검증
            Assert.AreEqual(target.NodeId, restored.SelectedNodeId); // 선택 노드 유지 검증
            Assert.AreEqual(target.Position, restored.KingMapPosition); // 지도 킹 좌표 유지 검증
            CollectionAssert.Contains(restored.SelectedPathNodeIds, target.NodeId); // 선택 경로 기록 유지 검증
            CollectionAssert.Contains(restored.VisitedNodeIds, target.NodeId); // 방문 노드 이력 유지 검증
            Assert.IsTrue(restored.FindNode(target.NodeId).Visited); // 방문 플래그 유지 검증
        }

        [Test]
        public void RunSaveData_RoundTrip_PreservesFlowEconomyKingAndRoute()
        {
            var run = new RunState(2); // 테스트 런 생성
            run.CurrentRound = 4; // 현재 스테이지 설정
            run.Round.Restore(4, RoundProgressStatus.Cleared, ProjectEta.Battle.BattleOutcome.Victory); // 스테이지 완료 상태 설정
            var current = new StageNode("depth_4_resolved", new Vector2Int(4, 3), 4, "ResolvedStage"); // 현재 지도 노드 생성
            var next = new StageNode("depth_5_boss_midboss", new Vector2Int(4, 4), 5, "stage_5_midboss"); // 중간 보스 노드 생성
            current.SetNextNodeIds(new[] { next.NodeId }); // 다음 노드 연결
            run.RouteMap.Configure(4, current, new[] { next }); // 경로 지도 구성
            run.Flow.EnterMap(); // 안전 저장 지점 지도 흐름 설정
            RunEconomyService.Restore(run, 85); // 런 Gold 복원용 값 설정
            KingRunStateService.Restore(run, KingArchetype.Defense); // 방어형 킹 선택 상태 설정

            RunSaveData data = run.ToSaveData(); // 전체 런 저장 DTO 생성
            string json = JsonUtility.ToJson(data); // JSON 직렬화
            RunSaveData decoded = JsonUtility.FromJson<RunSaveData>(json); // JSON 역직렬화
            RunState restored = RunState.FromSaveData(decoded, null); // 저장 DTO 기반 런 복원

            Assert.AreEqual(RunSaveData.CurrentVersion, decoded.saveVersion); // 최신 저장 버전 검증
            Assert.AreEqual(RunFlowPhase.Map, restored.CurrentFlowPhase); // 상위 런 흐름 복원 검증
            Assert.AreEqual(85, RunEconomyService.GetOrCreate(restored).Currency); // 런 Gold 복원 검증
            Assert.AreEqual(KingArchetype.Defense, KingRunStateService.Get(restored).Archetype); // 선택 킹 복원 검증
            Assert.AreEqual(run.RouteMap.MapSeed, restored.RouteMap.MapSeed); // 맵 시드 복원 검증
            Assert.AreEqual(run.RouteMap.CurrentNodeId, restored.RouteMap.CurrentNodeId); // 현재 지도 노드 복원 검증
        }

        [Test]
        public void RunSaveData_CapturesRuntimeCardUpgradeStats()
        {
            PieceDefinition baseCard = CreateCard("pawn_test", "Pawn", 2, 1); // 기본 테스트 카드 생성
            PieceDefinition upgraded = RuntimeCardUpgradeService.CreateRestoredCard(baseCard, 3, 2, "Pawn +1"); // 런타임 강화 카드 생성
            var run = new RunState(3); // 테스트 런 생성
            run.Deck.AddToOwnedPool(upgraded); // 강화 카드를 런 보유 풀에 추가

            RunSaveData data = run.ToSaveData(); // 저장 DTO 생성

            Assert.AreEqual(1, data.ownedCards.Count); // 강화 카드 스냅샷 수 검증
            Assert.AreEqual("pawn_test", data.ownedCards[0].pieceId); // 원본 PieceId 저장 검증
            Assert.AreEqual(3, data.ownedCards[0].baseHp); // 강화 HP 저장 검증
            Assert.AreEqual(2, data.ownedCards[0].baseAtk); // 강화 ATK 저장 검증
            Assert.AreEqual("Pawn +1", data.ownedCards[0].displayName); // 강화 표시 이름 저장 검증

            if (upgraded != null && upgraded != baseCard) Object.DestroyImmediate(upgraded); // 테스트 런타임 복제 정리
            Object.DestroyImmediate(baseCard); // 테스트 원본 ScriptableObject 정리
        }

        [Test]
        public void RuntimeCardUpgradeService_CreateRestoredCard_RebuildsSavedStats()
        {
            PieceDefinition baseCard = CreateCard("rook_test", "Rook", 4, 2); // 기본 테스트 카드 생성

            PieceDefinition restored = RuntimeCardUpgradeService.CreateRestoredCard(baseCard, 6, 4, "Rook +2"); // 저장 스탯 기반 런타임 복원

            Assert.IsNotNull(restored); // 복원 카드 존재 검증
            Assert.AreNotSame(baseCard, restored); // 원본 에셋 직접 변형 방지 검증
            Assert.AreEqual(6, restored.BaseHp); // 저장 HP 복원 검증
            Assert.AreEqual(4, restored.BaseAtk); // 저장 ATK 복원 검증
            Assert.AreEqual("Rook +2", restored.DisplayName); // 저장 표시 이름 복원 검증

            Object.DestroyImmediate(restored); // 런타임 복제 정리
            Object.DestroyImmediate(baseCard); // 테스트 원본 정리
        }

        [Test]
        public void RunSaveSystem_SafeCheckpoint_AllowsOnlyStableNonBattleState()
        {
            var run = new RunState(3); // 테스트 런 생성

            Assert.IsFalse(RunSaveSystem.IsSafeCheckpoint(run)); // 전투 중 자동 저장 차단 검증

            run.RouteMap.PreparePrototypeAfterBattle(1); // 지도 분기 준비
            run.Flow.EnterMap(); // 안정적인 지도 선택 전 상태 진입
            Assert.IsTrue(RunSaveSystem.IsSafeCheckpoint(run)); // 선택 전 지도 자동 저장 허용 검증

            StageNode selectable = run.RouteMap.GetSelectableNodes()[0]; // 첫 선택 가능 노드 조회
            Assert.IsTrue(run.RouteMap.TryMoveKingTo(selectable.NodeId)); // 지도 노드 선택
            Assert.IsFalse(RunSaveSystem.IsSafeCheckpoint(run)); // 선택 후 전환 대기 지도 저장 차단 검증

            run.Flow.EnterShop(); // 실제 비전투 스테이지 진입
            Assert.IsTrue(RunSaveSystem.IsSafeCheckpoint(run)); // 상점 안전 저장 허용 검증

            run.Flow.CompleteRun(); // 런 종료 처리
            Assert.IsFalse(RunSaveSystem.IsSafeCheckpoint(run)); // 종료 상태 저장 차단 검증
        }

        [Test]
        public void RunFlowState_Restore_RejectsInvalidPhaseToBattle()
        {
            var flow = new RunFlowState(); // 새 런 흐름 생성

            flow.Restore(999); // 잘못된 저장 Phase 복원 시도

            Assert.AreEqual(RunFlowPhase.Battle, flow.Phase); // 잘못된 값은 Battle fallback 검증
            Assert.AreEqual(BoardMode.Battle, flow.BoardMode); // 보드 모드도 Battle fallback 검증
        }


        [Test]
        public void RunId_RoundTrip_RemainsStable()
        {
            var run = new RunState(3); // 테스트 런 생성
            string originalRunId = run.RunId; // 최초 런 고유 ID 저장

            RunSaveData data = run.ToSaveData(); // 런 저장 DTO 생성
            RunState restored = RunState.FromSaveData(data, null); // 저장 DTO 기반 런 복원

            Assert.IsFalse(string.IsNullOrWhiteSpace(originalRunId)); // 신규 런 ID 생성 검증
            Assert.AreEqual(originalRunId, restored.RunId); // 저장·복원 후 동일 런 ID 유지 검증
        }

        [Test]
        public void MetaProgress_RunRewardClaim_PersistsAndBlocksDuplicate()
        {
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성
            string runId = "run_day51_test"; // 테스트 런 고유 ID 지정

            Assert.IsTrue(progress.TryClaimRunReward(runId)); // 최초 런 보상 Claim 허용 검증
            Assert.IsFalse(progress.TryClaimRunReward(runId)); // 같은 세션 중복 Claim 차단 검증

            MetaProgressSaveData data = progress.ToSaveData(); // 영구 진행 저장 DTO 생성
            MetaProgressState restored = MetaProgressState.FromSaveData(data); // 저장 DTO 기반 영구 진행 복원

            Assert.IsTrue(restored.IsRunRewardClaimed(runId)); // 재실행 후 Claim 이력 유지 검증
            Assert.IsFalse(restored.TryClaimRunReward(runId)); // 복원 후 동일 런 중복 Claim 차단 검증
        }

        private static PieceDefinition CreateCard(string pieceId, string displayName, int hp, int atk)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 카드 ScriptableObject 생성
            SetPrivateField(definition, "_pieceId", pieceId); // PieceId 설정
            SetPrivateField(definition, "_displayName", displayName); // 표시 이름 설정
            SetPrivateField(definition, "_baseHp", hp); // 기본 HP 설정
            SetPrivateField(definition, "_baseAtk", atk); // 기본 ATK 설정
            return definition; // 테스트 카드 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance); // private 직렬화 필드 조회
            Assert.IsNotNull(field); // 대상 필드 존재 검증
            field.SetValue(target, value); // 테스트 값 설정
        }
    }
}
