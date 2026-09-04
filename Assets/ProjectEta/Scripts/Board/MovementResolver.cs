using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public static class MovementResolver // 외부 호출부를 유지하면서 실제 계산을 이동 규칙 객체에 위임하는 파사드
    {
        public static MovementResult GetReachableTiles(PieceMovementType movementType, Vector2Int origin, bool isPlayerPiece, BoardState board) // 기존 9종과 기존 테스트용 하위 호환 API
        {
            var rule = MovementRuleFactory.CreateLegacy(movementType); // 구형 enum을 실제 이동 규칙 객체로 변환
            return rule.Resolve(origin, isPlayerPiece, board); // 실제 계산 위임
        }

        public static MovementResult GetReachableTiles(PieceDefinition definition, Vector2Int origin, bool isPlayerPiece, BoardState board) // 데이터 기반 API
        {
            var rule = MovementRuleFactory.CreateFor(definition); // PieceDefinition의 MovementRules를 우선해 규칙 생성
            return rule.Resolve(origin, isPlayerPiece, board); // 데이터 규칙으로 이동·공격 후보 계산
        }

        public static MovementResult GetReachableTiles(PieceRuntimeState piece, BoardState board) // 25일차: 런타임 순환 상태까지 포함해 계산하는 테스트·확장용 API
        {
            if (piece == null) return new MovementResult(); // 런타임 기물이 없으면 빈 결과 반환

            if (!piece.CanMove && !piece.CanAttack) // 28일차: 기절 등으로 이동·공격이 모두 불가능하면
            {
                return new MovementResult(); // 후보 없이 빈 결과 반환(행동 자체를 스킵)
            }

            MovementResult result; // 기절이 아닐 때 실제로 계산할 이동·공격 후보

            if (piece.Definition != null && piece.Definition.PieceId == "chameleon") // Chameleon이면 현재 순환 단계 직접 반영
            {
                PieceMovementType movementType; // 현재 단계에 대응할 기존 체스 이동 타입
                switch (piece.MovementCycleIndex) // 0~3 단계 선택
                {
                    case 1: movementType = PieceMovementType.Bishop; break; // Bishop 단계
                    case 2: movementType = PieceMovementType.Rook; break; // Rook 단계
                    case 3: movementType = PieceMovementType.Queen; break; // Queen 단계
                    default: movementType = PieceMovementType.Knight; break; // 초기 및 순환 복귀는 Knight
                }

                result = GetReachableTiles(movementType, piece.BoardPosition, piece.IsPlayerPiece, board); // 선택된 기존 이동 규칙으로 계산
            }
            else // Chameleon이 아닌 일반 기물
            {
                result = GetReachableTiles(piece.Definition, piece.BoardPosition, piece.IsPlayerPiece, board); // 일반 기물은 PieceDefinition 데이터 경로 사용
            }

            if (!piece.CanMove) // 28일차: 속박 등으로 이동만 불가능하면
            {
                result.MoveTiles.Clear(); // 이동 후보만 제거하고 공격 후보는 그대로 유지
            }

            return result; // 상태 이상이 반영된 최종 이동·공격 후보 반환
        }
    }
}
