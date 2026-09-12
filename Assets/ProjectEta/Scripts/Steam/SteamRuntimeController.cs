using UnityEngine; // MonoBehaviour·GameObject·RuntimeInitializeOnLoadMethod 사용

namespace ProjectEta.Steam
{
    [DefaultExecutionOrder(-10000)]
    public sealed class SteamRuntimeController : MonoBehaviour
    {
        private static SteamRuntimeController _instance; // 전역 RuntimeController 단일 인스턴스
        private SteamPlatformService _service; // 현재 Steam 서비스

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (FindFirstObjectByType<SteamRuntimeController>() != null) return; // 씬 배치 인스턴스가 있으면 자동 생성 생략

            var host = new GameObject("SteamRuntimeController_Day77"); // Steam Runtime 호스트 생성
            host.AddComponent<SteamRuntimeController>(); // 전역 Steam Runtime 연결
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject); // 중복 Steam Runtime 제거
                return;
            }

            _instance = this; // 전역 인스턴스 등록
            DontDestroyOnLoad(gameObject); // Scene 전환에도 Steam Runtime 유지

            _service = new SteamPlatformService(SteamBackendFactory.Create()); // 현재 환경 Backend 기반 서비스 생성
            SteamPlatform.Bind(_service); // 전역 접근점에 서비스 연결

            bool initialized = _service.Initialize(); // Steam 초기화 시도
            if (initialized)
            {
                Debug.Log($"77일차 Steam 초기화 성공: Backend={_service.BackendName} / Overlay={_service.IsOverlayAvailable}"); // 초기화 결과 기록
            }
            else
            {
                Debug.LogWarning($"77일차 Steam 비활성 상태로 실행합니다: Backend={_service.BackendName} / Reason={_service.LastError}"); // Steam 없는 환경 fallback 기록
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GetComponent<SteamOverlayDebugShortcut>() == null) gameObject.AddComponent<SteamOverlayDebugShortcut>(); // 개발 환경 F8 Overlay 테스트 추가
#endif
        }

        private void Update()
        {
            _service?.Tick(); // 매 프레임 Steam Callback 처리
        }

        private void OnApplicationQuit()
        {
            _service?.Shutdown(); // 애플리케이션 종료 시 Steam Runtime 종료
        }

        private void OnDestroy()
        {
            if (_instance != this) return; // 중복 제거 객체는 전역 상태 변경 금지

            _service?.Shutdown(); // Domain Reload·씬 종료에도 안전 종료
            SteamPlatform.Unbind(_service); // 전역 서비스 참조 해제
            _service = null; // 서비스 참조 제거
            _instance = null; // 전역 인스턴스 해제
        }
    }
}
