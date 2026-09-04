using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class LeapMovementRule : IMovementRule // 나이트처럼 중간 칸을 무시하고 지정 상대 좌표로 이동하는 규칙
    {
        private readonly Vector2Int[] _offsets; // 기준 좌표에서 더할 도약 상대 좌표 목록

        public LeapMovementRule(Vector2Int[] offsets) // 도약 상대 좌표 목록을 받는 생성자
        {
            _offsets = offsets ?? Array.Empty<Vector2Int>(); // null 목록은 빈 배열로 보정
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 도약 이동 후보를 계산
        {
            var result = new MovementResult(); // 이번 규칙의 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            foreach (var offset in _offsets) // 모든 도약 상대 좌표를 순회
            {
                if (offset == Vector2Int.zero) continue; // 제자리 도약은 무시

                var target = origin + offset; // 실제 착지 좌표 계산
                if (!board.IsInsideBoard(target)) continue; // 보드 밖 착지점은 제외

                var tile = board.GetTile(target); // 착지 타일 조회
                if (tile == null || tile.IsBlockedByObstacle) continue; // 착지점 자체가 장애물이면 제외

                if (!tile.IsOccupied) result.AddMove(target); // 빈 착지점은 이동 후보로 추가
                else if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 적이 있으면 공격 후보로 추가
            }

            return result; // 완성된 도약 결과 반환
        }
    }
}
