using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class TileState // 보드 한 칸의 상태를 담는 클래스
    {
        public Vector2Int BoardPosition { get; } // 이 타일의 보드 좌표
        public PieceRuntimeState OccupyingPiece { get; set; } // 이 칸을 점유한 기물(없으면 null)
        public bool IsPlayerPlacementArea { get; set; } // 아군 배치 가능 영역 여부
        public bool IsEnemyPlacementArea { get; set; } // 적군 배치 가능 영역 여부
        public bool IsBlockedByObstacle { get; set; } // 장애물로 막혀있는지 여부
        public bool IsOccupied => OccupyingPiece != null; // 기물이 있으면 점유 상태로 판정

        public TileState(Vector2Int boardPosition) // 타일 상태 생성자
        {
            BoardPosition = boardPosition; // 좌표 저장
        }
    }
}
