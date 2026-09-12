using UnityEngine; // Vector2Int 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public sealed class EnemyEncounterSpawn
    {
        public PieceDefinition Piece { get; } // 배치 적 기물
        public Vector2Int Position { get; } // 배치 보드 좌표

        public EnemyEncounterSpawn(PieceDefinition piece, Vector2Int position)
        {
            Piece = piece; // 적 기물 저장
            Position = position; // 배치 좌표 저장
        }
    }
}
