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

            var highlightMaterial = tileState.IsPlayerPlacementArea ? _installableHighlightMaterial : _blockedHighlightMaterial;
            var tileView = tile.AddComponent<TileView>();
            tileView.Initialize(tileState, _idleMaterial, highlightMaterial);
        }

        private static Material CreateTileMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(shader) { color = color };
            return material;
        }
    }
}
