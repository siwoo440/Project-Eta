using NUnit.Framework; // EditMode 단위 테스트 기능을 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, Object, Vector2를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager와 TurnStatusUI를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class TurnStatusUITests // 상단 중앙 턴 UI의 자유 배치 표시를 검증하는 테스트 모음
    {
        [Test] // 시작 시 킹 필수 문구가 표시되는지 검증
        public void Bind_ShowsInitialKingRequired()
        {
            var host = new GameObject("TurnStatusUITest"); // 테스트 UI 호스트 생성

            try // 정리 보장
            {
                var manager = new TurnManager(); // 시작 배치 상태 생성
                var ui = host.AddComponent<TurnStatusUI>(); // UI 추가
                ui.Bind(manager); // 연결

                Assert.That(ui.PanelRect.anchorMin, Is.EqualTo(new Vector2(0.5f, 1f))); // 상단 중앙 확인
                Assert.That(ui.DisplayText, Does.Contain("킹 배치 필수")); // 킹 필수 문구 확인
            }
            finally // 성공/실패와 무관하게 정리
            {
                Object.DestroyImmediate(host); // UI 제거
            }
        }

        [Test] // 킹 배치 후에도 자유 배치 상태가 표시되고 턴이 자동 종료되지 않는지 검증
        public void InitialKingPlaced_ShowsFreePlacementUntilExplicitEnd()
        {
            var host = new GameObject("TurnStatusUIFreePlacementTest"); // 테스트 UI 호스트 생성

            try // 정리 보장
            {
                var manager = new TurnManager(); // 시작 배치 상태 생성
                var ui = host.AddComponent<TurnStatusUI>(); // UI 추가
                ui.Bind(manager); // 연결

                manager.MarkInitialKingPlaced(); // 킹 배치 조건 충족
                manager.RegisterDeployment(); // 킹 배치 수 등록
                manager.RegisterDeployment(); // 추가 카드 배치 수 등록

                Assert.AreEqual(TurnState.DeploymentTurn, manager.CurrentState); // 여전히 배치 턴
                Assert.That(ui.DisplayText, Does.Contain("자유 배치")); // 자유 배치 문구
                Assert.That(ui.DisplayText, Does.Contain("2장")); // 배치 수 표시
                Assert.That(ui.DisplayText, Does.Contain("Space 종료")); // 명시적 종료 안내
            }
            finally // 정리 보장
            {
                Object.DestroyImmediate(host); // UI 제거
            }
        }
    }
}
