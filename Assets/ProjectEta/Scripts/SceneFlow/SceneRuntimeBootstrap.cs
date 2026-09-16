using System.Collections.Generic; // 초기화 진단 목록 사용
using UnityEngine; // MonoBehaviour·GameObject·Object 사용
using UnityEngine.SceneManagement; // sceneLoaded 이벤트 사용
using ProjectEta.AI; // 적 AI·보스 페이즈 런타임 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Board; // 지도 런타임 관리자 사용
using ProjectEta.Boss; // 대형 보스·호환 브리지 사용
using ProjectEta.Cards; // 시작 덱 확장 관리자 사용
using ProjectEta.Environment; // 전투방 환경 사용
using ProjectEta.King; // 킹 능력·선택·전투 HUD 사용
using ProjectEta.Meta; // 메타 결과·영구 성장 관리자 사용
using ProjectEta.Pieces; // 전투 연출 관리자 사용
using ProjectEta.Round; // 전투 Round 관리자 사용
using ProjectEta.Run; // 런·스테이지 관리자 사용
using ProjectEta.Settings; // 재사용 설정 패널 사용
using ProjectEta.UI; // MainMenuController·BattleHUD 사용

namespace ProjectEta.SceneFlow // 씬 흐름 네임스페이스
{ // 네임스페이스 범위
    public sealed class SceneRuntimeBootstrap : MonoBehaviour // 전체 씬 런타임 초기화 관리자
    { // 클래스 범위
        private static SceneRuntimeBootstrap _instance; // 플레이 세션 공통 부트스트랩 인스턴스
        private static float _lastInitializationMilliseconds; // 마지막 Battle 초기화 소요 시간
        private static BattleRuntimeDiagnostics _latestDiagnostics = BattleRuntimeDiagnostics.Empty; // 최신 Battle 진단 결과

