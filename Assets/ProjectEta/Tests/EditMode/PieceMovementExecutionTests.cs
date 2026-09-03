using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class PieceMovementExecutionTests // 11일차: MovementResolver 결과를 실제 클릭 이동으로 연결하는 로직을 검증하는 테스트 모음
    {
        private static (GameObject Root, BoardInputController Input, RunState RunState, TurnManager TurnManager) CreateBoundContext() // 테스트마다 반복되는 초기화를 모아둔 도우미 메서드
        {
            var root = new GameObject("MovementTestRoot"); // 테스트용 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 입력 컨트롤러 추가
            var runState = new RunState(3); // 실제 전투와 같은 방식의 런 상태 생성
            var turnManager = new TurnManager(); // 실제 전투와 같은 방식의 턴 매니저 생성

            boardView.Bind(runState.Board); // 보드 뷰에 실제 보드 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력에 실제 런 상태와 턴 매니저 연결

            return (root, boardInput, runState, turnManager); // 테스트에서 바로 쓸 수 있도록 묶어서 반환
        }

        [Test] // 내 기물이 있는 칸을 선택하면 이동/공격 후보가 계산되는지 확인하는 테스트
        public void TrySelectPieceAt_ComputesMovementCandidates_ForOwnPiece()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try
            {
                var kingDefinition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 킹 정의(기본 이동 타입 King)
                var origin = new Vector2Int(4, 1); // 아군 영역 안의 시작 좌표
                var piece = new PieceRuntimeState(kingDefinition, origin, isPlayerPiece: true); // 아군 기물 런타임 상태 생성
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 실제 보드에 기물을 직접 배치

                bool selected = context.Input.TrySelectPieceAt(origin); // 기물이 있는 칸 선택 시도

                Assert.IsTrue(selected); // 선택이 성공해야 함
                Assert.AreSame(piece, context.Input.SelectedPiece); // 선택된 기물이 정확히 이 인스턴스여야 함
                CollectionAssert.Contains(context.Input.PendingMovement.MoveTiles, new Vector2Int(4, 2)); // 킹의 8방향 1칸 후보 중 하나가 포함돼야 함
            }
            finally
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 이동 후보 칸으로 이동을 실행하면 보드 점유와 좌표, 턴 상태가 함께 갱신되는지 확인하는 테스트
        public void TryMoveSelectedPieceTo_UpdatesBoardOccupancy_AndCompletesPlayerTurn()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try
            {
                var kingDefinition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 킹 정의
                var origin = new Vector2Int(4, 1); // 시작 좌표
                var destination = new Vector2Int(4, 2); // 킹의 8방향 1칸 이동 후보 중 하나
                var piece = new PieceRuntimeState(kingDefinition, origin, isPlayerPiece: true); // 아군 기물 런타임 상태 생성
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 실제 보드에 기물을 직접 배치
                context.Input.TrySelectPieceAt(origin); // 기물 선택

                bool moved = context.Input.TryMoveSelectedPieceTo(destination); // 후보 칸으로 이동 실행

                Assert.IsTrue(moved); // 이동이 성공해야 함
                Assert.AreEqual(destination, piece.BoardPosition); // 기물의 실제 좌표가 갱신돼야 함
                Assert.IsNull(context.RunState.Board.GetTile(origin).OccupyingPiece); // 원래 칸은 비워져야 함
                Assert.AreSame(piece, context.RunState.Board.GetTile(destination).OccupyingPiece); // 새 칸이 이 기물로 점유돼야 함
                Assert.IsNull(context.Input.SelectedPiece); // 이동 후에는 선택이 해제돼야 함
                Assert.AreEqual(TurnState.EnemyTurn, context.TurnManager.CurrentState); // 이동이 플레이어 행동으로 처리돼 적 턴으로 전환돼야 함
            }
            finally
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 이동 후보가 아닌 칸으로는 이동이 거부되고 턴도 넘어가지 않는지 확인하는 테스트
        public void TryMoveSelectedPieceTo_Fails_WhenDestinationIsNotACandidate()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try
            {
                var kingDefinition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 킹 정의
                var origin = new Vector2Int(4, 1); // 시작 좌표
                var farAway = new Vector2Int(9, 9); // 킹의 이동 후보에 포함될 수 없는 먼 좌표
                var piece = new PieceRuntimeState(kingDefinition, origin, isPlayerPiece: true); // 아군 기물 런타임 상태 생성
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 실제 보드에 기물을 직접 배치
                context.Input.TrySelectPieceAt(origin); // 기물 선택

                bool moved = context.Input.TryMoveSelectedPieceTo(farAway); // 후보가 아닌 칸으로 이동 시도

                Assert.IsFalse(moved); // 이동이 거부돼야 함
                Assert.AreEqual(origin, piece.BoardPosition); // 좌표는 그대로 유지돼야 함
                Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 턴도 넘어가지 않아야 함
            }
            finally
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }

        [Test] // 적 턴에는 기물 선택 자체가 되지 않아야 함을 확인하는 테스트
        public void TrySelectPieceAt_Fails_DuringEnemyTurn()
        {
            var context = CreateBoundContext(); // 공통 초기화 수행

            try
            {
                var kingDefinition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 킹 정의
                var origin = new Vector2Int(4, 1); // 시작 좌표
                var piece = new PieceRuntimeState(kingDefinition, origin, isPlayerPiece: true); // 아군 기물 런타임 상태 생성
                context.RunState.Board.GetTile(origin).OccupyingPiece = piece; // 실제 보드에 기물을 직접 배치
                context.TurnManager.TryCompletePlayerAction(); // 강제로 적 턴으로 전환

                bool selected = context.Input.TrySelectPieceAt(origin); // 적 턴 중 선택 시도

                Assert.IsFalse(selected); // 적 턴에는 CanReceivePlayerInput이 false가 되어 선택 자체가 거부돼야 함
                Assert.IsNull(context.Input.SelectedPiece); // 선택 상태도 비어 있어야 함
            }
            finally
            {
                Object.DestroyImmediate(context.Root); // 테스트 오브젝트 정리
            }
        }
    }
}
