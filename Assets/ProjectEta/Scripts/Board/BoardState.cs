using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardState // 10x10 보드 전체 상태를 담는 클래스
    {
        public const int Width = 10; // 보드 가로 칸 수
        public const int Height = 10; // 보드 세로 칸 수

        private readonly TileState[,] _tiles = new TileState[Width, Height]; // 칸별 상태를 담는 2차원 배열

        public BoardState() // 보드 상태 생성자
        {
            for (int x = 0; x < Width; x++) // 가로 방향으로 순회
            {
                for (int y = 0; y < Height; y++) // 세로 방향으로 순회
                {
                    var position = new Vector2Int(x, y); // 현재 칸 좌표 생성
                    _tiles[x, y] = new TileState(position) // 좌표로 타일 상태 생성
                    {
                        IsPlayerPlacementArea = y < Height / 2, // 아래쪽 절반은 아군 배치 영역
                        IsEnemyPlacementArea = y >= Height / 2 // 위쪽 절반은 적군 배치 영역
                    };
                }
            }
        }

        public bool IsInsideBoard(Vector2Int position) // 좌표가 보드 범위 안인지 검사하는 메서드
        {
            return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height; // 가로/세로 범위를 모두 만족하는지 반환
        }

        public TileState GetTile(Vector2Int position) // 좌표에 해당하는 타일 상태를 가져오는 메서드
        {
            return IsInsideBoard(position) ? _tiles[position.x, position.y] : null; // 범위 안이면 타일 반환, 아니면 null 반환
        }

        public bool CanOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState ignorePiece = null) // 대형 기물의 사각 점유 영역 전체가 사용 가능한지 실제 점유 전에 검사하는 메서드
        {
            if (size.x <= 0 || size.y <= 0) return false; // 0 이하 크기는 잘못된 점유 데이터이므로 실패

            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회
                {
                    var position = anchor + new Vector2Int(x, y); // 검사할 실제 보드 좌표 계산
                    if (!IsInsideBoard(position)) return false; // 한 칸이라도 보드 밖이면 전체 영역 사용 불가

                    var tile = GetTile(position); // 현재 검사 칸 조회
                    if (tile == null || tile.IsBlockedByObstacle) return false; // 타일이 없거나 장애물이 있으면 사용 불가

                    if (tile.OccupyingPiece != null && tile.OccupyingPiece != ignorePiece) return false; // 다른 기물이 점유 중이면 전체 영역 사용 불가
                }
            }

            return true; // 모든 칸이 유효하면 영역 전체를 사용할 수 있음
        }

        public bool TryOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState piece) // 1x1부터 2x2 이상까지 사각 영역 점유를 원자적으로 시도하는 메서드
        {
            if (piece == null) return false; // 점유시킬 런타임 기물이 없으면 실패
            if (!CanOccupyArea(anchor, size, piece)) return false; // 모든 칸을 먼저 검사해 부분 점유가 생기지 않게 함

            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회하며 실제 점유 처리
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회하며 실제 점유 처리
                {
                    var position = anchor + new Vector2Int(x, y); // 점유할 칸 좌표 계산
                    GetTile(position).OccupyingPiece = piece; // 모든 칸에 같은 PieceRuntimeState 하나를 등록
                }
            }

            return true; // 전체 영역 점유 성공
        }

        public void ClearArea(Vector2Int anchor, Vector2Int size) // 지정한 사각 영역의 점유를 비우는 기존 호환 메서드
        {
            if (size.x <= 0 || size.y <= 0) return; // 잘못된 크기면 처리하지 않음

            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회
                {
                    var tile = GetTile(anchor + new Vector2Int(x, y)); // 비울 칸의 타일 조회
                    if (tile != null) tile.OccupyingPiece = null; // 보드 범위 안이면 점유 기물 해제
                }
            }
        }

        public int ClearPiece(PieceRuntimeState piece) // 보드 전체에서 같은 런타임 기물을 참조하는 모든 칸을 안전하게 해제하는 37일차 메서드
        {
            if (piece == null) return 0; // 제거할 기물이 없으면 해제 칸도 0개

            int clearedCount = 0; // 실제로 해제한 칸 수

            for (int x = 0; x < Width; x++) // 보드 가로 전체 순회
            {
                for (int y = 0; y < Height; y++) // 보드 세로 전체 순회
                {
                    if (_tiles[x, y].OccupyingPiece != piece) continue; // 다른 기물 또는 빈 칸이면 건너뜀
                    _tiles[x, y].OccupyingPiece = null; // 같은 런타임 기물 참조를 해제
                    clearedCount++; // 해제한 칸 수 증가
                }
            }

            return clearedCount; // 실제 해제한 전체 칸 수 반환
        }

        public int CountPieces(bool isPlayerPiece) // 보드 위에 남아 있는 살아 있는 기물 수를 런타임 기물 기준으로 세는 메서드
        {
            var uniquePieces = new HashSet<PieceRuntimeState>(); // 2x2 보스처럼 여러 칸이 같은 기물을 가리켜도 한 번만 세기 위한 집합

            for (int x = 0; x < Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < Height; y++) // 보드 세로 방향으로 순회
                {
                    var piece = _tiles[x, y].OccupyingPiece; // 이 칸의 점유 기물 조회
                    if (piece == null) continue; // 빈 칸은 제외
                    if (piece.IsDead) continue; // 사망했지만 연출 때문에 점유가 잠깐 남은 기물은 남은 기물 수에서 제외
                    if (piece.IsPlayerPiece != isPlayerPiece) continue; // 요청한 진영이 아니면 제외
                    uniquePieces.Add(piece); // 같은 PieceRuntimeState는 HashSet에서 한 번만 등록
                }
            }

            return uniquePieces.Count; // 1x1 일반 기물과 2x2 대형 기물을 모두 실제 기물 수로 반환
        }
    }
}
