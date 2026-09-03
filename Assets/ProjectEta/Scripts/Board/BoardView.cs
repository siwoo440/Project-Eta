using UnityEngine;

namespace ProjectEta.Board
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _tileGap = 0.05f;
        [SerializeField] private Color _playerAreaColor = new Color(0.25f, 0.45f, 0.95f);
        [SerializeField] private Color _enemyAreaColor = new Color(0.95f, 0.3f, 0.3f);

        private BoardState _boardState;
        private Material _playerMaterial;
        private Material _enemyMaterial;

        private void Awake()
        {
            _boardState = new BoardState();
            _playerMaterial = CreateTileMaterial(_playerAreaColor);
            _enemyMaterial = CreateTileMaterial(_enemyAreaColor);
            BuildTiles();
        }

        private void BuildTiles()
        {
            float offsetX = (BoardState.Width - 1) / 2f;
            float offsetY = (BoardState.Height - 1) / 2f;

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    var tileState = _boardState.GetTile(new Vector2Int(x, y));
                    CreateTileObject(x, y, offsetX, offsetY, tileState);
                }
            }
        }

        private void CreateTileObject(int x, int y, float offsetX, float offsetY, TileState tileState)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{x}_{y}";
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = new Vector3((x - offsetX) * _tileSize, 0f, (y - offsetY) * _tileSize);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = Vector3.one * (_tileSize - _tileGap);

            var tileRenderer = tile.GetComponent<Renderer>();
            tileRenderer.sharedMaterial = tileState.IsPlayerPlacementArea ? _playerMaterial : _enemyMaterial;
        }

        private static Material CreateTileMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = color };
            return material;
        }
    }
}
