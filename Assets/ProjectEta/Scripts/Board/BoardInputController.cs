using UnityEngine;
using UnityEngine.InputSystem;

namespace ProjectEta.Board
{
    public class BoardInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;

        private TileView _selectedTile;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void Update()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            TrySelectTileUnderCursor();
        }

        private void TrySelectTileUnderCursor()
        {
            var screenPosition = Mouse.current.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit))
            {
                var tileView = hit.collider.GetComponent<TileView>();
                if (tileView != null)
                {
                    SelectTile(tileView);
                    return;
                }
            }

            DeselectCurrentTile();
        }

        private void SelectTile(TileView tileView)
        {
            if (_selectedTile == tileView)
            {
                DeselectCurrentTile();
                return;
            }

            DeselectCurrentTile();

            _selectedTile = tileView;
            _selectedTile.Select();

            var tileState = _selectedTile.TileState;
            Debug.Log($"Tile selected: {tileState.BoardPosition} - Occupied: {tileState.IsOccupied}, PlayerArea: {tileState.IsPlayerPlacementArea}");
        }

        private void DeselectCurrentTile()
        {
            if (_selectedTile == null)
            {
                return;
            }

            _selectedTile.Deselect();
            _selectedTile = null;
        }
    }
}
