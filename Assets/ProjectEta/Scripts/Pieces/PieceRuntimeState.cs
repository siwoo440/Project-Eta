using UnityEngine;

namespace ProjectEta.Pieces
{
    public class PieceRuntimeState
    {
        private int _currentHp;

        public PieceDefinition Definition { get; }
        public Vector2Int BoardPosition { get; set; }
        public bool IsPlayerPiece { get; set; }
        public bool IsSelected { get; set; }
        public bool CanMove { get; set; } = true;
        public bool CanAttack { get; set; } = true;
        public bool IsDead => _currentHp <= 0;

        public int CurrentHp
        {
            get => _currentHp;
            set => _currentHp = Mathf.Max(0, value);
        }

        public PieceRuntimeState(PieceDefinition definition, Vector2Int boardPosition, bool isPlayerPiece)
        {
            Definition = definition;
            BoardPosition = boardPosition;
            IsPlayerPiece = isPlayerPiece;
            _currentHp = definition.BaseHp;
        }
    }
}
