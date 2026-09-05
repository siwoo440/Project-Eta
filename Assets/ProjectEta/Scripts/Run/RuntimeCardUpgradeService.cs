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
            if (deckState == null || source == null) return false;
            if (source.MovementType == PieceMovementType.King) return false;
            if (source.Category == PieceCategory.Monster || source.Category == PieceCategory.Boss) return false;
            if (!ContainsReference(deckState, source)) return false;

            PieceDefinition clone = Object.Instantiate(source); // 원본 ScriptableObject 런타임 복제
            if (clone == null) return false;

            clone.name = $"{source.name}_RuntimeUpgrade"; // 런타임 복제 이름 설정
            clone.hideFlags = HideFlags.DontSave; // 에셋 저장 대상 제외

            bool hpSet = SetField(clone, "_baseHp", source.BaseHp + 1); // HP +1 적용
            bool atkSet = SetField(clone, "_baseAtk", source.BaseAtk + 1); // ATK +1 적용
            bool nameSet = SetField(clone, "_displayName", $"{source.DisplayName} +1"); // 강화 이름 적용

            if (!hpSet || !atkSet || !nameSet)
            {
                DestroyClone(clone); // 불완전 복제 제거
                return false;
            }

            if (!deckState.RemoveFromOwnedPool(source))
            {
                DestroyClone(clone); // 교체 실패 복제 제거
                return false;
            }

            deckState.AddToOwnedPool(clone); // 강화 카드 보유 풀 추가
            upgradedCard = clone; // 강화 결과 반환
            return true;
        }

        private static bool ContainsReference(DeckState deckState, PieceDefinition source)
        {
            for (int i = 0; i < deckState.OwnedCardPool.Count; i++)
            {
                if (ReferenceEquals(deckState.OwnedCardPool[i], source)) return true; // 실제 보유 카드 참조 확인
            }

            return false;
        }

        private static bool SetField<T>(PieceDefinition target, string fieldName, T value)
        {
            FieldInfo field = typeof(PieceDefinition).GetField(fieldName, FieldFlags); // 대상 직렬화 필드 조회
            if (field == null) return false;
            field.SetValue(target, value); // 런타임 복제 값 적용
            return true;
        }

        private static void DestroyClone(PieceDefinition clone)
        {
            if (clone == null) return;
            if (Application.isPlaying) Object.Destroy(clone); // 플레이 모드 지연 제거
            else Object.DestroyImmediate(clone); // EditMode 즉시 제거
        }
    }
}
