using NUnit.Framework; // EditMode 단위 테스트 기능을 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, Object, Vector2를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnStatusUI를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class TurnStatusUITests // 상단 중앙 Canvas 턴 UI를 검증하는 테스트 모음
    {
        [Test] // TurnStatusUI가 상단 중앙 Canvas를 만들고 플레이어 턴 문구를 표시하는지 검증
        public void Bind_CreatesTopCenterCanvasAndShowsPlayerTurn()
        {
            var host = new GameObject("TurnStatusUITest"); // 테스트용 UI 호스트 오브젝트 생성

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var turnManager = new TurnManager(); // 테스트용 턴 매니저 생성
                var ui = host.AddComponent<TurnStatusUI>(); // 턴 상태 UI 컴포넌트 추가
                ui.Bind(turnManager); // 턴 매니저를 UI에 연결

                Assert.That(ui.StatusCanvas, Is.Not.Null); // Screen Space Canvas가 생성됐는지 검증
                Assert.That(ui.PanelRect, Is.Not.Null); // 상단 상태 패널 RectTransform이 생성됐는지 검증
                Assert.That(ui.PanelRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f))); // 패널 최소 앵커가 화면 상단 중앙인지 검증
                Assert.That(ui.PanelRect.anchorMax, Is.EqualTo(new Vector2(0.5f, 1f))); // 패널 최대 앵커가 화면 상단 중앙인지 검증
                Assert.That(ui.DisplayText, Does.Contain("1턴")); // 1턴 텍스트가 표시되는지 검증
                Assert.That(ui.DisplayText, Does.Contain("플레이어")); // 플레이어 턴 텍스트가 표시되는지 검증
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(host); // 테스트 오브젝트와 자식 Canvas를 정리
            }
        }

        [Test] // 턴 변경 이벤트가 UI 텍스트에 즉시 반영되는지 검증
        public void TurnChanged_RefreshesDisplayedTurnState()
        {
            var host = new GameObject("TurnStatusUIRefreshTest"); // 테스트용 UI 호스트 오브젝트 생성

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var turnManager = new TurnManager(); // 테스트용 턴 매니저 생성
                var ui = host.AddComponent<TurnStatusUI>(); // 턴 상태 UI 컴포넌트 추가
                ui.Bind(turnManager); // 턴 매니저를 UI에 연결

                turnManager.TryCompletePlayerAction(); // 플레이어 행동을 완료해 적 턴으로 전환
                Assert.That(ui.DisplayText, Does.Contain("적 턴")); // UI가 적 턴 문구로 바뀌었는지 검증

                turnManager.CompleteEnemyTurn(); // 적 턴을 끝내 다음 플레이어 턴으로 전환
                Assert.That(ui.DisplayText, Does.Contain("2턴")); // 다음 턴 번호가 UI에 반영되는지 검증
                Assert.That(ui.DisplayText, Does.Contain("플레이어")); // 다음 플레이어 턴 문구가 표시되는지 검증
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(host); // 테스트 오브젝트와 자식 Canvas를 정리
            }
        }
    }
}
