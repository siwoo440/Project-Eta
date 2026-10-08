using System; // 형식과 배열 사용
using System.Collections; // 비제네릭 목록 검사
using System.Collections.Generic; // 테스트 후보와 생성 객체 목록
using System.Linq; // 분포와 기록 집계
using System.Reflection; // 새 인터페이스와 private 필드 검사
using NUnit.Framework; // NUnit 검증
using UnityEditor; // 프로젝트 데이터 에셋 로드
using UnityEngine; // ScriptableObject와 보드 좌표
using ProjectEta.Battle; // 전투 결과 사용
using ProjectEta.Board; // 실제 보드 입력과 화면 사용
using ProjectEta.Pieces; // 기물 데이터 사용
using ProjectEta.Round; // 라운드 데이터 사용
using ProjectEta.Run; // 적 편성과 측정 사용

namespace ProjectEta.Tests.EditMode // 96일차 편성 검증 영역
{ // 범위 시작
    public sealed class Day96EnemyEncounterTests // 편성 원형과 난이도 기록 검증
    { // 범위 시작
        private readonly List<UnityEngine.Object> _created = new List<UnityEngine.Object>(); // 생성 테스트 객체 정리 목록
        [TearDown] // 테스트 후 정리
        public void TearDown() // 생성 객체와 경제 상태 제거
        { // 범위 시작
            foreach (var item in _created) // 생성 객체 순회
            { // 범위 시작
                if (item != null) // 남아 있는 객체 확인
                { // 범위 시작
                    UnityEngine.Object.DestroyImmediate(item); // 테스트 객체 즉시 제거
                } // 범위 종료
            } // 범위 종료
            _created.Clear(); // 정리 목록 초기화
            RunEconomyService.ResetForTests(); // 런별 경제 캐시 초기화
        } // 범위 종료

        [Test] // 편성 원형 개수 검증
        public void Profiles_일반5종정예3종고유ID제공() // 콘텐츠 다양성 기준 확인
        { // 범위 시작
            Type catalog = RunType("EnemyEncounterProfileCatalog"); // 편성 원형 카탈로그 조회
            IList normal = Profiles(catalog, StageType.Battle); // 일반 편성 원형 조회
            IList elite = Profiles(catalog, StageType.Elite); // 정예 편성 원형 조회
            Assert.That(normal.Count, Is.EqualTo(5)); // 일반 원형 5종 확인
            Assert.That(elite.Count, Is.EqualTo(3)); // 정예 원형 3종 확인
            var ids = normal.Cast<object>().Concat(elite.Cast<object>()).Select(x => Property<string>(x, "ProfileId")).ToArray(); // 전체 원형 ID 수집
            Assert.That(ids.Distinct().Count(), Is.EqualTo(8)); // 중복 없는 8종 ID 확인
            Assert.That(normal.Cast<object>().All(x => ((Array)Property<object>(x, "PreferredRoles")).Length >= 4), Is.True); // 일반 역할 순서 확인
            Assert.That(elite.Cast<object>().All(x => ((Array)Property<object>(x, "PreferredRoles")).Length >= 5), Is.True); // 정예 역할 순서 확인
        } // 범위 종료

