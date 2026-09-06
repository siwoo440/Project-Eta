using System.Reflection; // PieceDefinition private 스탯 복제 설정
using UnityEngine; // Object.Instantiate·Application 사용
using ProjectEta.Cards; // DeckState 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class RuntimeCardUpgradeService
    {
        private static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic; // 직렬화 private 필드 조회 플래그

        public static bool TryUpgradeOwnedCard(DeckState deckState, PieceDefinition source, out PieceDefinition upgradedCard)
        {
            upgradedCard = null; // 기본 실패 결과
            if (deckState == null || source == null) return false; // 필수 정보 누락 차단
            if (source.MovementType == PieceMovementType.King) return false; // 킹 강화 차단
            if (source.Category == PieceCategory.Monster || source.Category == PieceCategory.Boss) return false; // 몬스터·보스 강화 차단
            if (!ContainsReference(deckState, source)) return false; // 실제 보유 카드가 아니면 차단

            PieceDefinition clone = CreateRestoredCard(source, source.BaseHp + 1, source.BaseAtk + 1, $"{source.DisplayName} +1"); // 강화 스탯 런타임 복제 생성
            if (clone == null || ReferenceEquals(clone, source)) return false; // 강화 복제 실패 차단

            clone.name = $"{source.name}_RuntimeUpgrade"; // 런타임 복제 이름 설정

            if (!deckState.RemoveFromOwnedPool(source))
            {
                DestroyClone(clone); // 교체 실패 복제 제거
                return false; // 보유 풀 교체 실패 반환
            }

            deckState.AddToOwnedPool(clone); // 강화 카드 보유 풀 추가
            upgradedCard = clone; // 강화 결과 반환
            return true; // 강화 성공 반환
        }

        public static PieceDefinition CreateRestoredCard(PieceDefinition baseDefinition, int savedHp, int savedAtk, string savedDisplayName)
        {
            if (baseDefinition == null) return null; // 원본 정의 누락 차단

            int safeHp = savedHp > 0 ? savedHp : baseDefinition.BaseHp; // 잘못된 저장 HP 원본값 보정
            int safeAtk = savedAtk >= 0 ? savedAtk : baseDefinition.BaseAtk; // 잘못된 저장 ATK 원본값 보정
            string safeName = string.IsNullOrWhiteSpace(savedDisplayName) ? baseDefinition.DisplayName : savedDisplayName; // 빈 저장 이름 원본값 보정

            bool sameStats = safeHp == baseDefinition.BaseHp && safeAtk == baseDefinition.BaseAtk; // 원본 스탯 동일 여부 계산
            bool sameName = string.Equals(safeName, baseDefinition.DisplayName, System.StringComparison.Ordinal); // 원본 이름 동일 여부 계산
            if (sameStats && sameName) return baseDefinition; // 강화가 없는 카드면 원본 정의 그대로 사용

            PieceDefinition clone = Object.Instantiate(baseDefinition); // 원본 ScriptableObject 런타임 복제
            if (clone == null) return null; // 복제 실패 차단

            clone.name = $"{baseDefinition.name}_RuntimeRestored"; // 복원 런타임 복제 이름 설정
            clone.hideFlags = HideFlags.DontSave; // Unity 에셋 저장 대상 제외

            bool hpSet = SetField(clone, "_baseHp", safeHp); // 저장 HP 복원
            bool atkSet = SetField(clone, "_baseAtk", safeAtk); // 저장 ATK 복원
            bool nameSet = SetField(clone, "_displayName", safeName); // 저장 표시 이름 복원

            if (!hpSet || !atkSet || !nameSet)
            {
                DestroyClone(clone); // 불완전 복제 제거
                return null; // 복원 실패 반환
            }

            return clone; // 복원 카드 정의 반환
        }

        private static bool ContainsReference(DeckState deckState, PieceDefinition source)
        {
            for (int i = 0; i < deckState.OwnedCardPool.Count; i++)
            {
                if (ReferenceEquals(deckState.OwnedCardPool[i], source)) return true; // 실제 보유 카드 참조 확인
            }

            return false; // 동일 참조 없음
        }

        private static bool SetField<T>(PieceDefinition target, string fieldName, T value)
        {
            FieldInfo field = typeof(PieceDefinition).GetField(fieldName, FieldFlags); // 대상 직렬화 필드 조회
            if (field == null) return false; // 필드 누락 실패
            field.SetValue(target, value); // 런타임 복제 값 적용
            return true; // 필드 설정 성공 반환
        }

        private static void DestroyClone(PieceDefinition clone)
        {
            if (clone == null) return; // 빈 복제 제거 차단
            if (Application.isPlaying) Object.Destroy(clone); // 플레이 모드 지연 제거
            else Object.DestroyImmediate(clone); // EditMode 즉시 제거
        }
    }
}
