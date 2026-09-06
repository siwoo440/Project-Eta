namespace ProjectEta.UI
{
    public enum MainMenuPanel
    {
        Main = 0, // 메인 메뉴
        Meta = 1, // 영구 성장
        Settings = 2, // 설정
        NewGameConfirm = 3, // 새 게임 확인
        QuitConfirm = 4 // 게임 종료 확인
    }

    public sealed class MainMenuNavigationState
    {
        public MainMenuPanel CurrentPanel { get; private set; } = MainMenuPanel.Main; // 현재 표시 패널

        public bool RequestNewGame(bool hasContinueData)
        {
            if (!hasContinueData) return true; // 진행 저장이 없으면 즉시 새 게임 허용

            CurrentPanel = MainMenuPanel.NewGameConfirm; // 기존 런 삭제 확인 팝업 진입
            return false; // 확인 전 새 게임 시작 차단
        }

        public void RequestQuit()
        {
            CurrentPanel = MainMenuPanel.QuitConfirm; // 게임 종료 확인 팝업 진입
        }

        public void ShowMeta()
        {
            CurrentPanel = MainMenuPanel.Meta; // 영구 성장 패널 진입
        }

        public void ShowSettings()
        {
            CurrentPanel = MainMenuPanel.Settings; // 설정 패널 진입
        }

        public void ShowMain()
        {
            CurrentPanel = MainMenuPanel.Main; // 메인 메뉴 복귀
        }

        public bool TryBack()
        {
            if (CurrentPanel == MainMenuPanel.Main) return false; // 메인 메뉴에서는 뒤로가기 소비 안 함

            CurrentPanel = MainMenuPanel.Main; // 서브 패널·팝업에서 메인 메뉴 복귀
            return true; // 뒤로가기 처리 성공
        }
    }
}