        [Test] // Seed 결정성 검증
        public void Generate_동일조건에서원형과배치동일() // 저장 복원 재현성 확인
        { // 범위 시작
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            RoundDefinition round = CreateRound(pool.Take(4).ToArray()); // 기본 4기 라운드 생성
            EnemyEncounterResult first = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 9812, 3, 4, "phase_3_node_a"); // 첫 편성 생성
            EnemyEncounterResult second = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, 9812, 3, 4, "phase_3_node_a"); // 동일 편성 재생성
            Assert.That(ResultString(first, "ProfileId"), Is.EqualTo(ResultString(second, "ProfileId"))); // 원형 ID 동일 확인
            Assert.That(ResultString(first, "EncounterId"), Is.EqualTo(ResultString(second, "EncounterId"))); // 편성 ID 동일 확인
            Assert.That(first.Spawns.Select(x => x.Piece.PieceId), Is.EqualTo(second.Spawns.Select(x => x.Piece.PieceId))); // 기물 순서 동일 확인
            Assert.That(first.Spawns.Select(x => x.Position), Is.EqualTo(second.Spawns.Select(x => x.Position))); // 배치 좌표 동일 확인
        } // 범위 종료

        [Test] // 원형 분포 검증
        public void Generate_Seed분포에서일반5종정예3종모두선택() // 변형 편성 도달 확인
        { // 범위 시작
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            RoundDefinition round = CreateRound(pool.Take(4).ToArray()); // 기본 4기 라운드 생성
            var normal = new HashSet<string>(); // 일반 원형 ID 집합
            var elite = new HashSet<string>(); // 정예 원형 ID 집합
            for (int seed = 0; seed < 200; seed++) // 충분한 Seed 순회
            { // 범위 시작
                normal.Add(ResultString(EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, seed, 3, 4, "node_" + seed), "ProfileId")); // 일반 원형 수집
                elite.Add(ResultString(EnemyEncounterGenerator.Generate(pool, round, StageType.Elite, seed, 3, 4, "node_" + seed), "ProfileId")); // 정예 원형 수집
            } // 범위 종료
            Assert.That(normal.Count, Is.EqualTo(5)); // 일반 원형 전부 도달 확인
            Assert.That(elite.Count, Is.EqualTo(3)); // 정예 원형 전부 도달 확인
        } // 범위 종료

        [Test] // 적 후보 제한 검증
        public void Generate_3성이상특수기물과왕합성보스제외() // 일반 전투 우회 차단 확인
        { // 범위 시작
            PieceDefinition valid = CreatePiece("valid", 2, 2, PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Knight, PieceRoleTag.Jumper); // 정상 2성 적 생성
            PieceDefinition high = CreatePiece("high", 8, 8, PieceCategory.Special, PieceGrade.FiveStar, PieceMovementType.Custom, PieceRoleTag.Attacker); // 금지 5성 특수 생성
            PieceDefinition king = CreatePiece("king", 8, 8, PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.King, PieceRoleTag.None); // 금지 왕 생성
            PieceDefinition fusion = CreatePiece("fusion", 8, 8, PieceCategory.Fusion, PieceGrade.TwoStar, PieceMovementType.Rook, PieceRoleTag.Ranged); // 금지 합성 생성
            PieceDefinition boss = CreatePiece("boss", 8, 8, PieceCategory.Boss, PieceGrade.OneStar, PieceMovementType.Rook, PieceRoleTag.Ranged); // 금지 보스 생성
            RoundDefinition round = CreateRound(valid, valid, valid, valid); // 정상 라운드 생성
            EnemyEncounterResult result = EnemyEncounterGenerator.Generate(new[] { valid, high, king, fusion, boss }, round, StageType.Battle, 4, 5, 9, "node"); // 혼합 후보 편성 생성
            Assert.That(result.Spawns, Is.Not.Empty); // 정상 편성 생성 확인
            Assert.That(result.Spawns.All(x => x.Piece == valid), Is.True); // 정상 2성 후보만 사용 확인
        } // 범위 종료

        [Test] // 안전 배치 검증
        public void Generate_모든일반정예적은후방3행과고유칸사용() // 즉시 공격 위험과 겹침 차단 확인
        { // 범위 시작
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            RoundDefinition unsafeRound = CreateRoundAtRow(5, pool.Take(4).ToArray()); // 중앙에 가까운 기존 좌표 생성
            for (int seed = 0; seed < 100; seed++) // 여러 원형과 좌표 순회
            { // 범위 시작
                foreach (StageType type in new[] { StageType.Battle, StageType.Elite }) // 일반과 정예 순회
                { // 범위 시작
                    EnemyEncounterResult result = EnemyEncounterGenerator.Generate(pool, unsafeRound, type, seed, 4, 7, "node_" + seed); // 편성 생성
                    Assert.That(result.Spawns.All(x => x.Position.y >= 7 && x.Position.y < 10), Is.True); // 후방 3행 배치 확인
                    Assert.That(result.Spawns.Select(x => x.Position).Distinct().Count(), Is.EqualTo(result.Spawns.Count)); // 겹치지 않는 칸 확인
                } // 범위 종료
            } // 범위 종료
        } // 범위 종료

        [Test] // 정예 강도 검증
        public void Generate_정예는일반보다적수와평균위협도높음() // 정예 구분 기준 확인
        { // 범위 시작
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            RoundDefinition round = CreateRound(pool.Take(4).ToArray()); // 기본 라운드 생성
            long normalThreat = 0; // 일반 위협도 합계
            long eliteThreat = 0; // 정예 위협도 합계
            int normalCount = 0; // 일반 적 수 합계
            int eliteCount = 0; // 정예 적 수 합계
            for (int seed = 0; seed < 200; seed++) // 비교 Seed 순회
            { // 범위 시작
                EnemyEncounterResult normal = EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, seed, 4, 7, "node_" + seed); // 일반 편성 생성
                EnemyEncounterResult elite = EnemyEncounterGenerator.Generate(pool, round, StageType.Elite, seed, 4, 7, "node_" + seed); // 정예 편성 생성
                normalThreat += normal.ThreatScore; // 일반 위협도 누적
                eliteThreat += elite.ThreatScore; // 정예 위협도 누적
                normalCount += normal.Spawns.Count; // 일반 적 수 누적
                eliteCount += elite.Spawns.Count; // 정예 적 수 누적
            } // 범위 종료
            Assert.That(eliteCount, Is.GreaterThan(normalCount)); // 정예 적 수 증가 확인
            Assert.That(eliteThreat, Is.GreaterThan(normalThreat)); // 정예 위협도 증가 확인
        } // 범위 종료

        [Test] // 페이즈 상승 검증
        public void Generate_후반평균위협도가초반보다높음() // 진행도별 난이도 증가 확인
        { // 범위 시작
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            RoundDefinition round = CreateRound(pool.Take(4).ToArray()); // 기본 라운드 생성
            long early = 0; // 초반 위협도 합계
            long late = 0; // 후반 위협도 합계
            for (int seed = 0; seed < 300; seed++) // 충분한 Seed 순회
            { // 범위 시작
                early += EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, seed, 1, 2, "early_" + seed).ThreatScore; // 1페이즈 위협도 누적
                late += EnemyEncounterGenerator.Generate(pool, round, StageType.Battle, seed, 5, 9, "late_" + seed).ThreatScore; // 5페이즈 위협도 누적
            } // 범위 종료
            Assert.That(late, Is.GreaterThan(early)); // 후반 평균 위협도 증가 확인
        } // 범위 종료

        [Test] // 편성 기록 검증
        public void Telemetry_편성과승패턴왕체력저장복원() // 실제 난이도 측정 항목 확인
        { // 범위 시작
            RunState run = new RunState(3); // 새 런 생성
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(pool, CreateRound(pool.Take(4).ToArray()), StageType.Battle, 7, 2, 3, "phase_2_stage_3_battle"); // 기록할 편성 생성
            InvokeTelemetry("RecordEncounter", run, encounter); // 전투 시작 편성 기록
            run.KingHp = 2; // 전투 종료 왕 체력 적용
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Victory, 6); // 실제 승리 결과 기록
            RunSaveData save = run.ToSaveData(); // 측정 포함 저장 생성
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 복원용 기물 데이터베이스
            RunState copy = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(save)), database); // JSON 저장 복원
            object start = copy.BalanceData.entries.First(x => x.kind == "Encounter"); // 편성 시작 기록 조회
            object result = copy.BalanceData.entries.First(x => x.kind == "BattleResult"); // 전투 결과 기록 조회
            Assert.That(Field<string>(start, "encounterId"), Is.EqualTo(ResultString(encounter, "EncounterId"))); // 편성 ID 저장 확인
            Assert.That(Field<int>(start, "enemyCount"), Is.EqualTo(encounter.Spawns.Count)); // 시작 적 수 저장 확인
            Assert.That(Field<int>(start, "threatScore"), Is.EqualTo(encounter.ThreatScore)); // 시작 위협도 저장 확인
            Assert.That(Field<int>(start, "kingHp"), Is.EqualTo(3)); // 전투 시작 왕 체력 확인
            Assert.That(Field<int>(result, "turn"), Is.EqualTo(6)); // 종료 턴 저장 확인
            Assert.That(Field<int>(result, "kingHp"), Is.EqualTo(2)); // 종료 왕 체력 저장 확인
            Assert.That(Field<int>(result, "outcome"), Is.EqualTo((int)BattleOutcome.Victory)); // 승리 결과 저장 확인
        } // 범위 종료

        [Test] // 결과 중복 검증
        public void Telemetry_같은전투결과중복기록차단() // 종료 경로 중복 호출 방어
        { // 범위 시작
            RunState run = new RunState(3); // 새 런 생성
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Defeat, 4); // 첫 결과 기록
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Defeat, 4); // 같은 결과 재기록 시도
            Assert.That(run.BalanceData.entries.Count(x => x.kind == "BattleResult"), Is.EqualTo(1)); // 결과 한 건만 저장 확인
        } // 범위 종료

        [Test] // 집계와 상한 검증
        public void Report_상한이후에도전투승패와턴누적() // 전체 런 난이도 총계 유지 확인
        { // 범위 시작
            RunState run = new RunState(3); // 새 런 생성
            for (int index = 0; index < RunBalanceTelemetry.MaximumEntries; index++) // 상세 기록 상한 채우기
            { // 범위 시작
                RunBalanceTelemetry.RecordGold(run, 100, 100, -999, false, "Test"); // 실패 지출 기록 추가
            } // 범위 종료
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Victory, 7); // 상한 이후 승리 기록
            RunBalanceSummary summary = RunBalanceReport.Calculate(run); // 누적 보고서 계산
            Assert.That(Property<int>(summary, "BattleCount"), Is.EqualTo(1)); // 전체 전투 수 보존 확인
            Assert.That(Property<int>(summary, "VictoryCount"), Is.EqualTo(1)); // 승리 수 보존 확인
            Assert.That(Property<int>(summary, "DefeatCount"), Is.Zero); // 패배 수 확인
            Assert.That(Property<int>(summary, "TotalBattleTurns"), Is.EqualTo(7)); // 전체 턴 합계 확인
            Assert.That(run.BalanceData.droppedEntries, Is.EqualTo(1)); // 상세 생략 수 확인
        } // 범위 종료

        [Test] // 외부 종료 경로 검증
        public void BattleController_AI직접패배도런실패와측정완료() // 일반 AI·보스 우회 종료 방지
        { // 범위 시작
            RunState run = new RunState(3); // 전투 중 새 런 생성
            TurnManager turnManager = new TurnManager(); // 독립 턴 상태 생성
            turnManager.EndBattle(BattleOutcome.Defeat); // AI 실행기와 같은 직접 종료 재현
            MethodInfo method = typeof(BattleController).GetMethod("FinalizeCompletedBattle", BindingFlags.Public | BindingFlags.Static); // 공통 완료 함수 검색
            Assert.That(method, Is.Not.Null, "AI 직접 종료 공통 처리 누락"); // 새 완료 함수 존재 확인
            bool completed = (bool)method.Invoke(null, new object[] { run, turnManager }); // 직접 종료 후 런 결과 처리
            Assert.That(completed, Is.True); // 공통 완료 성공 확인
            Assert.That(run.CurrentFlowPhase, Is.EqualTo(RunFlowPhase.Failed)); // 런 실패 흐름 확인
            Assert.That(run.KingHp, Is.Zero); // 패배 왕 체력 정규화 확인
            Assert.That(run.BalanceData.entries.Count(x => x.kind == "BattleResult"), Is.EqualTo(1)); // 패배 측정 한 건 확인
        } // 범위 종료

        [Test] // F1 요약 검증
        public void Report_최근편성원형과승패표시() // 실제 플레이 확인 정보 노출
        { // 범위 시작
            RunState run = new RunState(3); // 새 런 생성
            run.CurrentRound = 3; // 측정 깊이 설정
            run.RouteMap.Configure(3, new StageNode("phase_2_stage_3_battle", Vector2Int.zero, 3, "stage_3_battle"), null); // 측정 노드 설정
            List<PieceDefinition> pool = CreateRolePool(); // 역할별 후보 생성
            EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(pool, CreateRound(pool.Take(4).ToArray()), StageType.Battle, 7, 2, 3, "phase_2_stage_3_battle"); // 표시할 편성 생성
            InvokeTelemetry("RecordEncounter", run, encounter); // 편성 시작 기록
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Victory, 6); // 승리 결과 기록
            string summary = RunBalanceReport.BuildSummary(run); // F1 표시 문자열 생성
            Assert.That(summary, Does.Contain(encounter.ProfileId)); // 최근 원형 ID 표시 확인
            Assert.That(summary, Does.Contain("Victory")); // 최근 승패 표시 확인
            Assert.That(summary, Does.Contain("위협도")); // 위협도 표시 확인
        } // 범위 종료

        [Test] // 다음 전투 시작 표시 검증
        public void Report_이전결과뒤새편성은새편성을표시() // 완료 결과보다 새로운 편성 우선 표시
        { // 범위 시작
            RunState run = new RunState(3); // 새 런 생성
            run.RouteMap.Configure(1, new StageNode("battle_a", Vector2Int.zero, 1, "stage_1_battle"), null); // 첫 전투 노드 설정
            var first = new EnemyEncounterResult(1, StageType.Battle, 1, 1, "battle_a", "normal_frontline", "encounter_a", new List<EnemyEncounterSpawn>(), 10); // 첫 편성 결과 생성
            InvokeTelemetry("RecordEncounter", run, first); // 첫 편성 기록
            InvokeTelemetry("RecordBattleResult", run, BattleOutcome.Victory, 4); // 첫 전투 결과 기록
            run.CurrentRound = 2; // 다음 전투 깊이 설정
            run.RouteMap.Configure(1, new StageNode("battle_b", Vector2Int.zero, 2, "stage_2_battle"), null); // 다음 전투 노드 설정
            var second = new EnemyEncounterResult(2, StageType.Battle, 1, 2, "battle_b", "normal_ranged", "encounter_b", new List<EnemyEncounterSpawn>(), 20); // 다음 편성 결과 생성
            InvokeTelemetry("RecordEncounter", run, second); // 다음 편성 기록
            string summary = RunBalanceReport.BuildSummary(run); // F1 표시 문자열 생성
            Assert.That(summary, Does.Contain("normal_ranged")); // 최신 편성 표시 확인
            Assert.That(summary, Does.Not.Contain("최근 편성: normal_frontline")); // 이전 완료 편성 미표시 확인
        } // 범위 종료

        [Test] // 런타임 후보 원본 검증
        public void RuntimeContentPool_전체기물DB허용후보와같음() // 표본과 실제 전투 후보 통일
        { // 범위 시작
            RunContentCatalog content = Resources.Load<RunContentCatalog>("RunContent"); // 실제 런 콘텐츠 로드
            Assert.That(content, Is.Not.Null); // 런 콘텐츠 존재 확인
            Assert.That(content.PieceDatabase, Is.Not.Null); // 전체 기물 DB 연결 확인
            MethodInfo method = typeof(EnemyEncounterRules).GetMethod("BuildPool", BindingFlags.Public | BindingFlags.Static); // 공용 후보 생성기 검색
            Assert.That(method, Is.Not.Null, "런타임·표본 공용 후보 생성기 누락"); // 공용 생성기 존재 확인
            IList pool = (IList)method.Invoke(null, new object[] { content.PieceDatabase.Definitions }); // 실제 런 콘텐츠 후보 생성
            int expected = content.PieceDatabase.Definitions.Count(EnemyEncounterRules.CanUsePiece); // DB 기준 허용 후보 수 계산
            Assert.That(pool.Count, Is.EqualTo(expected)); // 실제 전투와 표본 후보 수 일치 확인
            Assert.That(pool.Count, Is.GreaterThan(16)); // 시작 덱 일부 후보만 사용하는 회귀 차단
        } // 범위 종료

        [Test] // 부분 배치 롤백 검증
        public void BoardInput_생성적롤백은보드와화면등록제거() // 실패 편성 참조 누수 방지
        { // 범위 시작
            var root = new GameObject("Day96RollbackBoard"); // 보드 테스트 루트 생성
            _created.Add(root); // 테스트 종료 정리 등록
            BoardView boardView = root.AddComponent<BoardView>(); // 실제 보드 화면 생성
            BoardInputController input = root.AddComponent<BoardInputController>(); // 실제 보드 입력 생성
            RunState run = new RunState(3); // 독립 런 생성
            boardView.Bind(run.Board); // 보드 상태와 화면 연결
            input.Bind(run, boardView, new TurnManager(), new BattleHooks()); // 입력에 전투 상태 연결
            PieceRuntimeState enemy = input.SpawnTestEnemy(CreatePiece("rollback_enemy", 2, 2, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn, PieceRoleTag.Melee), new Vector2Int(4, 8)); // 실제 적 생성
            Assert.That(enemy, Is.Not.Null); // 적 생성 성공 확인
            MethodInfo method = typeof(BoardInputController).GetMethod("RollbackSpawnedEnemy", BindingFlags.Public | BindingFlags.Instance); // 롤백 진입점 검색
            Assert.That(method, Is.Not.Null, "생성 적 화면 등록 롤백 누락"); // 롤백 함수 존재 확인
            bool removed = (bool)method.Invoke(input, new object[] { enemy }); // 실제 롤백 실행
            Assert.That(removed, Is.True); // 롤백 성공 확인
            Assert.That(run.Board.GetTile(new Vector2Int(4, 8)).OccupyingPiece, Is.Null); // 보드 점유 제거 확인
            FieldInfo viewsField = typeof(BoardInputController).GetField("_pieceViews", BindingFlags.NonPublic | BindingFlags.Instance); // 화면 등록 사전 검색
            IDictionary views = (IDictionary)viewsField.GetValue(input); // 화면 등록 사전 조회
            Assert.That(views.Contains(enemy), Is.False); // 화면 등록 참조 제거 확인
        } // 범위 종료

        private static Type RunType(string name) // 런타임 형식 조회
        { // 범위 시작
            Type type = typeof(RunState).Assembly.GetType("ProjectEta.Run." + name); // 런 어셈블리에서 형식 검색
            Assert.That(type, Is.Not.Null, name + " 누락"); // 새 형식 존재 확인
            return type; // 조회 형식 반환
        } // 범위 종료
        private static IList Profiles(Type catalog, StageType stageType) // 타입별 원형 목록 조회
        { // 범위 시작
            MethodInfo method = catalog.GetMethod("GetProfiles", BindingFlags.Public | BindingFlags.Static); // 공개 조회 메서드 검색
            Assert.That(method, Is.Not.Null); // 조회 메서드 존재 확인
            return (IList)method.Invoke(null, new object[] { stageType }); // 원형 목록 반환
        } // 범위 종료
        private static string ResultString(EnemyEncounterResult result, string name) // 결과 문자열 속성 조회
        { // 범위 시작
            PropertyInfo property = typeof(EnemyEncounterResult).GetProperty(name); // 새 결과 속성 검색
            Assert.That(property, Is.Not.Null, name + " 누락"); // 결과 속성 존재 확인
            return (string)property.GetValue(result); // 문자열 값 반환
        } // 범위 종료
        private static T Property<T>(object value, string name) // 공개 속성 값 조회
        { // 범위 시작
            PropertyInfo property = value.GetType().GetProperty(name); // 이름 기반 속성 검색
            Assert.That(property, Is.Not.Null, name + " 누락"); // 속성 존재 확인
            return (T)property.GetValue(value); // 형식화한 속성 값 반환
        } // 범위 종료
        private static T Field<T>(object value, string name) // 공개 필드 값 조회
        { // 범위 시작
            FieldInfo field = value.GetType().GetField(name); // 이름 기반 필드 검색
            Assert.That(field, Is.Not.Null, name + " 누락"); // 필드 존재 확인
            return (T)field.GetValue(value); // 형식화한 필드 값 반환
        } // 범위 종료
        private static void InvokeTelemetry(string name, params object[] arguments) // 측정 메서드 호출
        { // 범위 시작
            MethodInfo method = typeof(RunBalanceTelemetry).GetMethod(name, BindingFlags.Public | BindingFlags.Static); // 공개 측정 메서드 검색
            Assert.That(method, Is.Not.Null, name + " 누락"); // 측정 메서드 존재 확인
            method.Invoke(null, arguments); // 실제 측정 호출
        } // 범위 종료
        private List<PieceDefinition> CreateRolePool() // 역할과 강도가 다양한 후보 생성
        { // 범위 시작
            return new List<PieceDefinition> // 테스트 후보 목록 반환
            { // 범위 시작
                CreatePiece("pawn_a", 1, 1, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn, PieceRoleTag.Melee), // 약한 근접 후보
                CreatePiece("pawn_b", 2, 1, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn, PieceRoleTag.Melee), // 근접 후보
                CreatePiece("jumper_a", 2, 2, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Knight, PieceRoleTag.Jumper), // 도약 후보
                CreatePiece("slider_a", 2, 2, PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Bishop, PieceRoleTag.Slider), // 슬라이더 후보
                CreatePiece("ranged_a", 2, 2, PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.Custom, PieceRoleTag.Ranged), // 원거리 후보
                CreatePiece("tank_a", 3, 1, PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.Custom, PieceRoleTag.Tanker), // 방어 후보
                CreatePiece("support_a", 2, 1, PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.Custom, PieceRoleTag.Support), // 지원 후보
                CreatePiece("attacker_a", 2, 3, PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.Custom, PieceRoleTag.Attacker), // 공격 후보
                CreatePiece("jumper_b", 3, 3, PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Custom, PieceRoleTag.Jumper | PieceRoleTag.Attacker), // 강한 도약 후보
                CreatePiece("ranged_b", 3, 3, PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Custom, PieceRoleTag.Ranged | PieceRoleTag.Attacker), // 강한 원거리 후보
                CreatePiece("tank_b", 4, 2, PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Custom, PieceRoleTag.Tanker | PieceRoleTag.Support), // 강한 방어 후보
                CreatePiece("assault_b", 3, 4, PieceCategory.Special, PieceGrade.TwoStar, PieceMovementType.Custom, PieceRoleTag.Attacker) // 강한 공격 후보
            }; // 범위 종료
        } // 범위 종료
        private PieceDefinition CreatePiece(string id, int hp, int atk, PieceCategory category, PieceGrade grade, PieceMovementType movement, PieceRoleTag roles) // 테스트 기물 생성
        { // 범위 시작
            PieceDefinition piece = ScriptableObject.CreateInstance<PieceDefinition>(); // 임시 기물 에셋 생성
            _created.Add(piece); // 정리 목록에 등록
            SetPrivateField(piece, "_pieceId", id); // 기물 ID 설정
            SetPrivateField(piece, "_displayName", id); // 표시 이름 설정
            SetPrivateField(piece, "_baseHp", hp); // HP 설정
            SetPrivateField(piece, "_baseAtk", atk); // 공격력 설정
            SetPrivateField(piece, "_category", category); // 카테고리 설정
            SetPrivateField(piece, "_grade", grade); // 등급 설정
            SetPrivateField(piece, "_movementType", movement); // 이동 유형 설정
            SetPrivateField(piece, "_roleTags", roles); // 역할 태그 설정
            return piece; // 완성 기물 반환
        } // 범위 종료
        private RoundDefinition CreateRound(params PieceDefinition[] enemies) // 후방 배치 라운드 생성
        { // 범위 시작
            return CreateRoundAtRow(8, enemies); // 기존 후방 행 사용
        } // 범위 종료
        private RoundDefinition CreateRoundAtRow(int row, params PieceDefinition[] enemies) // 지정 행 라운드 생성
        { // 범위 시작
            RoundDefinition round = ScriptableObject.CreateInstance<RoundDefinition>(); // 임시 라운드 생성
            _created.Add(round); // 정리 목록 등록
            var spawns = new List<EnemySpawnDefinition>(); // 초기 적 배치 목록
            for (int index = 0; index < enemies.Length; index++) // 전달 적 순회
            { // 범위 시작
                spawns.Add(new EnemySpawnDefinition(enemies[index].PieceId, new Vector2Int(1 + index * 2, row + index % Math.Max(1, 10 - row)), 0)); // 서로 다른 적 진영 좌표 추가
            } // 범위 종료
            SetPrivateField(round, "_initialEnemies", spawns); // 초기 적 목록 주입
            SetPrivateField(round, "_reinforcements", new List<EnemySpawnDefinition>()); // 증원 없음 설정
            return round; // 완성 라운드 반환
        } // 범위 종료
        private static void SetPrivateField(object target, string name, object value) // 상속 포함 private 필드 설정
        { // 범위 시작
            Type type = target.GetType(); // 현재 형식부터 검색
            while (type != null) // 상속 계층 순회
            { // 범위 시작
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic); // 현재 형식 필드 검색
                if (field != null) // 필드 발견 확인
                { // 범위 시작
                    field.SetValue(target, value); // 테스트 값 주입
                    return; // 설정 완료
                } // 범위 종료
                type = type.BaseType; // 부모 형식으로 이동
            } // 범위 종료
            Assert.Fail("필드를 찾지 못했습니다: " + name); // 잘못된 테스트 구성 보고
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
