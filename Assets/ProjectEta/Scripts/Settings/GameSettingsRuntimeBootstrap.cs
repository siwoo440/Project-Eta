using System.Collections; // IEnumerator 사용
using UnityEngine; // MonoBehaviour·GameObject·Object 사용
using UnityEngine.SceneManagement; // sceneLoaded 이벤트 사용

namespace ProjectEta.Settings
{
    public sealed class GameSettingsRuntimeBootstrap : MonoBehaviour
    {
        private static GameSettingsRuntimeBootstrap _instance; // 플레이 세션 설정 부트스트랩 인스턴스

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null; // Domain Reload 비활성 환경 정적 참조 초기화
            GameSettingsService.ResetRuntimeState(); // 설정·오디오 서비스 정적 상태 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeFirstScene()
        {
            GameSettingsService.EnsureLoaded(); // 첫 씬 전 저장 설정 로드·화면·오디오 적용
            if (_instance != null) return; // 중복 설정 부트스트랩 생성 차단

            var host = new GameObject("GameSettingsRuntimeBootstrap_Day56"); // 설정 세션 호스트 생성
            Object.DontDestroyOnLoad(host); // 씬 전환 사이 설정 부트스트랩 유지
            _instance = host.AddComponent<GameSettingsRuntimeBootstrap>(); // 설정 부트스트랩 컴포넌트 연결
            SceneManager.sceneLoaded += _instance.HandleSceneLoaded; // 모든 씬 로드 후 UI·오디오 설정 재적용
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StartCoroutine(ReapplyRuntimeSettingsNextFrame()); // 런타임 Canvas·AudioSource 생성 이후 설정 적용 예약
        }

        private static IEnumerator ReapplyRuntimeSettingsNextFrame()
        {
            yield return null; // 각 씬 Start 기반 런타임 객체 생성 완료 대기
            GameSettingsService.ReapplyRuntimeSettings(); // 새 Canvas·오디오에 저장 설정 재적용
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 플레이 종료 시 씬 이벤트 구독 해제
            if (_instance == this) _instance = null; // 현재 부트스트랩 정적 참조 정리
        }
    }
}
