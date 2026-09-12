#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine; // MonoBehaviour·Debug 사용
using UnityEngine.InputSystem; // Keyboard 사용

namespace ProjectEta.Steam
{
    public sealed class SteamOverlayDebugShortcut : MonoBehaviour
    {
        private void Update()
        {
            Keyboard keyboard = Keyboard.current; // 현재 키보드 조회
            if (keyboard == null || !keyboard.f8Key.wasPressedThisFrame) return; // F8 입력 외 무시

            bool opened = SteamPlatform.OpenOverlay(); // 친구 Overlay 테스트 호출
            Debug.Log($"77일차 Steam Overlay F8 테스트: Opened={opened} / Backend={SteamPlatform.BackendName} / Available={SteamPlatform.IsOverlayEnabled}"); // 테스트 결과 기록
        }
    }
}
#endif
