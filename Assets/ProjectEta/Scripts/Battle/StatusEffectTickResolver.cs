using ProjectEta.Pieces; // PieceRuntimeState, StatusEffectType을 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public static class StatusEffectTickResolver // 28일차: 턴 종료 시 독·화상처럼 피해를 주는 상태 이상을 정산하는 정적 클래스
    {
        public static int ResolveTurnEndDamage(PieceRuntimeState piece, BattleHooks hooks = null) // 기물 1개의 이번 턴 종료 피해를 계산해 적용하는 메서드(29일차: 훅을 통한 피해 적용으로 전환)
        {
            if (piece == null) return 0; // 대상이 없으면 처리할 피해 없음

            int totalDamage = 0; // 이번 턴에 합산할 피해량

            foreach (var effect in piece.StatusEffects) // 현재 걸려 있는 모든 상태 이상을 순회
            {
                var statusType = effect.Definition.StatusType; // 이번 상태의 종류
                if (statusType == StatusEffectType.Poison || statusType == StatusEffectType.Burn) // 피해를 주는 상태(독·화상)만 대상
                {
                    totalDamage += effect.Definition.TickDamagePerStack * effect.StackCount; // 중첩 수만큼 피해 누적(임시값: 기본 1)
                }
            }

            return DamageResolver.ApplyDamage(piece, totalDamage, null, hooks); // BeforeDamage/AfterDamage 훅과 함께 합산 피해를 한 번에 적용(공격자가 없으므로 source는 null)
        }
    }
}
