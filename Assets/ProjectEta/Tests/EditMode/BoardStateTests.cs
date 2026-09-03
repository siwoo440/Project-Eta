using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 테스트 코드를 모아두는 네임스페이스
{
    public class BoardStateTests // BoardState 동작을 검증하는 테스트 클래스
    {
        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Board_Is_10x10() // 보드 크기가 10x10인지 확인하는 테스트
        {
            Assert.AreEqual(10, BoardState.Width); // 가로 크기가 10인지 검증
            Assert.AreEqual(10, BoardState.Height); // 세로 크기가 10인지 검증
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void IsInsideBoard_Returns_False_Outside_Bounds() // 범위 판정이 올바른지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 상태 생성
            Assert.IsFalse(board.IsInsideBoard(new Vector2Int(-1, 0))); // 왼쪽 범위 밖은 false여야 함
            Assert.IsFalse(board.IsInsideBoard(new Vector2Int(10, 0))); // 오른쪽 범위 밖은 false여야 함
            Assert.IsTrue(board.IsInsideBoard(new Vector2Int(0, 0))); // 좌하단 모서리는 true여야 함
            Assert.IsTrue(board.IsInsideBoard(new Vector2Int(9, 9))); // 우상단 모서리는 true여야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void PlacementAreas_Split_Evenly_Between_Player_And_Enemy() // 아군/적군 영역 분할이 올바른지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 상태 생성
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 방향으로 순회
                {
                    var tile = board.GetTile(new Vector2Int(x, y)); // 현재 좌표의 타일 조회
                    if (y < BoardState.Height / 2) // 아래쪽 절반이면
                    {
                        Assert.IsTrue(tile.IsPlayerPlacementArea); // 아군 영역이어야 함
                        Assert.IsFalse(tile.IsEnemyPlacementArea); // 적군 영역이 아니어야 함
                    }
                    else // 위쪽 절반이면
                    {
                        Assert.IsFalse(tile.IsPlayerPlacementArea); // 아군 영역이 아니어야 함
                        Assert.IsTrue(tile.IsEnemyPlacementArea); // 적군 영역이어야 함
                    }
                }
            }
        }

        [Test] // 13일차: 보드 위 아군/적군 기물 수를 정확히 세는지 확인하는 테스트
        public void CountPieces_ReturnsExactCountPerSide()
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성

            board.GetTile(new Vector2Int(4, 1)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(4, 1), isPlayerPiece: true); // 아군 기물 1 배치
            board.GetTile(new Vector2Int(3, 1)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(3, 1), isPlayerPiece: true); // 아군 기물 2 배치
            board.GetTile(new Vector2Int(4, 8)).OccupyingPiece = new PieceRuntimeState(definition, new Vector2Int(4, 8), isPlayerPiece: false); // 적군 기물 1 배치

            Assert.AreEqual(2, board.CountPieces(isPlayerPiece: true)); // 아군 기물이 정확히 2개여야 함
            Assert.AreEqual(1, board.CountPieces(isPlayerPiece: false)); // 적군 기물이 정확히 1개여야 함
        }
    }
}
