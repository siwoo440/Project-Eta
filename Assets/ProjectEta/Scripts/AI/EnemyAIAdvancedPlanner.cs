using System; // StringComparison과 Exception을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Debug 로그를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIAdvancedPlanner // Base·Role·Threat·Special 점수를 유지하면서 캐시·예산·fallback을 적용한 39일차 최종 플래너
    {
        private readonly EnemyAIPlanner _basePlanner = new EnemyAIPlanner(); // 33일차 합법 후보와 공통 점수 생성기
        private readonly EnemyAIEvaluationBudget _budget; // 한 번의 정밀 평가 후보 수를 제한하는 예산

        public EnemyAIPerformanceStats LastPerformanceStats { get; private set; } = EnemyAIPerformanceStats.Empty; // 가장 최근 이 플래너 호출의 계산량 통계

        public EnemyAIAdvancedPlanner() : this(new EnemyAIEvaluationBudget()) // 기존 생성 코드와 호환되는 기본 생성자
        {
        }

        public EnemyAIAdvancedPlanner(EnemyAIEvaluationBudget budget) // 테스트에서 작은 예산을 주입할 수 있는 생성자
        {
            _budget = budget ?? new EnemyAIEvaluationBudget(); // null 예산은 기본값으로 안전하게 보정
        }

        public List<AIActionCandidate> BuildCandidates(BoardState board) // 기존 호출부와 동일하게 최종 점수 후보 목록을 반환하는 API
        {
            var scoredCandidates = BuildScoredCandidates(board); // 점수 분해와 성능 측정을 한 번만 수행
            var finalCandidates = new List<AIActionCandidate>(scoredCandidates.Count); // 최종 후보 목록 준비

            for (int i = 0; i < scoredCandidates.Count; i++) finalCandidates.Add(scoredCandidates[i].FinalCandidate); // 디버그용 분해 객체에서 실제 선택 후보만 추출

            return finalCandidates; // 기존 API 형태로 반환
        }

        public List<EnemyAIScoredCandidate> BuildScoredCandidates(BoardState board) // 디버그와 실제 선택이 같은 계산 결과를 재사용하도록 점수 분해 후보를 만드는 메서드
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Base 후보 생성부터 최종 점수 계산까지 실제 경과 시간 측정 시작
            var scoredCandidates = new List<EnemyAIScoredCandidate>(); // 최종 분해 점수 목록

            if (board == null) // 보드가 없으면
            {
                stopwatch.Stop(); // 측정 종료
                LastPerformanceStats = EnemyAIPerformanceStats.Empty; // 빈 통계 기록
                return scoredCandidates; // 빈 후보 반환
            }

            var baseCandidates = _basePlanner.BuildCandidates(board); // 33일차 합법 후보와 Base Score 생성
            var candidates = EnemyAICandidatePruner.Prune( // 비싼 점수 계층 전에 값싼 후보 필터 수행
                board, // 현재 보드
                baseCandidates, // 전체 Base 후보
                _budget.MaxHeavyEvaluationCandidates, // 정밀 평가 예산
                out int invalidOrDuplicateCount, // 무효·중복 제거 수
                out bool budgetCapped); // 예산 상한 적용 여부

            var context = new EnemyAIEvaluationContext(board); // King·Threat·미래 이동 결과를 이번 평가 동안 공유할 컨텍스트
            bool usedFallback = false; // 후보별 평가 예외가 발생했는지 기록

            for (int i = 0; i < candidates.Count; i++) // 정밀 평가 대상으로 남은 후보만 순회
            {
                var candidate = candidates[i]; // 현재 Base 후보
                int roleBonus = 0; // 평가 예외 시에도 Base 후보로 안전하게 남길 역할 보너스 기본값
                int threatScore = 0; // 평가 예외 시 사용할 위협 점수 기본값
                int specialBonus = 0; // 평가 예외 시 사용할 특수 점수 기본값

                try // 한 후보의 고급 평가가 실패해도 전체 EnemyTurn이 교착되지 않도록 후보 단위 보호
                {
                    roleBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(candidate, board, context); // 공유 King·미래 이동 캐시를 사용하는 역할별 점수
                    threatScore = EnemyAIThreatScoreEvaluator.Evaluate(candidate, board, context.ThreatMap); // Lazy Threat Map으로 실제 필요한 좌표만 위협 계산
                    specialBonus = EnemyAISpecialScoreEvaluator.Evaluate(candidate, board, context); // 공유 미래 이동 캐시를 사용하는 특수 기물 점수
                }
                catch (Exception exception) // 특정 특수 규칙 평가에서 예외가 나도 Base 행동을 fallback으로 보존
                {
                    usedFallback = true; // 성능 통계와 F1 창에 fallback 사용 기록
                    Debug.LogWarning($"AI 정밀 평가 실패, Base 점수로 fallback: {candidate}\n{exception.Message}"); // 문제가 난 후보를 추적 가능한 로그로 남김
                }

                int finalScore = candidate.Score + roleBonus + threatScore + specialBonus; // 기존 35일차와 동일한 네 계층 합산식
                var finalCandidate = new AIActionCandidate( // 행동 데이터는 그대로 유지하고 Score만 최종값으로 교체
                    candidate.Actor, // 행동 주체 유지
                    candidate.Origin, // 원점 유지
                    candidate.Target, // 목표 좌표 유지
                    candidate.ActionType, // 행동 종류 유지
                    candidate.TargetPiece, // 공격 대상 유지
                    finalScore); // 최종 점수 저장

                scoredCandidates.Add(new EnemyAIScoredCandidate(candidate, roleBonus, threatScore, specialBonus, finalCandidate)); // 디버그와 실제 선택이 함께 사용할 한 번의 계산 결과 저장
            }

            stopwatch.Stop(); // 모든 정밀 후보 평가 완료 후 시간 측정 종료
            int discardedCount = Math.Max(0, baseCandidates.Count - scoredCandidates.Count); // 무효·중복·예산으로 실제 평가되지 않은 총 후보 수

            LastPerformanceStats = new EnemyAIPerformanceStats( // 이번 평가의 측정값을 외부에서 읽을 수 있게 저장
                baseCandidates.Count, // Base 후보 총수
                scoredCandidates.Count, // 정밀 평가 후보 수
                Math.Max(discardedCount, invalidOrDuplicateCount), // 제외 후보 수
                context.ThreatMap.ProbeCount, // 실제 위협 좌표 계산 수
                context.FutureMovementResolveCount, // 실제 미래 MovementResolver 계산 수
                stopwatch.Elapsed.TotalMilliseconds, // 경과 시간
                budgetCapped, // 예산 상한 적용 여부
                usedFallback); // 후보 단위 fallback 사용 여부

            return scoredCandidates; // 한 번 계산한 점수 분해 후보 반환
        }

        public bool TryChooseAction(BoardState board, out AIActionCandidate selectedAction) // 최종 후보 중 가장 높은 행동 하나를 결정론적으로 선택하는 메서드
        {
            var scoredCandidates = BuildScoredCandidates(board); // 캐시·예산이 적용된 최종 후보를 한 번 계산

            if (TryChooseBestAction(scoredCandidates, out selectedAction)) return true; // 정밀 평가 후보가 있으면 기존 동점 규칙으로 선택

            if (_basePlanner.TryChooseAction(board, out selectedAction)) // 정밀 후보가 비정상적으로 사라졌지만 Base Planner에는 행동이 있다면
            {
                LastPerformanceStats = LastPerformanceStats.WithFallback(true); // 안전한 Base fallback 사용 기록
                return true; // 최소한의 합법 행동으로 EnemyTurn 진행
            }

            selectedAction = null; // Base 후보까지 없으면 실제 행동 없음
            return false; // 합법 행동이 없음을 반환
        }

        public bool TryChooseBestAction(IReadOnlyList<EnemyAIScoredCandidate> scoredCandidates, out AIActionCandidate selectedAction) // 이미 계산한 점수 목록에서 재계산 없이 최고 행동을 고르는 메서드
        {
            selectedAction = null; // 기본 선택 없음
            if (scoredCandidates == null || scoredCandidates.Count == 0) return false; // 후보가 없으면 선택 불가

            for (int i = 0; i < scoredCandidates.Count; i++) // 점수 계산이 끝난 후보만 순회
            {
                var candidate = scoredCandidates[i]?.FinalCandidate; // 실제 선택용 최종 후보 참조
                if (candidate == null) continue; // 잘못된 항목은 건너뜀
                if (selectedAction == null || IsBetterCandidate(candidate, selectedAction)) selectedAction = candidate; // 기존 결정론적 우선순위로 최고 후보 갱신
            }

            return selectedAction != null; // 실제 후보를 하나라도 찾았는지 반환
        }

        public static bool IsBetterCandidate(AIActionCandidate challenger, AIActionCandidate currentBest) // 디버그와 테스트도 동일한 33~35일차 결정론적 동점 규칙을 재사용하도록 공개
        {
            if (challenger == null) return false; // 도전 후보가 없으면 교체하지 않음
            if (currentBest == null) return true; // 현재 최고 후보가 없으면 도전 후보 채택
            if (challenger.Score != currentBest.Score) return challenger.Score > currentBest.Score; // 1순위: 최종 점수가 높은 행동
            if (challenger.ActionType != currentBest.ActionType) return challenger.ActionType == AIActionType.Attack; // 2순위: 동점이면 공격 우선

            string challengerId = challenger.Actor?.Definition?.PieceId ?? string.Empty; // 도전자 PieceId
            string currentId = currentBest.Actor?.Definition?.PieceId ?? string.Empty; // 현재 최고 후보 PieceId
            int idComparison = string.Compare(challengerId, currentId, StringComparison.Ordinal); // 문화권 영향 없는 비교
            if (idComparison != 0) return idComparison < 0; // 3순위: PieceId 사전순

            if (challenger.Origin.y != currentBest.Origin.y) return challenger.Origin.y < currentBest.Origin.y; // 4순위: 원점 Y
            if (challenger.Origin.x != currentBest.Origin.x) return challenger.Origin.x < currentBest.Origin.x; // 5순위: 원점 X
            if (challenger.Target.y != currentBest.Target.y) return challenger.Target.y < currentBest.Target.y; // 6순위: 목표 Y
            return challenger.Target.x < currentBest.Target.x; // 7순위: 목표 X
        }
    }
}
