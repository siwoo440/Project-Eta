using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

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
    }
}
