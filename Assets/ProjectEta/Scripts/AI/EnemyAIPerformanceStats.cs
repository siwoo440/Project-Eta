namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIPerformanceStats // 한 번의 AI 후보 평가에서 발생한 계산량을 F1 디버그와 로그에 전달하는 39일차 통계 객체
    {
        public static EnemyAIPerformanceStats Empty { get; } = new EnemyAIPerformanceStats(0, 0, 0, 0, 0, 0d, false, false); // 아직 측정값이 없을 때 사용할 빈 통계

        public int TotalCandidateCount { get; } // Base Planner가 처음 생성한 전체 후보 수
        public int EvaluatedCandidateCount { get; } // Role·Threat·Special까지 실제 정밀 평가한 후보 수
        public int DiscardedCandidateCount { get; } // 중복·무효·예산 상한으로 정밀 평가에서 제외된 후보 수
        public int ThreatProbeCount { get; } // Lazy Threat Map에서 실제로 계산한 고유 좌표 수
        public int FutureMovementResolveCount { get; } // 후보 위치의 다음 이동 범위를 실제 MovementResolver로 다시 계산한 횟수
        public double ElapsedMilliseconds { get; } // AI 정밀 평가에 걸린 실제 경과 시간
        public bool BudgetCapped { get; } // 후보 수가 예산 상한을 넘어 일부 후보를 줄였는지 여부
        public bool UsedFallback { get; } // 평가 예외 또는 정밀 후보 부재 시 안전한 Base fallback을 사용했는지 여부

        public EnemyAIPerformanceStats(int totalCandidateCount, int evaluatedCandidateCount, int discardedCandidateCount, int threatProbeCount, int futureMovementResolveCount, double elapsedMilliseconds, bool budgetCapped, bool usedFallback) // 모든 성능 값을 한 번에 저장하는 생성자
        {
            TotalCandidateCount = totalCandidateCount; // 전체 후보 수 저장
            EvaluatedCandidateCount = evaluatedCandidateCount; // 정밀 평가 후보 수 저장
            DiscardedCandidateCount = discardedCandidateCount; // 제외 후보 수 저장
            ThreatProbeCount = threatProbeCount; // 위협 좌표 계산 수 저장
            FutureMovementResolveCount = futureMovementResolveCount; // 미래 이동 계산 수 저장
            ElapsedMilliseconds = elapsedMilliseconds; // 경과 시간 저장
            BudgetCapped = budgetCapped; // 예산 상한 여부 저장
            UsedFallback = usedFallback; // fallback 여부 저장
        }

        public EnemyAIPerformanceStats WithFallback(bool usedFallback) // 기존 측정값을 유지하면서 fallback 여부만 갱신하는 도우미
        {
            return new EnemyAIPerformanceStats(TotalCandidateCount, EvaluatedCandidateCount, DiscardedCandidateCount, ThreatProbeCount, FutureMovementResolveCount, ElapsedMilliseconds, BudgetCapped, usedFallback); // 새 불변 통계 반환
        }

        public override string ToString() // Unity Console에서 한 줄로 성능 상태를 확인하기 위한 문자열 표현
        {
            return $"Candidates={EvaluatedCandidateCount}/{TotalCandidateCount}, Discarded={DiscardedCandidateCount}, ThreatProbe={ThreatProbeCount}, FutureResolve={FutureMovementResolveCount}, Time={ElapsedMilliseconds:0.###}ms, Budget={BudgetCapped}, Fallback={UsedFallback}"; // 핵심 지표 압축 출력
        }
    }
}
