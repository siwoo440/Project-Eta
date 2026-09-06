using UnityEngine; // FullScreenMode 사용

namespace ProjectEta.Settings
{
    public sealed class GameSettingsEditState
    {
        public GameSettingsData Saved { get; private set; } // 마지막 적용 설정
        public GameSettingsData Editing { get; private set; } // 현재 편집 설정
        public bool IsDirty => !Editing.ContentEquals(Saved); // 적용 전 변경 여부

        public GameSettingsEditState(GameSettingsData initial)
        {
            Saved = (initial ?? GameSettingsData.CreateDefault()).Normalized(); // 초기 저장 설정 안전 보정
            Editing = Saved.Clone(); // 초기 편집 설정 복제
        }

        public void SetResolution(int width, int height)
        {
            Editing.ResolutionWidth = width; // 편집 해상도 너비 변경
            Editing.ResolutionHeight = height; // 편집 해상도 높이 변경
            Editing = Editing.Normalized(); // 편집 설정 안전 범위 보정
        }

        public void SetScreenMode(FullScreenMode mode)
        {
            Editing.ScreenMode = (int)mode; // 편집 화면 모드 변경
            Editing = Editing.Normalized(); // 지원 화면 모드 보정
        }

        public void SetUiScale(float value)
        {
            Editing.UiScale = value; // 편집 UI 배율 변경
            Editing = Editing.Normalized(); // UI 배율 안전 범위 보정
        }

        public GameSettingsData Apply()
        {
            Saved = Editing.Normalized(); // 편집값을 저장값으로 확정
            Editing = Saved.Clone(); // 확정값 기준 편집 상태 재생성
            return Saved.Clone(); // 외부 적용용 복제 반환
        }

        public void Cancel()
        {
            Editing = Saved.Clone(); // 적용되지 않은 편집값 폐기
        }

        public void ResetToDefault()
        {
            Editing = GameSettingsData.CreateDefault().Normalized(); // 기본 설정을 편집값으로 적용
        }
    }
}
