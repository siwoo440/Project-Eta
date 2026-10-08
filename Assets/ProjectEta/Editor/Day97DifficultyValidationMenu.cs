#if UNITY_EDITOR // 에디터 전용 도구 시작
using System; // 예외와 수치 집계
using System.Collections.Generic; // 원형별 표본 집계
using System.IO; // 보고서 파일 저장
using System.Linq; // 씬 목록과 원형 정렬
using System.Text; // Markdown 문자열 생성
using UnityEditor; // 에디터 메뉴와 에셋 로드
using UnityEditor.Build.Reporting; // Windows 빌드 결과
using UnityEngine; // 로그와 프로젝트 경로
using ProjectEta.Pieces; // 기물 데이터베이스
using ProjectEta.Round; // 라운드 데이터
using ProjectEta.Run; // 편성 생성 규칙

namespace ProjectEta.EditorTools // 97일차 난이도 검증 도구 영역
{ // 네임스페이스 시작
    public static class Day97DifficultyValidationMenu // 난이도 표본 보고서와 개발 빌드
    { // 클래스 시작
        private const int SamplesPerPhase = 500; // 페이즈·종류별 표본 수

        [MenuItem("Project Eta/Day 97/Generate Difficulty Report")] // 수동 난이도 보고서 메뉴
        public static void GenerateReport() // 잠금·반복·강도와 보스 차이 검증
        { // 메서드 시작
            PieceDatabase database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 기물 데이터 로드
            RoundDefinition normalRound = AssetDatabase.LoadAssetAtPath<RoundDefinition>("Assets/ProjectEta/Resources/PrototypeRound36.asset"); // 일반 라운드 로드
            RoundDefinition midBossRound = AssetDatabase.LoadAssetAtPath<RoundDefinition>("Assets/ProjectEta/Resources/MidBossRound74.asset"); // 중간 보스 라운드 로드
            RoundDefinition finalBossRound = AssetDatabase.LoadAssetAtPath<RoundDefinition>("Assets/ProjectEta/Resources/FinalBossRound74.asset"); // 최종 보스 라운드 로드
            if (database == null || normalRound == null || midBossRound == null || finalBossRound == null) // 필수 에셋 확인
            { // 조건 시작
                throw new InvalidOperationException("97일차 난이도 검증 필수 에셋 누락"); // 누락 에셋 보고
            } // 조건 종료
            List<PieceDefinition> pool = EnemyEncounterRules.BuildPool(database.Definitions); // 런타임과 같은 적 후보 생성
            if (pool.Count == 0) // 적 후보 존재 확인
            { // 조건 시작
                throw new InvalidOperationException("97일차 일반·정예 적 후보 누락"); // 빈 후보 보고
            } // 조건 종료
            ValidateBossDifference(midBossRound, finalBossRound); // 중간·최종 보스 구성 차이 검증
            var report = new StringBuilder(); // Markdown 작성기 생성
            report.AppendLine("---"); // 문서 제목 구분선
            report.AppendLine("# 97일차 난이도 곡선 자동 표본"); // 문서 제목
            report.AppendLine(); // 제목 아래 빈 줄
            report.AppendLine("각 페이즈와 전투 종류마다 Seed 500개를 연속 생성해 해금 원형, 직전 원형 반복, 적 수와 위협도를 검사했다."); // 표본 조건 안내
            report.AppendLine("자동 위협도 표본이며 실제 승률·종료 턴·왕 HP 손실은 F1 런 기록으로 별도 확인한다."); // 실측 구분 안내
            report.AppendLine(); // 단락 구분
            report.AppendLine("| 페이즈 | 종류 | 해금 원형 | 표본 | 평균 적 수 | 평균 위협도 | 연속 반복 | 원형 분포 |"); // 표 머리글
            report.AppendLine("|---:|---|---:|---:|---:|---:|---:|---|"); // 표 정렬 행
            var averages = new Dictionary<string, double>(); // 구간별 평균 위협도
            foreach (int phase in Enumerable.Range(1, RunPhaseProgressService.TotalPhases)) // 전체 페이즈 순회
            { // 반복 시작
                foreach (StageType stageType in new[] { StageType.Battle, StageType.Elite }) // 일반·정예 순회
                { // 반복 시작
                    DifficultySampleSummary summary = Sample(pool, normalRound, stageType, phase); // 현재 구간 표본 생성
                    averages[phase + ":" + stageType] = summary.AverageThreat; // 평균 위협도 저장
                    string distribution = string.Join(", ", summary.ProfileCounts.OrderBy(pair => pair.Key).Select(pair => pair.Key + "=" + pair.Value)); // 원형 분포 문자열 생성
                    report.AppendLine("| " + phase + " | " + stageType + " | " + summary.ProfileCounts.Count + " | " + summary.Samples + " | " + summary.AverageEnemyCount.ToString("0.00") + " | " + summary.AverageThreat.ToString("0.00") + " | " + summary.ConsecutiveRepeats + " | " + distribution + " |"); // 구간 결과 행 추가
                } // 반복 종료
            } // 반복 종료
            for (int phase = 1; phase <= RunPhaseProgressService.TotalPhases; phase++) // 페이즈별 정예 강도 비교
            { // 반복 시작
                if (averages[phase + ":Elite"] <= averages[phase + ":Battle"]) // 정예 위협도 우위 확인
                { // 조건 시작
                    throw new InvalidOperationException(phase + "페이즈 정예 평균 위협도가 일반보다 높지 않음"); // 강도 회귀 보고
                } // 조건 종료
            } // 반복 종료
            if (averages["5:Battle"] <= averages["1:Battle"] || averages["5:Elite"] <= averages["1:Elite"]) // 전후반 강도 상승 확인
            { // 조건 시작
                throw new InvalidOperationException("5페이즈 평균 위협도가 1페이즈보다 높지 않음"); // 난이도 곡선 회귀 보고
            } // 조건 종료
            report.AppendLine(); // 보스 표 앞 빈 줄
            report.AppendLine("| 보스 | 시작 적 | 라운드 증원 | 증원 턴 | 제한 턴 |"); // 보스 표 머리글
            report.AppendLine("|---|---:|---:|---|---:|"); // 보스 표 정렬 행
            AppendBossRow(report, "중간 보스", midBossRound); // 중간 보스 구성 행
            AppendBossRow(report, "최종 보스", finalBossRound); // 최종 보스 구성 행
            report.AppendLine(); // 결과 단락 구분
            report.AppendLine("해금 원형이 둘 이상인 구간은 같은 원형이 연속하지 않았다. 최종 보스는 중간 보스보다 시작 적과 증원 수가 많고 증원이 더 이르게 시작한다."); // 자동 검증 결론
            report.AppendLine("실제 플레이 3런 이상에서 원형별 승률·평균 턴·왕 HP 손실을 수집한 뒤 수치를 확정한다."); // 수동 실측 기준
            string directory = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY97_REPORT"); // 배치 보고서 폴더 조회
            if (string.IsNullOrWhiteSpace(directory)) // 수동 실행 경로 확인
            { // 조건 시작
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Validation")); // 기본 검증 문서 폴더 사용
            } // 조건 종료
            Directory.CreateDirectory(directory); // 보고서 폴더 생성
            string path = Path.Combine(directory, "Day97DifficultySampling.md"); // 보고서 파일 경로
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false)); // UTF8 보고서 저장
            Debug.Log("[Day97] 난이도 표본 보고서 생성: " + path); // 저장 경로 안내
        } // 메서드 종료

        private static DifficultySampleSummary Sample(IReadOnlyList<PieceDefinition> pool, RoundDefinition round, StageType stageType, int phase) // 한 구간 연속 표본 생성
        { // 메서드 시작
            var summary = new DifficultySampleSummary(); // 빈 표본 집계 생성
            string previousProfileId = string.Empty; // 직전 편성 원형 초기화
            int unlockedCount = EnemyEncounterProfileCatalog.GetAvailableProfiles(stageType, phase).Count; // 현재 해금 원형 수 조회
            for (int sample = 0; sample < SamplesPerPhase; sample++) // 고정 Seed 표본 순회
            { // 반복 시작
                int stage = sample % RoundState.FinalRound + 1; // 깊이 1~10 순환
                string nodeId = "day97_phase_" + phase + "_" + stageType.ToString().ToLowerInvariant() + "_" + sample; // 재현 노드 ID 생성
                EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(pool, round, stageType, sample, phase, stage, nodeId, previousProfileId, string.Empty); // 반복 제외 포함 편성 생성
                IReadOnlyList<EnemyEncounterContentIssue> issues = EnemyEncounterContentValidator.Validate(encounter); // 편성 무결성 검사
                if (issues.Count > 0) // 무결성 문제 확인
                { // 조건 시작
                    throw new InvalidOperationException("편성 검증 실패: " + encounter.EncounterId + " / Issues=" + issues.Count); // 재현 정보 보고
                } // 조건 종료
                if (unlockedCount > 1 && encounter.ProfileId == previousProfileId) // 반복 가능한 구간 연속 원형 확인
                { // 조건 시작
                    summary.ConsecutiveRepeats++; // 연속 반복 수 증가
                } // 조건 종료
                summary.Add(encounter); // 정상 표본 누적
                previousProfileId = encounter.ProfileId; // 다음 표본 제외 원형 저장
            } // 반복 종료
            if (summary.ProfileCounts.Count != unlockedCount) // 모든 해금 원형 도달 확인
            { // 조건 시작
                throw new InvalidOperationException(stageType + " " + phase + "페이즈 원형 도달 불일치: " + summary.ProfileCounts.Count + "/" + unlockedCount); // 원형 누락 보고
            } // 조건 종료
            if (unlockedCount > 1 && summary.ConsecutiveRepeats > 0) // 반복 방지 결과 확인
            { // 조건 시작
                throw new InvalidOperationException(stageType + " " + phase + "페이즈 연속 원형 반복: " + summary.ConsecutiveRepeats); // 반복 회귀 보고
            } // 조건 종료
            return summary; // 완성 표본 반환
        } // 메서드 종료

        private static void ValidateBossDifference(RoundDefinition midBoss, RoundDefinition finalBoss) // 중간·최종 보스 구성 차이 검증
        { // 메서드 시작
            if (!midBoss.HasBossConfiguration || !finalBoss.HasBossConfiguration) // 보스 설정 연결 확인
            { // 조건 시작
                throw new InvalidOperationException("중간 또는 최종 보스 설정 누락"); // 설정 누락 보고
            } // 조건 종료
            if (finalBoss.InitialEnemies.Count <= midBoss.InitialEnemies.Count) // 최종 보스 시작 적 우위 확인
            { // 조건 시작
                throw new InvalidOperationException("최종 보스 시작 적 수가 중간 보스보다 많지 않음"); // 구성 차이 누락 보고
            } // 조건 종료
            if (finalBoss.Reinforcements.Count <= midBoss.Reinforcements.Count) // 최종 보스 증원 우위 확인
            { // 조건 시작
                throw new InvalidOperationException("최종 보스 증원 수가 중간 보스보다 많지 않음"); // 구성 차이 누락 보고
            } // 조건 종료
            int midFirstTurn = midBoss.Reinforcements.Min(item => item.SpawnTurn); // 중간 보스 첫 증원 턴
            int finalFirstTurn = finalBoss.Reinforcements.Min(item => item.SpawnTurn); // 최종 보스 첫 증원 턴
            if (finalFirstTurn >= midFirstTurn) // 최종 보스 초기 압박 확인
            { // 조건 시작
                throw new InvalidOperationException("최종 보스 첫 증원이 중간 보스보다 빠르지 않음"); // 증원 시점 회귀 보고
            } // 조건 종료
        } // 메서드 종료

        private static void AppendBossRow(StringBuilder report, string label, RoundDefinition round) // 보스 구성 표 한 행 추가
        { // 메서드 시작
            string turns = string.Join(", ", round.Reinforcements.Select(item => item.SpawnTurn.ToString())); // 증원 턴 문자열 생성
            report.AppendLine("| " + label + " | " + round.InitialEnemies.Count + " | " + round.Reinforcements.Count + " | " + turns + " | " + round.TurnLimit + " |"); // 보스 구성 행 추가
        } // 메서드 종료

        public static void BuildWindowsDevelopment() // 보고서 포함 Windows 개발 빌드
        { // 메서드 시작
            GenerateReport(); // 빌드 전 난이도 데이터 검증
            string path = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY97_BUILD"); // 빌드 출력 경로 조회
            if (string.IsNullOrWhiteSpace(path)) // 출력 경로 확인
            { // 조건 시작
                throw new InvalidOperationException("PROJECT_ETA_DAY97_BUILD 경로 누락"); // 명확한 실행 오류
            } // 조건 종료
            var options = new BuildPlayerOptions // Windows 개발 빌드 옵션
            { // 객체 시작
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(), // 활성 출시 씬 목록
                locationPathName = path, // Windows 실행 파일 경로
                target = BuildTarget.StandaloneWindows64, // Windows 64비트 대상
                options = BuildOptions.Development // 개발 로그 포함
            }; // 객체 종료
            BuildReport result = BuildPipeline.BuildPlayer(options); // 실제 플레이어 빌드
            if (result.summary.result != BuildResult.Succeeded) // 빌드 성공 여부 확인
            { // 조건 시작
                throw new InvalidOperationException("97일차 Windows 개발 빌드 실패: " + result.summary.result); // 실패 결과 보고
            } // 조건 종료
            Debug.Log("[Day97] Windows 개발 빌드 성공: " + path); // 빌드 성공 경로 안내
        } // 메서드 종료

        private sealed class DifficultySampleSummary // 한 페이즈·종류 표본 통계
        { // 클래스 시작
            public int Samples; // 표본 수
            public long EnemyCountTotal; // 적 수 합계
            public long ThreatTotal; // 위협도 합계
            public int ConsecutiveRepeats; // 직전 원형 연속 반복 수
            public readonly Dictionary<string, int> ProfileCounts = new Dictionary<string, int>(); // 원형별 사용 횟수
            public double AverageEnemyCount => Samples > 0 ? EnemyCountTotal / (double)Samples : 0d; // 평균 적 수
            public double AverageThreat => Samples > 0 ? ThreatTotal / (double)Samples : 0d; // 평균 위협도
            public void Add(EnemyEncounterResult encounter) // 정상 표본 한 건 누적
            { // 메서드 시작
                Samples++; // 표본 수 증가
                EnemyCountTotal += encounter.Spawns.Count; // 적 수 합산
                ThreatTotal += encounter.ThreatScore; // 위협도 합산
                ProfileCounts[encounter.ProfileId] = ProfileCounts.TryGetValue(encounter.ProfileId, out int count) ? count + 1 : 1; // 원형 사용 횟수 증가
            } // 메서드 종료
        } // 클래스 종료
    } // 클래스 종료
} // 네임스페이스 종료
#endif // 에디터 전용 도구 종료
