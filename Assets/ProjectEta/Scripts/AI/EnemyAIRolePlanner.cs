using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIRolePlanner // 33일차 공통 후보에 34일차 역할별 이동 보너스를 더해 최종 행동을 고르는 플래너
    {
        private readonly EnemyAIPlanner _basePlanner = new EnemyAIPlanner(); // 합법 후보와 공통 점수는 검증된 33일차 플래너를 그대로 재사용

        public List<AIActionCandidate> BuildCandidates(BoardState board) // 공통 후보에 역할 점수를 합산한 새 후보 목록을 만드는 메서드
        {
            var baseCandidates = _basePlanner.BuildCandidates(board); // 33일차 합법 후보·공통 점수 생성
            var roleCandidates = new List<AIActionCandidate>(baseCandidates.Count); // 같은 수의 역할 보정 후보 목록 준비

            for (int i = 0; i < baseCandidates.Count; i++) // 모든 공통 후보를 순회
            {
                var candidate = baseCandidates[i]; // 현재 공통 후보
                int roleBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(candidate, board); // 34일차 기본 역할 이동 보너스 계산
                int finalScore = candidate.Score + roleBonus; // 기존 점수를 절대 대체하지 않고 추가 보너스만 합산

                roleCandidates.Add(new AIActionCandidate( // 기존 후보의 합법 행동 정보는 그대로 유지하고 점수만 확장
                    candidate.Actor, // 행동 주체 유지
                    candidate.Origin, // 원점 유지
                    candidate.Target, // 목표 좌표 유지
                    candidate.ActionType, // 이동/공격 종류 유지
                    candidate.TargetPiece, // 공격 대상 유지
                    finalScore)); // 공통 점수 + 역할 보너스 저장
            }

            return roleCandidates; // 역할 성격이 적용된 최종 후보 반환
        }

        public bool TryChooseAction(BoardState board, out AIActionCandidate selectedAction) // 역할 보정 후보 중 최고 행동을 결정론적으로 고르는 메서드
        {
            var candidates = BuildCandidates(board); // 역할 점수가 반영된 후보 생성
            if (candidates.Count == 0) // 가능한 적 행동이 하나도 없으면
            {
                selectedAction = null; // 선택 결과 없음
                return false; // 행동 없음 반환
            }

            selectedAction = candidates[0]; // 첫 후보를 임시 최고 후보로 설정

            for (int i = 1; i < candidates.Count; i++) // 나머지 후보 순회
            {
                if (IsBetterCandidate(candidates[i], selectedAction)) selectedAction = candidates[i]; // 더 좋은 후보면 최고 행동 교체
            }

            return true; // 최종 행동 선택 성공
        }

        private static bool IsBetterCandidate(AIActionCandidate challenger, AIActionCandidate currentBest) // 33일차와 같은 결정론적 동점 규칙을 유지하는 비교 메서드
        {
            if (challenger.Score != currentBest.Score) return challenger.Score > currentBest.Score; // 1순위: 최종 점수가 높은 행동
            if (challenger.ActionType != currentBest.ActionType) return challenger.ActionType == AIActionType.Attack; // 2순위: 동점이면 공격 우선

            string challengerId = challenger.Actor?.Definition?.PieceId ?? string.Empty; // 도전자 PieceId 읽기
            string currentId = currentBest.Actor?.Definition?.PieceId ?? string.Empty; // 현재 최고 후보 PieceId 읽기
            int idComparison = string.Compare(challengerId, currentId, StringComparison.Ordinal); // 문화권과 무관한 문자열 비교
            if (idComparison != 0) return idComparison < 0; // 3순위: PieceId 사전순

            if (challenger.Origin.y != currentBest.Origin.y) return challenger.Origin.y < currentBest.Origin.y; // 4순위: 행동 주체 Y 좌표
            if (challenger.Origin.x != currentBest.Origin.x) return challenger.Origin.x < currentBest.Origin.x; // 5순위: 행동 주체 X 좌표
            if (challenger.Target.y != currentBest.Target.y) return challenger.Target.y < currentBest.Target.y; // 6순위: 목표 Y 좌표
            return challenger.Target.x < currentBest.Target.x; // 7순위: 목표 X 좌표
        }
    }
}
