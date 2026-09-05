using UnityEngine; // MonoBehaviour·Renderer·Color 사용

namespace ProjectEta.Board // 보드 경로 지도 시각화 네임스페이스
{
    public sealed class RouteMapNodeView : MonoBehaviour // 체스판 위 선택 가능 스테이지 노드 표시
    {
        private Renderer _renderer; // 노드 마커 렌더러
        private Color _normalColor; // 기본 선택 가능 색상
        private Color _hoverColor; // 마우스 오버 색상
        private Color _selectedColor; // 선택 완료 색상
        private Color _dimmedColor; // 비선택 후보 색상
        private bool _hovered; // 현재 마우스 오버 여부
        private bool _selected; // 현재 선택 노드 여부
        private bool _dimmed; // 다른 노드 선택 후 흐리게 표시 여부

        public string NodeId { get; private set; } // 연결된 StageNode ID

        public void Initialize(string nodeId, Renderer targetRenderer, Color normalColor, Color hoverColor, Color selectedColor, Color dimmedColor) // 노드 마커 데이터 연결
        {
            NodeId = nodeId ?? string.Empty; // null 노드 ID 방지
            _renderer = targetRenderer; // 렌더러 참조 저장
            _normalColor = normalColor; // 기본 색 저장
            _hoverColor = hoverColor; // 오버 색 저장
            _selectedColor = selectedColor; // 선택 색 저장
            _dimmedColor = dimmedColor; // 흐림 색 저장
            ApplyColor(); // 초기 색상 반영
        }

        public void SetHovered(bool hovered) // 마우스 오버 상태 변경
        {
            _hovered = hovered; // 오버 상태 기록
            ApplyColor(); // 표시 색상 갱신
        }

        public void SetSelectionState(bool selected, bool dimmed) // 선택 완료 이후 노드 표시 상태 변경
        {
            _selected = selected; // 선택 여부 기록
            _dimmed = dimmed; // 흐림 여부 기록
            _hovered = false; // 선택 후 오버 표시 해제
            ApplyColor(); // 표시 색상 갱신
        }

        private void ApplyColor() // 현재 상태 우선순위에 맞춰 머티리얼 색상 적용
        {
            if (_renderer == null) return; // 렌더러 누락 방어
            if (_selected) _renderer.material.color = _selectedColor; // 선택 노드 색상 적용
            else if (_dimmed) _renderer.material.color = _dimmedColor; // 비선택 후보 흐림 적용
            else if (_hovered) _renderer.material.color = _hoverColor; // 마우스 오버 색상 적용
            else _renderer.material.color = _normalColor; // 기본 선택 가능 색상 적용
        }
    }
}