        public static BattleRuntimeDiagnostics LatestDiagnostics => _latestDiagnostics; // 최신 진단 결과 제공

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 세션 정적 상태 초기화
        private static void ResetStaticState() // 정적 필드 초기화
        { // 메서드 범위
            _instance = null; // 이전 부트스트랩 참조 제거
            _lastInitializationMilliseconds = 0f; // 이전 초기화 시간 제거
            _latestDiagnostics = BattleRuntimeDiagnostics.Empty; // 이전 진단 결과 제거
        } // 메서드 종료

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] // 첫 씬 이전 공통 관리자 생성
        private static void InitializeBeforeFirstScene() // 최초 부트스트랩 생성
        { // 메서드 범위
            if (_instance != null) return; // 중복 부트스트랩 생성 차단

            var host = new GameObject("SceneRuntimeBootstrap_Day54"); // 플레이 세션 공통 호스트 생성
            Object.DontDestroyOnLoad(host); // 씬 전환 사이 부트스트랩 유지
            _instance = host.AddComponent<SceneRuntimeBootstrap>(); // 씬 로드 이벤트 관리자 연결
            SceneManager.sceneLoaded += _instance.HandleSceneLoaded; // 모든 씬 로드 감지
        } // 메서드 종료

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) // 씬 로드 완료 처리
        { // 메서드 범위
            SceneFlowController.NotifySceneLoaded(); // 새 씬 전환 잠금 해제

            if (scene.name == SceneFlowController.BootSceneName) // Boot 씬 확인
            { // 조건 범위
                _latestDiagnostics = BattleRuntimeDiagnostics.Empty; // 이전 Battle 진단 제거
                EnsureComponent<BootController>("BootController_Day54"); // Boot 초기화 관리자 주입
                return; // Boot 구성 종료
            } // 조건 종료

            if (scene.name == SceneFlowController.MainMenuSceneName) // MainMenu 씬 확인
            { // 조건 범위
                _latestDiagnostics = BattleRuntimeDiagnostics.Empty; // 이전 Battle 진단 제거
                EnsureComponent<MainMenuController>("MainMenuController_Day54"); // MainMenu UI 주입
                EnsureComponent<MainMenuSettingsBridge>("MainMenuSettingsBridge_Day56"); // 설정 패널 연결
                EnsureComponent<MainMenuMetaProgressBridge>("MainMenuMetaProgressBridge_Day58"); // 영구 성장 패널 연결
                return; // MainMenu 구성 종료
            } // 조건 종료

            if (scene.name == SceneFlowController.BattleSceneName) EnsureBattleRuntime(); // Battle 런타임 순차 초기화
        } // 메서드 종료

        private static void EnsureBattleRuntime() // Battle 전체 초기화 진입점
        { // 메서드 범위
            var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // 초기화 시간 측정 시작
            EnsureCoreRuntime(); // 핵심 전투 상태 초기화
            EnsureCombatRuntime(); // 전투 규칙·AI 초기화
            EnsureRunRuntime(); // 경로·보상·저장 초기화
            EnsurePresentationRuntime(); // HUD·연출 UI 초기화
            stopwatch.Stop(); // 초기화 시간 측정 종료
            _lastInitializationMilliseconds = (float)stopwatch.Elapsed.TotalMilliseconds; // 소요 시간 저장
            RefreshBattleDiagnostics(); // 최종 관리자 상태 수집
        } // 메서드 종료

        private static void EnsureCoreRuntime() // 핵심 상태 초기화 단계
        { // 메서드 범위
            EnsureComponent<BattleController>("BattleController"); // 전투 상태 관리자 생성
            EnsureComponent<RoundRuntimeController>("RoundRuntimeController_Day40"); // 라운드 데이터 관리자 생성
            EnsureComponent<RoundStateBattleBridge>("RoundStateBattleBridge_Day43"); // 전투 결과와 런 상태 연결
            EnsureComponent<PrototypePlayerDeck26Bootstrap>("PrototypePlayerDeck26Bootstrap"); // 시작 덱 확장 작업 생성
        } // 메서드 종료

        private static void EnsureCombatRuntime() // 전투 기능 초기화 단계
        { // 메서드 범위
            EnsureComponent<Day41BattleRoomBootstrap>(Day41BattleRoomLayout.RootName); // 전투방 환경 생성
            EnsureComponent<LargePieceLifecycleController>("LargePieceLifecycleController_Day37"); // 대형 기물 생명주기 생성
            EnsureComponent<LargePiecePlayerAttackBridge>("LargePiecePlayerAttackBridge_Day39"); // 대형 기물 공격 연결
            EnsureComponent<LargePieceTurnEndStatusBridge>("LargePieceTurnEndStatusBridge_Day40"); // 대형 기물 상태 정산 연결
            EnsureComponent<EnemyAITurnDriver>("EnemyAITurnDriver_Day39"); // 적 AI 관리자 생성
            EnsureComponent<PrototypeBoss37Spawner>("PrototypeBoss37Spawner"); // 보스 생성 관리자 생성
            EnsureComponent<PlayerActionTurnDelayController>("PlayerActionTurnDelayController_Day44"); // 플레이어 연출 대기 관리자 생성
            EnsureComponent<LethalAttackVisualBridge>("LethalAttackVisualBridge_Day44"); // 치명 공격 연출 연결
            EnsureKingRuntime(); // 킹 능력·선택 관리자 생성
        } // 메서드 종료

        private static void EnsureRunRuntime() // 런 진행 초기화 단계
        { // 메서드 범위
            EnsureComponent<RouteMapBoardController>("RouteMapBoardController_Day44"); // 경로 지도 관리자 생성
            EnsureComponent<FullRouteMapPreviewController>("FullRouteMapPreviewController_Day52"); // 전체 경로 미리보기 생성
            EnsureComponent<StageTransitionController>("StageTransitionController_Day45"); // 스테이지 전환기 생성
            EnsureComponent<CardRewardController>("CardRewardController_Day46"); // 카드 보상 관리자 생성
            EnsureComponent<StageActivityController>("StageActivityController_Day47"); // Shop·Event 활동 관리자 생성
            EnsureComponent<MetaProgressRunResultController>("MetaProgressController_Day48"); // 런 종료 메타 보상 관리자 생성
            EnsureComponent<RunPersistenceController>("RunPersistenceController_Day51"); // 안전 지점 자동 저장 관리자 생성
        } // 메서드 종료

        private static void EnsurePresentationRuntime() // UI·연출 초기화 단계
        { // 메서드 범위
            EnsureComponent<RunResultMainMenuController>("RunResultMainMenuController_Day54"); // 런 결과 복귀 UI 생성
            EnsureComponent<BattleSettingsOverlayController>("BattleSettingsOverlayController_Day56"); // Battle 설정 패널 생성
            EnsureComponent<BattleHUD>("BattleHUD_Day62"); // 전투 상단 HUD 생성
            EnsureComponent<BattleInteractionStatusUI>("BattleInteractionStatusUI_Day63"); // 전투 입력 안내 UI 생성
            EnsureComponent<CombatFloatingTextUI>("CombatFloatingTextUI_Day63"); // 전투 숫자 UI 생성
            EnsureComponent<Day64FusionUI>("Day64FusionUI"); // 합성 UI 생성
            EnsureComponent<Day65StageActivityHUD>("Day65StageActivityHUD"); // 활동 HUD 생성
            EnsureComponent<Day66HandCardMotionController>("Day66HandCardMotionController"); // 손패 모션 생성
            EnsureComponent<Day66BoardTransitionFX>("Day66BoardTransitionFX"); // 보드 전환 연출 생성
            EnsureBattleAnnouncementRuntime(); // 전투 알림 UI와 관리자 생성
            EnsureComponent<FirstRunTutorialController>("FirstRunTutorialController_Day68"); // 최초 튜토리얼 생성
            EnsureComponent<SystemToastUI>("SystemToastUI_Day68"); // 시스템 Toast 생성
        } // 메서드 종료

        private static void EnsureKingRuntime() // 킹 관련 공통 호스트 초기화
        { // 메서드 범위
            KingCombatHUD legacyCombatHud = Object.FindFirstObjectByType<KingCombatHUD>(); // 기존 좌측 King HUD 조회
            if (legacyCombatHud != null) Object.Destroy(legacyCombatHud); // 구형 King HUD 제거

            GameObject legacyCombatCanvas = GameObject.Find("KingCombatHUDCanvas_Day61"); // 구형 King HUD Canvas 조회
            if (legacyCombatCanvas != null) Object.Destroy(legacyCombatCanvas); // 구형 Canvas 제거

            KingAbilityController existing = Object.FindFirstObjectByType<KingAbilityController>(); // 기존 킹 능력 관리자 조회
            if (existing != null) // 기존 관리자 확인
            { // 조건 범위
                if (existing.GetComponent<KingSelectionUI>() == null) existing.gameObject.AddComponent<KingSelectionUI>(); // 킹 선택 UI 보강
                if (existing.GetComponent<StrategyKingSelectionUI>() == null) existing.gameObject.AddComponent<StrategyKingSelectionUI>(); // 전략형 선택 UI 보강
                return; // 기존 호스트 재사용
            } // 조건 종료

            var host = new GameObject("KingAbilityController_Day49"); // 킹 공통 호스트 생성
            host.AddComponent<KingAbilityController>(); // 킹 패시브 관리자 추가
            host.AddComponent<KingSelectionUI>(); // 킹 선택 UI 추가
            host.AddComponent<StrategyKingSelectionUI>(); // 전략형 선택 UI 추가
        } // 메서드 종료

        private static void EnsureBattleAnnouncementRuntime() // 전투 알림 복합 호스트 초기화
        { // 메서드 범위
            Day67BattleAnnouncementCoordinator existing = Object.FindFirstObjectByType<Day67BattleAnnouncementCoordinator>(); // 기존 알림 관리자 조회
            if (existing != null) // 기존 관리자 확인
            { // 조건 범위
                if (existing.GetComponent<Day67BattleAnnouncementUI>() == null) existing.gameObject.AddComponent<Day67BattleAnnouncementUI>(); // 알림 UI 보강
                return; // 기존 호스트 재사용
            } // 조건 종료

            GameObject host = new GameObject("Day67BattleAnnouncementCoordinator"); // 전투 알림 호스트 생성
            host.AddComponent<Day67BattleAnnouncementUI>(); // 공통 알림 UI 우선 추가
            host.AddComponent<Day67BattleAnnouncementCoordinator>(); // 상태 연결 관리자 추가
        } // 메서드 종료

        public static BattleRuntimeDiagnostics RefreshBattleDiagnostics() // 현재 Battle 관리자 상태 재수집
        { // 메서드 범위
            if (SceneManager.GetActiveScene().name != SceneFlowController.BattleSceneName) // Battle 씬 여부 확인
            { // 조건 범위
                _latestDiagnostics = BattleRuntimeDiagnostics.Empty; // Battle 외 빈 결과 적용
                return _latestDiagnostics; // 빈 결과 반환
            } // 조건 종료

            var statuses = new List<RuntimeComponentStatus>(); // 관리자 상태 목록 생성
            AddStatus<BattleController>(statuses); // 전투 관리자 수집
            AddStatus<RoundRuntimeController>(statuses); // 라운드 관리자 수집
            AddStatus<RoundStateBattleBridge>(statuses); // 라운드 연결 수집
            AddStatus<Day41BattleRoomBootstrap>(statuses); // 전투방 관리자 수집
            AddStatus<LargePieceLifecycleController>(statuses); // 대형 기물 생명주기 수집
            AddStatus<LargePiecePlayerAttackBridge>(statuses); // 대형 기물 공격 연결 수집
            AddStatus<LargePieceTurnEndStatusBridge>(statuses); // 대형 기물 상태 연결 수집
            AddStatus<EnemyAITurnDriver>(statuses); // 적 AI 관리자 수집
            AddStatus<PrototypeBoss37Spawner>(statuses); // 보스 생성 관리자 수집
            AddStatus<PlayerActionTurnDelayController>(statuses); // 턴 지연 관리자 수집
            AddStatus<LethalAttackVisualBridge>(statuses); // 치명타 연출 관리자 수집
            AddStatus<KingAbilityController>(statuses); // 킹 능력 관리자 수집
            AddStatus<RouteMapBoardController>(statuses); // 경로 지도 관리자 수집
            AddStatus<FullRouteMapPreviewController>(statuses); // 전체 지도 관리자 수집
            AddStatus<StageTransitionController>(statuses); // 스테이지 전환 관리자 수집
            AddStatus<CardRewardController>(statuses); // 카드 보상 관리자 수집
            AddStatus<StageActivityController>(statuses); // 스테이지 활동 관리자 수집
            AddStatus<MetaProgressRunResultController>(statuses); // 메타 결과 관리자 수집
            AddStatus<RunPersistenceController>(statuses); // 저장 관리자 수집
            AddStatus<RunResultMainMenuController>(statuses); // 결과 복귀 UI 수집
            AddStatus<BattleSettingsOverlayController>(statuses); // 설정 패널 수집
            AddStatus<BattleHUD>(statuses); // 전투 HUD 수집
            AddStatus<BattleInteractionStatusUI>(statuses); // 전투 안내 UI 수집
            AddStatus<CombatFloatingTextUI>(statuses); // 전투 숫자 UI 수집
            AddStatus<Day64FusionUI>(statuses); // 합성 UI 수집
            AddStatus<Day65StageActivityHUD>(statuses); // 활동 HUD 수집
            AddStatus<Day66HandCardMotionController>(statuses); // 손패 모션 수집
            AddStatus<Day66BoardTransitionFX>(statuses); // 보드 전환 연출 수집
            AddStatus<Day67BattleAnnouncementCoordinator>(statuses); // 전투 알림 관리자 수집
            AddStatus<Day67BattleAnnouncementUI>(statuses); // 전투 알림 UI 수집
            AddStatus<FirstRunTutorialController>(statuses); // 최초 튜토리얼 수집
            AddStatus<SystemToastUI>(statuses); // 시스템 Toast 수집
            _latestDiagnostics = BattleRuntimeDiagnostics.Create(SceneFlowController.BattleSceneName, _lastInitializationMilliseconds, statuses); // 최신 진단 결과 생성
            return _latestDiagnostics; // 최신 진단 결과 반환
        } // 메서드 종료

        private static void AddStatus<T>(List<RuntimeComponentStatus> statuses) where T : Component // 단일 관리자 개수 수집
        { // 메서드 범위
            int count = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length; // 활성·비활성 객체 전체 개수 계산
            statuses.Add(new RuntimeComponentStatus(typeof(T).Name, count)); // 관리자 상태 목록 추가
        } // 메서드 종료

        private static T EnsureComponent<T>(string objectName) where T : Component // 단일 관리자 생성 보장
        { // 메서드 범위
            T existing = Object.FindFirstObjectByType<T>(); // 현재 활성 관리자 조회
            if (existing != null) return existing; // 기존 인스턴스 재사용

            var host = new GameObject(objectName); // 런타임 관리자 호스트 생성
            return host.AddComponent<T>(); // 요청 컴포넌트 생성·반환
        } // 메서드 종료

        private void OnDestroy() // 부트스트랩 제거 처리
        { // 메서드 범위
            SceneManager.sceneLoaded -= HandleSceneLoaded; // 씬 이벤트 구독 해제
            if (_instance == this) _instance = null; // 현재 정적 인스턴스 제거
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
