using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class StepMovementRule : IMovementRule // 지정 방향으로 제한된 거리만큼 진행하는 단거리 이동 규칙
    {
        private readonly Vector2Int[] _directions; // 한 칸씩 진행할 방향 목록
        private readonly int _maxSteps; // 각 방향으로 진행할 최대 칸 수

        public StepMovementRule(Vector2Int[] directions, int maxSteps = 1) // 방향과 최대 이동 칸 수를 받는 생성자
        {
            _directions = directions ?? Array.Empty<Vector2Int>(); // null 방향 목록은 빈 배열로 안전하게 변환
            _maxSteps = Mathf.Max(1, maxSteps); // 잘못된 0 이하 값은 최소 1칸으로 보정
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 단거리 이동 후보를 계산
        {
            var result = new MovementResult(); // 이번 규칙의 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var direction in _directions) // 설정된 각 방향을 순회
            {
                if (direction == Vector2Int.zero) continue; // 제자리 방향은 이동 후보에서 제외

                for (int step = 1; step <= _maxSteps; step++) // 1칸부터 최대 거리까지 차례로 검사
                {
                    var target = origin + direction * step; // 이번에 검사할 목표 좌표 계산
                    if (!board.IsInsideBoard(target)) break; // 보드 밖이면 해당 방향 탐색 종료

                    var tile = board.GetTile(target); // 목표 타일 상태 조회
                    if (tile == null || tile.IsBlockedByObstacle) break; // 장애물 또는 비정상 타일이면 더 진행하지 않음

                    if (!tile.IsOccupied) // 빈 칸이면
                    {
                        result.AddMove(target); // 이동 가능 칸으로 추가
                        continue; // 같은 방향의 다음 칸도 계속 검사
                    }

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 적 기물이면 공격 후보로 추가
                    break; // 아군·적군 어느 쪽이든 기물을 만나면 해당 방향 탐색 종료
                }
            }

            return result; // 완성된 단거리 이동 결과 반환
        }
    }
}
