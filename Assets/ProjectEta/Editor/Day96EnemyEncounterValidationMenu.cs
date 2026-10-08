#if UNITY_EDITOR // 에디터 전용 도구 시작
using System; // 예외와 환경 변수 사용
using System.Collections.Generic; // 원형별 횟수 집계
using System.IO; // 보고서 파일 저장
using System.Linq; // 기물 후보 필터와 씬 목록 변환
using System.Text; // Markdown 보고서 조합
using UnityEditor; // 에디터 메뉴와 데이터 로드
using UnityEditor.Build.Reporting; // Windows 빌드 결과
using UnityEngine; // ScriptableObject와 로그
using ProjectEta.Pieces; // 기물 데이터베이스
using ProjectEta.Round; // 기본 라운드 데이터
using ProjectEta.Run; // 적 편성 생성과 검증

namespace ProjectEta.EditorTools // 96일차 편성 검증 도구 영역
{ // 범위 시작
    public static class Day96EnemyEncounterValidationMenu // 편성 표본 보고서와 개발 빌드
    { // 범위 시작
        private const int SamplesPerPhase = 500; // 페이즈·전투 종류별 고정 Seed 표본 수
        [MenuItem("Project Eta/Day 96/Generate Encounter Report")] // 수동 보고서 생성 메뉴
        public static void GenerateReport() // 실제 데이터 기반 편성 분포 검증
        { // 범위 시작
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 81종 기물 데이터 로드
            RoundDefinition round = AssetDatabase.LoadAssetAtPath<RoundDefinition>("Assets/ProjectEta/Resources/PrototypeRound36.asset"); // 일반 전투 라운드 로드
            if (database == null || round == null) // 필수 데이터 확인
            { // 범위 시작
                throw new InvalidOperationException("PieceDatabase 또는 PrototypeRound36 누락"); // 데이터 누락 보고
            } // 범위 종료
            List<PieceDefinition> pool = EnemyEncounterRules.BuildPool(database.Definitions); // 런타임과 같은 공용 적 후보 생성
            if (database.Definitions.Count == 0 || pool.Count == 0) // 실제 콘텐츠 후보 존재 확인
            { // 범위 시작
                throw new InvalidOperationException("기물 데이터 또는 일반 적 후보 누락"); // 빈 콘텐츠 데이터 보고
            } // 범위 종료
            if (pool.Any(piece => piece.Category != PieceCategory.Monster && piece.Grade > PieceGrade.TwoStar)) // 높은 플레이어 기물 포함 확인
            { // 범위 시작
                throw new InvalidOperationException("일반 적 후보에 3~5성 플레이어 기물 포함"); // 후보 정책 위반 보고
            } // 범위 종료
            var report = new StringBuilder(); // Markdown 보고서 작성기
            report.AppendLine("---"); // 문서 제목 구분선
            report.AppendLine("# 96일차 일반·정예 적 편성 표본"); // 문서 제목
            report.AppendLine(); // 제목 아래 빈 줄
            report.AppendLine("실제 기물 " + database.Definitions.Count + "종 중 일반·정예 후보 " + pool.Count + "종. 각 페이즈와 전투 종류마다 Seed 500개를 생성했다."); // 표본 조건 설명
            report.AppendLine("표본 위협도와 적 수는 자동 생성 결과이며 실제 승률·왕 HP 손실·플레이 시간 결과가 아니다."); // 실측과 시뮬레이션 구분
            report.AppendLine(); // 단락 구분
            report.AppendLine("| 페이즈 | 종류 | 표본 | 평균 적 수 | 최소~최대 적 수 | 평균 위협도 | 최소~최대 위협도 | 원형 사용 횟수 |"); // 표 머리글
            report.AppendLine("|---:|---|---:|---:|---:|---:|---:|---|"); // 표 정렬 행
            var phaseThreats = new Dictionary<string, double>(); // 전후반 비교용 평균 위협도
            foreach (int phase in Enumerable.Range(1, 5)) // 1~5페이즈 순회
            { // 범위 시작
                foreach (StageType stageType in new[] { StageType.Battle, StageType.Elite }) // 일반과 정예 순회
                { // 범위 시작
                    EncounterSampleSummary summary = Sample(pool, round, stageType, phase); // 현재 구간 표본 집계
                    phaseThreats[phase + ":" + stageType] = summary.AverageThreat; // 평균 위협도 보존
                    string profiles = string.Join(", ", summary.ProfileCounts.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value)); // 원형별 횟수 문자열
                    report.AppendLine("| " + phase + " | " + stageType + " | " + summary.Samples + " | " + summary.AverageEnemyCount.ToString("0.00") + " | " + summary.MinimumEnemyCount + "~" + summary.MaximumEnemyCount + " | " + summary.AverageThreat.ToString("0.00") + " | " + summary.MinimumThreat + "~" + summary.MaximumThreat + " | " + profiles + " |"); // 표본 결과 행
                } // 범위 종료
            } // 범위 종료
            if (phaseThreats["5:Battle"] <= phaseThreats["1:Battle"] || phaseThreats["5:Elite"] <= phaseThreats["1:Elite"]) // 후반 강도 증가 확인
            { // 범위 시작
                throw new InvalidOperationException("5페이즈 평균 위협도가 1페이즈보다 높지 않음"); // 진행도 난이도 회귀 보고
            } // 범위 종료
            report.AppendLine(); // 표 아래 단락 구분
            report.AppendLine("일반 5종·정예 3종이 모든 페이즈에서 사용되며, 모든 적은 후방 3행의 서로 다른 칸에 배치됐다."); // 자동 검증 결과
            report.AppendLine("실제 승률·종료 턴·왕 HP는 F1 상태 페이지의 JSON/CSV 기록으로 3런 이상 수집한 뒤 조정한다."); // 다음 실측 기준
            string directory = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY96_REPORT"); // 배치 보고서 폴더 조회
            if (string.IsNullOrWhiteSpace(directory)) // 수동 메뉴 실행 확인
            { // 범위 시작
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Validation")); // 프로젝트 검증 문서 폴더 사용
            } // 범위 종료
            Directory.CreateDirectory(directory); // 보고서 폴더 생성
            string path = Path.Combine(directory, "Day96EnemyEncounterSampling.md"); // 보고서 파일 경로
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false)); // UTF8 Markdown 저장
            Debug.Log("[Day96] 적 편성 표본 보고서 생성: " + path); // 저장 경로 안내
        } // 범위 종료
        private static EncounterSampleSummary Sample(IReadOnlyList<PieceDefinition> pool, RoundDefinition round, StageType stageType, int phase) // 한 구간 편성 표본 집계
        { // 범위 시작
            var summary = new EncounterSampleSummary(); // 빈 집계 생성
            for (int sample = 0; sample < SamplesPerPhase; sample++) // 고정 Seed 표본 순회
            { // 범위 시작
                int stage = sample % RoundState.FinalRound + 1; // 깊이 1~10 순환
                string nodeId = "phase_" + phase + "_sample_" + sample + "_" + stageType.ToString().ToLowerInvariant(); // 실제 노드 형식 표본 ID
                EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(pool, round, stageType, sample, phase, stage, nodeId); // 실제 생성기로 편성 생성
                IReadOnlyList<EnemyEncounterContentIssue> issues = EnemyEncounterContentValidator.Validate(encounter); // 생성 결과 무결성 검사
                if (issues.Count > 0) // 배치 문제 확인
                { // 범위 시작
                    throw new InvalidOperationException("편성 검증 실패: Phase=" + phase + " / Type=" + stageType + " / Seed=" + sample + " / Issues=" + issues.Count); // 재현 정보 포함 오류
                } // 범위 종료
                if (encounter.Spawns.Any(spawn => spawn.Position.y < EnemyEncounterRules.EnemyFallbackStartRow)) // 안전 행 위반 확인
                { // 범위 시작
                    throw new InvalidOperationException("적 후방 배치 규칙 위반: " + encounter.EncounterId); // 위험 배치 보고
                } // 범위 종료
                summary.Add(encounter); // 정상 표본 집계
            } // 범위 종료
            int expectedProfiles = stageType == StageType.Battle ? 5 : 3; // 종류별 요구 원형 수
            if (summary.ProfileCounts.Count != expectedProfiles) // 모든 원형 도달 확인
            { // 범위 시작
                throw new InvalidOperationException(stageType + " 원형 도달 수 불일치: " + summary.ProfileCounts.Count + "/" + expectedProfiles); // 누락 원형 보고
            } // 범위 종료
            return summary; // 완성 집계 반환
        } // 범위 종료
        public static void BuildWindowsDevelopment() // 보고서 검증 포함 Windows 개발 빌드
        { // 범위 시작
            GenerateReport(); // 빌드 전 실제 콘텐츠 검증
            string path = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY96_BUILD"); // 빌드 출력 경로 조회
            if (string.IsNullOrWhiteSpace(path)) // 경로 누락 확인
            { // 범위 시작
                throw new InvalidOperationException("PROJECT_ETA_DAY96_BUILD 경로 누락"); // 명확한 실행 오류
            } // 범위 종료
            var options = new BuildPlayerOptions // 현재 출시 씬 기반 개발 빌드 옵션
            { // 범위 시작
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(), // 활성 출시 씬 목록
                locationPathName = path, // Windows 실행 파일 경로
                target = BuildTarget.StandaloneWindows64, // Windows 64비트 대상
                options = BuildOptions.Development // 개발 로그 포함 빌드
            }; // 범위 종료
            BuildReport result = BuildPipeline.BuildPlayer(options); // 실제 Windows 플레이어 빌드
            if (result.summary.result != BuildResult.Succeeded) // 빌드 결과 확인
            { // 범위 시작
                throw new InvalidOperationException("96일차 Windows 개발 빌드 실패: " + result.summary.result); // 실패 결과 보고
            } // 범위 종료
            Debug.Log("[Day96] Windows 개발 빌드 성공: " + path); // 빌드 성공 경로 안내
        } // 범위 종료
        private sealed class EncounterSampleSummary // 한 페이즈·종류의 표본 통계
        { // 범위 시작
            public int Samples; // 표본 수
            public long EnemyCountTotal; // 적 수 합계
            public int MinimumEnemyCount = int.MaxValue; // 최소 적 수
            public int MaximumEnemyCount; // 최대 적 수
            public long ThreatTotal; // 위협도 합계
            public int MinimumThreat = int.MaxValue; // 최소 위협도
            public int MaximumThreat; // 최대 위협도
            public readonly Dictionary<string, int> ProfileCounts = new Dictionary<string, int>(); // 원형별 사용 횟수
            public double AverageEnemyCount => Samples > 0 ? EnemyCountTotal / (double)Samples : 0d; // 평균 적 수
            public double AverageThreat => Samples > 0 ? ThreatTotal / (double)Samples : 0d; // 평균 위협도
            public void Add(EnemyEncounterResult encounter) // 정상 표본 하나 집계
            { // 범위 시작
                Samples++; // 표본 수 증가
                EnemyCountTotal += encounter.Spawns.Count; // 적 수 합산
                MinimumEnemyCount = Math.Min(MinimumEnemyCount, encounter.Spawns.Count); // 최소 적 수 갱신
                MaximumEnemyCount = Math.Max(MaximumEnemyCount, encounter.Spawns.Count); // 최대 적 수 갱신
                ThreatTotal += encounter.ThreatScore; // 위협도 합산
                MinimumThreat = Math.Min(MinimumThreat, encounter.ThreatScore); // 최소 위협도 갱신
                MaximumThreat = Math.Max(MaximumThreat, encounter.ThreatScore); // 최대 위협도 갱신
                ProfileCounts[encounter.ProfileId] = ProfileCounts.TryGetValue(encounter.ProfileId, out int count) ? count + 1 : 1; // 원형 사용 횟수 증가
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
#endif // 에디터 전용 도구 종료
