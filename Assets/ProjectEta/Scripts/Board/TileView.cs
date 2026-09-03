using UnityEngine;

namespace ProjectEta.Board
{
    public class TileView : MonoBehaviour
    {
        public TileState TileState { get; private set; }
        public bool IsSelected { get; private set; }

        private Renderer _renderer;
        private Material _idleMaterial;
        private Material _highlightMaterial;

        public void Initialize(TileState tileState, Material idleMaterial, Material highlightMaterial)
        {
            TileState = tileState;
            _idleMaterial = idleMaterial;
            _highlightMaterial = highlightMaterial;
            _renderer = GetComponent<Renderer>();
            _renderer.sharedMaterial = _idleMaterial;
        }

        public void Select()
        {
            IsSelected = true;
            _renderer.sharedMaterial = _highlightMaterial;
        }

        public void Deselect()
        {
            IsSelected = false;
            _renderer.sharedMaterial = _idleMaterial;
        }
    }
}
