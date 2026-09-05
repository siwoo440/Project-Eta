using System.Reflection; // private 메서드 호출 검증
using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // GameObject·MonoBehaviour 사용
using UnityEngine.UI; // Canvas 사용
using ProjectEta.Board; // RouteMapBoardController 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day45RouteMapUiVisibilityTests // 지도 모드 UI 숨김이 시스템 호스트를 비활성화하지 않는지 검증
    {
        private sealed class DummyUiHost : MonoBehaviour // BattleController와 같은 UI 컴포넌트 호스트 대역
        {
        }

        [Test] // UI Canvas만 숨기고 호스트 GameObject는 유지하는지 검증
        public void HideUiRoot_KeepsComponentHostActive_AndHidesCanvasChild() // BattleController 비활성화 회귀 방지
        {
            var controllerObject = new GameObject("RouteMapController"); // 테스트용 지도 컨트롤러 호스트 생성
            var controller = controllerObject.AddComponent<RouteMapBoardController>(); // 실제 지도 컨트롤러 추가

            var battleControllerLikeHost = new GameObject("BattleController"); // 실제 오류와 같은 공용 UI 호스트 생성
            var dummyUi = battleControllerLikeHost.AddComponent<DummyUiHost>(); // UI 컴포넌트 대역 추가

            var canvasObject = new GameObject("BattleUiCanvas", typeof(RectTransform), typeof(Canvas)); // 호스트 자식 Canvas 생성
            canvasObject.transform.SetParent(battleControllerLikeHost.transform, false); // BattleController 자식 구조 재현

            MethodInfo hideMethod = typeof(RouteMapBoardController).GetMethod("HideUiRoot", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 숨김 메서드 조회
            MethodInfo restoreMethod = typeof(RouteMapBoardController).GetMethod("RestoreBattleUi", BindingFlags.Instance | BindingFlags.NonPublic); // 실제 복원 메서드 조회

            Assert.IsNotNull(hideMethod); // 숨김 메서드 존재 확인
            Assert.IsNotNull(restoreMethod); // 복원 메서드 존재 확인

            hideMethod.Invoke(controller, new object[] { dummyUi }); // 지도 모드 UI 숨김 실행

            Assert.IsTrue(battleControllerLikeHost.activeSelf); // BattleController 같은 시스템 호스트 활성 유지 검증
            Assert.IsFalse(canvasObject.activeSelf); // 실제 Canvas만 숨겨졌는지 검증

            restoreMethod.Invoke(controller, null); // 전투 모드 UI 복원 실행

            Assert.IsTrue(battleControllerLikeHost.activeSelf); // 복원 후에도 호스트 활성 유지 검증
            Assert.IsTrue(canvasObject.activeSelf); // Canvas 표시 복원 검증

            Object.DestroyImmediate(controllerObject); // 테스트 지도 컨트롤러 정리
            Object.DestroyImmediate(battleControllerLikeHost); // 테스트 UI 호스트 정리
        }
    }
}
