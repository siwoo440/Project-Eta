using UnityEngine; // ScriptableObject, SerializeField 등을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Fusion // 합성 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "FusionRecipe", menuName = "ProjectEta/Fusion Recipe")] // 에디터 메뉴에서 에셋 생성 가능하게 등록
    public class FusionRecipe : ScriptableObject // 재료 2종을 결과 1종으로 합성하는 규칙을 담는 데이터 에셋
    {
        [SerializeField] private PieceDefinition _materialA; // 합성 재료 A
        [SerializeField] private PieceDefinition _materialB; // 합성 재료 B
        [SerializeField] private PieceDefinition _result; // 합성 결과 기물
        [SerializeField] private bool _isHiddenRecipe; // 숨김 레시피 여부

        public PieceDefinition MaterialA => _materialA; // 외부에서 읽는 재료 A
        public PieceDefinition MaterialB => _materialB; // 외부에서 읽는 재료 B
        public PieceDefinition Result => _result; // 외부에서 읽는 합성 결과
        public bool IsHiddenRecipe => _isHiddenRecipe; // 외부에서 읽는 숨김 여부
    }
}
