using UnityEngine;

namespace ProjectEta.Board
{
    public class BoardState
    {
        public const int Width = 10;
        public const int Height = 10;

        private readonly TileState[,] _tiles = new TileState[Width, Height];

        public BoardState()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var position = new Vector2Int(x, y);
                    _tiles[x, y] = new TileState(position)
                    {
                        IsPlayerPlacementArea = y < Height / 2,
                        IsEnemyPlacementArea = y >= Height / 2
                    };
                }
            }
        }

        public bool IsInsideBoard(Vector2Int position)
        {
            return position.x >= 0 && position.x < Width && position.y >= 0 && position.y < Height;
        }

        public TileState GetTile(Vector2Int position)
        {
            return IsInsideBoard(position) ? _tiles[position.x, position.y] : null;
        }
    }
}
