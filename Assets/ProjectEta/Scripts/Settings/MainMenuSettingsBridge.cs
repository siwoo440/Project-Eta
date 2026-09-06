using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·Transform 사용
using ProjectEta.UI; // MainMenuController 사용

namespace ProjectEta.Settings
{
    [DefaultExecutionOrder(50)]
    public sealed class MainMenuSettingsBridge : MonoBehaviour
    {
        private MainMenuController _mainMenuController; // 현재 MainMenu 컨트롤러
        private SettingsPanelController _settingsPanelController; // 재사용 설정 패널

        private IEnumerator Start()
        {
            const int maxWaitFrames = 30; // MainMenu 런타임 UI 최대 대기 프레임
            int waitedFrames = 0; // 현재 UI 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _mainMenuController = Object.FindFirstObjectByType<MainMenuController>(); // MainMenu 컨트롤러 조회
                Transform settingsRoot = _mainMenuController != null ? FindChildRecursive(_mainMenuController.transform, "SettingsRoot") : null; // 기존 설정 패널 루트 조회

                if (settingsRoot != null)
                {
                    AttachReusablePanel(settingsRoot); // 기존 SettingsRoot에 재사용 설정 UI 연결
                    yield break; // MainMenu 설정 연결 완료
                }

                waitedFrames++; // UI 대기 프레임 증가
                yield return null; // 다음 프레임 재탐색
            }

            Debug.LogWarning("56일차 MainMenu 설정 패널 연결 실패: SettingsRoot를 찾지 못했습니다."); // MainMenu 설정 루트 누락 기록
        }

        private void AttachReusablePanel(Transform settingsRoot)
        {
            for (int i = 0; i < settingsRoot.childCount; i++)
            {
                settingsRoot.GetChild(i).gameObject.SetActive(false); // 55일차 설정 안내 Placeholder 숨김
            }

            _settingsPanelController = settingsRoot.GetComponent<SettingsPanelController>(); // 기존 재사용 설정 패널 조회
            if (_settingsPanelController == null) _settingsPanelController = settingsRoot.gameObject.AddComponent<SettingsPanelController>(); // 재사용 설정 패널 추가
            _settingsPanelController.Initialize(RequestClose); // MainMenu 뒤로가기 콜백 연결
        }

        private void RequestClose()
        {
            if (_mainMenuController == null) return; // MainMenu 컨트롤러 누락 방어
            _mainMenuController.gameObject.SendMessage("HandleBack", SendMessageOptions.DontRequireReceiver); // 기존 55일차 MainMenu 내비게이션 뒤로가기 재사용
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null) return null; // 빈 루트 탐색 차단
            if (root.name == childName) return root; // 현재 루트 이름 일치 반환

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildRecursive(root.GetChild(i), childName); // 하위 계층 재귀 탐색
                if (found != null) return found; // 첫 일치 자식 반환
            }

            return null; // 대상 자식 없음
        }
    }
}
