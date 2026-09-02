using UnityEngine;

namespace ProjectEta.Pieces
{
    [CreateAssetMenu(fileName = "PieceDefinition", menuName = "ProjectEta/Piece Definition")]
    public class PieceDefinition : ScriptableObject
    {
        [Header("식별")]
        [SerializeField] private string _pieceId;
        [SerializeField] private string _displayName;

        [Header("분류")]
        [SerializeField] private PieceCategory _category;
        [SerializeField] private PieceGrade _grade;
        [SerializeField] private PieceMovementType _movementType;
        [SerializeField] private PieceRoleTag _roleTags;

        [Header("기본 스탯")]
        [SerializeField] private int _baseHp;
        [SerializeField] private int _baseAtk;

        [Header("점유")]
        [SerializeField] private Vector2Int _occupancySize = Vector2Int.one;

        [Header("설명")]
        [TextArea]
        [SerializeField] private string _description;

        public string PieceId => _pieceId;
        public string DisplayName => _displayName;
        public PieceCategory Category => _category;
        public PieceGrade Grade => _grade;
        public PieceMovementType MovementType => _movementType;
        public PieceRoleTag RoleTags => _roleTags;
        public int BaseHp => _baseHp;
        public int BaseAtk => _baseAtk;
        public Vector2Int OccupancySize => _occupancySize;
        public string Description => _description;
    }
}
