using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 관련 타입을 모아두는 네임스페이스
{
    public class CombatResult // 한 번의 공격 판정 결과를 담는 클래스
    {
        public PieceRuntimeState Attacker { get; } // 공격을 실행한 기물
        public PieceRuntimeState Defender { get; } // 공격을 받은 기물
        public int DamageDealt { get; } // 이번 공격으로 실제 감소한 체력
        public bool DefenderDied { get; } // 이 공격으로 대상이 사망했는지 여부

        public CombatResult(PieceRuntimeState attacker, PieceRuntimeState defender, int damageDealt, bool defenderDied) // 판정 결과를 한 번에 구성하는 생성자
        {
            Attacker = attacker; // 공격자 저장
            Defender = defender; // 대상 저장
            DamageDealt = damageDealt; // 실제 피해량 저장
            DefenderDied = defenderDied; // 사망 여부 저장
        }
    }
}
