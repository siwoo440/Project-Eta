using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class CannonMovementRule : IMovementRule // 십자 1칸 이동 + 직교 장거리 원거리 공격을 분리하는 캐논 규칙
    {
        private readonly Vector2Int[] _directions; // 이동과 사격에 사용할 직교 방향 목록

        public CannonMovementRule(Vector2Int[] directions) // 직교 방향 목록을 받는 생성자
        {
            _directions = directions ?? Array.Empty<Vector2Int>(); // null 방향 목록을 빈 배열로 안전하게 변환
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // Cannon의 이동·원거리 공격 후보를 계산
        {
            var result = new MovementResult(); // 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var direction in _directions) // 상하좌우 각 방향을 순회
            {
                if (direction == Vector2Int.zero) continue; // 제자리 방향은 제외

                var oneStep = origin + direction; // 이동은 정확히 직교 1칸만 검사
                if (board.IsInsideBoard(oneStep)) // 한 칸 위치가 보드 안이면
                {
                    var moveTile = board.GetTile(oneStep); // 한 칸 타일 조회
                    if (moveTile != null && !moveTile.IsBlockedByObstacle && !moveTile.IsOccupied) result.AddMove(oneStep); // 빈 칸일 때만 1칸 이동 허용
                }

                var target = origin + direction; // 원거리 공격 탐색은 원점 바로 다음 칸부터 시작
                while (board.IsInsideBoard(target)) // 같은 직교 방향으로 보드 끝까지 탐색
                {
                    var tile = board.GetTile(target); // 현재 사격선 타일 조회
                    if (tile == null || tile.IsBlockedByObstacle) break; // 장애물은 사격선을 차단

                    if (tile.IsOccupied) // 첫 번째 기물을 만났으면
                    {
                        if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 첫 기물이 적이면 원거리 공격 후보
                        break; // 아군·적군 어느 쪽이든 첫 기물 뒤는 관통하지 않음
                    }

                    target += direction; // 빈 칸이면 같은 방향으로 다음 사거리 검사
                }
            }

            return result; // 이동 1칸과 원거리 공격 후보를 함께 반환
        }
    }
}
