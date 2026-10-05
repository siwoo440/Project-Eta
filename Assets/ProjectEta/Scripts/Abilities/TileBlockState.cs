using UnityEngine;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public sealed class TileBlockState
    {
        public BoardState Board { get; }
        public Vector2Int Position { get; }
        public PieceRuntimeState Source { get; }
        public TurnManager TurnManager { get; }
        public int ExpiresOnPlayerTurn { get; }

        internal TileBlockState(
            BoardState board,
            Vector2Int position,
            PieceRuntimeState source,
            TurnManager turnManager,
            int expiresOnPlayerTurn)
        {
            Board = board;
            Position = position;
            Source = source;
            TurnManager = turnManager;
            ExpiresOnPlayerTurn = expiresOnPlayerTurn;
        }
    }
}
