using UnityEngine; // ScriptableObject, SerializeField, Sprite 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    [CreateAssetMenu(fileName = "PieceDefinition", menuName = "ProjectEta/Piece Definition")] // 에디터 메뉴에서 기물 정의 에셋을 생성할 수 있게 등록
    public class PieceDefinition : ScriptableObject // 기물의 고정 정보를 담고 카드 UI에서도 같은 데이터를 사용하는 ScriptableObject
    {
        [Header("식별")] // 인스펙터 식별 정보 구분선
        [SerializeField] private string _pieceId; // 기물 고유 식별자
        [SerializeField] private string _displayName; // 화면과 카드에 표시할 기물 이름

        [Header("분류")] // 인스펙터 분류 정보 구분선
        [SerializeField] private PieceCategory _category; // 기물 획득 경로 분류
        [SerializeField] private PieceGrade _grade; // 기물 등급
        [SerializeField] private PieceMovementType _movementType; // 기물 이동 규칙 종류
        [SerializeField] private PieceRoleTag _roleTags; // 기물 역할 태그

        [Header("기본 스탯")] // 인스펙터 기본 스탯 구분선
        [SerializeField] private int _baseHp; // 카드 우하단과 실제 런타임에 사용할 기본 체력
        [SerializeField] private int _baseAtk; // 카드 좌하단과 실제 전투에 사용할 기본 공격력

        [Header("점유")] // 인스펙터 보드 점유 구분선
        [SerializeField] private Vector2Int _occupancySize = Vector2Int.one; // 보드에서 차지하는 칸 크기

        [Header("카드 UI")] // 18일차 카드 이미지 손패 UI 구분선
        [SerializeField] private Sprite _cardArtwork; // 카드 상단 초상화에 표시할 일러스트 Sprite

        [Header("설명")] // 인스펙터 설명 구분선
        [TextArea] // 여러 줄 설명을 편집할 수 있게 표시
        [SerializeField] private string _description; // 카드 하단 설명 영역에 표시할 기물 설명

        public string PieceId => _pieceId; // 외부에서 읽는 기물 식별자
        public string DisplayName => _displayName; // 외부에서 읽는 표시 이름
        public PieceCategory Category => _category; // 외부에서 읽는 분류
        public PieceGrade Grade => _grade; // 외부에서 읽는 등급
        public PieceMovementType MovementType => _movementType; // 외부에서 읽는 이동 규칙
        public PieceRoleTag RoleTags => _roleTags; // 외부에서 읽는 역할 태그
        public int BaseHp => _baseHp; // 외부에서 읽는 기본 체력
        public int BaseAtk => _baseAtk; // 외부에서 읽는 기본 공격력
        public Vector2Int OccupancySize => _occupancySize; // 외부에서 읽는 점유 크기
        public Sprite CardArtwork => _cardArtwork; // 손패 카드 초상화 Sprite
        public string Description => _description; // 외부에서 읽는 설명
    }
}
