using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using ProjectEta.Cards;
using ProjectEta.Pieces;

namespace ProjectEta.Board
{
    public class BoardInputController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private BoardView _boardView;
        [SerializeField] private PieceDefinition _kingDefinition;
        [SerializeField] private PieceDefinition _pawnDefinition;

        private readonly HandState _handState = new HandState();

        private TileView _selectedTile;
        private PieceDefinition _selectedCard;

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_boardView == null)
            {
                _boardView = GetComponent<BoardView>();
            }

            _handState.TryAddCard(_kingDefinition);
            _handState.TryAddCard(_pawnDefinition);
        }

        private void Update()
        {
            HandleCardSelectionInput();

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleBoardClick();
            }
        }

        private void HandleCardSelectionInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                ToggleCardSelection(_kingDefinition);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                ToggleCardSelection(_pawnDefinition);
            }
        }

        private void ToggleCardSelection(PieceDefinition card)
        {
            if (!_handState.Hand.Contains(card))
            {
                Debug.Log($"{card.DisplayName} 카드가 손패에 없습니다.");
                return;
            }

            _selectedCard = _selectedCard == card ? null : card;
            Debug.Log(_selectedCard != null
                ? $"카드 선택: {_selectedCard.DisplayName} (배치할 아군 칸을 클릭하세요)"
                : "카드 선택 해제");
        }

        private void HandleBoardClick()
        {
            var screenPosition = Mouse.current.position.ReadValue();
            var ray = _camera.ScreenPointToRay(screenPosition);

            if (Physics.Raycast(ray, out var hit))
            {
                var tileView = hit.collider.GetComponent<TileView>();
                if (tileView != null)
                {
                    if (_selectedCard != null)
                    {
                        TryPlayCardOnTile(tileView);
                    }
                    else
                    {
                        SelectTile(tileView);
                    }
                    return;
                }
            }

            DeselectCurrentTile();
        }

        private void TryPlayCardOnTile(TileView tileView)
        {
            var tileState = tileView.TileState;
            if (!tileState.IsPlayerPlacementArea || tileState.IsOccupied)
            {
                Debug.Log($"{tileState.BoardPosition}에는 소환할 수 없습니다 (아군 영역의 빈 칸만 가능).");
                return;
            }

            var runtimeState = new PieceRuntimeState(_selectedCard, tileState.BoardPosition, isPlayerPiece: true);
            var pieceObject = new GameObject("Piece");
            pieceObject.transform.SetParent(_boardView.transform, false);
            var pieceView = pieceObject.AddComponent<PieceView>();
            pieceView.Initialize(runtimeState, _boardView.TileSize);

            tileState.OccupyingPiece = runtimeState;
            _handState.RemoveCard(_selectedCard);

            Debug.Log($"{runtimeState.Definition.DisplayName} 소환: {tileState.BoardPosition}");
            _selectedCard = null;

            SelectTile(tileView);
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

        private void OnGUI()
        {
            GUI.Label(new Rect(10, 10, 420, 20), BuildCardLabel("[1] King", _kingDefinition));
            GUI.Label(new Rect(10, 30, 420, 20), BuildCardLabel("[2] Pawn", _pawnDefinition));
            GUI.Label(new Rect(10, 50, 420, 20), "카드 선택 후 아군 영역(파란 칸)을 클릭하면 그 자리에 소환됩니다.");
        }

        private string BuildCardLabel(string keyLabel, PieceDefinition card)
        {
            if (!_handState.Hand.Contains(card))
            {
                return $"{keyLabel} 카드 (사용됨)";
            }

            return _selectedCard == card ? $"{keyLabel} 카드 <선택됨>" : $"{keyLabel} 카드";
        }
    }
}
