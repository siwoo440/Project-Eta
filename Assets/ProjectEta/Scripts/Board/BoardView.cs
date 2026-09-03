using UnityEngine;

namespace ProjectEta.Board
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _tileGap = 0.05f;
        [SerializeField] private Color _idleColor = Color.white;
        [SerializeField] private Color _installableHighlightColor = new Color(0.55f, 0.75f, 1f);
        [SerializeField] private Color _blockedHighlightColor = new Color(1f, 0.55f, 0.55f);

        public float TileSize => _tileSize;

        private BoardState _boardState;
        private Material _idleMaterial;
        private Material _installableHighlightMaterial;
        private Material _blockedHighlightMaterial;

        private void Awake()
        {
            _boardState = new BoardState();
            _idleMaterial = CreateTileMaterial(_idleColor);
            _installableHighlightMaterial = CreateTileMaterial(_installableHighlightColor);
            _blockedHighlightMaterial = CreateTileMaterial(_blockedHighlightColor);
            BuildTiles();
        }

        private void BuildTiles()
        {
            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    var boardPosition = new Vector2Int(x, y);
                    var tileState = _boardState.GetTile(boardPosition);
                    CreateTileObject(boardPosition, tileState);
                }
            }
        }

        private void CreateTileObject(Vector2Int boardPosition, TileState tileState)
        {
            var tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{boardPosition.x}_{boardPosition.y}";
            tile.transform.SetParent(transform, false);
            tile.transform.localPosition = BoardToLocalPosition(boardPosition, _tileSize);
            tile.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            tile.transform.localScale = Vector3.one * (_tileSize - _tileGap);

            var highlightMaterial = tileState.IsPlayerPlacementArea ? _installableHighlightMaterial : _blockedHighlightMaterial;
            var tileView = tile.AddComponent<TileView>();
            tileView.Initialize(tileState, _idleMaterial, highlightMaterial);
        }

        public static Vector3 BoardToLocalPosition(Vector2Int boardPosition, float tileSize)
        {
            float offsetX = (BoardState.Width - 1) / 2f;
            float offsetY = (BoardState.Height - 1) / 2f;
            return new Vector3((boardPosition.x - offsetX) * tileSize, 0f, (boardPosition.y - offsetY) * tileSize);
        }

        private static Material CreateTileMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = color };
            return material;
        }
    }
}
