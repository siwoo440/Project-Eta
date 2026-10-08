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
        public int schemaVersion = 2; // 난이도 집계 포함 측정 데이터 버전
        public int initialGold = -1; // 측정 시작 시점 Gold
        public string profileId; // 사용한 밸런스 설정
        public int nextSequence; // 기록 순서
        public int droppedEntries; // 상한 초과 생략 수
        public List<RunBalanceEntry> entries = new List<RunBalanceEntry>(); // 행동과 후보 기록
        public List<RunBalanceEntry> firstFusions = new List<RunBalanceEntry>(); // 최초 등급 도달 기록
        public List<string> goldRewardClaims = new List<string>(); // 승리 보상 지급 이력
        public List<string> encounterClaims = new List<string>(); // 생성 편성 중복 기록 차단 이력
        public List<string> battleResultClaims = new List<string>(); // 전투 결과 중복 기록 차단 이력
        public List<RunBattleDifficultySummary> difficultySummaries = new List<RunBattleDifficultySummary>(); // 원형별 실제 전투 난이도 집계
        public RunBattleEncounterSnapshot activeEncounter; // 상세 상한과 독립된 최근 시작 편성
        public RunBattleResultSnapshot latestBattleResult; // 상세 상한과 독립된 최근 전투 결과
        public string lastBattleProfileId; // 상세 상한과 독립된 최근 일반 원형
        public string lastEliteProfileId; // 상세 상한과 독립된 최근 정예 원형
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
            difficultySummaries = difficultySummaries ?? new List<RunBattleDifficultySummary>(); // 난이도 집계 목록 복구
            entries.RemoveAll(x => x == null); // 손상된 기록 제외
            firstFusions.RemoveAll(x => x == null); // 손상된 등급 기록 제외
            goldRewardClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 지급 키 제외
            encounterClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 편성 키 제외
            battleResultClaims.RemoveAll(string.IsNullOrWhiteSpace); // 빈 결과 키 제외
            difficultySummaries.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.profileId)); // 손상된 원형 집계 제외
            lastBattleProfileId = lastBattleProfileId ?? string.Empty; // 일반 원형 null 보정
            lastEliteProfileId = lastEliteProfileId ?? string.Empty; // 정예 원형 null 보정
            MigrateLegacyDifficultyData(); // 96일차 상세 기록을 새 원형별 집계로 이관
            nextSequence = Math.Max(0, nextSequence); // 음수 순서 보정
            droppedEntries = Math.Max(0, droppedEntries); // 음수 누락 수 보정
            if (entries.Count > RunBalanceTelemetry.MaximumEntries) // 과도한 저장 기록 확인
            { // 범위 시작
                droppedEntries += entries.Count - RunBalanceTelemetry.MaximumEntries; // 생략 수 보존
                entries.RemoveRange(RunBalanceTelemetry.MaximumEntries, entries.Count - RunBalanceTelemetry.MaximumEntries); // 기록 상한 유지
            } // 범위 종료
        } // 범위 종료
        public string GetLastEncounterProfile(StageType stageType) // 전투 종류별 최근 원형 조회
        { // 범위 시작
            if (stageType == StageType.Battle) return lastBattleProfileId ?? string.Empty; // 최근 일반 원형 반환
            if (stageType == StageType.Elite) return lastEliteProfileId ?? string.Empty; // 최근 정예 원형 반환
            return string.Empty; // 비대상 스테이지 빈 값 반환
        } // 범위 종료
        public void SetLastEncounterProfile(StageType stageType, string profileId) // 전투 종류별 최근 원형 저장
        { // 범위 시작
            string safeProfileId = profileId ?? string.Empty; // null 없는 원형 ID 보정
            if (stageType == StageType.Battle) lastBattleProfileId = safeProfileId; // 최근 일반 원형 저장
            if (stageType == StageType.Elite) lastEliteProfileId = safeProfileId; // 최근 정예 원형 저장
        } // 범위 종료
        private void MigrateLegacyDifficultyData() // 구버전 상세 전투 기록 이관
        { // 범위 시작
            if (schemaVersion >= 2) return; // 이미 이관된 저장 제외
            difficultySummaries.Clear(); // 부분 이관 집계 초기화
            lastBattleProfileId = string.Empty; // 구버전 일반 최근 원형 초기화
            lastEliteProfileId = string.Empty; // 구버전 정예 최근 원형 초기화
            for (int index = 0; index < entries.Count; index++) // 시간 순 상세 기록 순회
            { // 반복 시작
                RunBalanceEntry entry = entries[index]; // 현재 상세 기록 조회
                if (entry == null) continue; // 빈 기록 제외
                if (entry.kind == "Encounter") // 편성 시작 기록 확인
                { // 조건 시작
                    string encounterProfileId = string.IsNullOrWhiteSpace(entry.profileId) ? entry.source : entry.profileId; // 구버전 원형 ID 보정
                    StageType encounterStageType = ResolveStageType(entry.stageType, encounterProfileId); // 구버전 전투 종류 보정
                    SetLastEncounterProfile(encounterStageType, encounterProfileId); // 종류별 최근 원형 복구
                    continue; // 결과 기록 처리로 이동
                } // 조건 종료
                if (entry.kind != "BattleResult") continue; // 완료 전투 이외 기록 제외
                RunBalanceEntry encounter = FindLegacyEncounter(index, entry); // 결과에 연결된 시작 편성 조회
                if (encounter == null) continue; // 연결 정보 없는 결과 제외
                string profileId = string.IsNullOrWhiteSpace(entry.profileId) ? (string.IsNullOrWhiteSpace(encounter.profileId) ? encounter.source : encounter.profileId) : entry.profileId; // 원형 ID 복구
                if (string.IsNullOrWhiteSpace(profileId)) continue; // 원형 없는 결과 제외
                StageType stageType = ResolveStageType(entry.stageType != 0 ? entry.stageType : encounter.stageType, profileId); // 전투 종류 복구
                int startingHp = entry.startingKingHp > 0 ? entry.startingKingHp : (encounter.startingKingHp > 0 ? encounter.startingKingHp : encounter.kingHp); // 시작 왕 체력 복구
                int threat = entry.threatScore > 0 ? entry.threatScore : encounter.threatScore; // 시작 위협도 복구
                entry.profileId = profileId; // 결과 원형 ID 새 형식 저장
                entry.stageType = (int)stageType; // 결과 전투 종류 새 형식 저장
                entry.startingKingHp = startingHp; // 결과 시작 왕 체력 새 형식 저장
                RunBattleDifficultySummary summary = difficultySummaries.Find(item => item != null && item.profileId == profileId && item.stageType == (int)stageType); // 기존 원형 집계 조회
                if (summary == null) // 첫 원형 결과 확인
                { // 조건 시작
                    summary = new RunBattleDifficultySummary // 새 원형 집계 생성
                    { // 객체 시작
                        profileId = profileId, // 원형 ID 저장
                        stageType = (int)stageType // 전투 종류 저장
                    }; // 객체 종료
                    difficultySummaries.Add(summary); // 이관 집계 목록 추가
                } // 조건 종료
                summary.Accumulate(entry.outcome, entry.turn, startingHp, entry.kingHp, threat); // 구버전 완료 결과 누적
            } // 반복 종료
            schemaVersion = 2; // 난이도 집계 저장 버전 갱신
        } // 메서드 종료
        private RunBalanceEntry FindLegacyEncounter(int resultIndex, RunBalanceEntry result) // 구버전 결과의 시작 편성 조회
        { // 범위 시작
            for (int index = resultIndex - 1; index >= 0; index--) // 결과 이전 기록 역순 검색
            { // 반복 시작
                RunBalanceEntry candidate = entries[index]; // 현재 이전 기록 조회
                if (candidate == null || candidate.kind != "Encounter") continue; // 편성 시작 이외 제외
                if (!string.IsNullOrWhiteSpace(result.encounterId) && candidate.encounterId == result.encounterId) return candidate; // 편성 ID 일치 반환
                if (candidate.phase == result.phase && candidate.stage == result.stage && candidate.nodeId == result.nodeId) return candidate; // 구버전 위치 일치 반환
            } // 반복 종료
            return null; // 연결 편성 없음
        } // 메서드 종료
        private static StageType ResolveStageType(int storedStageType, string profileId) // 구버전 전투 종류 추론
        { // 범위 시작
            if (!string.IsNullOrWhiteSpace(profileId) && profileId.StartsWith("elite_", StringComparison.Ordinal)) return StageType.Elite; // 정예 원형 접두사 우선
            return storedStageType == (int)StageType.Elite ? StageType.Elite : StageType.Battle; // 저장값 또는 일반 기본값 반환
        } // 메서드 종료
    } // 범위 종료
    [Serializable] // 최근 전투 결과 저장 대상
    public sealed class RunBattleResultSnapshot // 상세 기록 상한과 독립된 최신 결과
    { // 범위 시작
        public int phase; // 완료 페이즈
        public int stage; // 완료 스테이지
        public string nodeId; // 완료 지도 노드
        public string encounterId; // 연결 편성 ID
        public string profileId; // 연결 원형 ID
        public int stageType; // StageType 정수값
        public int enemyCount; // 시작 적 수
        public int threatScore; // 시작 위협도
        public int startingKingHp; // 시작 왕 체력
        public int endingKingHp; // 종료 왕 체력
        public int turn; // 종료 턴
        public int outcome; // 승패 열거값
        public bool Matches(RunBattleEncounterSnapshot encounter) // 시작 편성과 같은 전투 여부 확인
        { // 범위 시작
            return encounter != null && phase == encounter.phase && stage == encounter.stage && (nodeId ?? string.Empty) == (encounter.nodeId ?? string.Empty); // 위치 일치 여부 반환
        } // 범위 종료
    } // 범위 종료
    [Serializable] // 전투 시작 스냅샷 저장 대상
    public sealed class RunBattleEncounterSnapshot // 상세 기록 상한과 독립된 최근 편성
    { // 범위 시작
        public string encounterId; // 편성 고유 ID
        public string profileId; // 편성 원형 ID
        public int stageType; // StageType 정수값
        public int phase; // 시작 페이즈
        public int stage; // 시작 스테이지
        public string nodeId; // 시작 지도 노드
        public int enemyCount; // 시작 적 수
        public int threatScore; // 시작 위협도
        public int startingKingHp; // 전투 시작 왕 체력
        public bool isCompleted; // 전투 결과 기록 여부
        public int outcome; // 최근 전투 승패 값
        public int turn; // 최근 종료 턴
        public int endingKingHp; // 최근 종료 왕 체력
        public bool Matches(int targetPhase, int targetStage, string targetNodeId) // 전투 결과 위치 일치 확인
        { // 범위 시작
            return phase == targetPhase && stage == targetStage && (nodeId ?? string.Empty) == (targetNodeId ?? string.Empty); // 동일 전투 위치 여부 반환
        } // 범위 종료
    } // 범위 종료
    [Serializable] // 원형별 난이도 집계 저장 대상
    public sealed class RunBattleDifficultySummary // 실제 플레이 결과 누적 지표
    { // 범위 시작
        public string profileId; // 편성 원형 ID
        public int stageType; // StageType 정수값
        public int battleCount; // 완료 전투 수
        public int victoryCount; // 승리 수
        public int defeatCount; // 패배 수
        public long totalTurns; // 종료 턴 합계
        public long totalKingHpLoss; // 왕 HP 손실 합계
        public long totalThreatScore; // 시작 위협도 합계
        public void Accumulate(int outcome, int turn, int startingKingHp, int endingKingHp, int threatScore) // 완료 전투 한 건 누적
        { // 범위 시작
            battleCount = (int)Math.Min(int.MaxValue, (long)battleCount + 1); // 전투 수 안전 증가
            totalTurns = Math.Min(int.MaxValue, totalTurns + Math.Max(0, turn)); // 턴 합계 안전 증가
            totalKingHpLoss = Math.Min(int.MaxValue, totalKingHpLoss + Math.Max(0, startingKingHp - endingKingHp)); // 왕 HP 손실 안전 증가
            totalThreatScore = Math.Min(int.MaxValue, totalThreatScore + Math.Max(0, threatScore)); // 위협도 합계 안전 증가
            if (outcome == (int)ProjectEta.Battle.BattleOutcome.Victory) // 승리 결과 확인
            { // 범위 시작
                victoryCount = (int)Math.Min(int.MaxValue, (long)victoryCount + 1); // 승리 수 안전 증가
            } // 범위 종료
            if (outcome == (int)ProjectEta.Battle.BattleOutcome.Defeat) // 패배 결과 확인
            { // 범위 시작
                defeatCount = (int)Math.Min(int.MaxValue, (long)defeatCount + 1); // 패배 수 안전 증가
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
        public string profileId; // 연결된 편성 원형 ID
        public int stageType; // 연결된 StageType 정수값
        public int startingKingHp; // 결과에 연결된 시작 왕 체력
    } // 범위 종료
} // 범위 종료
