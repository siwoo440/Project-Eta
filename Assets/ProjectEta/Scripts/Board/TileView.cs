using UnityEngine; // MonoBehaviour, Renderer 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class TileView : MonoBehaviour // 타일 하나의 시각적 표시와 선택 상태를 담당하는 컴포넌트
    {
        public TileState TileState { get; private set; } // 이 뷰가 표시하는 타일 데이터
        public bool IsSelected { get; private set; } // 현재 선택된 상태인지 여부

        private Renderer _renderer; // 색상을 바꿀 렌더러 참조
        private Material _idleMaterial; // 선택되지 않았을 때 쓸 머티리얼
        private Material _highlightMaterial; // 선택됐을 때 쓸 머티리얼

        public void Initialize(TileState tileState, Material idleMaterial, Material highlightMaterial) // 외부에서 데이터를 주입해 초기화하는 메서드
        {
            TileState = tileState; // 타일 데이터 저장
            _idleMaterial = idleMaterial; // 기본 머티리얼 저장
            _highlightMaterial = highlightMaterial; // 강조 머티리얼 저장
            _renderer = GetComponent<Renderer>(); // 같은 오브젝트의 렌더러 캐싱
            _renderer.sharedMaterial = _idleMaterial; // 시작 상태는 기본 머티리얼로 표시
        }

        public void Select() // 타일을 선택 상태로 만드는 메서드
        {
            IsSelected = true; // 선택 상태로 표시
            _renderer.sharedMaterial = _highlightMaterial; // 강조 머티리얼로 교체
        }

        public void Deselect() // 타일 선택을 해제하는 메서드
        {
            IsSelected = false; // 선택 해제 상태로 표시
            _renderer.sharedMaterial = _idleMaterial; // 기본 머티리얼로 복원
        }
    }
}
