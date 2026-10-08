using System; // 저장 데이터 직렬화
using System.Collections.Generic; // 기록 목록
using UnityEngine; // JSON 데이터 복사

namespace ProjectEta.Run // 런 측정 데이터 영역
{ // 범위 시작
    [Serializable] // 저장 포함 데이터
    public sealed class RunBalanceData // 진행 중인 런의 경제 기록
    { // 범위 시작
        public bool totalsInitialized; // 직렬화로 생긴 빈 총계와 실제 누적값 구분
        public RunBalanceTotals totals; // 상세 상한 이후에도 보존되는 실제 누적 총계
        public int schemaVersion = 1; // 측정 데이터 버전
        public int initialGold = -1; // 측정 시작 시점 Gold
        public string profileId; // 사용한 밸런스 설정
        public int nextSequence; // 기록 순서
        public int droppedEntries; // 상한 초과 생략 수
        public List<RunBalanceEntry> entries = new List<RunBalanceEntry>(); // 행동과 후보 기록
        public List<RunBalanceEntry> firstFusions = new List<RunBalanceEntry>(); // 최초 등급 도달 기록
        public List<string> goldRewardClaims = new List<string>(); // 승리 보상 지급 이력
        public List<string> encounterClaims = new List<string>(); // 생성 편성 중복 기록 차단 이력
        public List<string> battleResultClaims = new List<string>(); // 전투 결과 중복 기록 차단 이력
        public RunBalanceData Copy() // 저장 스냅샷 독립 복사
        { // 범위 시작
            var data = JsonUtility.FromJson<RunBalanceData>(JsonUtility.ToJson(this)) ?? new RunBalanceData(); // 기록 깊은 복사
            data.Normalize(); // 구버전 목록 보정
            return data; // 독립 저장 데이터 반환
        } // 범위 종료
        public void Normalize() // 누락된 저장 목록 보정
        { // 범위 시작
            entries = entries ?? new List<RunBalanceEntry>(); // 행동 목록 복구
            firstFusions = firstFusions ?? new List<RunBalanceEntry>(); // 최초 합성 목록 복구
            goldRewardClaims = goldRewardClaims ?? new List<string>(); // 지급 이력 복구
            encounterClaims = encounterClaims ?? new List<string>(); // 편성 기록 이력 복구
            battleResultClaims = battleResultClaims ?? new List<string>(); // 결과 기록 이력 복구
            entries.RemoveAll(x => x == null); // 손상된 기록 제외
            firstFusions.RemoveAll(x => x == null); // 손상된 등급 기록 제외
            goldRewardClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 지급 키 제외
            encounterClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 편성 키 제외
            battleResultClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 결과 키 제외
            nextSequence = Math.Max(0, nextSequence); // 음수 순서 보정
            droppedEntries = Math.Max(0, droppedEntries); // 음수 누락 수 보정
            if (entries.Count > RunBalanceTelemetry.MaximumEntries) // 과도한 저장 기록 확인
            { // 범위 시작
                droppedEntries += entries.Count - RunBalanceTelemetry.MaximumEntries; // 생략 수 보존
                entries.RemoveRange(RunBalanceTelemetry.MaximumEntries, entries.Count - RunBalanceTelemetry.MaximumEntries); // 기록 상한 유지
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
    [Serializable] // 누적 총계 저장 대상
    public sealed class RunBalanceTotals // 상세 기록과 독립된 런 전체 누적값
    { // 범위 시작
        public long goldIncome; // 환불 제외 수입 합계
        public long netSpending; // 환불 차감 전후 순지출 합계
        public int failedSpends; // 실패 지출 시도 수
        public int acquiredCount; // 성공 카드 획득 수
        public int fusionCount; // 완료 합성 수
        public int battleCount; // 완료 전투 수
        public int victoryCount; // 승리 전투 수
        public int defeatCount; // 패배 전투 수
        public long totalBattleTurns; // 완료 전투 턴 합계
        public void Accumulate(RunBalanceEntry entry) // 실제 기록 발생 시 총계 갱신
        { // 범위 시작
            if (entry.kind == "Acquired") // 카드 획득 기록 확인
            { // 범위 시작
                acquiredCount = (int)Math.Min(int.MaxValue, (long)acquiredCount + 1); // 획득 횟수 안전 증가
            } // 범위 종료
            if (entry.kind == "Fusion") // 합성 완료 기록 확인
            { // 범위 시작
                fusionCount = (int)Math.Min(int.MaxValue, (long)fusionCount + 1); // 합성 횟수 안전 증가
            } // 범위 종료
            if (entry.kind == "BattleResult") // 전투 종료 기록 확인
            { // 범위 시작
                battleCount = (int)Math.Min(int.MaxValue, (long)battleCount + 1); // 전투 수 안전 증가
                totalBattleTurns = Math.Min(int.MaxValue, totalBattleTurns + Math.Max(0, entry.turn)); // 전투 턴 합계 안전 증가
                if (entry.outcome == (int)ProjectEta.Battle.BattleOutcome.Victory) // 승리 결과 확인
                { // 범위 시작
                    victoryCount = (int)Math.Min(int.MaxValue, (long)victoryCount + 1); // 승리 수 안전 증가
                } // 범위 종료
                if (entry.outcome == (int)ProjectEta.Battle.BattleOutcome.Defeat) // 패배 결과 확인
                { // 범위 시작
                    defeatCount = (int)Math.Min(int.MaxValue, (long)defeatCount + 1); // 패배 수 안전 증가
                } // 범위 종료
            } // 범위 종료
            if (entry.kind != "Gold") // 재화 이외 항목 확인
            { // 범위 시작
                return; // 재화 계산 제외
            } // 범위 종료
            if (!entry.successful) // 실패 지출 확인
            { // 범위 시작
                failedSpends = (int)Math.Min(int.MaxValue, (long)failedSpends + 1); // 실패 횟수 안전 증가
                return; // 실제 변동 없는 지출 제외
            } // 범위 종료
            long delta = (long)entry.afterGold - entry.beforeGold; // 실제 재화 변경량
            if (delta < 0 || entry.source == "Refund") // 지출 또는 환불 확인
            { // 범위 시작
                netSpending = Math.Max(-int.MaxValue, Math.Min(int.MaxValue, netSpending - delta)); // 순지출 합계 안전 보정
            } // 범위 종료
            else // 새로운 수입 확인
            { // 범위 시작
                goldIncome = Math.Min(int.MaxValue, goldIncome + delta); // 새 수입 합계 안전 보정
            } // 범위 종료
        } // 범위 종료
    } // 범위 종료
    [Serializable] // 개별 기록 저장
    public sealed class RunBalanceEntry // 소비자와 CSV가 공유하는 측정 항목
    { // 범위 시작
        public int sequence; // 기록 순서
        public string kind; // Offer·Acquired·Gold·Fusion 구분
        public string source; // 행동 발생 경로
        public int phase; // 실제 발생 페이즈
        public int stage; // 실제 발생 깊이
        public int turn; // 실제 합성 턴
        public string nodeId; // 실제 지도 노드
        public int seed; // 후보 생성 Seed
        public string pieceId; // 획득 또는 합성 기물
        public string recipeId; // 사용한 합성식
        public int grade; // 합성 결과 등급
        public int beforeGold; // 변경 전 Gold
        public int afterGold; // 변경 후 Gold
        public int amount; // 요청한 재화 변동
        public bool successful; // 실제 적용 여부
        public List<string> candidateIds = new List<string>(); // 제시된 재료 후보
        public List<string> materialIds = new List<string>(); // 소모한 합성 재료
        public string encounterId; // 생성된 적 편성 고유 ID
        public int enemyCount; // 전투 시작 적 수
        public int threatScore; // 전투 시작 위협도
        public int kingHp; // 전투 시작 또는 종료 왕 체력
        public int outcome; // 전투 종료 승패 값
    } // 범위 종료
} // 범위 종료
