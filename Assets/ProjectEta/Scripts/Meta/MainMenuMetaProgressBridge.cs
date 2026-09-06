using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·Transform·Object 사용
using ProjectEta.UI; // MainMenuController 사용

namespace ProjectEta.Meta
{
    [DefaultExecutionOrder(60)]
    public sealed class MainMenuMetaProgressBridge : MonoBehaviour
    {
        private MainMenuController _mainMenuController; // 현재 MainMenu 컨트롤러
        private MetaProgressPanelController _panelController; // 정식 영구 성장 패널 컨트롤러

        private IEnumerator Start()
        {
            const int maxWaitFrames = 30; // MainMenu 런타임 UI 최대 대기 프레임
            int waitedFrames = 0; // 현재 UI 대기 프레임

            while (waitedFrames < maxWaitFrames)
            {
                _mainMenuController = Object.FindFirstObjectByType<MainMenuController>(); // MainMenu 컨트롤러 조회
                Transform metaRoot = _mainMenuController != null ? FindChildRecursive(_mainMenuController.transform, "MetaRoot") : null; // 기존 영구 성장 루트 조회

                if (metaRoot != null)
                {
                    AttachReusablePanel(metaRoot); // 기존 MetaRoot에 정식 영구 성장 패널 연결
                    yield break; // MainMenu 영구 성장 연결 완료
                }

                waitedFrames++; // UI 대기 프레임 증가
                yield return null; // 다음 프레임 재탐색
            }

            Debug.LogWarning("58일차 영구 성장 패널 연결 실패: MetaRoot를 찾지 못했습니다."); // MainMenu 영구 성장 루트 누락 기록
        }

        private void AttachReusablePanel(Transform metaRoot)
        {
            for (int i = 0; i < metaRoot.childCount; i++)
            {
                metaRoot.GetChild(i).gameObject.SetActive(false); // 55일차 개발용 영구 성장 UI 숨김
            }

            _panelController = metaRoot.GetComponent<MetaProgressPanelController>(); // 기존 정식 영구 성장 패널 조회
            if (_panelController == null) _panelController = metaRoot.gameObject.AddComponent<MetaProgressPanelController>(); // 정식 영구 성장 패널 추가
            _panelController.Initialize(RequestClose); // MainMenu 뒤로가기 콜백 연결
        }

        private void RequestClose()
        {
            if (_mainMenuController == null) return; // MainMenu 컨트롤러 누락 방어
            _mainMenuController.gameObject.SendMessage("HandleBack", SendMessageOptions.DontRequireReceiver); // 기존 MainMenu 내비게이션 뒤로가기 재사용
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null) return null; // 빈 탐색 루트 차단
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
