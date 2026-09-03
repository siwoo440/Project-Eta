using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState, MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceMovementType, PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 테스트 코드를 모아두는 네임스페이스
{
    public class MovementResolverTests // MovementResolver의 이동/공격 계산을 검증하는 테스트 클래스
    {
        private static PieceRuntimeState PlacePiece(BoardState board, Vector2Int position, bool isPlayerPiece) // 테스트용 기물을 보드에 배치하는 도우미 메서드
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트용 임시 기물 정의 생성
            var piece = new PieceRuntimeState(definition, position, isPlayerPiece); // 테스트용 런타임 상태 생성
            board.GetTile(position).OccupyingPiece = piece; // 해당 칸에 기물 배치
            return piece; // 생성한 기물 반환
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Rook_Slides_Until_Board_Edge_When_Path_Is_Clear() // 룩이 빈 보드에서 끝까지 미끄러지는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(0, 0); // 룩 시작 좌표(왼쪽 아래 모서리)

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Rook, origin, isPlayerPiece: true, board); // 룩 이동 계산

            Assert.Contains(new Vector2Int(9, 0), result.MoveTiles); // 가로 끝까지 이동 가능해야 함
            Assert.Contains(new Vector2Int(0, 9), result.MoveTiles); // 세로 끝까지 이동 가능해야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(1, 1))); // 대각선 칸은 룩 이동 범위가 아니어야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Rook_Stops_At_Enemy_And_Adds_Attack_Tile() // 룩이 적을 만나면 그 칸을 공격 가능으로 추가하고 멈추는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(0, 0); // 룩 시작 좌표
            PlacePiece(board, new Vector2Int(3, 0), isPlayerPiece: false); // 가로 방향 3칸 앞에 적 기물 배치

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Rook, origin, isPlayerPiece: true, board); // 룩 이동 계산

            Assert.Contains(new Vector2Int(2, 0), result.MoveTiles); // 적 앞 칸까지는 이동 가능해야 함
            Assert.Contains(new Vector2Int(3, 0), result.AttackTiles); // 적이 있는 칸은 공격 가능해야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 0))); // 적 뒤 칸으로는 진행하지 못해야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Rook_Stops_At_Ally_Without_Attack_Tile() // 룩이 아군을 만나면 공격 없이 멈추는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(0, 0); // 룩 시작 좌표
            PlacePiece(board, new Vector2Int(2, 0), isPlayerPiece: true); // 가로 방향 2칸 앞에 아군 기물 배치

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Rook, origin, isPlayerPiece: true, board); // 룩 이동 계산

            Assert.Contains(new Vector2Int(1, 0), result.MoveTiles); // 아군 앞 칸까지는 이동 가능해야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(2, 0))); // 아군이 있는 칸에는 이동할 수 없어야 함
            Assert.IsFalse(result.AttackTiles.Contains(new Vector2Int(2, 0))); // 아군이 있는 칸은 공격 대상도 아니어야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Knight_Ignores_Blocking_Pieces() // 나이트가 중간 기물을 무시하고 도약하는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(4, 4); // 나이트 시작 좌표
            PlacePiece(board, new Vector2Int(4, 5), isPlayerPiece: true); // 바로 위 칸에 아군 기물 배치(직선 경로를 막아도 나이트는 무관해야 함)

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Knight, origin, isPlayerPiece: true, board); // 나이트 이동 계산

            Assert.Contains(new Vector2Int(5, 6), result.MoveTiles); // 도약 좌표 중 하나가 이동 가능해야 함
            Assert.Contains(new Vector2Int(6, 5), result.MoveTiles); // 다른 도약 좌표도 이동 가능해야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Pawn_Moves_Two_Tiles_When_Path_Is_Clear() // 폰이 전방이 비어있을 때 2칸까지 전진하는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(4, 1); // 아군 폰 시작 좌표

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Pawn, origin, isPlayerPiece: true, board); // 폰 이동 계산

            Assert.Contains(new Vector2Int(4, 2), result.MoveTiles); // 1칸 전진이 가능해야 함
            Assert.Contains(new Vector2Int(4, 3), result.MoveTiles); // 2칸 전진도 가능해야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Pawn_Blocked_At_One_Tile_Cannot_Reach_Two_Tiles() // 폰이 1칸 앞이 막히면 2칸 전진이 불가능한지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(4, 1); // 아군 폰 시작 좌표
            PlacePiece(board, new Vector2Int(4, 2), isPlayerPiece: false); // 1칸 앞에 적 기물 배치

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Pawn, origin, isPlayerPiece: true, board); // 폰 이동 계산

            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 2))); // 적이 있는 칸으로는 이동할 수 없어야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(4, 3))); // 1칸이 막혔으므로 2칸 전진도 불가능해야 함
            Assert.Contains(new Vector2Int(4, 2), result.AttackTiles); // 전방 1칸의 적은 공격 가능해야 함
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Pawn_Can_Attack_Diagonally() // 폰이 대각선의 적을 공격할 수 있는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(4, 1); // 아군 폰 시작 좌표
            PlacePiece(board, new Vector2Int(5, 2), isPlayerPiece: false); // 대각선 앞에 적 기물 배치

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Pawn, origin, isPlayerPiece: true, board); // 폰 이동 계산

            Assert.Contains(new Vector2Int(5, 2), result.AttackTiles); // 대각선 칸의 적을 공격 가능해야 함
            Assert.IsFalse(result.MoveTiles.Contains(new Vector2Int(5, 2))); // 대각선 칸은 비어있어도 이동으로는 쓸 수 없어야 함(적이 있을 때만 공격)
        }

        [Test] // NUnit이 테스트로 인식하게 하는 속성
        public void Archbishop_Combines_Bishop_And_Knight_Tiles() // 아크비숍이 비숍+나이트 이동을 모두 포함하는지 확인하는 테스트
        {
            var board = new BoardState(); // 테스트용 보드 생성
            var origin = new Vector2Int(4, 4); // 아크비숍 시작 좌표

            var result = MovementResolver.GetReachableTiles(PieceMovementType.Archbishop, origin, isPlayerPiece: true, board); // 아크비숍 이동 계산

            Assert.Contains(new Vector2Int(5, 5), result.MoveTiles); // 비숍형 대각선 이동이 포함돼야 함
            Assert.Contains(new Vector2Int(6, 5), result.MoveTiles); // 나이트형 도약 이동도 포함돼야 함
        }
    }
}
