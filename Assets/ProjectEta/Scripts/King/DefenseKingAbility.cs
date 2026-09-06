using ProjectEta.Battle; // DamageContext 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용
using UnityEngine; // Vector2Int 사용

namespace ProjectEta.King
{
    public static class DefenseKingAbility
    {
        public static void BeginPlayerTurn(KingRunState kingState)
        {
            if (kingState == null) return; // 킹 상태 누락 방어
            kingState.BeginPlayerTurn(); // 새 플레이어 턴 이동 추적 초기화
        }

        public static bool HandleAfterMove(KingRunState kingState, PieceRuntimeState piece, Vector2Int origin, Vector2Int destination)
        {
            if (kingState == null || piece == null) return false; // 상태·기물 누락 차단
            if (origin == destination) return false; // 실제 이동이 없는 호출 제외
            if (!AttackKingAbility.IsPlayerKing(piece)) return false; // 플레이어 킹 이동 외 제외
            return kingState.MarkKingMoved(); // 방어형 킹 현재 턴 이동 기록
        }

        public static bool HandleEnemyTurnStarting(KingRunState kingState)
        {
            if (kingState == null) return false; // 킹 상태 누락 방어
            return kingState.TryGainBarrier(); // 이동하지 않은 방어형 킹 방벽 획득
        }

        public static bool ApplyBeforeDamage(KingRunState kingState, DamageContext context, out int reducedAmount)
        {
            reducedAmount = 0; // 기본 피해 감소량 초기화
            if (kingState == null || context == null) return false; // 상태·피해 컨텍스트 누락 차단
            if (kingState.Archetype != KingArchetype.Defense) return false; // 방어형 킹 외 방벽 차단
            if (!kingState.BarrierActive) return false; // 방벽이 없으면 피해 조정 없음
            if (!AttackKingAbility.IsPlayerKing(context.Target)) return false; // 플레이어 킹 피해 외 방벽 미소비
            if (context.Amount <= 0) return false; // 실제 피해가 없는 경우 방벽 유지

            int before = context.Amount; // 방벽 적용 전 피해 저장
            context.Amount = System.Math.Max(1, context.Amount - 1); // 다음 피해 1 감소·최소 피해 1 적용
            reducedAmount = before - context.Amount; // 실제 감소한 피해량 계산
            kingState.ConsumeBarrier(); // 피해를 받은 뒤 방벽 1회 소비
            return true; // 방벽 처리 성공 반환
        }
    }
}
