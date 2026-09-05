using NUnit.Framework; // EditMode Assert 사용
using UnityEngine; // Vector3·Quaternion 사용
using ProjectEta.UI; // StageActivityCameraPoseUtility 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day47StageActivityCameraTests
    {
        [Test]
        public void PrimaryCameraPose_UsesBattleCameraOnePosition()
        {
            Vector3 localPosition = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalPosition(1f); // 1번 카메라 로컬 위치 계산

            Assert.That(localPosition.x, Is.EqualTo(0f).Within(0.001f)); // 중앙 X 위치 검증
            Assert.That(localPosition.y, Is.EqualTo(9f).Within(0.001f)); // 기본 카메라 높이 검증
            Assert.That(localPosition.z, Is.EqualTo(-9f).Within(0.001f)); // 플레이어 측 기본 Z 위치 검증
        }

        [Test]
        public void PrimaryCameraRotation_LooksTowardBoardCenter()
        {
            Vector3 cameraPosition = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalPosition(1f); // 1번 카메라 위치 계산
            Quaternion cameraRotation = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalRotation(); // 1번 카메라 회전 계산
            Vector3 cameraForward = cameraRotation * Vector3.forward; // 실제 카메라 전방 계산
            Vector3 toBoardCenter = (-cameraPosition).normalized; // 카메라에서 보드 중앙 방향 계산

            Assert.Greater(Vector3.Dot(cameraForward.normalized, toBoardCenter), 0.999f); // 정확히 보드 중앙을 바라보는지 검증
        }

        [Test]
        public void FixedCanvasRotation_FrontFacesPrimaryCamera()
        {
            float tileSize = 1f; // 기본 타일 크기 사용
            Vector3 cameraPosition = StageActivityCameraPoseUtility.ResolvePrimaryCameraLocalPosition(tileSize); // 고정 카메라 위치 계산
            Vector3 canvasAnchor = StageActivityCameraPoseUtility.ResolveCanvasLocalAnchor(tileSize); // 고정 UI 하단 위치 계산
            Quaternion canvasRotation = StageActivityCameraPoseUtility.ResolveCanvasLocalRotation(tileSize); // 고정 UI 회전 계산
            Vector3 canvasFront = canvasRotation * Vector3.back; // Unity World Space UI 앞면 계산
            Vector3 toCamera = (cameraPosition - canvasAnchor).normalized; // UI에서 카메라 방향 계산

            Assert.Greater(Vector3.Dot(canvasFront.normalized, toCamera), 0.999f); // UI 앞면이 1번 카메라를 정확히 향하는지 검증
        }

        [Test]
        public void FixedCanvasRotation_TextTopPointsIntoBoard()
        {
            float tileSize = 1f; // 기본 타일 크기 사용
            Quaternion canvasRotation = StageActivityCameraPoseUtility.ResolveCanvasLocalRotation(tileSize); // 고정 UI 회전 계산
            Vector3 textTop = canvasRotation * Vector3.up; // 글자 윗방향 계산

            Assert.Greater(Vector3.Dot(textTop.normalized, Vector3.forward), 0.5f); // 제목이 플레이어 반대쪽 보드 안쪽을 향하는지 검증
        }
    }
}
