using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState, MovementResult, MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAICandidateSimulation // 실제 보드 상태를 영구 변경하지 않고 후보 위치에서 다음 이동 규칙을 계산하는 공통 시뮬레이션 도우미
    {
        public static bool TryResolveFutureMovement(AIActionCandidate candidate, BoardState board, out MovementResult futureMovement) // 현재 PieceDefinition 규칙으로 후보 위치의 다음 행동을 계산
        {
            return TryResolveFutureMovement(candidate, board, null, out futureMovement); // 이동 타입 강제 지정 없이 공통 구현 호출
        }

        public static bool TryResolveFutureMovement(AIActionCandidate candidate, BoardState board, PieceMovementType? overrideMovementType, out MovementResult futureMovement) // Chameleon처럼 다음 형태의 Legacy 이동 타입을 강제할 수 있는 오버로드
        {
            futureMovement = new MovementResult(); // 실패 시에도 빈 결과를 반환
            if (candidate == null || candidate.Actor == null || board == null) return false; // 필수 데이터가 없으면 시뮬레이션 불가
            if (candidate.ActionType != AIActionType.Move) return false; // 후보 위치 시뮬레이션은 이동 행동에만 사용

            var actor = candidate.Actor; // 행동 주체 저장
            var originTile = board.GetTile(candidate.Origin); // 현재 원점 타일 조회
            var targetTile = board.GetTile(candidate.Target); // 가상 이동 목표 타일 조회

            if (originTile == null || targetTile == null) return false; // 보드 밖 좌표면 실패
            if (originTile.OccupyingPiece != actor) return false; // 현재 보드와 후보가 일치하지 않으면 오래된 후보로 판단
            if (targetTile.IsOccupied || targetTile.IsBlockedByObstacle) return false; // 이동 후보는 빈 정상 칸이어야 함

            var originalOriginOccupant = originTile.OccupyingPiece; // 원점 점유 복원을 위해 저장
            var originalTargetOccupant = targetTile.OccupyingPiece; // 목표 점유 복원을 위해 저장

            try // 점유만 잠시 변경해 실제 BoardPosition을 건드리지 않고 계산
            {
                originTile.OccupyingPiece = null; // 원점 점유를 잠시 해제
                targetTile.OccupyingPiece = actor; // 후보 위치에 행동 주체가 있다고 가정

                if (overrideMovementType.HasValue) // Chameleon 등 다음 이동 타입을 명시했다면
                {
                    futureMovement = MovementResolver.GetReachableTiles(overrideMovementType.Value, candidate.Target, actor.IsPlayerPiece, board); // 지정 Legacy 이동 규칙으로 계산
                }
                else // 일반 특수 기물이라면
                {
                    futureMovement = MovementResolver.GetReachableTiles(actor.Definition, candidate.Target, actor.IsPlayerPiece, board); // PieceDefinition의 실제 이동 규칙 재사용
                }

                return true; // 정상 계산 성공
            }
            finally // 예외 여부와 상관없이 보드 원상 복구
            {
                originTile.OccupyingPiece = originalOriginOccupant; // 원점 점유 복원
                targetTile.OccupyingPiece = originalTargetOccupant; // 목표 점유 복원
            }
        }
    }
}
