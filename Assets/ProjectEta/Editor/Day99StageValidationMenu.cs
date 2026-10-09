#if UNITY_EDITOR // 에디터 전용 범위
using System; // 예외와 환경 변수
using System.IO; // 보고서 저장
using System.Linq; // 노드 종류 집계
using System.Text; // 보고서 문자열
using ProjectEta.Battle; // 승리 지급 검사
using ProjectEta.Meta; // 고정 해금 검사
using ProjectEta.Run; // 실제 지도·규칙
using UnityEditor; // 실행 메뉴
using UnityEngine; // JSON 복원·자원 정리

namespace ProjectEta.EditorTools // 자동 표본 검증 영역
{ // 영역 시작
    public static class Day99StageValidationMenu // 지도 규칙·저장·지급 표본
    { // 타입 시작
        [MenuItem("Project Eta/Day 99/Generate Stage Rules Report")] // 에디터 실행 메뉴
        public static void GenerateReport() // 실제 에셋의 3개 Seed·5페이즈 검사
        { // 메서드 시작
            var report = new StringBuilder(); // 보고서 작성기
            report.AppendLine("---\n# 99일차 자동 경로·규칙 표본\n"); // 문서 제목
            report.AppendLine("실제 라운드 에셋으로 3개 Seed의 5페이즈 지도 전체 노드를 검사. 전투 플레이·승률·종료 턴 실측과 구분.\n"); // 자동 표본 범위
            report.AppendLine("| Seed | 페이즈 | 전체 노드 | 전투 노드 | 정예 노드 | 저장·Gold 일치 |"); // 표 제목
            report.AppendLine("|---:|---:|---:|---:|---:|---|"); // 표 정렬
            int totalBattles = 0; // 전투 검사 수
            foreach (int seed in new[] { 99, 440, 2026 }) // 재현 Seed 순회
            { // 반복 시작
                for (int phase = 1; phase <= RunPhaseProgressService.TotalPhases; phase++) // 전체 페이즈 순회
                { // 반복 시작
                    var nodes = RunPhaseRouteGenerator.CreateFullRoute(seed, phase); // 실제 페이즈 지도 생성
                    var run = new RunState(3); // 파일 저장 없는 독립 런
                    StageNode start = nodes.First(node => node.Depth == 1); // 페이즈 시작 노드
                    run.RouteMap.Configure(1, start, nodes); // 실제 경로 연결
                    int battles = 0; // 현재 페이즈 전투 수
                    int elites = 0; // 현재 페이즈 정예 수
                    foreach (StageNode node in nodes) // 분기를 포함한 전체 노드 순회
                    { // 반복 시작
                        StageDefinition definition = StageDefinitionCatalog.Resolve(node.StageDefinitionId, node.Depth); // 실제 정의 조회
                        string preview = StagePreviewFormatter.Build(run, node); // 실제 지도 안내 생성
                        if (definition == null) // 정의 누락 확인
                        { // 조건 시작
                            throw new InvalidOperationException("노드 정의 누락: " + node.NodeId); // 검사 실패 보고
                        } // 조건 종료
                        if (!definition.RequiresBattle) // 비전투 노드 확인
                        { // 조건 시작
                            continue; // 전투 검사 제외
                        } // 조건 종료
                        StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(run, definition, phase); // 실제 저장 규칙
                        if (rules == null || !rules.IsValid || !preview.Contains(rules.turnLimit + "턴") || !preview.Contains(rules.victoryGold + " Gold")) // 안내·규칙 일치 확인
                        { // 조건 시작
                            throw new InvalidOperationException("안내 규칙 불일치: " + node.NodeId); // 재현 노드 보고
                        } // 조건 종료
                        RunState restored = RunState.FromSaveData(JsonUtility.FromJson<RunSaveData>(JsonUtility.ToJson(run.ToSaveData())), null); // 실제 저장 복원
                        StageRuleSnapshot saved = RunStageRuleService.GetOrCreate(restored, definition, phase); // 복원 규칙 조회
                        if (JsonUtility.ToJson(rules) != JsonUtility.ToJson(saved)) // 제한·배치·보스·보상 값 비교
                        { // 조건 시작
                            throw new InvalidOperationException("저장 규칙 불일치: " + node.NodeId); // 저장 회귀 보고
                        } // 조건 종료
                        var runtime = saved.CreateRuntimeRound(); // 실제 소비 라운드 생성
                        if (runtime.TurnLimit != rules.turnLimit || runtime.Reinforcements.Count != rules.reinforcements.Count) // 전투 소비 값 확인
                        { // 조건 시작
                            throw new InvalidOperationException("전투 적용 불일치: " + node.NodeId); // 전투 회귀 보고
                        } // 조건 종료
                        UnityEngine.Object.DestroyImmediate(runtime); // 시험 라운드 정리
                        restored.CurrentRound = node.Depth; // 완료 깊이 설정
                        restored.RouteMap.Configure(node.Depth, new StageNode(node.NodeId, node.Position, node.Depth, node.StageDefinitionId), nodes); // 완료 위치 연결
                        restored.RecordBattleOutcome(BattleOutcome.Victory); // 승리 지급 상태 설정
                        int before = RunEconomyService.GetOrCreate(restored).Currency; // 지급 전 Gold 조회
                        if (!RunBattleGoldRewardService.TryGrant(restored) || RunEconomyService.GetOrCreate(restored).Currency - before != rules.victoryGold || RunBattleGoldRewardService.TryGrant(restored)) // 실제 지급·중복 차단 확인
                        { // 조건 시작
                            throw new InvalidOperationException("Gold 지급 불일치: " + node.NodeId); // 지급 회귀 보고
                        } // 조건 종료
                        battles++; // 전투 검사 수 증가
                        elites += definition.StageType == StageType.Elite ? 1 : 0; // 정예 검사 수 증가
                    } // 반복 종료
                    totalBattles += battles; // 전체 검사 수 누적
                    report.AppendLine($"| {seed} | {phase} | {nodes.Count} | {battles} | {elites} | 통과 |"); // 페이즈 검사 결과
                } // 반복 종료
            } // 반복 종료
            var progress = new MetaProgressState(); // 영구 진행 없는 시험 상태
            var frozen = RunContentUnlockSnapshot.Capture("current", progress); // 현재 런 해금 고정
            int lockedBefore = RunContentEligibility.CountLocked(frozen); // 현재 런 미해금 수
            foreach (var piece in RunContentEligibility.GetCandidatePool()) // 실제 후보 원본 순회
            { // 반복 시작
                if (!string.IsNullOrWhiteSpace(piece.RequiredMetaUnlockId)) // 영구 해금 요구 확인
                { // 조건 시작
                    progress.Unlock(MetaUnlockType.Piece, piece.RequiredMetaUnlockId); // 다음 런 해금 재현
                } // 조건 종료
            } // 반복 종료
            int lockedCurrent = RunContentEligibility.CountLocked(frozen); // 기존 런 잠금 재조회
            int lockedNext = RunContentEligibility.CountLocked(RunContentUnlockSnapshot.Capture("next", progress)); // 다음 런 잠금 조회
            if (lockedBefore != lockedCurrent || lockedNext != 0) // 해금 고정·다음 런 반영 확인
            { // 조건 시작
                throw new InvalidOperationException("런 고정 해금 불일치"); // 해금 회귀 보고
            } // 조건 종료
            report.AppendLine($"\n전투 노드 {totalBattles}개에서 안내·전투 적용·저장 복원·Gold 지급·중복 지급 차단 통과."); // 표본 결과
            report.AppendLine($"해금 표본: 현재 런 미해금 {lockedCurrent}종 유지, 전체 해금 후 다음 런 미해금 {lockedNext}종."); // 해금 결과
            report.AppendLine("\n---\n## 실측 확인\n\n정예 25턴·2/4턴 증원은 임시값. 전체 런 3회 이상의 실제 승률·종료 턴·왕 HP 손실로 조정. F1 → 상태 → 측정 JSON / CSV 저장."); // 실제 플레이 과제
            string directory = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY99_REPORT"); // 배치 출력 경로
            if (string.IsNullOrWhiteSpace(directory)) // 수동 메뉴 기본 경로 확인
            { // 조건 시작
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Validation")); // 프로젝트 문서 경로
            } // 조건 종료
            Directory.CreateDirectory(directory); // 보고서 폴더 준비
            string path = Path.Combine(directory, "Day99StageRuleSampling.md"); // 보고서 파일 경로
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false)); // UTF8 문서 저장
            Debug.Log("[Day99] 자동 경로·규칙 표본 생성: " + path); // 검사 결과 경로
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
#endif // 에디터 전용 범위 종료
