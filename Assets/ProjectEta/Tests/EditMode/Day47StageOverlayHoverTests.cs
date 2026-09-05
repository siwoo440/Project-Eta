using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // GameObject 사용
using ProjectEta.UI; // StageOverlayHoverRelay 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day47StageOverlayHoverTests
    {
        [Test]
        public void HoverRelay_PointerEnter_InvokesEnterCallback()
        {
            var host = new GameObject("HoverRelayTest"); // 테스트 호스트 생성
            var relay = host.AddComponent<StageOverlayHoverRelay>(); // Hover Relay 추가
            bool entered = false; // 진입 콜백 상태 초기화
            relay.Configure(() => entered = true, null); // 진입 콜백 연결

            relay.OnPointerEnter(null); // 마우스 진입 직접 실행

            Assert.IsTrue(entered); // 진입 콜백 실행 검증
            UnityEngine.Object.DestroyImmediate(host); // 테스트 호스트 제거
        }

        [Test]
        public void HoverRelay_PointerExit_InvokesExitCallback()
        {
            var host = new GameObject("HoverRelayTest"); // 테스트 호스트 생성
            var relay = host.AddComponent<StageOverlayHoverRelay>(); // Hover Relay 추가
            bool exited = false; // 이탈 콜백 상태 초기화
            relay.Configure(null, () => exited = true); // 이탈 콜백 연결

            relay.OnPointerExit(null); // 마우스 이탈 직접 실행

            Assert.IsTrue(exited); // 이탈 콜백 실행 검증
            UnityEngine.Object.DestroyImmediate(host); // 테스트 호스트 제거
        }

        [Test]
        public void HoverRelay_Clear_RemovesCallbacks()
        {
            var host = new GameObject("HoverRelayTest"); // 테스트 호스트 생성
            var relay = host.AddComponent<StageOverlayHoverRelay>(); // Hover Relay 추가
            int callCount = 0; // 콜백 호출 수 초기화
            relay.Configure(() => callCount++, () => callCount++); // 진입·이탈 콜백 연결
            relay.Clear(); // 현재 콜백 제거

            relay.OnPointerEnter(null); // 제거 후 진입 실행
            relay.OnPointerExit(null); // 제거 후 이탈 실행

            Assert.AreEqual(0, callCount); // 제거된 콜백 미실행 검증
            UnityEngine.Object.DestroyImmediate(host); // 테스트 호스트 제거
        }
    }
}
