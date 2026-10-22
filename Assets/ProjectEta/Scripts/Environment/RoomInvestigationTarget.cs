using UnityEngine; // 실제 Physics 조사 판정

namespace ProjectEta.Environment // 방 조사 대상
{ // 영역 시작
    public sealed class RoomInvestigationTarget : MonoBehaviour // 모델과 조사 데이터 연결
    { // 타입 시작
        public const float MinimumLookDot = 0.86f; // 중앙 시선 약 30도 허용
        private GameObject _focusMarker; // 금빛 조사 강조
        public RoomInvestigationDefinition Definition { get; private set; } // 고정 조사 데이터
        public Vector3 Anchor => transform.TransformPoint(new Vector3(0f, Mathf.Min(Definition.Size.y - 0.25f, 4.4f), Definition.Size.z * 0.5f - 0.04f)); // 충돌체 안쪽 조사 기준점
        public bool IsFocused => _focusMarker != null && _focusMarker.activeSelf; // 실제 강조 표시
        public void Configure(RoomInvestigationDefinition definition, GameObject marker) // 자동 모델 연결
        { // 메서드 시작
            Definition = definition; // 원본 정의 참조
            _focusMarker = marker; // 대상별 강조 참조
            SetFocused(false); // 초기 강조 해제
        } // 메서드 종료
        public void SetFocused(bool focused) // 조사 가능한 대상의 시각 안내
        { // 메서드 시작
            if (_focusMarker != null) _focusMarker.SetActive(focused); // 다른 모델 재질과 독립된 표시
        } // 메서드 종료
        public bool CanInspectFrom(Vector3 eye, Vector3 forward) // 거리·바라보기·가림 판정
        { // 메서드 시작
            if (!isActiveAndEnabled || Definition == null || forward.sqrMagnitude < 0.001f) return false; // 비활성·잘못된 시선 제외
            Vector3 offset = Anchor - eye; // 눈에서 조사 기준까지 거리
            float distance = offset.magnitude; // 실제 3차원 거리
            if (distance < 0.001f || distance > Definition.Reach || Vector3.Dot(forward.normalized, offset / distance) < MinimumLookDot) return false; // 멀거나 시선 밖인 대상 제외
            RaycastHit[] hits = Physics.RaycastAll(eye, offset / distance, distance + 0.02f, ~0, QueryTriggerInteraction.Ignore); // 보드 전용 제외 레이어도 실제 가림 검사
            float nearest = float.PositiveInfinity; // 가장 앞의 실제 장애물
            Collider first = null; // 시선을 막는 충돌체
            foreach (RaycastHit hit in hits) // 순서가 보장되지 않는 물리 결과
            { // 반복 시작
                if (hit.collider is CharacterController || hit.distance >= nearest) continue; // 플레이어 몸체와 뒤쪽 결과 제외
                nearest = hit.distance; // 앞쪽 거리 갱신
                first = hit.collider; // 앞쪽 장애물 갱신
            } // 반복 종료
            return first != null && first.GetComponentInParent<RoomInvestigationTarget>() == this; // 벽·테이블 뒤 조사 방지
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
