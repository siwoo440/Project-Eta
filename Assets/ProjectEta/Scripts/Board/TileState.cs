using UnityEngine;
using ProjectEta.Pieces;

namespace ProjectEta.Board
{
    public class TileState
    {
        private bool _hasObstacle;

        public Vector2Int BoardPosition { get; }
        public PieceRuntimeState OccupyingPiece { get; set; }
        public bool IsPlayerPlacementArea { get; set; }
        public bool IsEnemyPlacementArea { get; set; }

        // 기존 이동 규칙이 이 값을 사용하므로 물리 장애물 + Ability 봉쇄를 모두 "통과 불가"로 노출한다.
        public bool IsBlockedByObstacle
        {
            get => _hasObstacle || IsBlockedByAbility;
            set => _hasObstacle = value;
        }

        public bool HasObstacle => _hasObstacle;
        public bool IsObstacleDestructible { get; set; }
        public bool IsBlockedByAbility { get; internal set; }
        public bool IsBlocked => _hasObstacle || IsBlockedByAbility;
        public bool IsOccupied => OccupyingPiece != null;

        public TileState(Vector2Int boardPosition)
        {
            BoardPosition = boardPosition;
        }

        public void SetObstacle(bool exists, bool destructible = false)
        {
            _hasObstacle = exists;
            IsObstacleDestructible = exists && destructible;
        }

        public bool TryDestroyObstacle()
        {
            if (!_hasObstacle || !IsObstacleDestructible) return false;

            _hasObstacle = false;
            IsObstacleDestructible = false;
            return true;
        }
    }
}
