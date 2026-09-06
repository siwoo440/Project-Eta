using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용

namespace ProjectEta.Meta
{
    public enum MetaProgressCategory
    {
        All = 0, // 전체 해금 목록
        Piece = 1, // 기물 해금 목록
        King = 2, // 킹 해금 목록
        Passive = 3 // 패시브 해금 목록
    }

    public enum MetaUnlockDisplayState
    {
        Locked = 0, // 향후 조건 잠금 상태
        Available = 1, // 현재 토큰으로 해금 가능
        Insufficient = 2, // 토큰 부족 상태
        Unlocked = 3 // 영구 해금 완료 상태
    }

    public sealed class MetaProgressPanelState
    {
        private readonly List<MetaUnlockDefinition> _filteredDefinitions = new List<MetaUnlockDefinition>(); // 필터 결과 재사용 목록

        public MetaProgressCategory CurrentCategory { get; private set; } = MetaProgressCategory.All; // 현재 선택 카테고리
        public string SelectedUnlockId { get; private set; } = string.Empty; // 현재 선택 해금 ID

        public void ShowCategory(MetaProgressCategory category)
        {
            CurrentCategory = category; // 현재 카테고리 변경
            SelectedUnlockId = string.Empty; // 카테고리 전환 시 선택 초기화
        }

        public IReadOnlyList<MetaUnlockDefinition> GetFilteredDefinitions()
        {
            _filteredDefinitions.Clear(); // 이전 필터 결과 정리
            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 전체 해금 카탈로그 조회

            for (int i = 0; i < definitions.Count; i++)
            {
                MetaUnlockDefinition definition = definitions[i]; // 현재 해금 정의 조회
                if (!MatchesCategory(definition, CurrentCategory)) continue; // 비대상 카테고리 제외
                _filteredDefinitions.Add(definition); // 필터 결과 등록
            }

            return _filteredDefinitions; // 현재 필터 결과 반환
        }

        public void Select(MetaUnlockDefinition definition)
        {
            SelectedUnlockId = definition != null ? definition.UnlockId : string.Empty; // 선택 해금 ID 저장
        }

        public MetaUnlockDefinition GetSelectedDefinition()
        {
            if (string.IsNullOrWhiteSpace(SelectedUnlockId)) return null; // 선택 없음 처리
            IReadOnlyList<MetaUnlockDefinition> definitions = MetaUnlockCatalog.All; // 전체 해금 카탈로그 조회

            for (int i = 0; i < definitions.Count; i++)
            {
                MetaUnlockDefinition definition = definitions[i]; // 현재 해금 정의 조회
                if (definition.UnlockId == SelectedUnlockId) return definition; // 선택 ID 일치 정의 반환
            }

            return null; // 선택 정의 누락 반환
        }

        public static MetaUnlockDisplayState Evaluate(MetaProgressState progress, MetaUnlockDefinition definition)
        {
            if (progress == null || definition == null) return MetaUnlockDisplayState.Locked; // 잘못된 상태 잠금 처리
            if (progress.IsUnlocked(definition.UnlockType, definition.UnlockId)) return MetaUnlockDisplayState.Unlocked; // 영구 해금 완료 상태 반환
            if (MetaUnlockService.CanUnlock(progress, definition)) return MetaUnlockDisplayState.Available; // 현재 해금 가능 상태 반환
            return MetaUnlockDisplayState.Insufficient; // 기본 토큰 부족 상태 반환
        }

        private static bool MatchesCategory(MetaUnlockDefinition definition, MetaProgressCategory category)
        {
            if (definition == null) return false; // 빈 정의 제외
            if (category == MetaProgressCategory.All) return true; // 전체 카테고리 허용
            if (category == MetaProgressCategory.Piece) return definition.UnlockType == MetaUnlockType.Piece; // 기물 카테고리 판정
            if (category == MetaProgressCategory.King) return definition.UnlockType == MetaUnlockType.King; // 킹 카테고리 판정
            if (category == MetaProgressCategory.Passive) return definition.UnlockType == MetaUnlockType.Passive; // 패시브 카테고리 판정
            return false; // 예외 카테고리 제외
        }
    }
}
