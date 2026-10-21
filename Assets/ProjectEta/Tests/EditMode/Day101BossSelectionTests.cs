using NUnit.Framework; // 보스 선택 계약 검사
using ProjectEta.Run; // 런 어셈블리 조회
using ProjectEta.Boss; // 보스 등록·선택
using ProjectEta.Pieces; // 실제 전투 기물
using UnityEngine; // 에셋과 저장 JSON

namespace ProjectEta.Tests.EditMode // 101일차 검사 영역
{ // 영역 시작
    public sealed class Day101BossSelectionTests // 보스 등록 기반 검사
    { // 타입 시작
        [Test] // 신규 보스 목록 계약
        public void Catalog_RegistersFifteenBossIdentities() // 누락된 카탈로그 재현
        { // 메서드 시작
            Assert.That(BossCatalog.All.Count, Is.EqualTo(15)); // 실제 고유 보스 수 확인
            for (int phase = 1; phase <= 4; phase++) // 페이즈 후보 순회
            { // 반복 시작
                Assert.That(BossCatalog.GetPool(phase).Count, Is.EqualTo(3)); // 페이즈별 세 후보
            } // 반복 종료
            Assert.That(BossCatalog.GetPool(5).Count, Is.EqualTo(2)); // 마지막 페이즈 고정 보스 수
            Assert.That(BossCatalog.Find("serdin").size, Is.EqualTo(Vector2Int.one)); // 기사 점유 확인
            Assert.That(BossCatalog.Find("vargas").size, Is.EqualTo(new Vector2Int(2, 2))); // 골렘 점유 확인
            foreach (BossDefinition definition in BossCatalog.All) // 등록 데이터 순회
            { // 반복 시작
                Assert.That(definition.IsValid, Is.True, definition.name); // 실제 에셋 무결성
                Assert.That(definition.provisional, Is.True); // 임시 수치 구분
                Assert.That(definition.bespokePatternsImplemented, Is.False); // 전용 패턴 완료 오표시 차단
            } // 반복 종료
        } // 메서드 종료
        [TearDown] // 검사 자원 정리
        public void Cleanup() // 생성된 보스 복제 제거
        { // 메서드 시작
            BossRuntimeDefinitionFactory.Clear(); // 런타임 정의 정리
        } // 메서드 종료
        [Test] // 결정론·중복 방지 검사
        public void Selection_IsStableAndDifferentWithinPhaseForManySeeds() // 다수 Seed의 후보 계약
        { // 메서드 시작
            for (int seed = -32; seed < 128; seed++) // 음수 포함 지도 Seed
            { // 반복 시작
                RunState run = CreateRun(seed); // 동일 지도 조건
                for (int phase = 1; phase <= 4; phase++) // 모든 무작위 페이즈
                { // 반복 시작
                    BossSelectionSnapshot first = BossSelectionService.Select(run, phase, 5); // 기본 보스 선택
                    BossSelectionSnapshot second = BossSelectionService.Select(run, phase, 10); // 강화 보스 선택
                    Assert.That(first.bossId, Is.Not.EqualTo(second.bossId)); // 같은 페이즈 중복 차단
                    Assert.That(BossCatalog.Find(first.bossId).phase, Is.EqualTo(phase)); // 기본 후보 범위
                    Assert.That(BossCatalog.Find(second.bossId).phase, Is.EqualTo(phase)); // 강화 후보 범위
                    Assert.That(JsonUtility.ToJson(first), Is.EqualTo(JsonUtility.ToJson(BossSelectionService.Select(CreateRun(seed), phase, 5)))); // 동일 Seed 재현
                } // 반복 종료
            } // 반복 종료
        } // 메서드 종료
        [Test] // 고정 보스와 진엔딩 구분
        public void FixedBosses_AndMasterKingAreSeparatedFromNormalPool() // 고정·일반 경로 구분
        { // 메서드 시작
            Assert.That(BossSelectionService.Select(CreateRun(1), 5, 5).bossId, Is.EqualTo("astragon")); // 중간 보스 고정
            Assert.That(BossSelectionService.Select(CreateRun(99), 5, 10).bossId, Is.EqualTo("mordrain")); // 최종 보스 고정
            Assert.That(BossSelectionService.Select(CreateRun(1), 1, 3), Is.Null); // 일반 노드 제외
            Assert.That(BossSelectionService.CreateMasterKingPreview().bossId, Is.EqualTo("master_king")); // 진엔딩 데이터 등록만 확인
        } // 메서드 종료
        [Test] // 조회 순서·강화 차이 검사
        public void StageRules_PreservePairAndAddEnhancedReinforcementOnce() // 저장 서비스의 실제 선택 연결
        { // 메서드 시작
            RunState forward = CreateRun(440); // 정방향 조회 런
            RunState reverse = CreateRun(440); // 역방향 조회 런
            StageDefinition early = StageDefinitionCatalog.Resolve("stage_5_midboss", 5); // 기본 스테이지
            StageDefinition late = StageDefinitionCatalog.Resolve("stage_10_midboss", 10); // 강화 스테이지
            StageRuleSnapshot first = RunStageRuleService.GetOrCreate(forward, early, 2); // 기본 먼저 조회
            StageRuleSnapshot second = RunStageRuleService.GetOrCreate(forward, late, 2); // 강화 나중 조회
            StageRuleSnapshot reverseSecond = RunStageRuleService.GetOrCreate(reverse, late, 2); // 강화 먼저 조회
            StageRuleSnapshot reverseFirst = RunStageRuleService.GetOrCreate(reverse, early, 2); // 기본 나중 조회
            Assert.That(first.bossSelection.bossId, Is.EqualTo(reverseFirst.bossSelection.bossId)); // 기본 조회 순서 독립
            Assert.That(second.bossSelection.bossId, Is.EqualTo(reverseSecond.bossSelection.bossId)); // 강화 조회 순서 독립
            Assert.That(second.bossSelection.variant, Is.EqualTo(BossVariant.Enhanced)); // 실제 강화형 저장
            Assert.That(second.reinforcements.Count, Is.EqualTo(late.RoundDefinition.Reinforcements.Count + 1)); // 추가 증원 연결
            Assert.That(RunStageRuleService.GetOrCreate(forward, late, 2), Is.SameAs(second)); // 재추첨·중복 증원 차단
        } // 메서드 종료
        [Test] // 실제 저장 복원 검사
        public void SavedBoss_RestoresFrozenStatsAndActualBoardOccupancy() // 원본 변경에도 선택 수치 유지
        { // 메서드 시작
            RunState run = CreateRun(99); // 독립 런 생성
            StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(run, StageDefinitionCatalog.Resolve("stage_5_midboss", 5), 1); // 보스 규칙 저장
            BossSelectionSnapshot selection = rules.bossSelection; // 선택한 보스 기준
            PieceDefinition definition = BossRuntimeDefinitionFactory.Resolve(selection); // 실제 전투 정의
            var piece = new PieceRuntimeState(definition, new Vector2Int(0, 8), false); // 실제 적 보스
            piece.CurrentHp = 5; // 손상 체력 재현
            Assert.That(LargePieceBoardUtility.TryPlace(run.Board, piece, piece.BoardPosition), Is.True); // 실제 점유 등록
            RunSaveData saved = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())); // JSON 왕복 저장
            BossDefinition source = BossCatalog.Find(selection.bossId); // 원본 데이터 조회
            int originalHp = source.baseHp; // 원본 수치 보관
            try // 밸런스 변경 재현
            { // 검사 시작
                source.baseHp = 999; // 저장 이후 원본 변경
                BossRuntimeDefinitionFactory.Clear(); // 메모리 재사용 없는 복원
                RunState restored = RunState.FromSaveData(saved, null); // 저장 데이터로 실제 복원
                PieceRuntimeState recovered = restored.Board.GetTile(new Vector2Int(0, 8)).OccupyingPiece; // 복원된 보스
                Assert.That(recovered, Is.Not.Null); // 런타임 보스 누락 차단
                Assert.That(recovered.Definition.PieceId, Is.EqualTo(selection.pieceId)); // 같은 정체성·형태 유지
                Assert.That(recovered.Definition.BaseHp, Is.EqualTo(selection.maxHp)); // 저장 체력 기준 유지
                Assert.That(recovered.Definition.BaseAtk, Is.EqualTo(selection.attack)); // 저장 공격력 유지
                Assert.That(recovered.CurrentHp, Is.EqualTo(5)); // 손상 체력 유지
                Assert.That(LargePieceBoardUtility.IsFootprintComplete(restored.Board, recovered), Is.True); // 전체 점유 복원
            } // 검사 종료
            finally // 원본 데이터 정리
            { // 정리 시작
                source.baseHp = originalHp; // 테스트 이후 원본 복원
            } // 정리 종료
        } // 메서드 종료
        [Test] // 구버전 저장 검사
        public void LegacySnapshot_DoesNotRerollOriginalBoss() // 기존 런 보존
        { // 메서드 시작
            RunState run = CreateRun(2026); // 기존 저장 조건
            StageDefinition definition = StageDefinitionCatalog.Resolve("stage_5_midboss", 5); // 이전 중간 보스
            var legacy = new StageRuleSnapshot // 보스 선택 필드 없는 이전 저장
            { // 객체 시작
                phase = 1, // 기존 페이즈
                stageDefinitionId = definition.StageId, // 이전 정의 ID
                stageType = (int)definition.StageType, // 기존 전투 종류
                turnLimit = 30, // 기존 제한
                victoryGold = 40, // 기존 보상
                isBossRound = true, // 이전 보스 여부
                bossResourceName = "MidBoss74" // 이전 리소스
            }; // 객체 종료
            run.BalanceData.stageRuleSnapshots.Add(legacy); // 이전 규칙 연결
            StageRuleSnapshot result = RunStageRuleService.GetOrCreate(run, definition, 1); // 업데이트 이후 조회
            Assert.That(result, Is.SameAs(legacy)); // 이전 규칙 유지
            Assert.That(result.bossSelection, Is.Null); // 신규 보스 소급 추첨 차단
            Assert.That(result.bossResourceName, Is.EqualTo("MidBoss74")); // 이전 보스 유지
        } // 메서드 종료
        private static RunState CreateRun(int seed) // 재현 가능한 지도 런
        { // 메서드 시작
            var run = new RunState(3); // 독립 런 생성
            run.RouteMap.Restore(new RouteMapState(seed).ToSaveData()); // 지도 Seed 고정
            return run; // 시험 런 반환
        } // 메서드 종료
        [Test] // 보스 풀 업데이트 호환
        public void PoolChanges_KeepStoredBossAndAvoidDuplicateAtOtherDepth() // 기존 보스와 신규 후보 충돌 차단
        { // 메서드 시작
            BossDefinition moved = BossCatalog.Find("bragon"); // 후보 변경 대상
            int originalPhase = moved.phase; // 원본 페이즈 보관
            try // 후보 풀 변경 재현
            { // 검사 시작
                for (int seed = 0; seed < 64; seed++) // 다양한 기존 선택 조건
                { // 반복 시작
                    moved.phase = originalPhase; // 원래 후보로 첫 선택
                    RunState run = CreateRun(seed); // 독립 런 생성
                    StageRuleSnapshot stored = RunStageRuleService.GetOrCreate(run, StageDefinitionCatalog.Resolve("stage_5_midboss", 5), 1); // 기존 보스 선택 저장
                    moved.phase = 2; // 다음 버전 후보 풀 변경 재현
                    StageRuleSnapshot next = RunStageRuleService.GetOrCreate(run, StageDefinitionCatalog.Resolve("stage_10_midboss", 10), 1); // 변경 이후 다음 보스 선택
                    Assert.That(next.bossSelection.bossId, Is.Not.EqualTo(stored.bossSelection.bossId)); // 저장 보스 중복 방지
                    Assert.That(RunStageRuleService.GetOrCreate(run, StageDefinitionCatalog.Resolve("stage_5_midboss", 5), 1), Is.SameAs(stored)); // 기존 선택 재추첨 차단
                } // 반복 종료
            } // 검사 종료
            finally // 원본 에셋 복원
            { // 정리 시작
                moved.phase = originalPhase; // 테스트 원본 데이터 복원
            } // 정리 종료
        } // 메서드 종료
        [Test] // 실제 자동 이어하기 정책 검사
        public void SafeMapCheckpoint_PreservesPreviewedBossAndKeepsBattleResumeDisabled() // 안전 지점 복원 계약
        { // 메서드 시작
            RunState run = CreateRun(440); // 재현 가능한 런
            run.CurrentRound = 4; // 다음 보스 직전 깊이
            var current = new StageNode("phase_1_current", Vector2Int.zero, 4, "stage_4_battle"); // 지도 현재 위치
            var next = new StageNode("phase_1_next_boss", Vector2Int.one, 5, "stage_5_midboss"); // 다음 보스 위치
            run.RouteMap.Configure(4, current, new[] { next }); // 실제 지도 상태 연결
            run.Flow.EnterMap(); // 자동 이어하기 가능한 상태
            StageRuleSnapshot preview = RunStageRuleService.GetOrCreate(run, StageDefinitionCatalog.Resolve("stage_5_midboss", 5), 1); // 지도 미리보기 선택 저장
            Assert.That(RunSaveSystem.IsSafeCheckpoint(run), Is.True); // 실제 안전 저장 정책 확인
            RunSaveData saved = JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())); // 안전 지점 JSON 저장
            Assert.That(RunSaveSystem.IsContinueDataValid(saved), Is.True); // 실제 이어하기 데이터 정책 확인
            RunState restored = RunState.FromSaveData(saved, null); // 저장된 지도 복원
            Assert.That(RunStageRuleService.GetOrCreate(restored, StageDefinitionCatalog.Resolve("stage_5_midboss", 5), 1).bossSelection.pieceId, Is.EqualTo(preview.bossSelection.pieceId)); // 미리 본 보스 유지
            restored.Flow.EnterBattle(); // 전투 중 저장 상태 재현
            Assert.That(RunSaveSystem.IsSafeCheckpoint(restored), Is.False); // 기존 전투 중 자동 저장 제한 유지
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
