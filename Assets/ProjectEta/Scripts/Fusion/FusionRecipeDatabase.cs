using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, SerializeField 등을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Fusion // 합성 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "FusionRecipeDatabase", menuName = "ProjectEta/Fusion Recipe Database")] // 에디터 메뉴에서 에셋 생성 가능하게 등록
    public class FusionRecipeDatabase : ScriptableObject // 재료 2장으로 일치하는 FusionRecipe를 찾아주는 조회용 데이터 에셋
    {
        [SerializeField] private List<FusionRecipe> _recipes = new List<FusionRecipe>(); // 등록된 합성 레시피 목록

        public bool TryFindRecipe(PieceDefinition materialA, PieceDefinition materialB, out FusionRecipe recipe) // 재료 2장(순서 무관)으로 일치하는 레시피를 찾는 메서드
        {
            for (int i = 0; i < _recipes.Count; i++) // 등록된 레시피를 처음부터 순회
            {
                var candidate = _recipes[i]; // 이번 순회의 레시피 후보
                if (candidate == null) continue; // 비어있는 항목은 건너뜀

                bool matchesInOrder = candidate.MaterialA == materialA && candidate.MaterialB == materialB; // 등록된 순서 그대로 일치하는지 확인
                bool matchesReversed = candidate.MaterialA == materialB && candidate.MaterialB == materialA; // 반대 순서로 일치하는지 확인(재료 선택 순서 무관 요구사항)
                if (matchesInOrder || matchesReversed) // 둘 중 하나라도 일치하면
                {
                    recipe = candidate; // 찾은 레시피 반환값에 저장
                    return true; // 매칭 성공 반환
                }
            }

            recipe = null; // 끝까지 찾지 못하면 결과 없음
            return false; // 매칭 실패 반환
        }
    }
}
