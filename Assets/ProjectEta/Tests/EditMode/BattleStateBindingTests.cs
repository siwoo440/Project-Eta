using NUnit.Framework; // EditMode 테스트 어트리뷰트와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, Object를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class BattleStateBindingTests // 9일차 단일 상태 연결을 검증하는 테스트 모음
    {
        [Test] // RunState.Board와 BoardView가 서로 다른 보드를 만들지 않는지 검증
        public void BoardViewBind_UsesExactRunStateBoardInstance()
        {
            var boardObject = new GameObject("BoardViewTest"); // 테스트용 오브젝트 생성

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var boardView = boardObject.AddComponent<BoardView>(); // 보드 뷰 생성
                var runState = new RunState(3); // 실제 전투에서 사용할 것과 같은 런 상태 생성

                boardView.Bind(runState.Board); // RunState.Board를 뷰에 주입

                Assert.That(boardView.State, Is.SameAs(runState.Board)); // 복사본이 아니라 정확히 같은 BoardState 인스턴스여야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(boardObject); // 테스트 오브젝트 정리
            }
        }

        [Test] // BoardInputController가 별도 HandState 대신 RunState.Hand를 사용하는지 검증
        public void BoardInputBind_UsesExactRunStateHandInstance()
        {
            var boardObject = new GameObject("BoardInputTest"); // 테스트용 오브젝트 생성

            try // 테스트 중 예외가 나도 아래 finally에서 오브젝트를 정리하도록 보장하는 블록
            {
                var boardView = boardObject.AddComponent<BoardView>(); // 같은 오브젝트에 보드 뷰 추가
                var boardInput = boardObject.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
                var runState = new RunState(3); // 실제 런 상태 생성

                boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
                boardInput.Bind(runState, boardView); // 입력에 실제 런 상태 연결

                Assert.That(boardInput.RunState, Is.SameAs(runState)); // 입력이 정확히 같은 RunState를 참조해야 함
                Assert.That(boardInput.HandState, Is.SameAs(runState.Hand)); // 입력 손패가 정확히 RunState.Hand여야 함
                Assert.That(boardView.State, Is.SameAs(runState.Board)); // 입력과 화면이 같은 런의 보드를 바라봐야 함
            }
            finally // 성공/실패와 무관하게 테스트 오브젝트를 정리하는 블록
            {
                Object.DestroyImmediate(boardObject); // 테스트 오브젝트 정리
            }
        }
    }
}
