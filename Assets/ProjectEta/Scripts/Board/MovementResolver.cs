using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public static class MovementResolver // 외부 호출부를 유지하면서 실제 계산을 이동 규칙 객체에 위임하는 파사드
    {
        public static MovementResult GetReachableTiles(PieceMovementType movementType, Vector2Int origin, bool isPlayerPiece, BoardState board) // 기존 9종과 기존 테스트가 사용하는 하위 호환 API
        {
            var rule = MovementRuleFactory.CreateLegacy(movementType); // 구형 enum을 새 이동 규칙 객체로 변환
            return rule.Resolve(origin, isPlayerPiece, board); // 실제 계산은 규칙 객체에 위임
        }

        public static MovementResult GetReachableTiles(PieceDefinition definition, Vector2Int origin, bool isPlayerPiece, BoardState board) // 23일차 이후 새 기물이 사용하는 데이터 기반 API
        {
            var rule = MovementRuleFactory.CreateFor(definition); // PieceDefinition의 MovementRules를 우선해 규칙 객체 생성
            return rule.Resolve(origin, isPlayerPiece, board); // 데이터 조합 결과로 이동·공격 후보 계산
        }
    }
}
