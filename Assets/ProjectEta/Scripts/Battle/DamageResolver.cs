using UnityEngine; // Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public static class DamageResolver // 29일차: 모든 HP 차감을 한 곳에서 처리해 BeforeDamage/AfterDamage 훅을 보장하는 정적 클래스
    {
        public static int ApplyDamage(PieceRuntimeState target, int amount, PieceRuntimeState source = null, BattleHooks hooks = null) // 피해 1건을 훅과 함께 적용하는 메서드
        {
            if (target == null || amount <= 0) return 0; // 대상이 없거나 피해가 없으면 아무 것도 하지 않음

            var context = new DamageContext(target, source, amount); // 구독자가 조정할 수 있는 가변 컨텍스트 생성
            hooks?.RaiseBeforeDamage(context); // 실제 적용 전 통지(보호막 등이 여기서 Amount를 줄일 수 있음)

            int finalAmount = Mathf.Max(0, context.Amount); // 구독자가 음수로 만들어도 0 미만으로는 내려가지 않도록 보정
            if (finalAmount > 0) // 실제로 적용할 피해가 남아 있으면
            {
                target.CurrentHp -= finalAmount; // HP 차감(0 미만 방지는 PieceRuntimeState.CurrentHp가 처리)
            }

            hooks?.RaiseAfterDamage(target, source, finalAmount); // 실제 적용된 최종 피해량을 발생원과 함께 통지(32일차: 전투 피해와 상태 이상 틱 피해를 구분할 수 있게 발생원 포함)
            return finalAmount; // 호출부가 실제 적용량을 알 수 있도록 반환
        }
    }
}
