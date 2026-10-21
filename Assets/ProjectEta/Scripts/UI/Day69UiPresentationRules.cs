using UnityEngine.InputSystem; // 모달 진입 단축키 확인
using ProjectEta.Debugging; // 디버그 입력 소유
using ProjectEta.Environment; // 걷기 입력 소유
using ProjectEta.SceneFlow; // 씬 전환 입력 차단
using UnityEngine; // Object 런타임 조회 사용
using ProjectEta.Meta; // MetaProgressUI 사용
using ProjectEta.Settings; // BattleSettingsOverlayController 사용

namespace ProjectEta.UI
{
    public struct Day69UiModalState
    {
        public bool PauseOpen { get; } // Pause 표시 여부
        public bool TutorialOpen { get; } // 튜토리얼 표시 여부
        public bool RewardOpen { get; } // Reward 표시 여부
        public bool ActivityOpen { get; } // Shop·Event 표시 여부
        public bool RunResultOpen { get; } // Run Result 표시 여부
        public bool BattleAnnouncementBusy { get; } // 전투 알림 재생·대기 여부

        public Day69UiModalState(bool pauseOpen, bool tutorialOpen, bool rewardOpen, bool activityOpen, bool runResultOpen, bool battleAnnouncementBusy = false)
        {
            PauseOpen = pauseOpen; // Pause 상태 저장
            TutorialOpen = tutorialOpen; // 튜토리얼 상태 저장
            RewardOpen = rewardOpen; // Reward 상태 저장
            ActivityOpen = activityOpen; // Stage Activity 상태 저장
            RunResultOpen = runResultOpen; // Run Result 상태 저장
            BattleAnnouncementBusy = battleAnnouncementBusy; // 전투 알림 상태 저장
        }
    }

    public static class Day69UiPresentationRules
    {
        private static int _consumedFrame = -1; // 이미 화면 전환에 사용된 프레임
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 재실행 상태 초기화
        private static void ResetInputFrame() // 이전 런의 입력 소유 정리
        { // 메서드 시작
            _consumedFrame = -1; // 아직 소비한 프레임 없음
        } // 메서드 종료
        public static void ConsumeCurrentFrame() // 같은 입력의 뒤 화면 전달 차단
        { // 메서드 시작
            if (Application.isPlaying) _consumedFrame = Time.frameCount; // 실제 플레이 입력만 소비
        } // 메서드 종료
        public static bool IsCurrentFrameConsumed => Application.isPlaying && _consumedFrame == Time.frameCount; // 현재 전환 입력 소유
        public static bool CanUseWorldInput() // 걷기·시점의 공통 모달 보호
        { // 메서드 시작
            return !IsGameplayInputBlocked() && CanShowSystemToast(CaptureCurrent()); // 시스템 화면 우선
        } // 메서드 종료
        public static bool CanUseTableInput() // 보드·지도·턴 완료의 공통 보호
        { // 메서드 시작
            return !RoomWalkingController.BlocksTableInput && CanUseWorldInput(); // 걷기와 모달 아래 입력 차단
        } // 메서드 종료
        public static bool CanUseActivityInput() // 상점·이벤트 자신의 선택 허용
        { // 메서드 시작
            Day69UiModalState state = CaptureCurrent(); // 현재 화면 우선순위
            return !IsGameplayInputBlocked() && !RoomWalkingController.BlocksTableInput && !state.PauseOpen && !state.TutorialOpen && !state.RewardOpen && !state.RunResultOpen && !state.BattleAnnouncementBusy; // 자신보다 위의 화면 차단
        } // 메서드 종료
        public static bool CanUseRewardInput() // 카드 보상 자신의 선택 허용
        { // 메서드 시작
            Day69UiModalState state = CaptureCurrent(); // 현재 화면 우선순위
            return !IsGameplayInputBlocked() && !RoomWalkingController.BlocksTableInput && !state.PauseOpen && !state.TutorialOpen && !state.ActivityOpen && !state.RunResultOpen && !state.BattleAnnouncementBusy; // 보상 위 모달 입력 차단
        } // 메서드 종료
        private static bool IsGameplayInputBlocked() // 실행 순서와 독립된 전환 프레임 차단
        { // 메서드 시작
            Keyboard keyboard = Keyboard.current; // 현재 입력 장치
            bool systemKey = keyboard != null && (keyboard[GameInputBindingService.GetKey(GameInputAction.Pause)].wasPressedThisFrame || keyboard.f1Key.wasPressedThisFrame); // 관리자 실행 전에도 시스템 키 우선
            return Time.timeScale <= 0f || ProjectEtaDebugWindow.IsOpen || SceneFlowController.IsTransitioning || IsCurrentFrameConsumed || systemKey; // 진행 중단과 같은 프레임 재입력 보호
        } // 메서드 종료

        public static bool CanShowSystemToast(Day69UiModalState state)
        {
            return !state.PauseOpen
                && !state.TutorialOpen
                && !state.RewardOpen
                && !state.ActivityOpen
                && !state.RunResultOpen
                && !state.BattleAnnouncementBusy; // 주요 모달·전투 알림이 없을 때만 시스템 Toast 허용
        }

        public static bool CanOpenPause(Day69UiModalState state)
        {
            return !state.TutorialOpen && !state.RunResultOpen; // 튜토리얼·런 결과 위 Pause 중첩 차단
        }

        public static bool ShouldCreateDevelopmentUi(bool isEditor, bool isDebugBuild)
        {
            return isEditor || isDebugBuild; // 에디터·Development Build에서만 개발용 UI 허용
        }

        public static Day69UiModalState CaptureCurrent()
        {
            BattleSettingsOverlayController pause = Object.FindFirstObjectByType<BattleSettingsOverlayController>(); // 현재 Pause 관리자 조회
            CardRewardUI reward = Object.FindFirstObjectByType<CardRewardUI>(); // 현재 Reward UI 조회
            StageBoardOverlayUI activity = Object.FindFirstObjectByType<StageBoardOverlayUI>(); // 현재 Shop·Event UI 조회
            MetaProgressUI runResult = Object.FindFirstObjectByType<MetaProgressUI>(); // 현재 Run Result UI 조회
            Day67BattleAnnouncementUI announcement = Object.FindFirstObjectByType<Day67BattleAnnouncementUI>(); // 현재 전투 알림 UI 조회

            return new Day69UiModalState(
                pause != null && pause.IsOpen,
                FirstRunTutorialController.IsAnyTutorialOpen,
                reward != null && reward.IsVisible,
                activity != null && activity.IsVisible,
                runResult != null && runResult.IsVisible,
                announcement != null && announcement.IsBusy); // 현재 화면 모달·전투 알림 상태 묶음 반환
        }
    }
}
