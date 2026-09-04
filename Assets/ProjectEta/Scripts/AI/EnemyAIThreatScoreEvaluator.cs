using ProjectEta.Battle; // CombatMovementPolicy를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAIThreatScoreEvaluator // 행동 후 위치가 플레이어 공격에 얼마나 노출되는지 점수로 변환하는 35일차 평가기
    {
        private const int ThreatPenaltyPerAttacker = 120; // 최종 위치를 위협하는 플레이어 기물 1개당 감점
        private const int EscapeBonusPerReducedThreat = 40; // 현재보다 위협 수를 1 줄일 때 받는 작은 생존 보너스

        public static int Evaluate(AIActionCandidate candidate, BoardState board, EnemyAIThreatMap threatMap) // 후보 행동의 위협 점수를 계산하는 메서드
        {
            if (candidate == null || candidate.Actor == null || board == null || threatMap == null) return 0; // 필수 데이터가 없으면 위협 보정 없음

            var resultingPosition = ResolveResultingPosition(candidate); // 행동이 끝난 뒤 공격자가 있을 좌표 계산
            int currentThreat = threatMap.GetThreatCount(candidate.Origin); // 행동 전 현재 위치의 위협 수
            int resultingThreat = threatMap.GetThreatCount(resultingPosition); // 행동 후 위치의 위협 수

            int score = -resultingThreat * ThreatPenaltyPerAttacker; // 최종 위험도에 비례해 감점

            if (resultingThreat < currentThreat) // 현재보다 안전한 칸으로 빠져나가는 행동이면
            {
                score += (currentThreat - resultingThreat) * EscapeBonusPerReducedThreat; // 줄어든 위협 수만큼 추가 생존 보너스
            }

            return score; // 최종 위협 점수 반환
        }

        private static UnityEngine.Vector2Int ResolveResultingPosition(AIActionCandidate candidate) // 이동·공격 후 실제 공격자 위치를 예측하는 메서드
        {
            if (candidate.ActionType == AIActionType.Move) return candidate.Target; // 이동은 목표 칸이 최종 위치
            if (candidate.ActionType != AIActionType.Attack) return candidate.Origin; // 알 수 없는 행동은 원위치 유지

            var targetPiece = candidate.TargetPiece; // 공격 대상 참조
            if (targetPiece == null || candidate.Actor.Definition == null) return candidate.Origin; // 대상이나 정의가 없으면 원위치로 안전 처리

            bool lethal = candidate.Actor.Definition.BaseAtk >= targetPiece.CurrentHp; // 현재 고정 ATK 규칙에서 처치 가능한지 계산
            if (!lethal) return candidate.Origin; // 비치명 근접 공격은 기존 규칙대로 원위치 복귀

            bool occupyAfterKill = CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(candidate.Actor.Definition); // 원거리/근접 처치 후 점유 정책 확인
            return occupyAfterKill ? candidate.Target : candidate.Origin; // 근접 처치면 대상 칸, 원거리 처치면 원위치
        }
    }
}
