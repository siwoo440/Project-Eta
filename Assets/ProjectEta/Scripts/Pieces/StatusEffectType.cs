using System; // [Flags] 속성을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [Flags] // 면역 태그처럼 여러 상태를 비트 조합으로 다루기 위한 속성
    public enum StatusEffectType // 27일차: 상태 이상 종류를 나타내는 비트 플래그 열거형
    {
        None = 0, // 상태 없음
        Poison = 1 << 0, // 독
        Burn = 1 << 1, // 화상
        Stun = 1 << 2, // 기절
        Root = 1 << 3 // 속박
    }
}
