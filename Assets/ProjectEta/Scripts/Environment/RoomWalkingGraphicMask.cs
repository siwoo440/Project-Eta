using UnityEngine; // 실제 UI 정점과 클릭 위치
using UnityEngine.UI; // 기존 UI와 독립된 정점 효과

namespace ProjectEta.Environment // 걷기 화면 표시 영역
{ // 영역 시작
    public sealed class RoomWalkingGraphicMask : BaseMeshEffect, ICanvasRaycastFilter // 원본 표시 상태를 보존하는 걷기 마스크
    { // 타입 시작
        public override void ModifyMesh(VertexHelper vertices) // 최종 UI 정점 표시 억제
        { // 메서드 시작
            if (!IsActive()) return; // 좌석 복귀 후 원래 정점 보존
            UIVertex vertex = UIVertex.simpleVert; // 현재 표시 정점 임시 보관
            for (int index = 0; index < vertices.currentVertCount; index++) // 실제 화면 정점 순회
            { // 반복 시작
                vertices.PopulateUIVertex(ref vertex, index); // 원래 표시 정점 조회
                vertex.color.a = 0; // 원본 Graphic 색상과 별개 투명도 억제
                vertices.SetUIVertex(vertex, index); // 숨긴 최종 정점 적용
            } // 반복 종료
        } // 메서드 종료
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) // 원본 클릭 설정과 독립된 화면 통과
        { // 메서드 시작
            return !isActiveAndEnabled; // 걷기 중 숨긴 화면 클릭 제외
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
