using System; // [Serializable]과 Array.Empty<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 SerializeField를 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 이동 규칙 데이터를 모아두는 네임스페이스
{
    [Serializable] // PieceDefinition 안에서 배열 원소로 직렬화되도록 지정
    public class MovementRuleData // 코드 수정 없이 기물별 이동 규칙을 조합하기 위한 직렬화 데이터
    {
        [SerializeField] private MovementRuleKind _kind; // Step·Slide·Leap·Conditional·Rider 중 사용할 규칙 종류
        [SerializeField] private Vector2Int[] _vectors = Array.Empty<Vector2Int>(); // 방향·도약·라이더 반복 상대 좌표 목록
        [SerializeField] private int _maxSteps = 1; // Step·Slide 최대 칸 수 또는 Rider 최대 반복 횟수
        [SerializeField] private MovementConditionType _condition; // Conditional 규칙에서 사용할 조건 종류

        public MovementRuleKind Kind => _kind; // 외부 팩토리가 읽는 규칙 종류
        public Vector2Int[] Vectors => _vectors ?? Array.Empty<Vector2Int>(); // null 대신 빈 배열을 반환하는 벡터 목록
        public int MaxSteps => _maxSteps; // 외부 팩토리가 읽는 최대 진행 칸 수 또는 반복 횟수
        public MovementConditionType Condition => _condition; // 외부 팩토리가 읽는 조건 종류
    }
}
