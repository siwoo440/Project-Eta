using System; // Serializable·Enum 사용
using UnityEngine; // FullScreenMode·Mathf 사용

namespace ProjectEta.Settings
{
    [Serializable]
    public sealed class GameSettingsData
    {
        public const int MinimumWidth = 640; // 최소 지원 해상도 너비
        public const int MinimumHeight = 360; // 최소 지원 해상도 높이
        public const float MinimumUiScale = 0.75f; // 최소 UI 배율
        public const float MaximumUiScale = 1.35f; // 최대 UI 배율
        public const float DefaultUiScale = 1f; // 기본 UI 배율
        public const int DefaultWidth = 1920; // 기본 해상도 너비
        public const int DefaultHeight = 1080; // 기본 해상도 높이

        public int ResolutionWidth = DefaultWidth; // 저장 해상도 너비
        public int ResolutionHeight = DefaultHeight; // 저장 해상도 높이
        public int ScreenMode = (int)FullScreenMode.FullScreenWindow; // 저장 화면 모드
        public float UiScale = DefaultUiScale; // 저장 UI 배율

        public static GameSettingsData CreateDefault()
        {
            return new GameSettingsData
            {
                ResolutionWidth = DefaultWidth, // 기본 해상도 너비 적용
                ResolutionHeight = DefaultHeight, // 기본 해상도 높이 적용
                ScreenMode = (int)FullScreenMode.FullScreenWindow, // 기본 전체화면 창 적용
                UiScale = DefaultUiScale // 기본 UI 배율 적용
            };
        }

        public GameSettingsData Clone()
        {
            return new GameSettingsData
            {
                ResolutionWidth = ResolutionWidth, // 해상도 너비 복제
                ResolutionHeight = ResolutionHeight, // 해상도 높이 복제
                ScreenMode = ScreenMode, // 화면 모드 복제
                UiScale = UiScale // UI 배율 복제
            };
        }

        public GameSettingsData Normalized()
        {
            int width = Mathf.Max(MinimumWidth, ResolutionWidth); // 최소 너비 보정
            int height = Mathf.Max(MinimumHeight, ResolutionHeight); // 최소 높이 보정
            int mode = IsSupportedScreenMode(ScreenMode) ? ScreenMode : (int)FullScreenMode.FullScreenWindow; // 지원 화면 모드 보정
            float uiScale = Mathf.Clamp(UiScale, MinimumUiScale, MaximumUiScale); // UI 배율 안전 범위 보정

            return new GameSettingsData
            {
                ResolutionWidth = width, // 보정 너비 저장
                ResolutionHeight = height, // 보정 높이 저장
                ScreenMode = mode, // 보정 화면 모드 저장
                UiScale = uiScale // 보정 UI 배율 저장
            };
        }

        public bool ContentEquals(GameSettingsData other)
        {
            if (other == null) return false; // 비교 대상 누락 차단
            if (ResolutionWidth != other.ResolutionWidth) return false; // 너비 차이 확인
            if (ResolutionHeight != other.ResolutionHeight) return false; // 높이 차이 확인
            if (ScreenMode != other.ScreenMode) return false; // 화면 모드 차이 확인
            return Mathf.Abs(UiScale - other.UiScale) < 0.001f; // UI 배율 동일 여부 반환
        }

        public static bool IsSupportedScreenMode(int mode)
        {
            return mode == (int)FullScreenMode.FullScreenWindow || mode == (int)FullScreenMode.Windowed; // 56일차 지원 화면 모드 확인
        }
    }
}
