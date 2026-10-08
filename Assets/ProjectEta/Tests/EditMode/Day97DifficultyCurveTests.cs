using System.Collections.Generic; // 목록 검증
using System.Linq; // 원형 ID 집계
using NUnit.Framework; // NUnit 검증
using UnityEngine; // JSON 저장 복원
using ProjectEta.Battle; // 전투 결과 열거형
using ProjectEta.Boss; // 보스 페이즈 상태
using ProjectEta.Debugging; // F1 개발 도구
using ProjectEta.Run; // 편성·측정 시스템

namespace ProjectEta.Tests.EditMode // 97일차 난이도 검증 영역
{ // 네임스페이스 시작
    public sealed class Day97DifficultyCurveTests // 난이도 곡선과 개발 도구 검증
    { // 클래스 시작
        [TearDown] // 테스트 종료 정리
        public void TearDown() // 정적 개발 설정 초기화
        { // 메서드 시작
            EnemyEncounterDebugOverrides.Clear(); // 강제 편성 초기화
        } // 메서드 종료

        [Test] // 페이즈 잠금 검증
        public void Profiles_페이즈상승에따라원형단계적해제() // 초반 단순성과 후반 다양성 확인
        { // 메서드 시작
            IReadOnlyList<EnemyEncounterProfile> phaseOne = EnemyEncounterProfileCatalog.GetAvailableProfiles(StageType.Battle, 1); // 1페이즈 일반 원형
            IReadOnlyList<EnemyEncounterProfile> phaseTwo = EnemyEncounterProfileCatalog.GetAvailableProfiles(StageType.Battle, 2); // 2페이즈 일반 원형
            IReadOnlyList<EnemyEncounterProfile> phaseThree = EnemyEncounterProfileCatalog.GetAvailableProfiles(StageType.Battle, 3); // 3페이즈 일반 원형
            Assert.That(phaseOne.Select(x => x.ProfileId), Is.EquivalentTo(new[] { "normal_frontline", "normal_skirmish" })); // 초반 두 원형 확인
            Assert.That(phaseTwo.Count, Is.EqualTo(4)); // 중반 네 원형 확인
            Assert.That(phaseThree.Count, Is.EqualTo(5)); // 후반 전체 원형 확인
            Assert.That(EnemyEncounterProfileCatalog.GetAvailableProfiles(StageType.Elite, 1).Count, Is.EqualTo(1)); // 초반 정예 한 원형 확인
            Assert.That(EnemyEncounterProfileCatalog.GetAvailableProfiles(StageType.Elite, 3).Count, Is.EqualTo(3)); // 후반 정예 전체 확인
        } // 메서드 종료

        [Test] // 반복 방지 검증
        public void Select_직전원형제외후결정론유지() // 연속 같은 편성 차단 확인
        { // 메서드 시작
            EnemyEncounterProfile first = EnemyEncounterProfileCatalog.Select(StageType.Battle, 27, 3, string.Empty, string.Empty); // 최초 결정 선택
            EnemyEncounterProfile second = EnemyEncounterProfileCatalog.Select(StageType.Battle, 27, 3, first.ProfileId, string.Empty); // 직전 원형 제외 선택
            EnemyEncounterProfile repeated = EnemyEncounterProfileCatalog.Select(StageType.Battle, 27, 3, first.ProfileId, string.Empty); // 같은 조건 재선택
            Assert.That(second.ProfileId, Is.Not.EqualTo(first.ProfileId)); // 직전 원형 제외 확인
            Assert.That(repeated.ProfileId, Is.EqualTo(second.ProfileId)); // 제외 후 결정성 확인
        } // 메서드 종료

        [Test] // 개발 강제 원형 검증
        public void DebugOverride_해금된원형순환과초기화() // F1 다음 편성 선택 확인
        { // 메서드 시작
            string selected = EnemyEncounterDebugOverrides.Cycle(StageType.Battle, 1, 1); // 첫 원형 강제 선택
            Assert.That(selected, Is.EqualTo("normal_frontline")); // 첫 해금 원형 확인
            selected = EnemyEncounterDebugOverrides.Cycle(StageType.Battle, 1, 1); // 다음 원형 선택
            Assert.That(selected, Is.EqualTo("normal_skirmish")); // 둘째 해금 원형 확인
            Assert.That(EnemyEncounterDebugOverrides.Consume(selected), Is.True); // 다음 편성 일회 적용 소비 확인
            Assert.That(EnemyEncounterDebugOverrides.ForcedProfileId, Is.Empty); // 강제 설정 제거 확인
        } // 메서드 종료

