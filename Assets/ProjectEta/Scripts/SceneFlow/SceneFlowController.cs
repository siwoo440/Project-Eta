using UnityEngine; // Application·Debug 사용
using UnityEngine.SceneManagement; // SceneManager 사용
using ProjectEta.Run; // RunSaveSystem 사용

namespace ProjectEta.SceneFlow
{
    public static class SceneFlowController
    {
        public const string BootSceneName = "Boot"; // 부트 씬 이름
        public const string MainMenuSceneName = "MainMenu"; // 메인 메뉴 씬 이름
        public const string BattleSceneName = "Battle"; // 실제 런 진행 씬 이름
        public const string BootScenePath = "Assets/ProjectEta/Scenes/Boot.unity"; // Build Settings 0번 경로
        public const string MainMenuScenePath = "Assets/ProjectEta/Scenes/MainMenu.unity"; // Build Settings 1번 경로
        public const string BattleScenePath = "Assets/ProjectEta/Scenes/Battle.unity"; // Build Settings 2번 경로

        private static readonly SceneTransitionGate TransitionGate = new SceneTransitionGate(); // 중복 씬 전환 게이트

        public static bool IsTransitioning => TransitionGate.IsTransitioning; // 외부 UI 전환 잠금 상태 공개

        public static bool LoadMainMenu()
        {
            return TryLoadScene(MainMenuSceneName); // 메인 메뉴 씬 전환
        }

        public static bool StartNewRun()
        {
            if (!RunSaveSystem.TryDeleteForNewRun())
            {
                Debug.LogError("54일차 새 게임 시작 실패: 기존 Run Save를 정리하지 못했습니다."); // 이전 런 삭제 실패 기록
                return false; // 이전 런 복원 위험 차단
            }

            return TryLoadScene(BattleSceneName); // 새 RunState 생성을 위해 Battle 씬 진입
        }

        public static bool ContinueRun()
        {
            if (!RunSaveSystem.CanContinue)
            {
                Debug.LogWarning("54일차 이어하기 차단: 복원 가능한 안전 지점 Run Save가 없습니다."); // 이어하기 불가 사유 기록
                return false; // 잘못된 세이브 진입 차단
            }

            return TryLoadScene(BattleSceneName); // BattleController의 안전 복원 경로 재사용
        }

        public static bool ReturnToMainMenu()
        {
            return TryLoadScene(MainMenuSceneName); // 런 종료 후 메인 메뉴 복귀
        }

        public static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; // Editor Play Mode 종료
#else
            Application.Quit(); // 실제 빌드 종료
#endif
        }

        public static void NotifySceneLoaded()
        {
            TransitionGate.Complete(); // sceneLoaded 이벤트에서 전환 잠금 해제
        }

        private static bool TryLoadScene(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName)) return false; // 빈 씬 이름 차단

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"54일차 씬 전환 실패: Build Settings에 '{sceneName}' 씬이 없습니다."); // Build Settings 누락 기록
                return false; // 로드 불가능 씬 차단
            }

            if (!TransitionGate.TryBegin()) return false; // 중복 씬 전환 차단

            SceneManager.LoadScene(sceneName, LoadSceneMode.Single); // 현재 씬을 대상 씬으로 교체
            return true; // 씬 전환 요청 성공
        }
    }
}
