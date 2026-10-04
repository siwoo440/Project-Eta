using System.Collections.Generic; // IReadOnlyList과 List 사용

namespace ProjectEta.Meta
{
    public enum MetaUnlockType
    {
        Piece = 0, // 신규 기물 해금
        King = 1, // 신규 플레이어 킹 해금
        Passive = 2 // 신규 영구 패시브 해금
    }

    public static class PieceUnlockIds
    {
        public const string Spearman = "piece_spearman"; // 창병 해금 ID
        public const string Shooter = "piece_shooter"; // 사수 해금 ID
        public const string ShieldGuard = "piece_shield_guard"; // 방패병 해금 ID
        public const string FlagBearer = "piece_flag_bearer"; // 깃발병 해금 ID
        public const string Pursuer = "piece_pursuer"; // 추격병 해금 ID
        public const string Scout = "piece_scout"; // 척후병 해금 ID
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
            new MetaUnlockDefinition(PieceUnlockIds.Spearman, "창병", MetaUnlockType.Piece, 30, "상하좌우 1칸 이동과 직선 최대 2칸 공격을 사용하는 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition(PieceUnlockIds.Shooter, "사수", MetaUnlockType.Piece, 30, "전방 대각선 두 방향으로 이동하는 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition(PieceUnlockIds.ShieldGuard, "방패병", MetaUnlockType.Piece, 30, "좌우와 후방으로 1칸 이동하는 방어형 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition(PieceUnlockIds.FlagBearer, "깃발병", MetaUnlockType.Piece, 30, "전방과 좌우로 1칸 이동하는 지원형 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition(PieceUnlockIds.Pursuer, "추격병", MetaUnlockType.Piece, 30, "전방 1칸과 후방 대각선 이동을 사용하는 공격형 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition(PieceUnlockIds.Scout, "척후병", MetaUnlockType.Piece, 30, "전방 대각선과 좌우 1칸 이동을 사용하는 1성 기물을 영구 해금합니다."),
            new MetaUnlockDefinition("passive_unlock_01", "신규 패시브 슬롯", MetaUnlockType.Passive, 50, "향후 추가되는 영구 패시브를 사용할 수 있도록 준비하는 해금 슬롯입니다."),
            new MetaUnlockDefinition("king_attack", "공격형 킹", MetaUnlockType.King, 100, "직접 처치로 분노를 쌓고 다음 킹 공격을 강화하는 공격형 킹을 영구 해금합니다."),
            new MetaUnlockDefinition("king_defense", "방어형 킹", MetaUnlockType.King, 100, "이동하지 않은 턴에 방어막을 준비해 다음 피해를 줄이는 방어형 킹을 영구 해금합니다."),
            new MetaUnlockDefinition("king_strategy", "전략형 킹", MetaUnlockType.King, 100, "배치 시작 시 덱 상단 후보를 확인하고 원하는 카드를 선택하는 전략형 킹을 영구 해금합니다.")
        };

        public static IReadOnlyList<MetaUnlockDefinition> All => Definitions; // 영구 해금 목록 공개
    }
}
