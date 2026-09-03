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

        public bool TryOccupyArea(Vector2Int anchor, Vector2Int size, PieceRuntimeState piece) // 2x2 이상 대형 기물을 위한 사각 영역 점유를 시도하는 메서드
        {
            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회하며 사전 검사
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회하며 사전 검사
                {
                    var position = anchor + new Vector2Int(x, y); // 검사할 칸 좌표 계산
                    if (!IsInsideBoard(position)) // 보드 범위를 벗어나면
                    {
                        return false; // 점유 실패
                    }

                    var tile = GetTile(position); // 검사할 타일 조회
                    if (tile.IsOccupied || tile.IsBlockedByObstacle) // 이미 점유돼 있거나 장애물이 있으면
                    {
                        return false; // 점유 실패
                    }
                }
            }

            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회하며 실제 점유 처리
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회하며 실제 점유 처리
                {
                    var position = anchor + new Vector2Int(x, y); // 점유할 칸 좌표 계산
                    GetTile(position).OccupyingPiece = piece; // 모든 칸에 같은 기물 참조를 점유 기물로 지정
                }
            }

            return true; // 점유 성공
        }

        public int CountPieces(bool isPlayerPiece) // 13일차: 보드 위에 남아 있는 아군 또는 적군 기물 수를 세는 메서드(승리 조건 판정에 사용)
        {
            int count = 0; // 세어 나갈 기물 수

            for (int x = 0; x < Width; x++) // 보드 가로 방향으로 순회
            {
                for (int y = 0; y < Height; y++) // 보드 세로 방향으로 순회
                {
                    var piece = _tiles[x, y].OccupyingPiece; // 이 칸의 점유 기물 조회
                    if (piece != null && piece.IsPlayerPiece == isPlayerPiece) // 기물이 있고 찾는 진영과 일치하면
                    {
                        count++; // 개수 증가
                    }
                }
            }

            return count; // 최종 개수 반환
        }

        public void ClearArea(Vector2Int anchor, Vector2Int size) // TryOccupyArea로 점유했던 영역을 비우는 메서드
        {
            for (int x = 0; x < size.x; x++) // 영역 가로 방향으로 순회
            {
                for (int y = 0; y < size.y; y++) // 영역 세로 방향으로 순회
                {
                    var tile = GetTile(anchor + new Vector2Int(x, y)); // 비울 칸의 타일 조회
                    if (tile != null) // 보드 범위 안이면
                    {
                        tile.OccupyingPiece = null; // 점유 기물 해제
                    }
                }
            }
        }
    }
}
