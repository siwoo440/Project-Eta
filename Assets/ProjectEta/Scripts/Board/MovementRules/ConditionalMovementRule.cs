using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // MovementConditionType을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class ConditionalMovementRule : IMovementRule // 진영·상황에 따라 이동과 공격 계산이 달라지는 규칙
    {
        private readonly MovementConditionType _condition; // 실행할 조건부 규칙 종류

        public ConditionalMovementRule(MovementConditionType condition) // 조건 종류를 받는 생성자
        {
            _condition = condition; // 실행 시 사용할 조건 종류 저장
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 조건부 이동 후보를 계산
        {
            switch (_condition) // 현재 등록된 조건 종류에 따라 분기
            {
                case MovementConditionType.Pawn: // 프로젝트 η의 폰 규칙이면
                    return ResolvePawn(origin, isPlayerPiece, board); // 폰 전용 계산 실행
                default: // 아직 구현되지 않은 조건이면
                    return new MovementResult(); // 안전하게 빈 결과 반환
            }
        }

        private static MovementResult ResolvePawn(Vector2Int origin, bool isPlayerPiece, BoardState board) // 확정된 프로젝트 η 폰 이동·공격 규칙 계산
        {
            var result = new MovementResult(); // 폰 이동 결과 객체 생성
            if (board == null) return result; // 보드가 없으면 빈 결과 반환

            var forward = isPlayerPiece ? new Vector2Int(0, 1) : new Vector2Int(0, -1); // 아군은 +Y, 적군은 -Y를 전진 방향으로 사용
            var oneStep = origin + forward; // 전방 1칸 좌표 계산
            bool oneStepClear = board.IsInsideBoard(oneStep) && !board.GetTile(oneStep).IsOccupied && !board.GetTile(oneStep).IsBlockedByObstacle; // 전방 1칸이 비었는지 검사

            if (oneStepClear) // 전방 1칸이 비어 있으면
            {
                result.AddMove(oneStep); // 1칸 전진을 허용

                var twoStep = origin + forward * 2; // 전방 2칸 좌표 계산
                if (board.IsInsideBoard(twoStep) && !board.GetTile(twoStep).IsOccupied && !board.GetTile(twoStep).IsBlockedByObstacle) // 2칸째도 비어 있으면
                {
                    result.AddMove(twoStep); // 매 턴 최대 2칸 전진을 허용하는 현재 프로젝트 규칙 적용
                }
            }

            var attackOffsets = new[] { forward, forward + Vector2Int.left, forward + Vector2Int.right }; // 공격 후보는 전방 1칸과 전방 대각선 좌우 1칸
            foreach (var offset in attackOffsets) // 공격 후보 세 방향을 순회
            {
                var target = origin + offset; // 실제 공격 후보 좌표 계산
                if (!board.IsInsideBoard(target)) continue; // 보드 밖이면 제외

                var tile = board.GetTile(target); // 공격 후보 타일 조회
                if (tile.IsOccupied && tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target); // 적이 실제로 있을 때만 공격 후보로 추가
            }

            return result; // 완성된 폰 이동 결과 반환
        }
    }
}
