using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Battle
{
    public class DamageContext
    {
        public PieceRuntimeState OriginalTarget { get; } // 피해 이벤트가 처음 향했던 대상
        public PieceRuntimeState Target { get; private set; } // RedirectDamage 적용 후 실제 피해 대상
        public PieceRuntimeState Source { get; } // 피해 발생원
        public int OriginalAmount { get; } // Ability·훅 적용 전 최초 피해량
        public int Amount { get; set; } // 최종 적용 예정 피해량
        public int RedirectCount { get; private set; } // 한 피해 이벤트의 Redirect 적용 횟수

        public DamageContext(PieceRuntimeState target, PieceRuntimeState source, int amount)
        {
            OriginalTarget = target;
            Target = target;
            Source = source;
            OriginalAmount = amount;
            Amount = amount;
            RedirectCount = 0;
        }

        public bool TryRedirect(PieceRuntimeState newTarget)
        {
            if (newTarget == null || RedirectCount > 0 || object.ReferenceEquals(newTarget, Target)) return false;

            Target = newTarget;
            RedirectCount = 1; // 90일차: 한 피해 이벤트에서 Redirect는 최대 한 번
            return true;
        }
    }
}
