using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 이동 규칙 데이터 타입을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public static class MovementRuleFactory // PieceDefinition 데이터 또는 구형 enum을 실제 이동 규칙 객체로 변환하는 팩토리
    {
        private static readonly Vector2Int[] OrthogonalDirections = // 상하좌우 직교 방향 목록
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) // 우·좌·상·하
        };

        private static readonly Vector2Int[] DiagonalDirections = // 네 대각선 방향 목록
        {
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) // 우상·우하·좌상·좌하
        };

        private static readonly Vector2Int[] AllEightDirections = // 직교와 대각선을 합친 8방향 목록
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), // 직교 4방향
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) // 대각 4방향
        };

        private static readonly Vector2Int[] KnightOffsets = // 나이트 도약 상대 좌표 8개
        {
            new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1), // 위쪽 계열 네 좌표
            new Vector2Int(1, -2), new Vector2Int(2, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1) // 아래쪽 계열 네 좌표
        };

        public static IMovementRule CreateFor(PieceDefinition definition) // PieceDefinition의 데이터 규칙을 우선 사용해 실제 이동 규칙 생성
        {
            if (definition == null) return new CompoundMovementRule(); // 기물 정의가 없으면 빈 이동 규칙 반환

            var movementRules = definition.MovementRules; // 기물에 직렬화된 데이터 기반 이동 규칙 목록 조회
            if (movementRules != null && movementRules.Length > 0) // 데이터 기반 규칙이 하나 이상 있으면
            {
                var runtimeRules = new List<IMovementRule>(movementRules.Length); // 실제 계산 객체를 담을 목록 생성

                foreach (var ruleData in movementRules) // 모든 직렬화 규칙을 순회
                {
                    var runtimeRule = CreateFromData(ruleData); // 데이터 한 항목을 실제 규칙 객체로 변환
                    if (runtimeRule != null) runtimeRules.Add(runtimeRule); // 정상 생성된 규칙만 목록에 추가
                }

                return new CompoundMovementRule(runtimeRules.ToArray()); // 여러 규칙을 복합 규칙으로 묶어 반환
            }

            return CreateLegacy(definition.MovementType); // 기존 9종처럼 데이터가 비어 있으면 구형 enum 규칙으로 하위 호환
        }

        public static IMovementRule CreateLegacy(PieceMovementType movementType) // 기존 PieceMovementType API를 유지하기 위한 호환 규칙 생성
        {
            switch (movementType) // 현재 9종의 기존 이동 타입을 새 규칙 객체로 매핑
            {
                case PieceMovementType.King: // King은 8방향 1칸
                    return new StepMovementRule(AllEightDirections, 1); // Step 규칙으로 변환
                case PieceMovementType.Pawn: // Pawn은 진영 의존 조건부 이동
                    return new ConditionalMovementRule(MovementConditionType.Pawn); // Conditional 규칙으로 변환
                case PieceMovementType.Knight: // Knight는 L자 도약
                    return new LeapMovementRule(KnightOffsets); // Leap 규칙으로 변환
                case PieceMovementType.Bishop: // Bishop은 대각선 슬라이드
                    return new SlideMovementRule(DiagonalDirections, BoardState.Width); // Slide 규칙으로 변환
                case PieceMovementType.Rook: // Rook은 직교 슬라이드
                    return new SlideMovementRule(OrthogonalDirections, BoardState.Width); // Slide 규칙으로 변환
                case PieceMovementType.Queen: // Queen은 8방향 슬라이드
                    return new SlideMovementRule(AllEightDirections, BoardState.Width); // Slide 규칙으로 변환
                case PieceMovementType.Archbishop: // Archbishop은 Bishop+Knight
                    return new CompoundMovementRule(new SlideMovementRule(DiagonalDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // 두 규칙 조합
                case PieceMovementType.Chancellor: // Chancellor는 Rook+Knight
                    return new CompoundMovementRule(new SlideMovementRule(OrthogonalDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // 두 규칙 조합
                case PieceMovementType.Amazon: // Amazon은 Queen+Knight
                    return new CompoundMovementRule(new SlideMovementRule(AllEightDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // 두 규칙 조합
                default: // Custom처럼 별도 데이터가 필요한 타입이면
                    return new CompoundMovementRule(); // 데이터 오버로드 없이 호출된 경우 빈 결과를 유지
            }
        }

        private static IMovementRule CreateFromData(MovementRuleData data) // 직렬화 데이터 한 항목을 실제 이동 규칙으로 변환
        {
            if (data == null) return null; // 비어 있는 데이터 항목은 무시

            switch (data.Kind) // 저장된 기본 규칙 종류에 따라 실제 객체 생성
            {
                case MovementRuleKind.Step: // 제한 거리 단거리 이동이면
                    return new StepMovementRule(data.Vectors, data.MaxSteps); // Step 객체 생성
                case MovementRuleKind.Slide: // 연속 슬라이드 이동이면
                    return new SlideMovementRule(data.Vectors, data.MaxSteps); // Slide 객체 생성
                case MovementRuleKind.Leap: // 도약 이동이면
                    return new LeapMovementRule(data.Vectors); // Leap 객체 생성
                case MovementRuleKind.Conditional: // 조건부 이동이면
                    return new ConditionalMovementRule(data.Condition); // Conditional 객체 생성
                default: // 알 수 없는 데이터면
                    return null; // 팩토리 목록에서 제외
            }
        }
    }
}
