using System; // [Flags] 속성을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [Flags] // 여러 태그를 비트 조합으로 동시에 가질 수 있게 하는 속성
    public enum PieceRoleTag // 기물의 역할을 나타내는 비트 플래그 열거형
    {
        None = 0, // 역할 없음
        Melee = 1 << 0, // 근접 역할
        Ranged = 1 << 1, // 원거리 역할
        Jumper = 1 << 2, // 도약 역할
        Slider = 1 << 3, // 슬라이드(직선/대각선 이동) 역할
        Rider = 1 << 4, // 라이더(합성 이동) 역할
        Support = 1 << 5, // 지원 역할
        Tanker = 1 << 6, // 탱커 역할
        Attacker = 1 << 7, // 공격 역할
        Summoner = 1 << 8 // 소환 역할
    }
}
