using UnityEngine;
using ProjectEta.Pieces;

namespace ProjectEta.Board
{
    public class TileState
    {
        public Vector2Int BoardPosition { get; }
        public PieceRuntimeState OccupyingPiece { get; set; }
        public bool IsPlayerPlacementArea { get; set; }
        public bool IsEnemyPlacementArea { get; set; }
        public bool IsBlockedByObstacle { get; set; }
        public bool IsOccupied => OccupyingPiece != null;

        public TileState(Vector2Int boardPosition)
        {
            BoardPosition = boardPosition;
        }
    }
}
