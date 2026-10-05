using UnityEngine; // ScriptableObject·SerializeField 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Fusion
{
    [CreateAssetMenu(fileName = "FusionRecipe", menuName = "ProjectEta/Fusion Recipe")]
    public class FusionRecipe : ScriptableObject
    {
        [SerializeField] private string _recipeId; // 레시피 고유 식별자
        [SerializeField] private PieceDefinition _materialA; // 합성 재료 A
        [SerializeField] private PieceDefinition _materialB; // 합성 재료 B
        [SerializeField] private PieceDefinition _result; // 합성 결과 기물
        [SerializeField] private bool _isHiddenRecipe; // 숨김 레시피 여부
        [SerializeField] private bool _ignoresGradeStepRule; // 등급 상승 규칙 예외 여부
        [SerializeField] private bool _usesOrderedMaterials; // 87일차: A/B 선택 순서가 결과를 구분하는 레시피 여부

        public PieceDefinition MaterialA => _materialA;
        public PieceDefinition MaterialB => _materialB;
        public PieceDefinition Result => _result;
        public bool IsHiddenRecipe => _isHiddenRecipe;
        public bool IgnoresGradeStepRule => _ignoresGradeStepRule;
        public bool UsesOrderedMaterials => _usesOrderedMaterials;
        public bool UsesIdenticalMaterials => _materialA != null && _materialA == _materialB;
        public string RecipeId => string.IsNullOrEmpty(_recipeId) ? name : _recipeId;
    }
}
