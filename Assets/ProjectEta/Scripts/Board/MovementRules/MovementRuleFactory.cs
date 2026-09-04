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
            new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1), // 위쪽 계열
            new Vector2Int(1, -2), new Vector2Int(2, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1) // 아래쪽 계열
        };

        public static IMovementRule CreateFor(PieceDefinition definition) // PieceDefinition의 데이터 규칙을 우선 사용해 실제 이동 규칙 생성
        {
            if (definition == null) return new CompoundMovementRule(); // 기물 정의가 없으면 빈 이동 규칙 반환

            var movementRules = definition.MovementRules; // 직렬화된 데이터 기반 이동 규칙 목록 조회
            if (movementRules != null && movementRules.Length > 0) // 데이터 기반 규칙이 하나 이상 있으면
            {
                var runtimeRules = new List<IMovementRule>(movementRules.Length); // 실제 계산 객체 목록 생성

                foreach (var ruleData in movementRules) // 모든 직렬화 규칙 순회
                {
                    var runtimeRule = CreateFromData(ruleData); // 실제 규칙 객체로 변환
                    if (runtimeRule != null) runtimeRules.Add(runtimeRule); // 정상 생성된 규칙만 추가
                }

                return new CompoundMovementRule(runtimeRules.ToArray()); // 하나 또는 여러 규칙을 동일한 복합 진입점으로 반환
            }

            return CreateLegacy(definition.MovementType); // 데이터가 비어 있으면 기존 enum 경로로 하위 호환
        }

        public static IMovementRule CreateLegacy(PieceMovementType movementType) // 기존 PieceMovementType API 호환 규칙 생성
        {
            switch (movementType) // 기존 이동 타입을 새 규칙 객체로 매핑
            {
                case PieceMovementType.King:
                    return new StepMovementRule(AllEightDirections, 1); // King = 8방향 1칸
                case PieceMovementType.Pawn:
                    return new ConditionalMovementRule(MovementConditionType.Pawn); // Pawn = 조건부 규칙
                case PieceMovementType.Knight:
                    return new LeapMovementRule(KnightOffsets); // Knight = L자 도약
                case PieceMovementType.Bishop:
                    return new SlideMovementRule(DiagonalDirections, BoardState.Width); // Bishop = 대각 Slide
                case PieceMovementType.Rook:
                    return new SlideMovementRule(OrthogonalDirections, BoardState.Width); // Rook = 직교 Slide
                case PieceMovementType.Queen:
                    return new SlideMovementRule(AllEightDirections, BoardState.Width); // Queen = 8방향 Slide
                case PieceMovementType.Archbishop:
                    return new CompoundMovementRule(new SlideMovementRule(DiagonalDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // Bishop+Knight
                case PieceMovementType.Chancellor:
                    return new CompoundMovementRule(new SlideMovementRule(OrthogonalDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // Rook+Knight
                case PieceMovementType.Amazon:
                    return new CompoundMovementRule(new SlideMovementRule(AllEightDirections, BoardState.Width), new LeapMovementRule(KnightOffsets)); // Queen+Knight
                default:
                    return new CompoundMovementRule(); // Custom은 데이터가 없으면 빈 결과
            }
        }

        private static IMovementRule CreateFromData(MovementRuleData data) // 직렬화 데이터 한 항목을 실제 이동 규칙으로 변환
        {
            if (data == null) return null; // 비어 있는 데이터 항목은 무시

            switch (data.Kind) // 저장된 규칙 종류에 따라 객체 생성
            {
                case MovementRuleKind.Step:
                    return new StepMovementRule(data.Vectors, data.MaxSteps); // Step 생성
                case MovementRuleKind.Slide:
                    return new SlideMovementRule(data.Vectors, data.MaxSteps); // Slide 생성
                case MovementRuleKind.Leap:
                    return new LeapMovementRule(data.Vectors); // Leap 생성
                case MovementRuleKind.Conditional:
                    return new ConditionalMovementRule(data.Condition); // Conditional 생성
                case MovementRuleKind.Rider:
                    return new RiderMovementRule(data.Vectors, data.MaxSteps); // Rider 생성
                case MovementRuleKind.Hopper:
                    return new HopperMovementRule(data.Vectors); // Grasshopper용 Hopper 생성
                case MovementRuleKind.Cannon:
                    return new CannonMovementRule(data.Vectors); // Cannon용 이동+원거리 공격 규칙 생성
                default:
                    return null; // 알 수 없는 규칙은 제외
            }
        }
    }
}
