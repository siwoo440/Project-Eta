using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class SlideMovementRule : IMovementRule // 룩·비숍·퀸처럼 한 방향으로 계속 이동하는 규칙
    {
        private readonly Vector2Int[] _directions; // 슬라이드할 방향 목록
        private readonly int _maxSteps; // 각 방향의 최대 진행 거리

        public SlideMovementRule(Vector2Int[] directions, int maxSteps) // 방향과 최대 거리를 받는 생성자
        {
            _directions = directions ?? Array.Empty<Vector2Int>(); // null 방향 목록을 빈 배열로 보정
            _maxSteps = maxSteps <= 0 ? BoardState.Width : maxSteps; // 0 이하 값은 보드 폭만큼의 사실상 무제한 이동으로 해석
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 슬라이드 이동 후보를 계산
        {
            var result = new MovementResult(); // 이번 규칙의 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var direction in _directions) // 설정된 각 방향을 순회
            {
                if (direction == Vector2Int.zero) continue; // 제자리 방향은 무시

                for (int step = 1; step <= _maxSteps; step++) // 1칸씩 멀어지며 검사
                {
                    var target = origin + direction * step; // 이번 목표 좌표 계산
                    if (!board.IsInsideBoard(target)) break; // 보드 끝에 도달하면 해당 방향 종료

                    var tile = board.GetTile(target); // 목표 타일 조회
                    if (tile == null || tile.IsBlockedByObstacle) break; // 장애물이 있으면 통과하지 못하고 종료

                    if (!tile.IsOccupied) // 빈 칸이면
                    {
                        result.AddMove(target); // 이동 가능 칸으로 추가
                        continue; // 같은 방향을 계속 탐색
                    }

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 첫 적 기물은 공격 가능 칸으로 추가
                    break; // 첫 기물을 만난 뒤에는 그 뒤를 탐색하지 않음
                }
            }

            return result; // 완성된 슬라이드 결과 반환
        }
    }
}
