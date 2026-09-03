using System.Linq; // IReadOnlyList에 대한 Contains 확장 메서드를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Physics 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Mouse, Keyboard)을 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceView를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardInputController : MonoBehaviour // 마우스/키보드로 타일 선택과 카드 소환을 처리하는 컴포넌트
    {
        [SerializeField] private Camera _camera; // 레이캐스트에 사용할 카메라
        [SerializeField] private BoardView _boardView; // 좌표 변환·컨테이너로 쓸 보드 뷰
        [SerializeField] private PieceDefinition _kingDefinition; // 테스트용 킹 카드 데이터
        [SerializeField] private PieceDefinition _pawnDefinition; // 테스트용 폰 카드 데이터

        private readonly HandState _handState = new HandState(); // 테스트용 손패 상태

        private TileView _selectedTile; // 현재 선택된 타일
        private PieceDefinition _selectedCard; // 현재 선택된 카드

        private void Awake() // 씬 시작 시 자동 호출되는 초기화 메서드
        {
            if (_camera == null) // 카메라가 지정되지 않았으면
            {
                _camera = Camera.main; // 메인 카메라를 자동으로 사용
            }

            if (_boardView == null) // 보드 뷰가 지정되지 않았으면
            {
                _boardView = GetComponent<BoardView>(); // 같은 오브젝트의 보드 뷰를 자동으로 사용
            }

            _handState.TryAddCard(_kingDefinition); // 손패에 킹 카드 추가
            _handState.TryAddCard(_pawnDefinition); // 손패에 폰 카드 추가
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            HandleCardSelectionInput(); // 숫자키 카드 선택 입력 처리

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // 마우스가 있고 이번 프레임에 좌클릭했으면
            {
                HandleBoardClick(); // 보드 클릭 처리
            }
        }

        private void HandleCardSelectionInput() // 숫자키로 손패 카드를 선택/해제하는 메서드
        {
            if (Keyboard.current == null) // 키보드가 없으면
            {
                return; // 처리할 수 없으므로 종료
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) // 이번 프레임에 1번 키를 눌렀으면
            {
                ToggleCardSelection(_kingDefinition); // 킹 카드 선택 토글
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame) // 이번 프레임에 2번 키를 눌렀으면
            {
                ToggleCardSelection(_pawnDefinition); // 폰 카드 선택 토글
            }
        }

        private void ToggleCardSelection(PieceDefinition card) // 카드 선택 상태를 켜고 끄는 메서드
        {
            if (!_handState.Hand.Contains(card)) // 손패에 해당 카드가 없으면
            {
                Debug.Log($"{card.DisplayName} 카드가 손패에 없습니다."); // 사유를 콘솔에 출력
                return; // 선택하지 않고 종료
            }

            _selectedCard = _selectedCard == card ? null : card; // 이미 선택된 카드면 해제, 아니면 새로 선택
            Debug.Log(_selectedCard != null // 선택 결과에 따라
                ? $"카드 선택: {_selectedCard.DisplayName} (배치할 아군 칸을 클릭하세요)" // 선택됐을 때 안내 로그
                : "카드 선택 해제"); // 해제됐을 때 안내 로그
        }

        private void HandleBoardClick() // 보드 클릭 시 타일 선택 또는 카드 소환을 처리하는 메서드
        {
            var screenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            var ray = _camera.ScreenPointToRay(screenPosition); // 화면 좌표를 카메라 기준 광선으로 변환

            if (Physics.Raycast(ray, out var hit)) // 광선이 무언가에 맞으면
            {
                var tileView = hit.collider.GetComponent<TileView>(); // 맞은 오브젝트의 타일 뷰 컴포넌트 조회
                if (tileView != null) // 타일을 맞췄으면
                {
                    if (_selectedCard != null) // 선택된 카드가 있으면
                    {
                        TryPlayCardOnTile(tileView); // 그 칸에 카드 소환 시도
                    }
                    else // 선택된 카드가 없으면
                    {
                        SelectTile(tileView); // 일반 타일 선택 처리
                    }
                    return; // 처리 완료 후 종료
                }
            }

            DeselectCurrentTile(); // 타일 외의 곳을 클릭했으면 선택 해제
        }

        private void TryPlayCardOnTile(TileView tileView) // 선택된 카드를 지정한 타일에 소환 시도하는 메서드
        {
            var tileState = tileView.TileState; // 클릭한 타일의 데이터 조회
            if (!tileState.IsPlayerPlacementArea || tileState.IsOccupied) // 아군 영역이 아니거나 이미 점유돼 있으면
            {
                Debug.Log($"{tileState.BoardPosition}에는 소환할 수 없습니다 (아군 영역의 빈 칸만 가능)."); // 사유를 콘솔에 출력
                return; // 소환하지 않고 종료
            }

            var runtimeState = new PieceRuntimeState(_selectedCard, tileState.BoardPosition, isPlayerPiece: true); // 선택된 카드로 기물 런타임 상태 생성
            var pieceObject = new GameObject("Piece"); // 기물을 담을 빈 오브젝트 생성
            pieceObject.transform.SetParent(_boardView.transform, false); // 보드 뷰의 자식으로 배치(로컬 좌표 유지)
            var pieceView = pieceObject.AddComponent<PieceView>(); // 기물 뷰 컴포넌트 부착
            pieceView.Initialize(runtimeState, _boardView.TileSize); // 기물 뷰에 데이터와 타일 크기 주입

            tileState.OccupyingPiece = runtimeState; // 타일의 점유 기물 갱신
            _handState.RemoveCard(_selectedCard); // 손패에서 사용한 카드 제거

            Debug.Log($"{runtimeState.Definition.DisplayName} 소환: {tileState.BoardPosition}"); // 소환 결과를 콘솔에 출력
            _selectedCard = null; // 카드 선택 해제

            SelectTile(tileView); // 소환한 칸을 선택 상태로 표시
        }

        private void SelectTile(TileView tileView) // 타일을 선택 상태로 만드는 메서드
        {
            if (_selectedTile == tileView) // 이미 같은 타일이 선택돼 있으면
            {
                DeselectCurrentTile(); // 선택 해제(토글 동작)
                return; // 종료
            }

            DeselectCurrentTile(); // 기존 선택 해제

            _selectedTile = tileView; // 새 타일을 선택 상태로 저장
            _selectedTile.Select(); // 선택 하이라이트 적용

            var tileState = _selectedTile.TileState; // 선택한 타일의 데이터 조회
            Debug.Log($"Tile selected: {tileState.BoardPosition} - Occupied: {tileState.IsOccupied}, PlayerArea: {tileState.IsPlayerPlacementArea}"); // 좌표·점유·영역 상태를 콘솔에 출력
        }

        private void DeselectCurrentTile() // 현재 선택된 타일을 해제하는 메서드
        {
            if (_selectedTile == null) // 선택된 타일이 없으면
            {
                return; // 할 일이 없으므로 종료
            }

            _selectedTile.Deselect(); // 하이라이트 해제
            _selectedTile = null; // 선택 상태 초기화
        }

        private void OnGUI() // 화면에 디버그용 UI를 그리는 메서드
        {
            GUI.Label(new Rect(10, 10, 420, 20), BuildCardLabel("[1] King", _kingDefinition)); // 킹 카드 상태 라벨 표시
            GUI.Label(new Rect(10, 30, 420, 20), BuildCardLabel("[2] Pawn", _pawnDefinition)); // 폰 카드 상태 라벨 표시
            GUI.Label(new Rect(10, 50, 420, 20), "카드 선택 후 아군 영역(파란 칸)을 클릭하면 그 자리에 소환됩니다."); // 조작 안내 라벨 표시
        }

        private string BuildCardLabel(string keyLabel, PieceDefinition card) // 카드 상태 텍스트를 만드는 메서드
        {
            if (!_handState.Hand.Contains(card)) // 손패에 카드가 없으면(이미 사용됨)
            {
                return $"{keyLabel} 카드 (사용됨)"; // 사용됨 표시 텍스트 반환
            }

            return _selectedCard == card ? $"{keyLabel} 카드 <선택됨>" : $"{keyLabel} 카드"; // 선택 여부에 따른 텍스트 반환
        }
    }
}
