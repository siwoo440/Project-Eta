using System; // Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public sealed class CompoundMovementRule : IMovementRule // 여러 기본 이동 규칙을 하나의 기물 이동으로 합치는 규칙
    {
        private readonly IMovementRule[] _rules; // 함께 계산할 하위 이동 규칙 목록

        public CompoundMovementRule(params IMovementRule[] rules) // 하위 이동 규칙 목록을 받는 생성자
        {
            _rules = rules ?? Array.Empty<IMovementRule>(); // null 목록은 빈 배열로 보정
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board) // 모든 하위 규칙 결과를 합쳐 반환
        {
            var result = new MovementResult(); // 최종 병합 결과 생성

            foreach (var rule in _rules) // 등록된 하위 규칙을 순서대로 순회
            {
                if (rule == null) continue; // null 규칙은 안전하게 건너뜀
                result.MergeFrom(rule.Resolve(origin, isPlayerPiece, board)); // 각 규칙의 이동·공격 후보를 중복 없이 병합
            }

            return result; // 완성된 복합 이동 결과 반환
        }
    }
}
