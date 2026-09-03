using UnityEngine; // ScriptableObject, SerializeField 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "PieceDefinition", menuName = "ProjectEta/Piece Definition")] // 에디터 메뉴에서 에셋 생성 가능하게 등록
    public class PieceDefinition : ScriptableObject // 기물의 고정(불변) 정보를 담는 데이터 에셋
    {
        [Header("식별")] // 인스펙터에 구분선 표시
        [SerializeField] private string _pieceId; // 기물 고유 식별자
        [SerializeField] private string _displayName; // 화면에 표시할 이름

        [Header("분류")] // 인스펙터에 구분선 표시
        [SerializeField] private PieceCategory _category; // 기물 획득 경로 분류
        [SerializeField] private PieceGrade _grade; // 기물 등급(1~5성)
        [SerializeField] private PieceMovementType _movementType; // 기물 이동 규칙 종류
        [SerializeField] private PieceRoleTag _roleTags; // 기물 역할 태그(복수 선택 가능)

        [Header("기본 스탯")] // 인스펙터에 구분선 표시
        [SerializeField] private int _baseHp; // 기본 체력
        [SerializeField] private int _baseAtk; // 기본 공격력

        [Header("점유")] // 인스펙터에 구분선 표시
        [SerializeField] private Vector2Int _occupancySize = Vector2Int.one; // 보드에서 차지하는 칸 크기

        [Header("설명")] // 인스펙터에 구분선 표시
        [TextArea] // 인스펙터에서 여러 줄 입력 가능하게 표시
        [SerializeField] private string _description; // 기물 설명 텍스트

        public string PieceId => _pieceId; // 외부에서 읽는 기물 식별자
        public string DisplayName => _displayName; // 외부에서 읽는 표시 이름
        public PieceCategory Category => _category; // 외부에서 읽는 분류
        public PieceGrade Grade => _grade; // 외부에서 읽는 등급
        public PieceMovementType MovementType => _movementType; // 외부에서 읽는 이동 규칙
        public PieceRoleTag RoleTags => _roleTags; // 외부에서 읽는 역할 태그
        public int BaseHp => _baseHp; // 외부에서 읽는 기본 체력
        public int BaseAtk => _baseAtk; // 외부에서 읽는 기본 공격력
        public Vector2Int OccupancySize => _occupancySize; // 외부에서 읽는 점유 크기
        public string Description => _description; // 외부에서 읽는 설명
    }
}
