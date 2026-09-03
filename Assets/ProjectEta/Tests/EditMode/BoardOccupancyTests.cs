using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 테스트 코드를 모아두는 네임스페이스
{
    public class BoardOccupancyTests // 2x2 대형 기물 점유(BoardState.TryOccupyArea)를 검증하는 테스트 클래스
    {
        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void TryOccupyArea_Succeeds_And_Marks_All_Tiles_When_Area_Is_Empty() // 빈 2x2 영역 점유가 성공하고 4칸 모두 표시되는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var anchor = new Vector2Int(4, 4); // 2x2 영역의 기준(왼쪽 아래) 좌표
            var boss = new PieceRuntimeState(definition, anchor, isPlayerPiece: false); // 테스트용 보스 런타임 상태 생성

            bool succeeded = board.TryOccupyArea(anchor, new Vector2Int(2, 2), boss); // 2x2 영역 점유 시도

            Assert.IsTrue(succeeded); // 점유가 성공해야 함
            Assert.AreSame(boss, board.GetTile(new Vector2Int(4, 4)).OccupyingPiece); // 좌하단 칸이 점유됐는지 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(5, 4)).OccupyingPiece); // 우하단 칸이 점유됐는지 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(4, 5)).OccupyingPiece); // 좌상단 칸이 점유됐는지 확인
            Assert.AreSame(boss, board.GetTile(new Vector2Int(5, 5)).OccupyingPiece); // 우상단 칸이 점유됐는지 확인
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void TryOccupyArea_Fails_When_One_Tile_Is_Already_Occupied() // 영역 일부가 이미 점유돼 있으면 전체 점유가 실패하는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var blocker = new PieceRuntimeState(definition, new Vector2Int(5, 5), isPlayerPiece: false); // 영역 안 한 칸을 미리 점유할 기물 생성
            board.GetTile(new Vector2Int(5, 5)).OccupyingPiece = blocker; // 2x2 영역 안의 한 칸을 미리 점유시킴

            var boss = new PieceRuntimeState(definition, new Vector2Int(4, 4), isPlayerPiece: false); // 테스트용 보스 런타임 상태 생성
            bool succeeded = board.TryOccupyArea(new Vector2Int(4, 4), new Vector2Int(2, 2), boss); // 2x2 영역 점유 시도

            Assert.IsFalse(succeeded); // 점유가 실패해야 함
            Assert.IsNull(board.GetTile(new Vector2Int(4, 4)).OccupyingPiece); // 실패 시 다른 칸은 점유되지 않은 상태로 남아야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void ClearArea_Frees_All_Tiles_In_The_Footprint() // ClearArea가 점유했던 영역을 모두 비우는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var anchor = new Vector2Int(2, 2); // 2x2 영역의 기준 좌표
            var boss = new PieceRuntimeState(definition, anchor, isPlayerPiece: false); // 테스트용 보스 런타임 상태 생성
            board.TryOccupyArea(anchor, new Vector2Int(2, 2), boss); // 2x2 영역 점유

            board.ClearArea(anchor, new Vector2Int(2, 2)); // 점유했던 영역을 비움

            Assert.IsNull(board.GetTile(new Vector2Int(2, 2)).OccupyingPiece); // 좌하단 칸이 비워졌는지 확인
            Assert.IsNull(board.GetTile(new Vector2Int(3, 3)).OccupyingPiece); // 우상단 칸이 비워졌는지 확인
        }
    }
}
