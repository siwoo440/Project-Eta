#if UNITY_EDITOR // 에디터 전용 코드 시작
using System; // 직렬화와 예외 처리
using System.IO; // 보고서 파일 입출력
using System.Linq; // 기물과 레시피 집계
using System.Text; // 문자열 조합과 UTF8 인코딩
using System.Collections.Generic; // 후보 목록과 횟수 집계
using UnityEditor; // 에디터 검증 메뉴
using UnityEditor.Build.Reporting; // Windows 빌드 결과
using UnityEngine; // 설정 리소스와 로그
using ProjectEta.Pieces; // 기물 정의와 등급
using ProjectEta.Fusion; // 합성 레시피 데이터
using ProjectEta.Cards; // 기본 카드 카탈로그
using ProjectEta.Run; // 런 경제와 후보 생성
namespace ProjectEta.EditorTools // 96일차 경제 보고 영역
{ // 범위 시작
    public static class Day96BalanceValidationMenu // 에디터 후보 검증과 빌드 도구
    { // 범위 시작
        [MenuItem("Project Eta/Day 96/Generate Balance Report")] // 에디터 보고서 생성 메뉴
        public static void GenerateReport() // 고정 Seed 후보 분포 검증
        { // 범위 시작
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 기물 데이터베이스 로드
            var recipes = AssetDatabase.LoadAssetAtPath<FusionRecipeDatabase>("Assets/ProjectEta/Data/FusionRecipeDatabase.asset"); // 실제 합성식 데이터베이스 로드
            var catalog = Resources.Load<PlayerStartingDeckCatalog>("PlayerStartingDeck26"); // 기본 후보 카탈로그 로드
            var balance = Resources.Load<RunBalanceProfile>("RunBalance96"); // 현재 경제 설정 리소스 로드
            if (database == null || recipes == null || catalog == null || balance == null || database.Definitions.Count != 81 || recipes.Recipes.Count != 70) // 81종과 70개 레시피 연결 확인
            { // 범위 시작
                throw new InvalidOperationException("81종·70개 레시피·경제 설정 확인 실패"); // 확인 실패 사유 표시
            } // 범위 종료
            var materials = database.Definitions.Where(RunContentPoolRules.CanUseAsReward).OrderBy(x => x.PieceId).ToArray(); // 일반 획득 가능한 1성 재료 정렬
            if (materials.Length != 17) // 왕 제외 17종 재료 확인
            { // 범위 시작
                throw new InvalidOperationException("왕 제외 1성 재료 17종 확인 실패"); // 확인 실패 사유 표시
            } // 범위 종료
            var unlocked = new RunContentUnlockSnapshotSaveData(); // 전부 해금한 비교 데이터 준비
            foreach (var piece in database.Definitions) // 등록 기물 해금 조건 순회
            { // 범위 시작
                if (!string.IsNullOrWhiteSpace(piece.RequiredMetaUnlockId)) // 해금 조건 존재 확인
                { // 범위 시작
                    unlocked.unlockedPieceIds.Add(piece.RequiredMetaUnlockId); // 비교용 해금 ID 추가
                } // 범위 종료
            } // 범위 종료
            var snapshot = RunContentUnlockSnapshot.FromSaveData(unlocked); // 실제 사용자 저장 없는 해금 비교 상태
            var report = new StringBuilder("---\n# 96일차 재료 후보·가격 검증\n\n"); // 후보 검증 보고서 제목 준비
            report.AppendLine("81종·70개 레시피. 기준: " + balance.ProfileId + "."); // 관측 조건과 결과 행 추가
            report.AppendLine("후보 샘플링은 실제 런 완주·합성 속도 측정 결과가 아님."); // 관측 조건과 결과 행 추가
            report.AppendLine("각 경로·해금 상태당 Seed 0~999, 매 Seed 3후보, 보유·사망 카드 없음. 동일 후보 묶음 내 중복 제외."); // 관측 조건과 결과 행 추가
            report.AppendLine("모든 재료 가중치 100 유지. 미해금 기본 풀과 전부 해금한 풀을 별도 비교."); // 관측 조건과 결과 행 추가
            foreach (bool allUnlocked in new[] { false, true }) // 기본 해금과 전체 해금 별도 비교
            { // 범위 시작
                report.AppendLine("\n---\n## " + (allUnlocked ? "전부 해금" : "기본 해금") + " 후보 횟수\n"); // 관측 조건과 결과 행 추가
                var reward = Sample(catalog.Cards, allUnlocked ? snapshot : null, balance, 0); // 보상 경로 후보 분포 계산
                var shop = Sample(catalog.Cards, allUnlocked ? snapshot : null, balance, 1); // 상점 경로 후보 분포 계산
                var events = Sample(catalog.Cards, allUnlocked ? snapshot : null, balance, 2); // 이벤트 경로 후보 분포 계산
                report.AppendLine("| 재료 | 가중치 | Reward | Shop | Event | 레시피 직접 투입 수 |"); // 관측 조건과 결과 행 추가
                report.AppendLine("|---|---:|---:|---:|---:|---:|"); // 관측 조건과 결과 행 추가
                foreach (var material in materials) // 1성 재료별 결과 순회
                { // 범위 시작
                    int usage = recipes.Recipes.Sum(r => (r.MaterialA == material ? 1 : 0) + (r.MaterialB == material ? 1 : 0)); // 레시피 직접 재료 사용 횟수
                    report.AppendLine("| " + material.PieceId + " | " + balance.GetMaterialWeight(material) + " | " + Count(reward, material.PieceId) + " | " + Count(shop, material.PieceId) + " | " + Count(events, material.PieceId) + " | " + usage + " |"); // 관측 조건과 결과 행 추가
                } // 범위 종료
            } // 범위 종료
            report.AppendLine("\n---\n## 페이즈별 가격·승리 보상\n"); // 관측 조건과 결과 행 추가
            report.AppendLine("깊이 1 가격. 승리 보상은 개발용 임시값."); // 관측 조건과 결과 행 추가
            report.AppendLine("| 페이즈 | 1성 구매 | 제거 | 회복 | 강화 | 일반 승리 | 정예 | 중간 보스 | 최종 보스 |"); // 관측 조건과 결과 행 추가
            report.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|"); // 관측 조건과 결과 행 추가
            for (int phase = 1; phase <= 5; phase++) // 1페이즈부터 5페이즈 가격 조회
            { // 범위 시작
                report.AppendLine("| " + phase + " | " + ShopPriceRules.GetCardPurchasePrice(PieceGrade.OneStar, phase, 1) + " | " + ShopPriceRules.GetCardRemovePrice(phase) + " | " + ShopPriceRules.GetHealPrice(phase) + " | " + ShopPriceRules.GetUpgradePrice(phase, 1) + " | " + balance.GetBattleGold(StageType.Battle, phase) + " | " + balance.GetBattleGold(StageType.Elite, phase) + " | " + balance.GetBattleGold(StageType.MidBoss, phase) + " | " + balance.GetBattleGold(StageType.FinalBoss, phase) + " |"); // 관측 조건과 결과 행 추가
            } // 범위 종료
            report.AppendLine("\n관측 전 유지: 후보 접근성·재현성 검증과 실제 합성 도달 속도는 구분. F1 실플레이 기록 3런 이상 확보 후 재료 가중치·가격 조정."); // 관측 조건과 결과 행 추가
            string directory = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY96_REPORT"); // 배치 보고서 출력 폴더 조회
            if (string.IsNullOrWhiteSpace(directory)) // 수동 메뉴 기본 출력 위치 확인
            { // 범위 시작
                directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Validation")); // 프로젝트 검증 문서 폴더 적용
            } // 범위 종료
            Directory.CreateDirectory(directory); // 보고서 저장 폴더 준비
            string path = Path.Combine(directory, "Day96BalanceSampling.md"); // 후보 보고서 파일 경로
            File.WriteAllText(path, report.ToString(), new UTF8Encoding(false)); // UTF8 후보 검증 문서 저장
            Debug.Log("[Day96] 재현 후보 샘플 보고서 생성: " + path); // 후보 보고서 저장 경로 기록
        } // 범위 종료
        private static Dictionary<string, int> Sample(IReadOnlyList<PieceDefinition> pool, RunContentUnlockSnapshot snapshot, RunBalanceProfile balance, int source) // 경로별 후보 출현 횟수 집계
        { // 범위 시작
            var counts = new Dictionary<string, int>(); // 재료별 후보 횟수 목록 준비
            for (int seed = 0; seed < 1000; seed++) // 고정 Seed 천 개 순회
            { // 범위 시작
                int phase = seed % 5 + 1; // 다섯 페이즈 순환 표본
                int stage = seed % 10 + 1; // 열 깊이 순환 표본
                CardRewardProfile profile = source == 1 ? ShopOfferGenerator.CreateShopProfile(phase, stage) : source == 2 ? StageEventRules.GetCardRewardProfile(phase, stage) : CardRewardQualityRules.GetProfile(stage, CardRewardSource.RewardNode, string.Empty); // 실제 경로별 후보 품질 규칙 적용
                int actualSeed = source == 1 ? ShopOfferGenerator.CreateSeed(seed, phase, stage, "sample_" + seed, 0) : source == 2 ? StageEventGenerator.CreateFollowUpSeed(seed, phase, stage, "sample_" + seed, "sample_event", "sample_choice") : seed; // 실제 상점과 이벤트 Seed 규칙 적용
                var cards = CardRewardGenerator.GenerateBalanced(pool, null, null, 3, actualSeed, snapshot, profile, balance); // 동일 생성기로 3개 후보 추출
                if (cards.Count != 3 || cards.Select(x => x.PieceId).Distinct().Count() != 3 || cards.Any(x => !RunContentPoolRules.CanUseAsReward(x))) // 중복과 일반 획득 정책 검증
                { // 범위 시작
                    throw new InvalidOperationException("일반 획득 후보 정책 위반"); // 확인 실패 사유 표시
                } // 범위 종료
                foreach (var card in cards) // 실제 추출 후보 순회
                { // 범위 시작
                    counts[card.PieceId] = Count(counts, card.PieceId) + 1; // 해당 재료 후보 횟수 증가
                } // 범위 종료
            } // 범위 종료
            return counts; // 재료별 집계 결과 반환
        } // 범위 종료
        private static int Count(Dictionary<string, int> values, string key) // 재료별 누적 횟수 조회
        { // 범위 시작
            return values.TryGetValue(key, out int count) ? count : 0; // 미출현 재료 0회 처리
        } // 범위 종료
        public static void BuildWindowsDevelopment() // Windows 개발 빌드 실행
        { // 범위 시작
            GenerateReport(); // 빌드 전 콘텐츠와 후보 검증
            string path = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY96_BUILD"); // Windows 빌드 출력 경로 조회
            if (string.IsNullOrWhiteSpace(path)) // 빌드 출력 경로 누락 확인
            { // 범위 시작
                throw new InvalidOperationException("PROJECT_ETA_DAY96_BUILD 경로 누락"); // 확인 실패 사유 표시
            } // 범위 종료
            var options = new BuildPlayerOptions // 현재 프로젝트 설정 기반 빌드 옵션
            { // 범위 시작
                scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(), // 출시 설정에서 활성 씬 선택
                locationPathName = path, // 빌드 파일 저장 위치 지정
                target = BuildTarget.StandaloneWindows64, // Windows 64비트 대상 지정
                options = BuildOptions.Development // 개발 빌드 옵션 지정
            }; // 범위 종료
            var result = BuildPipeline.BuildPlayer(options); // Windows 플레이어 빌드 실행
            if (result.summary.result != BuildResult.Succeeded) // 빌드 성공 상태 확인
            { // 범위 시작
                throw new InvalidOperationException("96일차 Windows 개발 빌드 실패: " + result.summary.result); // 확인 실패 사유 표시
            } // 범위 종료
            Debug.Log("[Day96] Windows 개발 빌드 성공: " + path); // 개발 빌드 완료 경로 기록
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
#endif // 에디터 전용 코드 종료
