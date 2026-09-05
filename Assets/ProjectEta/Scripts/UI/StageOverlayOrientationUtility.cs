using UnityEngine; // Transform·Vector3·Quaternion 사용

namespace ProjectEta.UI
{
    public static class StageOverlayOrientationUtility
    {
        public static Quaternion ResolveCanvasWorldRotation(Transform boardTransform, Vector3 cameraPosition, Vector3 canvasPosition, float maxTiltDegrees = 28f)
        {
            Vector3 boardNormal = boardTransform != null ? boardTransform.up.normalized : Vector3.up; // 보드 표면 위쪽 법선 계산
            Vector3 toCamera = cameraPosition - canvasPosition; // Canvas 회전축에서 카메라 방향 계산

            if (Vector3.Dot(boardNormal, toCamera) < 0f) boardNormal = -boardNormal; // 카메라가 있는 쪽 법선 선택
            if (toCamera.sqrMagnitude <= 0.0001f) toCamera = boardNormal; // 카메라와 위치가 겹친 경우 기본 법선 사용

            Vector3 cameraDirection = toCamera.normalized; // 카메라 방향 정규화
            float safeTiltDegrees = Mathf.Clamp(maxTiltDegrees, 0f, 80f); // 지나친 수직 회전 방지
            Vector3 canvasFront = Vector3.RotateTowards(boardNormal, cameraDirection, safeTiltDegrees * Mathf.Deg2Rad, 0f).normalized; // 보드 법선에서 카메라 쪽으로 제한 각도만 세움

            Vector3 planarToCamera = Vector3.ProjectOnPlane(cameraDirection, boardNormal); // 보드 평면 위 플레이어 방향 계산
            if (planarToCamera.sqrMagnitude <= 0.0001f)
            {
                Vector3 fallbackForward = boardTransform != null ? boardTransform.forward : Vector3.forward; // 정면 카메라용 보드 전방 대체값
                planarToCamera = Vector3.ProjectOnPlane(-fallbackForward, boardNormal); // 플레이어 쪽 평면 방향 구성
            }

            if (planarToCamera.sqrMagnitude <= 0.0001f) planarToCamera = Vector3.back; // 최종 영벡터 방어
            planarToCamera.Normalize(); // 평면 플레이어 방향 정규화

            Vector3 textTopDirection = Vector3.ProjectOnPlane(-planarToCamera, canvasFront); // 기울어진 Canvas 표면에서 제목이 보드 안쪽을 향하도록 계산
            if (textTopDirection.sqrMagnitude <= 0.0001f) textTopDirection = Vector3.ProjectOnPlane(Vector3.forward, canvasFront); // 수직 특수 상황 대체 방향
            if (textTopDirection.sqrMagnitude <= 0.0001f) textTopDirection = Vector3.up; // 최종 글자 윗방향 방어
            textTopDirection.Normalize(); // 글자 윗방향 정규화

            Vector3 canvasPositiveZ = -canvasFront; // Unity World Space UI 앞면은 로컬 -Z이므로 +Z를 반대 방향으로 지정
            return Quaternion.LookRotation(canvasPositiveZ, textTopDirection); // 앞면은 플레이어, 제목은 보드 안쪽을 향하는 회전 반환
        }

        public static Vector3 ResolveCanvasAnchorWorldPosition(Transform boardTransform, Vector3 cameraPosition, float tileSize, float nearEdgeOffsetTiles, float baseHeightTiles)
        {
            Vector3 boardCenter = boardTransform != null ? boardTransform.position : Vector3.zero; // 보드 중앙 월드 위치 계산
            Vector3 boardNormal = boardTransform != null ? boardTransform.up.normalized : Vector3.up; // 보드 표면 법선 계산
            Vector3 boardRight = boardTransform != null ? boardTransform.right.normalized : Vector3.right; // 보드 오른쪽 축 계산
            Vector3 toCamera = cameraPosition - boardCenter; // 보드 중앙에서 플레이어 방향 계산
            Vector3 planarToCamera = Vector3.ProjectOnPlane(toCamera, boardNormal); // 보드 평면 위 플레이어 방향 계산

            if (planarToCamera.sqrMagnitude <= 0.0001f)
            {
                Vector3 fallbackForward = boardTransform != null ? -boardTransform.forward : Vector3.back; // 정면 카메라용 기본 플레이어 방향
                planarToCamera = Vector3.ProjectOnPlane(fallbackForward, boardNormal); // 보드 평면 대체 방향 계산
            }

            if (planarToCamera.sqrMagnitude <= 0.0001f) planarToCamera = Vector3.Cross(boardNormal, boardRight); // 마지막 평면 방향 구성
            planarToCamera.Normalize(); // 플레이어 방향 정규화

            float safeTileSize = Mathf.Max(0.1f, tileSize); // 잘못된 타일 크기 보정
            float nearOffset = Mathf.Max(0f, nearEdgeOffsetTiles) * safeTileSize; // 플레이어 쪽 하단 회전축 이동 거리 계산
            float baseHeight = Mathf.Max(0.02f, baseHeightTiles) * safeTileSize; // 돗자리 위 기본 높이 계산
            return boardCenter + planarToCamera * nearOffset + boardNormal * baseHeight; // 플레이어 쪽 돗자리 가장자리의 Canvas 하단 위치 반환
        }
    }
}
