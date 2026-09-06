namespace ProjectEta.King
{
    public sealed class KingSelectionPresentation
    {
        public KingArchetype Archetype { get; } // 표시 대상 King 타입
        public string DisplayName { get; } // King 표시 이름
        public string PassiveName { get; } // 패시브 이름
        public string PassiveDescription { get; } // 패시브 상세 설명
        public string PlayStyle { get; } // 플레이 스타일 요약
        public string RequiredUnlockId { get; } // 영구 해금 요구 ID

        public KingSelectionPresentation(
            KingArchetype archetype,
            string displayName,
            string passiveName,
            string passiveDescription,
            string playStyle,
            string requiredUnlockId)
        {
            Archetype = archetype; // King 타입 저장
            DisplayName = displayName ?? string.Empty; // 표시 이름 저장
            PassiveName = passiveName ?? string.Empty; // 패시브 이름 저장
            PassiveDescription = passiveDescription ?? string.Empty; // 패시브 설명 저장
            PlayStyle = playStyle ?? string.Empty; // 플레이 스타일 저장
            RequiredUnlockId = requiredUnlockId ?? string.Empty; // 영구 해금 ID 저장
        }
    }

    public static class KingSelectionPresentationCatalog
    {
        private static readonly KingSelectionPresentation Default = new KingSelectionPresentation(
            KingArchetype.Default,
            "기본 킹",
            "기본형",
            "별도의 특수 패시브 없이 기본 King 규칙으로 런을 시작합니다.",
            "균형형 · 기본 규칙 중심",
            string.Empty); // 기본 King 표시 정보

        private static readonly KingSelectionPresentation Attack = new KingSelectionPresentation(
            KingArchetype.Attack,
            "공격형 킹",
            "처형의 연쇄",
            "King이 직접 적을 처치하면 격노를 1 획득합니다. 다음 King 공격은 격노 수만큼 ATK가 증가하고 공격 후 격노를 모두 소비합니다. 격노는 최대 2입니다.",
            "공격적 · 직접 처치 중심 · 공격 강화",
            KingUnlockIds.Attack); // 공격형 King 표시 정보

        private static readonly KingSelectionPresentation Defense = new KingSelectionPresentation(
            KingArchetype.Defense,
            "방어형 킹",
            "왕의 요새",
            "플레이어 턴 동안 King이 이동하지 않았다면 턴 종료 시 방벽을 획득합니다. 방벽은 다음 피해를 1 줄이며 피해는 최소 1입니다.",
            "방어적 · 위치 유지 · 피해 완화",
            KingUnlockIds.Defense); // 방어형 King 표시 정보

        private static readonly KingSelectionPresentation Strategy = new KingSelectionPresentation(
            KingArchetype.Strategy,
            "전략형 킹",
            "전술적 준비",
            "배치 턴 시작 시 덱 위 3장을 확인하고 1장을 선택해 손패에 넣습니다. 나머지 2장은 덱 아래로 보냅니다.",
            "유연함 · 선택지 강화 · 배치 준비",
            KingUnlockIds.Strategy); // 전략형 King 표시 정보

        public static KingSelectionPresentation Get(KingArchetype archetype)
        {
            if (archetype == KingArchetype.Attack) return Attack; // 공격형 표시 정보 반환
            if (archetype == KingArchetype.Defense) return Defense; // 방어형 표시 정보 반환
            if (archetype == KingArchetype.Strategy) return Strategy; // 전략형 표시 정보 반환
            return Default; // 기본 King 표시 정보 반환
        }
    }
}