        [Test] // 난이도 집계 검증
        public void Telemetry_원형별승률턴왕체력손실저장복원() // 실제 플레이 난이도 지표 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 측정 런 생성
            EnemyEncounterResult encounter = CreateEncounter("normal_frontline", StageType.Battle, 11, 4, 90); // 시작 편성 생성
            RunBalanceTelemetry.RecordEncounter(run, encounter); // 전투 시작 기록
            run.KingHp = 1; // 종료 왕 체력 적용
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Victory, 7); // 승리 결과 기록
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), null); // JSON 저장 복원
            RunBattleDifficultySummary summary = restored.BalanceData.difficultySummaries.Single(); // 원형별 집계 조회
            Assert.That(summary.profileId, Is.EqualTo("normal_frontline")); // 원형 ID 확인
            Assert.That(summary.stageType, Is.EqualTo((int)StageType.Battle)); // 스테이지 타입 확인
            Assert.That(summary.battleCount, Is.EqualTo(1)); // 전투 수 확인
            Assert.That(summary.victoryCount, Is.EqualTo(1)); // 승리 수 확인
            Assert.That(summary.totalTurns, Is.EqualTo(7)); // 턴 합계 확인
            Assert.That(summary.totalKingHpLoss, Is.EqualTo(2)); // 왕 HP 손실 확인
            Assert.That(summary.totalThreatScore, Is.EqualTo(90)); // 위협도 합계 확인
        } // 메서드 종료

        [Test] // 중복 집계 검증
        public void Telemetry_같은결과난이도한번만집계() // 중복 종료 경로 차단 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_elite", 1); // 측정 런 생성
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("elite_vanguard", StageType.Elite, 12, 5, 130)); // 정예 시작 기록
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Defeat, 5); // 첫 패배 기록
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Defeat, 5); // 중복 패배 기록
            RunBattleDifficultySummary summary = run.BalanceData.difficultySummaries.Single(); // 원형별 집계 조회
            Assert.That(summary.battleCount, Is.EqualTo(1)); // 단일 집계 확인
            Assert.That(summary.defeatCount, Is.EqualTo(1)); // 단일 패배 확인
        } // 메서드 종료

        [Test] // 상세 상한 독립 집계 검증
        public void Telemetry_상한이후에도난이도집계보존() // 긴 런 통계 손실 차단 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 측정 런 생성
            for (int index = 0; index < RunBalanceTelemetry.MaximumEntries; index++) // 상세 기록 상한 채우기
            { // 반복 시작
                RunBalanceTelemetry.RecordGold(run, 100, 100, 999, false, "Test"); // 실패 지출 기록
            } // 반복 종료
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("normal_skirmish", StageType.Battle, 13, 4, 80)); // 상한 이후 편성 기록
            run.KingHp = 2; // 종료 왕 체력 적용
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Victory, 6); // 상한 이후 결과 기록
            RunBattleDifficultySummary summary = run.BalanceData.difficultySummaries.Single(); // 보존 집계 조회
            Assert.That(summary.battleCount, Is.EqualTo(1)); // 전투 집계 확인
            Assert.That(summary.totalKingHpLoss, Is.EqualTo(1)); // 왕 HP 손실 확인
            Assert.That(run.BalanceData.droppedEntries, Is.EqualTo(2)); // 상세 두 건 생략 확인
            string text = RunBalanceReport.BuildSummary(run); // 상한 이후 F1 요약 생성
            Assert.That(text, Does.Contain("normal_skirmish")); // 최근 원형 스냅샷 표시 확인
            Assert.That(text, Does.Contain("Victory")); // 최근 결과 스냅샷 표시 확인
        } // 메서드 종료

        [Test] // 최근 원형 상한 독립 저장 검증
        public void Telemetry_상한이후종류별최근원형저장복원() // 긴 런 반복 방지 상태 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 측정 런 생성
            for (int index = 0; index < RunBalanceTelemetry.MaximumEntries; index++) // 상세 기록 상한 채우기
            { // 반복 시작
                RunBalanceTelemetry.RecordGold(run, 100, 100, 999, false, "Test"); // 실패 지출 기록
            } // 반복 종료
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("normal_frontline", StageType.Battle, 21, 4, 80)); // 상한 이후 일반 원형 저장
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("elite_vanguard", StageType.Elite, 22, 5, 120)); // 상한 이후 정예 원형 저장
            RunSaveData save = run.ToSaveData(); // 저장 데이터 생성
            RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(save)), null); // JSON 저장 복원
            Assert.That(restored.BalanceData.GetLastEncounterProfile(StageType.Battle), Is.EqualTo("normal_frontline")); // 최근 일반 원형 복원 확인
            Assert.That(restored.BalanceData.GetLastEncounterProfile(StageType.Elite), Is.EqualTo("elite_vanguard")); // 최근 정예 원형 복원 확인
        } // 메서드 종료

        [Test] // 구버전 난이도 이관 검증
        public void Save_96일차상세전투를원형별난이도로한번이관() // 이전 저장 통계 호환 확인
        { // 메서드 시작
            var data = new RunBalanceData(); // 구버전 측정 데이터 생성
            data.schemaVersion = 1; // 96일차 저장 버전 설정
            data.difficultySummaries.Clear(); // 새 집계 없음 재현
            data.entries.Add(new RunBalanceEntry // 구버전 편성 기록 추가
            { // 객체 시작
                sequence = 0, // 시작 기록 순서
                kind = "Encounter", // 편성 기록 종류
                source = "elite_vanguard", // 구버전 원형 저장 위치
                phase = 2, // 시작 페이즈
                stage = 3, // 시작 스테이지
                nodeId = "legacy_elite", // 시작 노드
                encounterId = "legacy_encounter", // 편성 ID
                threatScore = 120, // 시작 위협도
                kingHp = 3 // 구버전 시작 왕 체력
            }); // 객체 종료
            data.entries.Add(new RunBalanceEntry // 구버전 결과 기록 추가
            { // 객체 시작
                sequence = 1, // 결과 기록 순서
                kind = "BattleResult", // 결과 기록 종류
                source = "Victory", // 구버전 승패 문자열
                phase = 2, // 완료 페이즈
                stage = 3, // 완료 스테이지
                turn = 6, // 종료 턴
                nodeId = "legacy_elite", // 완료 노드
                encounterId = "legacy_encounter", // 연결 편성 ID
                threatScore = 120, // 시작 위협도 복사값
                kingHp = 1, // 종료 왕 체력
                outcome = (int)BattleOutcome.Victory // 승리 열거값
            }); // 객체 종료
            data.Normalize(); // 새 저장 형식 이관 실행
            RunBattleDifficultySummary summary = data.difficultySummaries.Single(); // 이관 원형 집계 조회
            Assert.That(data.schemaVersion, Is.EqualTo(2)); // 저장 버전 갱신 확인
            Assert.That(summary.profileId, Is.EqualTo("elite_vanguard")); // 원형 ID 복구 확인
            Assert.That(summary.stageType, Is.EqualTo((int)StageType.Elite)); // 정예 종류 추론 확인
            Assert.That(summary.battleCount, Is.EqualTo(1)); // 단일 전투 이관 확인
            Assert.That(summary.totalTurns, Is.EqualTo(6)); // 종료 턴 이관 확인
            Assert.That(summary.totalKingHpLoss, Is.EqualTo(2)); // 왕 HP 손실 이관 확인
            Assert.That(data.GetLastEncounterProfile(StageType.Elite), Is.EqualTo("elite_vanguard")); // 최근 정예 원형 복구 확인
            data.Normalize(); // 중복 이관 방지 재실행
            Assert.That(data.difficultySummaries.Single().battleCount, Is.EqualTo(1)); // 재실행 중복 없음 확인
        } // 메서드 종료

        [Test] // 새 저장 이관 반복 방지 검증
        public void Telemetry_Schema2빈집계가최근원형을지우지않음() // 보스 결과 이후 상한 경계 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 시작 전투 런 생성
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Victory, 3); // 연결 편성 없는 결과 기록
            while (run.BalanceData.entries.Count < RunBalanceTelemetry.MaximumEntries) // 상세 기록 상한 채우기
            { // 반복 시작
                RunBalanceTelemetry.RecordGold(run, 100, 100, 999, false, "Test"); // 실패 지출 기록
            } // 반복 종료
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("normal_frontline", StageType.Battle, 31, 4, 80)); // 상한 이후 최근 일반 원형 저장
            Assert.That(run.BalanceData.GetLastEncounterProfile(StageType.Battle), Is.EqualTo("normal_frontline")); // 반복 이관 없는 최근 원형 확인
            Assert.That(run.BalanceData.schemaVersion, Is.EqualTo(2)); // 새 저장 버전 유지 확인
        } // 메서드 종료

        [Test] // 보스 최신 결과 표시 검증
        public void Report_보스결과를이전일반전투결과로덮지않음() // F1 최근 결과 순서 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 일반 전투 런 생성
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("normal_frontline", StageType.Battle, 32, 4, 80)); // 일반 편성 기록
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Victory, 5); // 일반 승리 기록
            run.CurrentRound = 5; // 중간 보스 깊이 설정
            run.RouteMap.Configure(5, new StageNode("phase_3_stage_5_midboss", Vector2Int.zero, 5, "stage_5_midboss"), null); // 중간 보스 노드 설정
            run.KingHp = 0; // 보스 패배 왕 체력 적용
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Defeat, 4); // 최신 보스 패배 기록
            string text = RunBalanceReport.BuildSummary(run); // F1 요약 생성
            Assert.That(text, Does.Contain("최근 결과: Defeat")); // 최신 보스 패배 표시 확인
            Assert.That(text, Does.Not.Contain("최근 결과: Victory")); // 이전 일반 승리 덮어쓰기 차단
            Assert.That(text, Does.Not.Contain("최근 편성: normal_frontline")); // 보스 결과에 오래된 편성 연결 차단
        } // 메서드 종료

        [Test] // F1 요약 검증
        public void Report_원형평균승률턴왕피해표시() // 즉시 난이도 판단 정보 확인
        { // 메서드 시작
            RunState run = CreateRun("phase_1_stage_1_battle", 1); // 측정 런 생성
            RunBalanceTelemetry.RecordEncounter(run, CreateEncounter("normal_frontline", StageType.Battle, 14, 4, 100)); // 시작 편성 기록
            run.KingHp = 2; // 종료 왕 체력 적용
            RunBalanceTelemetry.RecordBattleResult(run, BattleOutcome.Victory, 8); // 승리 결과 기록
            string text = RunBalanceReport.BuildSummary(run); // F1 요약 생성
            Assert.That(text, Does.Contain("원형 난이도")); // 난이도 구역 확인
            Assert.That(text, Does.Contain("승률 100%")); // 승률 표시 확인
            Assert.That(text, Does.Contain("평균 턴 8")); // 평균 턴 표시 확인
            Assert.That(text, Does.Contain("왕 HP 손실 1")); // 평균 피해 표시 확인
        } // 메서드 종료

        [TestCase(15, 0.50f, 7)] // 홀수 최대 HP 절반 이하 보정
        [TestCase(15, 0.25f, 3)] // 홀수 최대 HP 사분의 일 이하 보정
        [TestCase(1, 0.25f, 1)] // 생존 최소 HP 보정
        public void DebugBossHp_비율을생존범위로계산(int maximumHp, float ratio, int expected) // 보스 단계 이동 안전값 확인
        { // 메서드 시작
            Assert.That(ProjectEtaDebugWindow.CalculateDebugBossHp(maximumHp, ratio), Is.EqualTo(expected)); // 계산 결과 확인
        } // 메서드 종료

        [Test] // 보스 페이즈 경계 연결 검증
        public void DebugBossHp_50퍼센트버튼이Phase2진입조건충족() // 홀수 최대 HP 페이즈 확인
        { // 메서드 시작
            int debugHp = ProjectEtaDebugWindow.CalculateDebugBossHp(15, 0.50f); // 50퍼센트 개발 HP 계산
            var state = new BossPhaseRuntimeState(); // 새 보스 페이즈 상태 생성
            Assert.That(state.TryEnterPhase2(debugHp, 15), Is.True); // 실제 Phase2 규칙 진입 확인
        } // 메서드 종료

        private static RunState CreateRun(string nodeId, int stage) // 측정 위치가 있는 런 생성
        { // 메서드 시작
            var run = new RunState(3); // 왕 HP 3 런 생성
            run.CurrentRound = stage; // 현재 스테이지 설정
            run.RouteMap.Configure(stage, new StageNode(nodeId, Vector2Int.zero, stage, "stage_" + stage + "_battle"), null); // 현재 노드 설정
            return run; // 구성 런 반환
        } // 메서드 종료

        private static EnemyEncounterResult CreateEncounter(string profileId, StageType stageType, int seed, int enemyCount, int threatScore) // 측정용 편성 생성
        { // 메서드 시작
            var spawns = new List<EnemyEncounterSpawn>(); // 빈 배치 목록 생성
            for (int index = 0; index < enemyCount; index++) // 적 수만큼 항목 생성
            { // 반복 시작
                spawns.Add(null); // 수량 측정용 빈 배치 추가
            } // 반복 종료
            string nodeId = stageType == StageType.Elite ? "phase_1_stage_1_elite" : "phase_1_stage_1_battle"; // 스테이지별 노드 ID
            string encounterId = EnemyEncounterGenerator.CreateEncounterId(seed, 1, 1, stageType, profileId); // 편성 고유 ID 생성
            return new EnemyEncounterResult(seed, stageType, 1, 1, nodeId, profileId, encounterId, spawns, threatScore); // 측정 편성 반환
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
