using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class HopperMovementRule : IMovementRule // 첫 기물을 발판으로 넘어 바로 뒤 한 칸에 착지하는 이동 규칙
    {
        private readonly Vector2Int[] _directions; // 발판을 찾기 위해 탐색할 방향 목록

        public HopperMovementRule(Vector2Int[] directions) // 탐색 방향을 받는 생성자
        {
            _directions = directions ?? Array.Empty<Vector2Int>(); // null 방향 목록은 빈 배열로 안전하게 변환
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 발판과 착지점을 계산하는 메서드
        {
            var result = new MovementResult(); // 이번 규칙의 이동·공격 결과 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var direction in _directions) // 각 탐색 방향을 순회
            {
                if (direction == Vector2Int.zero) continue; // 제자리 방향은 제외

                Vector2Int hurdle = origin + direction; // 첫 탐색 위치를 원점 바로 다음 칸으로 설정
                while (board.IsInsideBoard(hurdle)) // 보드 안에서 첫 번째 기물을 찾을 때까지 반복
                {
                    var hurdleTile = board.GetTile(hurdle); // 현재 탐색 타일 조회
                    if (hurdleTile == null || hurdleTile.IsBlockedByObstacle) break; // 장애물은 발판이 아니며 해당 방향을 막음
                    if (hurdleTile.IsOccupied) // 첫 번째 기물을 찾았으면
                    {
                        var landing = hurdle + direction; // 발판 바로 뒤 한 칸을 실제 착지점으로 계산
                        if (!board.IsInsideBoard(landing)) break; // 착지점이 보드 밖이면 실패

                        var landingTile = board.GetTile(landing); // 착지점 타일 조회
                        if (landingTile == null || landingTile.IsBlockedByObstacle) break; // 착지점 장애물이면 이동 불가

                        if (!landingTile.IsOccupied) result.AddMove(landing); // 빈 착지점은 이동 후보로 추가
                        else if (landingTile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(landing); // 적이 있으면 공격 후보로 추가
                        break; // 첫 발판만 사용하므로 해당 방향 탐색 종료
                    }

                    hurdle += direction; // 빈 칸이면 같은 방향으로 다음 칸 탐색
                }
            }

            return result; // 완성된 Hopper 결과 반환
        }
    }
}
