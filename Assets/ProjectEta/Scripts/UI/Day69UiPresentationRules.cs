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
