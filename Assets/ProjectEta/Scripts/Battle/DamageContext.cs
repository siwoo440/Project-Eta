using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class DamageContext // 29일차: BeforeDamage 구독자가 최종 피해량을 조정할 수 있도록 담는 가변 컨텍스트
    {
        public PieceRuntimeState Target { get; } // 피해를 받는 기물
        public PieceRuntimeState Source { get; } // 피해를 준 기물(없으면 null, 예: 상태 이상 틱 피해)
        public int Amount { get; set; } // 최종 적용될 피해량(구독자가 줄이거나 늘릴 수 있음)

        public DamageContext(PieceRuntimeState target, PieceRuntimeState source, int amount) // 최초 피해량으로 컨텍스트를 구성하는 생성자
        {
            Target = target; // 대상 저장
            Source = source; // 발생원 저장(없을 수 있음)
            Amount = amount; // 초기 피해량 저장
        }
    }
}
