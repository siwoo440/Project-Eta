using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용

namespace ProjectEta.Meta
{
    public enum MetaUnlockType
    {
        Piece = 0, // 신규 기물·기물군 해금
        King = 1, // 신규 플레이어 킹 해금
        Passive = 2 // 신규 영구 패시브 해금
    }

    public sealed class MetaUnlockDefinition
    {
        public string UnlockId { get; } // 영구 해금 식별 ID
        public string DisplayName { get; } // UI 표시 이름
        public MetaUnlockType UnlockType { get; } // 해금 콘텐츠 종류
        public int Cost { get; } // 메타 토큰 비용

        public MetaUnlockDefinition(string unlockId, string displayName, MetaUnlockType unlockType, int cost)
        {
            UnlockId = unlockId ?? string.Empty; // 해금 ID 저장
            DisplayName = displayName ?? string.Empty; // 표시 이름 저장
            UnlockType = unlockType; // 해금 타입 저장
            Cost = cost < 0 ? 0 : cost; // 음수 비용 보정
        }
    }

    public static class MetaUnlockCatalog
    {
        private static readonly List<MetaUnlockDefinition> Definitions = new List<MetaUnlockDefinition>
        {
            new MetaUnlockDefinition("piece_unlock_01", "신규 기물 슬롯", MetaUnlockType.Piece, 30), // 향후 실제 기물 해금 연결용 슬롯
            new MetaUnlockDefinition("passive_unlock_01", "신규 패시브 슬롯", MetaUnlockType.Passive, 50), // 향후 실제 패시브 연결용 슬롯
            new MetaUnlockDefinition("king_attack", "공격형 킹", MetaUnlockType.King, 100), // 49일차 공격형 킹 해금 ID
            new MetaUnlockDefinition("king_defense", "방어형 킹", MetaUnlockType.King, 100), // 50일차 방어형 킹 해금 ID
            new MetaUnlockDefinition("king_strategy", "전략형 킹", MetaUnlockType.King, 100) // 50일차 전략형 킹 해금 ID
        }; // 48일차 프로토타입 영구 해금 목록

        public static IReadOnlyList<MetaUnlockDefinition> All => Definitions; // 프로토타입 영구 해금 목록 공개
    }
}
