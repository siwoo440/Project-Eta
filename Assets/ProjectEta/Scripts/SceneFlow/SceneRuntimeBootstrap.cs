using UnityEngine; // MonoBehaviour·GameObject·Object 사용
using UnityEngine.SceneManagement; // sceneLoaded 이벤트 사용
using ProjectEta.AI; // 적 AI·보스 페이즈 런타임 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // 지도 런타임 관리자 사용
using ProjectEta.Boss; // 대형 보스·호환 브리지 사용
using ProjectEta.Environment; // 41일차 전투방 환경 사용
using ProjectEta.King; // 킹 능력·선택 UI 사용
using ProjectEta.Meta; // 메타 결과·영구 성장 관리자 사용
using ProjectEta.Round; // 첫 전투 RoundRuntimeController 사용
using ProjectEta.Run; // 런·스테이지 관리자 사용
using ProjectEta.Settings; // 재사용 설정 패널 사용
using ProjectEta.UI; // MainMenuController 사용

namespace ProjectEta.SceneFlow
{
    public sealed class SceneRuntimeBootstrap : MonoBehaviour
    {
        private static SceneRuntimeBootstrap _instance; // 플레이 세션 공통 부트스트랩 인스턴스

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance = null; // Domain Reload 비활성 환경 정적 참조 초기화
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeFirstScene()
        {
            if (_instance != null) return; // 중복 부트스트랩 생성 차단

            var host = new GameObject("SceneRuntimeBootstrap_Day54"); // 플레이 세션 공통 호스트 생성
            Object.DontDestroyOnLoad(host); // 씬 전환 사이 부트스트랩 유지
            _instance = host.AddComponent<SceneRuntimeBootstrap>(); // 씬 로드 이벤트 관리자 연결
            SceneManager.sceneLoaded += _instance.HandleSceneLoaded; // 최초 이후 모든 씬 로드 감지
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneFlowController.NotifySceneLoaded(); // 새 씬 진입 시 전환 잠금 해제

            if (scene.name == SceneFlowController.BootSceneName)
            {
                EnsureComponent<BootController>("BootController_Day54"); // Boot 자동 초기화 관리자 주입
                return; // Boot 전용 구성 종료
            }

            if (scene.name == SceneFlowController.MainMenuSceneName)
            {
                EnsureComponent<MainMenuController>("MainMenuController_Day54"); // MainMenu 런타임 UI 주입
                EnsureComponent<MainMenuSettingsBridge>("MainMenuSettingsBridge_Day56"); // SettingsRoot 재사용 설정 패널 연결
                EnsureComponent<MainMenuMetaProgressBridge>("MainMenuMetaProgressBridge_Day58"); // MetaRoot 정식 영구 성장 패널 연결
                return; // MainMenu 전용 구성 종료
            }

            if (scene.name == SceneFlowController.BattleSceneName)
            {
                EnsureBattleRuntime(); // Boot 이후 늦게 로드된 Battle의 기존 자동 생성 관리자 복구
            }
        }

        private static void EnsureBattleRuntime()
        {
            EnsureComponent<BattleController>("BattleController"); // RunState·TurnManager 핵심 전투 관리자 생성
            EnsureComponent<RoundRuntimeController>("RoundRuntimeController_Day40"); // 첫 Stage 전투 데이터·증원 관리자 생성
            EnsureComponent<Day41BattleRoomBootstrap>(Day41BattleRoomLayout.RootName); // 늦게 로드된 Battle의 전투방·테이블·카메라 환경 복구
            EnsureComponent<LargePieceLifecycleController>("LargePieceLifecycleController_Day37"); // 2x2 보스 점유·시각 생명주기 복구
            EnsureComponent<LargePiecePlayerAttackBridge>("LargePiecePlayerAttackBridge_Day39"); // 2x2 보스 모델 클릭 공격 브리지 복구
            EnsureComponent<LargePieceTurnEndStatusBridge>("LargePieceTurnEndStatusBridge_Day40"); // 대형 기물 상태 효과 중복 정산 방지 브리지 복구
            EnsureComponent<EnemyAITurnDriver>("EnemyAITurnDriver_Day39"); // 일반 적 AI·보스 Phase2·HP UI 런타임 복구
            EnsureComponent<PrototypeBoss37Spawner>("PrototypeBoss37Spawner"); // 일반 개발 전투의 프로토타입 2x2 보스 복구
            EnsureComponent<RouteMapBoardController>("RouteMapBoardController_Day44"); // 10×10 Route Map 관리자 생성
            EnsureComponent<FullRouteMapPreviewController>("FullRouteMapPreviewController_Day52"); // 1~10 전체 경로 미리보기 생성
            EnsureComponent<StageTransitionController>("StageTransitionController_Day45"); // StageNode 실제 콘텐츠 전환기 생성
            EnsureComponent<CardRewardController>("CardRewardController_Day46"); // 전투·Reward 카드 보상 관리자 생성
            EnsureComponent<StageActivityController>("StageActivityController_Day47"); // Shop·Event 활동 관리자 생성
            EnsureComponent<MetaProgressRunResultController>("MetaProgressController_Day48"); // 런 종료 메타 보상 관리자 생성
            EnsureKingRuntime(); // 49~50일차 킹 패시브·선택 관리자 생성
            EnsureComponent<RunPersistenceController>("RunPersistenceController_Day51"); // 안전 지점 자동 저장 관리자 생성
            EnsureComponent<RunResultMainMenuController>("RunResultMainMenuController_Day54"); // Completed·Failed 메인 메뉴 복귀 UI 생성
            EnsureComponent<BattleSettingsOverlayController>("BattleSettingsOverlayController_Day56"); // Battle ESC 재사용 설정 패널 생성
        }

        private static void EnsureKingRuntime()
        {
            KingAbilityController existing = Object.FindFirstObjectByType<KingAbilityController>(); // 기존 킹 능력 관리자 조회
            if (existing != null)
            {
                if (existing.GetComponent<KingSelectionUI>() == null) existing.gameObject.AddComponent<KingSelectionUI>(); // 기존 호스트 킹 선택 UI 보강
                if (existing.GetComponent<StrategyKingSelectionUI>() == null) existing.gameObject.AddComponent<StrategyKingSelectionUI>(); // 기존 호스트 전략형 선택 UI 보강
                return; // 기존 킹 호스트 재사용
            }

            var host = new GameObject("KingAbilityController_Day49"); // 킹 능력 공통 호스트 생성
            host.AddComponent<KingAbilityController>(); // 공격·방어·전략형 패시브 관리자 추가
            host.AddComponent<KingSelectionUI>(); // 새 런 킹 선택 UI 추가
            host.AddComponent<StrategyKingSelectionUI>(); // 전략형 배치 카드 선택 UI 추가
        }

        private static T EnsureComponent<T>(string objectName) where T : Component
        {
            T existing = Object.FindFirstObjectByType<T>(); // 현재 씬 기존 관리자 조회
            if (existing != null) return existing; // 기존 인스턴스 재사용

            var host = new GameObject(objectName); // 런타임 관리자 호스트 생성
            return host.AddComponent<T>(); // 요청 컴포넌트 생성·반환
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 플레이 종료 시 씬 이벤트 구독 해제
            if (_instance == this) _instance = null; // 현재 정적 인스턴스 참조 정리
        }
    }
}
