using System.Linq; // IReadOnlyList에 대한 Contains 확장 메서드를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Physics 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Mouse, Keyboard)을 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceView를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardInputController : MonoBehaviour // 마우스/키보드로 칸 선택과 카드 소환을 처리하는 컴포넌트
    {
        [SerializeField] private Camera _camera; // 레이캐스트에 사용할 카메라
        [SerializeField] private BoardView _boardView; // 좌표 변환·컨테이너로 쓸 보드 뷰
        [SerializeField] private PieceDefinition _kingDefinition; // 테스트용 킹 카드 데이터
        [SerializeField] private PieceDefinition _pawnDefinition; // 테스트용 폰 카드 데이터

        public RunState RunState => _runState; // 현재 입력이 변경하는 실제 런 상태
        public HandState HandState => _handState; // 현재 입력이 변경하는 실제 손패 상태
        public bool IsBound => _runState != null && _handState != null && _boardView != null && _boardView.IsBound; // 전투 상태 연결 여부

        private RunState _runState; // BattleController가 소유하는 실제 런 상태
        private HandState _handState; // RunState.Hand 참조. 별도 테스트 손패를 만들지 않음
        private Vector2Int? _selectedCell; // 현재 선택된 칸 좌표(없으면 null)
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
        }

        public void Bind(RunState runState, BoardView boardView = null) // BattleController가 실제 RunState를 입력 시스템에 연결하는 메서드
        {
            if (runState == null) // 잘못된 런 상태가 들어오면
            {
                Debug.LogError("BoardInputController.Bind: RunState가 null입니다."); // 오류 원인을 로그로 남김
                return; // 기존 연결을 유지하고 종료
            }

            if (boardView != null) // 외부에서 보드 뷰를 명시했으면
            {
                _boardView = boardView; // 해당 뷰를 사용
            }
            else if (_boardView == null) // 아직 보드 뷰가 없으면
            {
                _boardView = GetComponent<BoardView>(); // 같은 오브젝트에서 자동 탐색
            }

            if (_boardView == null) // 그래도 보드 뷰를 찾지 못했으면
            {
                Debug.LogError("BoardInputController.Bind: BoardView를 찾지 못했습니다."); // 원인을 로그로 남김
                return; // 입력 연결을 완료하지 않음
            }

            _runState = runState; // 실제 런 상태 참조 저장
            _handState = runState.Hand; // 별도 HandState 대신 RunState.Hand를 그대로 사용
            _selectedCard = null; // 상태 교체 시 이전 카드 선택 제거
            DeselectCurrentCell(); // 이전 선택 칸 강조 제거
        }

        public void EnsurePrototypeStartingHand() // 새 런에서만 호출해 현재 테스트용 시작 카드 2장을 실제 RunState.Hand에 넣는 메서드
        {
            if (_handState == null) // 아직 런 상태가 연결되지 않았으면
            {
                Debug.LogWarning("시작 손패를 만들기 전에 BattleController 연결이 필요합니다."); // 순서 오류 안내
                return; // 처리하지 않고 종료
            }

            if (_handState.Hand.Count > 0) // 이미 손패가 존재하면 저장 복원/기존 상태로 간주
            {
                return; // 중복 추가하지 않음
            }

            if (_kingDefinition != null) // 킹 데이터가 지정돼 있으면
            {
                _handState.TryAddCard(_kingDefinition); // 실제 RunState.Hand에 킹 카드 추가
            }

            if (_pawnDefinition != null) // 폰 데이터가 지정돼 있으면
            {
                _handState.TryAddCard(_pawnDefinition); // 실제 RunState.Hand에 폰 카드 추가
            }
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            if (!IsBound) // 실제 런 상태가 아직 연결되지 않았으면
            {
                return; // 입력으로 임시 데이터를 만들지 않고 기다림
            }

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

            if (Keyboard.current.digit1Key.wasPressedThisFrame && _kingDefinition != null) // 이번 프레임에 1번 키를 눌렀고 킹 데이터가 있으면
            {
                ToggleCardSelection(_kingDefinition); // 킹 카드 선택 토글
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame && _pawnDefinition != null) // 이번 프레임에 2번 키를 눌렀고 폰 데이터가 있으면
            {
                ToggleCardSelection(_pawnDefinition); // 폰 카드 선택 토글
            }
        }

        private void ToggleCardSelection(PieceDefinition card) // 카드 선택 상태를 켜고 끄는 메서드
        {
            if (_handState == null || card == null) // 손패 연결 또는 카드 데이터가 없으면
            {
                return; // 처리하지 않고 종료
            }

            if (!_handState.Hand.Contains(card)) // 실제 RunState.Hand에 해당 카드가 없으면
            {
                Debug.Log($"{card.DisplayName} 카드가 손패에 없습니다."); // 사유를 콘솔에 출력
                return; // 선택하지 않고 종료
            }

            _selectedCard = _selectedCard == card ? null : card; // 이미 선택된 카드면 해제, 아니면 새로 선택
            Debug.Log(_selectedCard != null // 선택 결과에 따라
                ? $"카드 선택: {_selectedCard.DisplayName} (배치할 아군 칸을 클릭하세요)" // 선택됐을 때 안내 로그
                : "카드 선택 해제"); // 해제됐을 때 안내 로그
        }

        private void HandleBoardClick() // 보드 클릭 시 칸 선택 또는 카드 소환을 처리하는 메서드
        {
            if (_camera == null || _boardView == null) // 카메라 또는 보드 뷰가 없으면
            {
                return; // 클릭 처리를 할 수 없으므로 종료
            }

            var screenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            var ray = _camera.ScreenPointToRay(screenPosition); // 화면 좌표를 카메라 기준 광선으로 변환

            if (Physics.Raycast(ray, out var hit) && _boardView.TryGetCellFromWorldPoint(hit.point, out var cell)) // 광선이 보드 메시에 맞고 유효한 칸으로 변환되면
            {
                if (_selectedCard != null) // 선택된 카드가 있으면
                {
                    TryPlayCardOnCell(cell); // 그 칸에 카드 소환 시도
                }
                else // 선택된 카드가 없으면
                {
                    SelectCell(cell); // 일반 칸 선택 처리
                }
                return; // 처리 완료 후 종료
            }

            DeselectCurrentCell(); // 보드 밖을 클릭했으면 선택 해제
        }

        private void TryPlayCardOnCell(Vector2Int cell) // 선택된 카드를 지정한 칸에 소환 시도하는 메서드
        {
            if (_selectedCard == null || _handState == null) // 카드 선택 또는 실제 손패 연결이 없으면
            {
                return; // 소환하지 않고 종료
            }

            var tileState = _boardView.GetTile(cell); // RunState.Board의 클릭 칸 데이터 조회
            if (tileState == null) // 유효하지 않은 칸이면
            {
                return; // 소환하지 않고 종료
            }

            if (!tileState.IsPlayerPlacementArea || tileState.IsOccupied) // 아군 영역이 아니거나 이미 점유돼 있으면
            {
                Debug.Log($"{tileState.BoardPosition}에는 소환할 수 없습니다 (아군 영역의 빈 칸만 가능)."); // 사유를 콘솔에 출력
                return; // 소환하지 않고 종료
            }

            var runtimeState = new PieceRuntimeState(_selectedCard, tileState.BoardPosition, isPlayerPiece: true); // 선택된 카드로 기물 런타임 상태 생성
            tileState.OccupyingPiece = runtimeState; // 먼저 실제 RunState.Board의 타일 점유 상태 갱신

            var pieceObject = new GameObject("Piece"); // 기물을 담을 빈 오브젝트 생성
            pieceObject.transform.SetParent(_boardView.transform, false); // 보드 뷰의 자식으로 배치(로컬 좌표 유지)
            var pieceView = pieceObject.AddComponent<PieceView>(); // 기물 뷰 컴포넌트 부착
            pieceView.Initialize(runtimeState, _boardView.TileSize); // 기물 뷰에 실제 런타임 데이터와 타일 크기 주입

            _handState.RemoveCard(_selectedCard); // 실제 RunState.Hand에서 사용한 카드 제거

            Debug.Log($"{runtimeState.Definition.DisplayName} 소환: {tileState.BoardPosition} / RunState.Hand={_handState.Hand.Count}장"); // 실제 상태 변경 결과 출력
            _selectedCard = null; // 카드 선택 해제

            SelectCell(cell); // 소환한 칸을 선택 상태로 표시
        }

        private void SelectCell(Vector2Int cell) // 칸을 선택 상태로 만드는 메서드
        {
            if (_selectedCell == cell) // 이미 같은 칸이 선택돼 있으면
            {
                DeselectCurrentCell(); // 선택 해제(토글 동작)
                return; // 종료
            }

            _boardView.HighlightCell(cell); // 보드 메시에 강조 표시 반영
            _selectedCell = cell; // 새 칸을 선택 상태로 저장

            var tileState = _boardView.GetTile(cell); // 선택한 칸의 실제 RunState.Board 데이터 조회
            if (tileState != null) // 정상 타일이면
            {
                Debug.Log($"Tile selected: {tileState.BoardPosition} - Occupied: {tileState.IsOccupied}, PlayerArea: {tileState.IsPlayerPlacementArea}"); // 좌표·점유·영역 상태를 콘솔에 출력
            }
        }

        private void DeselectCurrentCell() // 현재 선택된 칸을 해제하는 메서드
        {
            if (_selectedCell == null) // 선택된 칸이 없으면
            {
                return; // 할 일이 없으므로 종료
            }

            if (_boardView != null) // 보드 뷰가 존재하면
            {
                _boardView.ClearHighlight(); // 보드 메시의 강조 표시 해제
            }
            _selectedCell = null; // 선택 상태 초기화
        }

        private void OnGUI() // 화면에 디버그용 UI를 그리는 메서드
        {
            if (!IsBound) // 실제 RunState가 아직 연결되지 않았으면
            {
                GUI.Label(new Rect(10, 10, 500, 20), "BattleController가 RunState를 연결하는 중입니다."); // 상태 연결 대기 안내
                return; // 카드 UI는 그리지 않음
            }

            GUI.Label(new Rect(10, 10, 420, 20), BuildCardLabel("[1] King", _kingDefinition)); // 킹 카드 상태 라벨 표시
            GUI.Label(new Rect(10, 30, 420, 20), BuildCardLabel("[2] Pawn", _pawnDefinition)); // 폰 카드 상태 라벨 표시
            GUI.Label(new Rect(10, 50, 420, 20), $"RunState.Hand: {_handState.Hand.Count}장 / 카드 선택 후 아군 영역을 클릭하면 소환됩니다."); // 실제 손패 상태 안내
        }

        private string BuildCardLabel(string keyLabel, PieceDefinition card) // 카드 상태 텍스트를 만드는 메서드
        {
            if (card == null) // 데이터가 지정되지 않았으면
            {
                return $"{keyLabel} 카드 (데이터 없음)"; // 누락 표시
            }

            if (_handState == null || !_handState.Hand.Contains(card)) // 실제 손패에 카드가 없으면(이미 사용됐거나 없음)
            {
                return $"{keyLabel} 카드 (사용됨/없음)"; // 사용됨 표시 텍스트 반환
            }

            return _selectedCard == card ? $"{keyLabel} 카드 <선택됨>" : $"{keyLabel} 카드"; // 선택 여부에 따른 텍스트 반환
        }
    }
}
