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
        public string Description { get; } // UI 상세 설명
        public MetaUnlockType UnlockType { get; } // 해금 콘텐츠 종류
        public int Cost { get; } // 메타 토큰 비용

        public MetaUnlockDefinition(string unlockId, string displayName, MetaUnlockType unlockType, int cost, string description = "")
        {
            UnlockId = unlockId ?? string.Empty; // 해금 ID 저장
            DisplayName = displayName ?? string.Empty; // 표시 이름 저장
            Description = description ?? string.Empty; // 상세 설명 저장
            UnlockType = unlockType; // 해금 타입 저장
            Cost = cost < 0 ? 0 : cost; // 음수 비용 보정
        }
    }

    public static class MetaUnlockCatalog
    {
        private static readonly List<MetaUnlockDefinition> Definitions = new List<MetaUnlockDefinition>
        {
            new MetaUnlockDefinition("piece_unlock_01", "신규 기물 슬롯", MetaUnlockType.Piece, 30, "향후 추가되는 신규 기물을 런 획득 후보에 연결하기 위한 영구 해금 슬롯입니다."), // 기물 해금 슬롯 정의
            new MetaUnlockDefinition("passive_unlock_01", "신규 패시브 슬롯", MetaUnlockType.Passive, 50, "향후 추가되는 영구 패시브를 사용할 수 있도록 준비하는 해금 슬롯입니다."), // 패시브 해금 슬롯 정의
            new MetaUnlockDefinition("king_attack", "공격형 킹", MetaUnlockType.King, 100, "직접 처치로 분노를 쌓고 다음 킹 공격을 강화하는 공격형 킹을 영구 해금합니다."), // 공격형 킹 정의
            new MetaUnlockDefinition("king_defense", "방어형 킹", MetaUnlockType.King, 100, "이동하지 않은 턴에 방어막을 준비해 다음 피해를 줄이는 방어형 킹을 영구 해금합니다."), // 방어형 킹 정의
            new MetaUnlockDefinition("king_strategy", "전략형 킹", MetaUnlockType.King, 100, "배치 시작 시 덱 상단 후보를 확인하고 원하는 카드를 선택하는 전략형 킹을 영구 해금합니다.") // 전략형 킹 정의
        }; // 영구 해금 카탈로그

        public static IReadOnlyList<MetaUnlockDefinition> All => Definitions; // 영구 해금 목록 공개
    }
}
