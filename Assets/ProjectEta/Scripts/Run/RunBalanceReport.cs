using System; // 직렬화와 예외 처리
using System.IO; // 보고서 파일 입출력
using System.Text; // 문자열 조합과 UTF8 인코딩
using UnityEngine; // 설정 리소스와 로그
namespace ProjectEta.Run // 96일차 경제 보고 영역
{ // 범위 시작
    public sealed class RunBalanceSummary // 수입과 소비 집계 결과
    { // 범위 시작
        public int GoldIncome // 환불 제외 실제 수입
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int GoldSpent // 환불 차감 순지출
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int FailedSpends // Gold 부족 지출 시도 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int AcquiredCount // 성공 카드 획득 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int FusionCount // 완료 합성 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int BattleCount // 완료 전투 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int VictoryCount // 승리 전투 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int DefeatCount // 패배 전투 수
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
        public int TotalBattleTurns // 완료 전투 턴 합계
        { // 속성 범위 시작
            get; // 현재 누적값 조회
            internal set; // 보고서 내부 값 갱신
        } // 속성 범위 종료
    } // 범위 종료
    [Serializable] // JSON 직렬화 대상
    public sealed class RunBalanceExport // 런 측정 내보내기 데이터
    { // 범위 시작
        public string runId; // 측정 대상 런 ID
        public int mapSeed; // 재현용 지도 Seed
        public int currentGold; // 내보내기 시점 잔액
        public RunBalanceData measurements; // 독립 복사한 측정 기록
    } // 범위 종료
    public static class RunBalanceReport // 실제 런 요약과 파일 출력
    { // 범위 시작
        public static RunBalanceSummary Calculate(RunState run) // 실제 기록 기준 경제 집계
        { // 범위 시작
            var result = new RunBalanceSummary(); // 빈 집계 결과 준비
            if (run == null) // 유효 런 존재 확인
            { // 범위 시작
                return result; // 계산된 집계 결과 반환
            } // 범위 종료
            var totals = GetTotals(run.BalanceData); // 상세 상한과 독립된 누적 총계
            result.GoldIncome = (int)Math.Max(0, Math.Min(int.MaxValue, totals.goldIncome)); // 수입 정수 범위 보정
            result.GoldSpent = (int)Math.Max(0, Math.Min(int.MaxValue, totals.netSpending)); // 순지출 정수 범위 보정
            result.FailedSpends = totals.failedSpends; // 누적 실패 지출 횟수
            result.AcquiredCount = totals.acquiredCount; // 누적 성공 획득 횟수
            result.FusionCount = totals.fusionCount; // 누적 완료 합성 횟수
            result.BattleCount = totals.battleCount; // 누적 완료 전투 수
            result.VictoryCount = totals.victoryCount; // 누적 승리 수
            result.DefeatCount = totals.defeatCount; // 누적 패배 수
            result.TotalBattleTurns = (int)Math.Max(0, Math.Min(int.MaxValue, totals.totalBattleTurns)); // 누적 전투 턴 정수 범위 보정
            return result; // 계산된 집계 결과 반환
        } // 범위 종료
        internal static RunBalanceTotals GetTotals(RunBalanceData data) // 이전 측정 저장과 누적 총계 호환
        { // 범위 시작
            if (data.totalsInitialized && data.totals != null) // 실제 누적 완료 총계 확인
            { // 범위 시작
                return data.totals; // 기존 누적 총계 반환
            } // 범위 종료
            var totals = new RunBalanceTotals(); // 이전 저장 복구용 총계
            foreach (var entry in data.entries) // 남아 있는 이전 상세 기록 순회
            { // 범위 시작
                if (entry != null) // 유효 기록 확인
                { // 범위 시작
                    totals.Accumulate(entry); // 이전 상세 기록 합산
                } // 범위 종료
            } // 범위 종료
            return totals; // 이전 저장의 확인 가능한 총계 반환
        } // 범위 종료
        public static string BuildSummary(RunState run) // F1 상태 패널 측정 요약
        { // 범위 시작
            if (run == null) // 유효 런 존재 확인
            { // 범위 시작
                return "측정할 런 없음"; // 활성 런 누락 안내
            } // 범위 종료
            var summary = Calculate(run); // 현재 기록 경제 집계
            var text = new StringBuilder(); // 패널 표시 문자열 준비
            text.AppendLine("설정: " + (run.BalanceData.profileId ?? RunBalanceProfile.Current.ProfileId)); // 측정 요약 항목 추가
            text.AppendLine("시작 / 현재 Gold: " + run.BalanceData.initialGold + " / " + RunEconomyService.GetOrCreate(run).Currency); // 측정 요약 항목 추가
            text.AppendLine("수입 / 순지출: " + summary.GoldIncome + " / " + summary.GoldSpent); // 측정 요약 항목 추가
            text.AppendLine("획득 / 합성 / 실패 지출: " + summary.AcquiredCount + " / " + summary.FusionCount + " / " + summary.FailedSpends); // 측정 요약 항목 추가
            int averageTurns = summary.BattleCount > 0 ? summary.TotalBattleTurns / summary.BattleCount : 0; // 완료 전투 평균 턴 계산
            text.AppendLine("전투 / 승 / 패 / 평균 턴: " + summary.BattleCount + " / " + summary.VictoryCount + " / " + summary.DefeatCount + " / " + averageTurns); // 난이도 측정 요약 추가
            RunBalanceEntry latestResult = null; // 최근 전투 결과 보관
            RunBalanceEntry latestEncounter = null; // 최근 적 편성 보관
            for (int index = run.BalanceData.entries.Count - 1; index >= 0; index--) // 최근 기록부터 역순 검색
            { // 범위 시작
                RunBalanceEntry entry = run.BalanceData.entries[index]; // 현재 측정 기록 조회
                if (entry == null) // 빈 기록 확인
                { // 범위 시작
                    continue; // 빈 기록 제외
                } // 범위 종료
                if (latestResult == null && entry.kind == "BattleResult") // 최근 전투 결과 확인
                { // 범위 시작
                    latestResult = entry; // 최근 전투 결과 보존
                } // 범위 종료
                if (latestEncounter == null && entry.kind == "Encounter") // 최근 적 편성 확인
                { // 범위 시작
                    latestEncounter = entry; // 최근 적 편성 보존
                } // 범위 종료
                if (latestResult != null && latestEncounter != null) // 필요한 두 기록 확인
                { // 범위 시작
                    break; // 역순 검색 종료
                } // 범위 종료
            } // 범위 종료
            RunBattleEncounterSnapshot activeEncounter = run.BalanceData.activeEncounter; // 상세 상한과 독립된 최근 편성 조회
            RunBattleResultSnapshot resultSnapshot = run.BalanceData.latestBattleResult; // 상세 상한과 독립된 최근 결과 조회
            bool useActiveEncounter = activeEncounter != null && !string.IsNullOrWhiteSpace(activeEncounter.profileId) && (resultSnapshot == null || !activeEncounter.isCompleted || resultSnapshot.Matches(activeEncounter)); // 현재 또는 최신 결과와 연결된 편성 확인
            if (useActiveEncounter) // 최근 편성 스냅샷 사용 확인
            { // 범위 시작
                latestEncounter = new RunBalanceEntry // F1 표시용 최근 편성 구성
                { // 객체 시작
                    sequence = int.MaxValue, // 상세 기록보다 최신 순서 보정
                    kind = "Encounter", // 편성 기록 종류
                    source = activeEncounter.profileId, // 기존 표시 호환 원형 ID
                    profileId = activeEncounter.profileId, // 전용 원형 ID
                    stageType = activeEncounter.stageType, // 전투 종류
                    encounterId = activeEncounter.encounterId, // 편성 고유 ID
                    enemyCount = activeEncounter.enemyCount, // 시작 적 수
                    threatScore = activeEncounter.threatScore, // 시작 위협도
                    kingHp = activeEncounter.startingKingHp, // 시작 왕 체력
                    startingKingHp = activeEncounter.startingKingHp // 결과 연결용 시작 왕 체력
                }; // 객체 종료
            } // 범위 종료
            else if (resultSnapshot != null && string.IsNullOrWhiteSpace(resultSnapshot.profileId)) // 보스처럼 생성 원형 없는 최신 결과 확인
            { // 범위 시작
                latestEncounter = null; // 오래된 일반·정예 편성 표시 제거
            } // 범위 종료
            if (resultSnapshot != null) // 최신 결과 스냅샷 확인
            { // 범위 시작
                latestResult = new RunBalanceEntry // F1 표시용 최신 결과 구성
                { // 객체 시작
                    sequence = int.MaxValue, // 상세 기록보다 최신 순서 보정
                    kind = "BattleResult", // 결과 기록 종류
                    source = ((ProjectEta.Battle.BattleOutcome)resultSnapshot.outcome).ToString(), // 승패 표시 문자열
                    profileId = resultSnapshot.profileId, // 연결 원형 ID
                    stageType = resultSnapshot.stageType, // 전투 종류
                    encounterId = resultSnapshot.encounterId, // 연결 편성 ID
                    enemyCount = resultSnapshot.enemyCount, // 시작 적 수
                    threatScore = resultSnapshot.threatScore, // 시작 위협도
                    startingKingHp = resultSnapshot.startingKingHp, // 시작 왕 체력
                    kingHp = resultSnapshot.endingKingHp, // 종료 왕 체력
                    turn = resultSnapshot.turn, // 종료 턴
                    outcome = resultSnapshot.outcome // 승패 열거값
                }; // 객체 종료
            } // 범위 종료
            if (latestResult != null && !string.IsNullOrWhiteSpace(latestResult.encounterId)) // 최근 결과와 연결된 편성 확인
            { // 범위 시작
                RunBalanceEntry matchedEncounter = run.BalanceData.entries.FindLast(entry => entry != null && entry.kind == "Encounter" && entry.encounterId == latestResult.encounterId); // 결과 편성 ID와 일치하는 시작 기록 검색
                if (matchedEncounter != null && (latestEncounter == null || matchedEncounter.sequence >= latestEncounter.sequence)) // 새 편성보다 오래되지 않은 연결 편성 확인
                { // 범위 시작
                    latestEncounter = matchedEncounter; // 완료 결과와 연결된 편성 선택
                } // 범위 종료
            } // 범위 종료
            if (latestEncounter != null) // 표시할 최근 편성 확인
            { // 범위 시작
                text.AppendLine("최근 편성: " + latestEncounter.source + " / 적 " + latestEncounter.enemyCount + " / 위협도 " + latestEncounter.threatScore); // 편성 원형과 난이도 표시
                string latestProfileId = string.IsNullOrWhiteSpace(latestEncounter.profileId) ? latestEncounter.source : latestEncounter.profileId; // 구버전 원형 ID 보정
                RunBattleDifficultySummary difficulty = run.BalanceData.difficultySummaries.Find(item => item != null && item.profileId == latestProfileId); // 최근 원형 실제 집계 조회
                if (difficulty != null && difficulty.battleCount > 0) // 완료 전투 집계 존재 확인
                { // 범위 시작
                    long winRate = (long)difficulty.victoryCount * 100L / difficulty.battleCount; // 정수 승률 계산
                    long averageTurnsByProfile = difficulty.totalTurns / difficulty.battleCount; // 원형 평균 턴 계산
                    long averageKingHpLoss = difficulty.totalKingHpLoss / difficulty.battleCount; // 원형 평균 왕 HP 손실 계산
                    text.AppendLine("원형 난이도: 승률 " + winRate + "% / 평균 턴 " + averageTurnsByProfile + " / 왕 HP 손실 " + averageKingHpLoss); // F1 난이도 지표 표시
                } // 범위 종료
            } // 범위 종료
            if (latestResult != null) // 표시할 최근 결과 확인
            { // 범위 시작
                text.AppendLine("최근 결과: " + latestResult.source + " / 턴 " + latestResult.turn + " / 왕 HP " + latestResult.kingHp); // 승패와 종료 상태 표시
            } // 범위 종료
            for (int grade = 2; grade <= 5; grade++) // 2성부터 5성 도달 이력 순회
            { // 범위 시작
                var first = run.BalanceData.firstFusions.Find(x => x != null && x.grade == grade); // 해당 등급 최초 합성 조회
                text.AppendLine(grade + "성 최초: " + (first == null ? "미측정" : "페이즈 " + first.phase + " / 깊이 " + first.stage + " / 턴 " + first.turn)); // 측정 요약 항목 추가
            } // 범위 종료
            text.AppendLine("기록 / 생략: " + run.BalanceData.entries.Count + " / " + run.BalanceData.droppedEntries); // 측정 요약 항목 추가
            text.Append("승리 Gold는 개발용 임시값. 실제 플레이 기록 기준."); // 개발용 임시 보상 안내 추가
            return text.ToString(); // 완성된 상태 요약 반환
        } // 범위 종료
        public static string Export(RunState run, string directory) // 현재 런 JSON과 CSV 저장
        { // 범위 시작
            if (run == null) // 유효 런 존재 확인
            { // 범위 시작
                throw new ArgumentNullException(nameof(run)); // 잘못된 런 인자 거부
            } // 범위 종료
            Directory.CreateDirectory(directory); // 보고서 저장 폴더 준비
            var export = new RunBalanceExport // 내보내기 전용 데이터 생성
            { // 범위 시작
                runId = run.RunId, // 현재 런 ID 복사
                mapSeed = run.RouteMap.MapSeed, // 현재 지도 Seed 복사
                currentGold = RunEconomyService.GetOrCreate(run).Currency, // 실제 경제 잔액 복사
                measurements = run.BalanceData.Copy() // 저장 중 변동 없는 기록 복사
            }; // 범위 종료
            string stem = "run_" + SafeId(run.RunId); // 안전한 보고서 파일명 생성
            string jsonPath = Path.Combine(directory, stem + ".json"); // JSON 출력 경로 조합
            string csvPath = Path.Combine(directory, stem + ".csv"); // CSV 출력 경로 조합
            File.WriteAllText(jsonPath, JsonUtility.ToJson(export, true), new UTF8Encoding(false)); // 전체 측정 JSON 저장
            var csv = new StringBuilder("sequence,kind,source,phase,stage,turn,node,seed,piece,recipe,grade,beforeGold,afterGold,amount,successful,candidates,materials,encounterId,profileId,stageType,enemyCount,threatScore,startingKingHp,kingHp,outcome\r\n"); // 난이도 열 포함 CSV 머리글 준비
            foreach (var entry in export.measurements.entries) // 실제 런 기록 순회
            { // 범위 시작
                string[] cells = // 기록 하나의 CSV 열 배열
                { // 범위 시작
                    entry.sequence.ToString(), entry.kind, entry.source, entry.phase.ToString(), entry.stage.ToString(), // 기록 순서와 위치 정보
                    entry.turn.ToString(), entry.nodeId, entry.seed.ToString(), entry.pieceId, entry.recipeId, entry.grade.ToString(), // 턴과 기물 및 합성 정보
                    entry.beforeGold.ToString(), entry.afterGold.ToString(), entry.amount.ToString(), entry.successful.ToString(), // 실제 재화 변화와 성공 여부
                    string.Join("|", entry.candidateIds ?? new System.Collections.Generic.List<string>()), // 후보 재료 ID 목록 조합
                    string.Join("|", entry.materialIds ?? new System.Collections.Generic.List<string>()), // 합성 소모 재료 ID 목록 조합
                    entry.encounterId, entry.profileId, entry.stageType.ToString(), entry.enemyCount.ToString(), entry.threatScore.ToString(), // 적 편성과 전투 종류 정보
                    entry.startingKingHp.ToString(), entry.kingHp.ToString(), entry.outcome.ToString() // 왕 체력과 전투 결과 정보
                }; // 범위 종료
                for (int index = 0; index < cells.Length; index++) // CSV 각 열 순회
                { // 범위 시작
                    if (index > 0) // 첫 열 이외 구분자 확인
                    { // 범위 시작
                        csv.Append(','); // 열 구분 쉼표 추가
                    } // 범위 종료
                    csv.Append(Quote(cells[index])); // 따옴표 보호한 열 값 추가
                } // 범위 종료
                csv.Append("\r\n"); // CSV 행 종료
            } // 범위 종료
            File.WriteAllText(csvPath, csv.ToString(), new UTF8Encoding(true)); // Excel 호환 UTF8 CSV 저장
            return jsonPath; // 저장한 JSON 경로 반환
        } // 범위 종료
        private static string SafeId(string value) // 파일명용 런 ID 보정
        { // 범위 시작
            var result = new StringBuilder(); // 안전한 파일명 문자열 준비
            foreach (char character in value ?? string.Empty) // 런 ID 문자 검사
            { // 범위 시작
                if (char.IsLetterOrDigit(character) || character == '-') // 파일명 허용 문자 확인
                { // 범위 시작
                    result.Append(character); // 안전한 문자 추가
                } // 범위 종료
                if (result.Length >= 64) // 파일명 최대 길이 확인
                { // 범위 시작
                    break; // 현재 처리 종료
                } // 범위 종료
            } // 범위 종료
            return result.Length == 0 ? "unknown" : result.ToString(); // 빈 ID 기본 파일명 적용
        } // 범위 종료
        private static string Quote(string value) // CSV 구분자와 따옴표 보호
        { // 범위 시작
            return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\""; // CSV 따옴표 중복과 열 감싸기
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
