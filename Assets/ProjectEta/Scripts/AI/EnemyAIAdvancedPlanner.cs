using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIAdvancedPlanner // Base·Role·Threat·Special 네 점수 계층을 합쳐 35일차 최종 행동을 선택하는 플래너
    {
        private readonly EnemyAIPlanner _basePlanner = new EnemyAIPlanner(); // 33일차 합법 후보와 공통 점수 생성기

        public List<AIActionCandidate> BuildCandidates(BoardState board) // 현재 보드의 최종 점수 후보 목록을 만드는 메서드
        {
            var baseCandidates = _basePlanner.BuildCandidates(board); // 33일차 합법 후보와 Base Score 생성
            var finalCandidates = new List<AIActionCandidate>(baseCandidates.Count); // 같은 수의 최종 후보 목록 준비
            var threatMap = EnemyAIThreatMap.Build(board); // 이번 평가 1회 동안 공유할 플레이어 위협 맵 생성

            for (int i = 0; i < baseCandidates.Count; i++) // 모든 합법 후보 순회
            {
                var candidate = baseCandidates[i]; // 현재 Base 후보
                int roleBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(candidate, board); // 34일차 역할별 위치 보너스
                int threatScore = EnemyAIThreatScoreEvaluator.Evaluate(candidate, board, threatMap); // 35일차 플레이어 위협 위험도
                int specialBonus = EnemyAISpecialScoreEvaluator.Evaluate(candidate, board); // 35일차 특수 기물 활용 보너스
                int finalScore = candidate.Score + roleBonus + threatScore + specialBonus; // 네 계층을 모두 합친 최종 행동 점수

                finalCandidates.Add(new AIActionCandidate( // 합법 행동 데이터는 그대로 두고 Score만 최종값으로 교체
                    candidate.Actor, // 행동 주체 유지
                    candidate.Origin, // 원점 유지
                    candidate.Target, // 목표 좌표 유지
                    candidate.ActionType, // 행동 종류 유지
                    candidate.TargetPiece, // 공격 대상 유지
                    finalScore)); // 최종 점수 저장
            }

            return finalCandidates; // 완성된 최종 후보 반환
        }

        public bool TryChooseAction(BoardState board, out AIActionCandidate selectedAction) // 최종 후보 중 가장 높은 행동 하나를 결정론적으로 선택하는 메서드
        {
            var candidates = BuildCandidates(board); // Base·Role·Threat·Special이 모두 반영된 후보 생성

            if (candidates.Count == 0) // 행동 후보가 하나도 없으면
            {
                selectedAction = null; // 선택 결과 없음
                return false; // AI 행동 불가 반환
            }

            selectedAction = candidates[0]; // 첫 후보를 임시 최고 후보로 설정

            for (int i = 1; i < candidates.Count; i++) // 나머지 후보 순회
            {
                if (IsBetterCandidate(candidates[i], selectedAction)) selectedAction = candidates[i]; // 더 높은 우선순위면 교체
            }

            return true; // 최종 행동 선택 성공
        }

        private static bool IsBetterCandidate(AIActionCandidate challenger, AIActionCandidate currentBest) // 33~34일차와 동일한 결정론적 동점 규칙
        {
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
