using NUnit.Framework; // EditMode Assert 사용
using UnityEngine; // GameObject·Vector3·Quaternion 사용
using ProjectEta.UI; // StageOverlayOrientationUtility 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day47StageOverlayOrientationTests
    {
        [Test]
        public void ResolveCanvasWorldRotation_TiltsTowardCameraWithoutBecomingFullBillboard()
        {
            var boardObject = new GameObject("Board"); // 기준 보드 생성
            Vector3 cameraPosition = new Vector3(0f, 9f, -9f); // Battle 씬 기본 플레이어 카메라 위치 사용
            const float requestedTilt = 28f; // 플레이어 친화 기울기 기준

            Quaternion rotation = StageOverlayOrientationUtility.ResolveCanvasWorldRotation(boardObject.transform, cameraPosition, Vector3.zero, requestedTilt); // 판 위 Canvas 회전 계산
            Vector3 canvasFront = rotation * Vector3.back; // Unity UI 앞면 로컬 -Z를 월드 방향으로 변환
            float actualTilt = Vector3.Angle(boardObject.transform.up, canvasFront); // 수평 보드 법선에서 실제 세워진 각도 계산
            float cameraAngle = Vector3.Angle(boardObject.transform.up, cameraPosition.normalized); // 원래 보드와 카메라 사이 각도 계산

            Assert.That(actualTilt, Is.EqualTo(requestedTilt).Within(0.5f)); // 지정한 28도만큼 세워졌는지 검증
            Assert.Less(Vector3.Angle(canvasFront, cameraPosition.normalized), cameraAngle); // 기존 수평보다 플레이어를 더 정면으로 보는지 검증
            Object.DestroyImmediate(boardObject); // 테스트 객체 정리
        }

        [Test]
        public void ResolveCanvasWorldRotation_TextTopPointsAwayFromPlayer()
        {
            var boardObject = new GameObject("Board"); // 기준 보드 생성
            Vector3 cameraPosition = new Vector3(0f, 9f, -9f); // Battle 씬 기본 플레이어 카메라 위치 사용

            Quaternion rotation = StageOverlayOrientationUtility.ResolveCanvasWorldRotation(boardObject.transform, cameraPosition, Vector3.zero, 28f); // 판 위 Canvas 회전 계산
            Vector3 textTop = rotation * Vector3.up; // UI 글자 윗방향 계산
            Vector3 planarToCamera = Vector3.ProjectOnPlane(cameraPosition, boardObject.transform.up).normalized; // 보드 평면 플레이어 방향 계산

            Assert.Greater(Vector3.Dot(textTop.normalized, -planarToCamera), 0.7f); // 제목이 플레이어 반대쪽 보드 안쪽을 향하는지 검증
            Object.DestroyImmediate(boardObject); // 테스트 객체 정리
        }

        [Test]
        public void ResolveCanvasAnchorWorldPosition_MovesPivotTowardPlayerNearEdge()
        {
            var boardObject = new GameObject("Board"); // 기준 보드 생성
            Vector3 cameraPosition = new Vector3(0f, 9f, -9f); // Battle 씬 기본 플레이어 카메라 위치 사용

            Vector3 anchor = StageOverlayOrientationUtility.ResolveCanvasAnchorWorldPosition(boardObject.transform, cameraPosition, 1f, 3f, 0.19f); // Canvas 하단 회전축 위치 계산

            Assert.That(anchor.z, Is.EqualTo(-3f).Within(0.01f)); // 플레이어 쪽 보드 가장자리로 이동했는지 검증
            Assert.That(anchor.y, Is.EqualTo(0.19f).Within(0.01f)); // 돗자리와 겹치지 않는 높이인지 검증
            Object.DestroyImmediate(boardObject); // 테스트 객체 정리
        }
    }
}
