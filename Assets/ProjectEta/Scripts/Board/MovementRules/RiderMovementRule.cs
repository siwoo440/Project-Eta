using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class RiderMovementRule : IMovementRule // 동일한 도약 벡터를 같은 방향으로 반복하는 라이더 이동 규칙
    {
        private readonly Vector2Int[] _vectors; // 한 번의 반복에 사용할 기본 도약 벡터 목록
        private readonly int _maxRepeats; // 각 벡터를 같은 방향으로 반복할 최대 횟수

        public RiderMovementRule(Vector2Int[] vectors, int maxRepeats = BoardState.Width) // 반복 벡터와 최대 반복 횟수를 받는 생성자
        {
            _vectors = vectors ?? Array.Empty<Vector2Int>(); // null 벡터 목록은 빈 배열로 안전하게 변환
            _maxRepeats = Mathf.Max(1, maxRepeats); // 잘못된 0 이하 값은 최소 1회로 보정
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 라이더 이동·공격 후보를 계산하는 메서드
        {
            var result = new MovementResult(); // 이번 라이더 규칙의 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var vector in _vectors) // 설정된 각 기본 도약 벡터를 순회
            {
                if (vector == Vector2Int.zero) continue; // 제자리 벡터는 무한 반복 위험이 있으므로 제외

                for (int repeat = 1; repeat <= _maxRepeats; repeat++) // 같은 벡터를 1회부터 최대 반복 횟수까지 누적
                {
                    var target = origin + vector * repeat; // 같은 방향의 이번 반복 착지점 계산
                    if (!board.IsInsideBoard(target)) break; // 보드 밖이면 해당 벡터 방향 탐색 종료

                    var tile = board.GetTile(target); // 이번 반복 착지점의 타일 상태 조회
                    if (tile == null || tile.IsBlockedByObstacle) break; // 착지점 장애물은 이후 반복까지 차단

                    if (!tile.IsOccupied) // 반복 착지점이 비어 있으면
                    {
                        result.AddMove(target); // 이동 가능 칸으로 추가
                        continue; // 같은 벡터의 다음 반복 착지점도 계속 검사
                    }

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 적 기물 착지점은 공격 후보로 추가
                    break; // 아군·적군 모두 반복 착지점을 점유하면 그 뒤 같은 벡터 진행은 차단
                }
            }

            return result; // 완성된 라이더 이동 결과 반환
        }
    }
}
