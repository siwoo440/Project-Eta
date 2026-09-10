namespace ProjectEta.Settings
{
    public enum BattlePausePanel
    {
        Closed = 0,
        Pause = 1,
        Controls = 2,
        Settings = 3
    }

    public sealed class BattlePauseNavigationState
    {
        public BattlePausePanel CurrentPanel { get; private set; } = BattlePausePanel.Closed; // 현재 Pause 하위 화면
        public bool IsOpen => CurrentPanel != BattlePausePanel.Closed; // Pause 계층 열림 여부

        public void OpenPause()
        {
            CurrentPanel = BattlePausePanel.Pause; // Pause 기본 화면 진입
        }

        public void ShowControls()
        {
            if (!IsOpen) return; // 닫힌 상태 직접 진입 차단
            CurrentPanel = BattlePausePanel.Controls; // 조작법 화면 진입
        }

        public void ShowSettings()
        {
            if (!IsOpen) return; // 닫힌 상태 직접 진입 차단
            CurrentPanel = BattlePausePanel.Settings; // 설정 화면 진입
        }

        public bool TryBack()
        {
            if (CurrentPanel == BattlePausePanel.Settings || CurrentPanel == BattlePausePanel.Controls)
            {
                CurrentPanel = BattlePausePanel.Pause; // 하위 화면에서 Pause로 복귀
                return true; // 뒤로가기 처리 완료
            }

            if (CurrentPanel == BattlePausePanel.Pause)
            {
                CurrentPanel = BattlePausePanel.Closed; // Pause에서 게임으로 복귀
                return true; // 뒤로가기 처리 완료
            }

            return false; // 닫힌 상태 처리 없음
        }

        public void Close()
        {
            CurrentPanel = BattlePausePanel.Closed; // Pause 계층 전체 닫기
        }
    }
}
