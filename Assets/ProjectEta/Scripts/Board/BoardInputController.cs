using System; // Action 이벤트를 사용하기 위한 네임스페이스
using System.Collections.Generic; // Dictionary를 사용하기 위한 네임스페이스
using System.Linq; // IReadOnlyList에 대한 Contains 확장 메서드를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Physics 등을 사용하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Mouse, Keyboard)을 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState, DeckState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceView를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardInputController : MonoBehaviour // 마우스/키보드로 칸 선택·손패 카드 선택·기물 행동을 처리하는 컴포넌트
    {
        [SerializeField] private Camera _camera; // 레이캐스트에 사용할 카메라
        [SerializeField] private BoardView _boardView; // 좌표 변환·컨테이너로 쓸 보드 뷰
        [SerializeField] private PieceDefinition _kingDefinition; // 프로토타입 시작 풀에 넣을 킹 카드 데이터
        [SerializeField] private PieceDefinition _pawnDefinition; // 프로토타입 시작 풀에 넣을 폰 카드 데이터
        [SerializeField] private PieceDefinition _knightDefinition; // 프로토타입 시작 풀에 넣을 나이트 카드 데이터
        [SerializeField] private PieceDefinition _bishopDefinition; // 프로토타입 시작 풀에 넣을 비숍 카드 데이터
        [SerializeField] private PieceDefinition _rookDefinition; // 프로토타입 시작 풀에 넣을 룩 카드 데이터
        [SerializeField] private PieceDefinition _queenDefinition; // 프로토타입 시작 풀에 넣을 퀸 카드 데이터

        public RunState RunState => _runState; // 현재 입력이 변경하는 실제 런 상태
        public HandState HandState => _handState; // 현재 입력이 변경하는 실제 손패 상태
        public TurnManager TurnManager => _turnManager; // 현재 입력 권한을 판단하는 실제 턴 매니저
        public bool IsBound => _runState != null && _handState != null && _boardView != null && _boardView.IsBound; // 전투 상태 연결 여부
        public bool CanReceivePlayerInput => IsBound && (_turnManager == null || _turnManager.CanPlayerInput); // 현재 플레이어가 보드/카드 입력을 할 수 있는지 여부
        public PieceDefinition SelectedCard => _selectedCard; // 16일차: 현재 손패에서 숫자키로 선택된 카드
        public PieceRuntimeState SelectedPiece => _selectedPiece; // 현재 선택된(이동 대기 중인) 기물
        public MovementResult PendingMovement => _pendingMovement; // 현재 선택된 기물의 이동/공격 후보 칸

        public event Action<CombatResult> AttackResolved; // 공격 판정이 끝날 때마다 알림

        private const int PrototypeInitialHandSize = 5; // 16일차: 기본 6종 중 5장을 초기 손패로 뽑는 테스트 값
        private RunState _runState; // BattleController가 소유하는 실제 런 상태
        private HandState _handState; // RunState.Hand 참조
        private TurnManager _turnManager; // BattleController가 소유하는 실제 턴 매니저
        private Vector2Int? _selectedCell; // 현재 선택된 칸 좌표
        private PieceDefinition _selectedCard; // 현재 선택된 손패 카드
        private PieceRuntimeState _selectedPiece; // 현재 이동을 위해 선택된 보드 위 기물
        private MovementResult _pendingMovement; // 선택된 기물의 이동/공격 후보 칸 계산 결과
        private readonly Dictionary<PieceRuntimeState, PieceView> _pieceViews = new Dictionary<PieceRuntimeState, PieceView>(); // 기물 데이터와 화면 표시 연결 목록

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

        public void Bind(RunState runState, BoardView boardView = null, TurnManager turnManager = null) // BattleController가 실제 RunState와 턴 매니저를 입력 시스템에 연결하는 메서드
        {
            if (runState == null) // 잘못된 런 상태가 들어오면
            {
                Debug.LogError("BoardInputController.Bind: RunState가 null입니다."); // 오류 원인을 로그로 남김
                return; // 기존 연결을 유지하고 종료
            }

            if (_turnManager != null) // 이전 턴 매니저가 연결돼 있었다면
            {
                _turnManager.TurnChanged -= HandleTurnChangedForCardDraw; // 재연결 전에 자동 드로우 이벤트 구독 해제
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
            _turnManager = turnManager; // 전달받은 턴 매니저를 입력 권한 기준으로 사용
            _selectedCard = null; // 상태 교체 시 이전 카드 선택 제거
            DeselectPiece(); // 상태 교체 시 이전 기물 선택도 제거
            DeselectCurrentCell(); // 이전 선택 칸 강조 제거

            if (_turnManager != null) // 턴 매니저가 실제로 전달됐다면
            {
                _turnManager.TurnChanged -= HandleTurnChangedForCardDraw; // 중복 구독을 방지하기 위해 먼저 해제
                _turnManager.TurnChanged += HandleTurnChangedForCardDraw; // 2턴 이후 플레이어 턴 시작 자동 드로우 구독
            }
        }

        public void EnsurePrototypeStartingHand() // 16일차: 기존 호출부 호환을 유지하면서 실제 DeckState→HandState 시작 흐름을 구성하는 메서드
        {
            if (_runState == null || _handState == null) // 아직 런 상태가 연결되지 않았으면
            {
                Debug.LogWarning("시작 덱을 만들기 전에 BattleController 연결이 필요합니다."); // 순서 오류 안내
                return; // 처리하지 않고 종료
            }

            var deck = _runState.Deck; // 실제 RunState가 소유하는 DeckState 참조
            if (deck.OwnedCardPool.Count > 0 || deck.DrawPile.Count > 0 || _handState.Hand.Count > 0) // 이미 카드 상태가 있으면 저장 복원/기존 런으로 간주
            {
                return; // 프로토타입 시작 카드가 중복되지 않도록 초기화를 건너뜀
            }

            foreach (var definition in GetPrototypeStartingCards()) // 인스펙터에 연결된 기본 6종 정의를 순회하며
            {
                if (definition != null) // 실제 데이터가 연결된 카드만
                {
                    deck.AddToOwnedPool(definition); // 손패가 아니라 먼저 보유 카드 풀에 등록
                }
            }

            deck.RebuildDrawPileFromOwnedPool(); // 보유 카드 풀 전체를 복사하고 실제 드로우 순서로 셔플

            int drawCount = Math.Min(PrototypeInitialHandSize, deck.DrawPile.Count); // 카드가 5장 미만인 경우도 안전하게 실제 가능한 초기 드로우 수 계산
            for (int i = 0; i < drawCount; i++) // 초기 손패 수만큼 반복하며
            {
                deck.TryDrawToHand(_handState); // 카드 유실 없는 DeckState→HandState 이동 실행
            }

            Debug.Log($"16일차 시작 덱 구성: Owned={deck.OwnedCardPool.Count}, Hand={_handState.Hand.Count}, Draw={deck.DrawPile.Count}"); // 실제 초기 카드 상태 로그
        }

        private PieceDefinition[] GetPrototypeStartingCards() // 16일차 프로토타입 시작 풀 6종을 한 곳에서 반환하는 메서드
        {
            return new[] // 현재 프로젝트에서 테스트 가능한 기본 6종 배열 생성
            {
                _kingDefinition, // 킹 카드
                _pawnDefinition, // 폰 카드
                _knightDefinition, // 나이트 카드
                _bishopDefinition, // 비숍 카드
                _rookDefinition, // 룩 카드
                _queenDefinition // 퀸 카드
            };
        }

        private void HandleTurnChangedForCardDraw(TurnState state, int turnNumber) // 16일차: 새로운 플레이어 턴에 일반 드로우를 연결하는 이벤트 처리 메서드
        {
            if (state != TurnState.PlayerTurn || turnNumber <= 1) // 첫 턴은 초기 손패와 중복되므로 2턴 이후 플레이어 턴만 처리
            {
                return; // 자동 드로우 없이 종료
            }

            TryDrawOneCard(); // 실제 DeckState에서 손패로 카드 1장 이동 시도
        }

        public bool TryDrawOneCard() // 16일차: 테스트와 턴 이벤트에서 함께 사용하는 일반 드로우 진입점
        {
            if (_runState == null || _handState == null) // 카드 상태가 연결되지 않았으면
            {
                return false; // 드로우할 수 없으므로 실패 반환
            }

            int beforeDrawCount = _runState.Deck.DrawPile.Count; // 드로우 전 덱 장수를 로그용으로 저장
            if (!_runState.Deck.TryDrawToHand(_handState)) // 손패 상한 또는 빈 덱 때문에 드로우가 실패하면
            {
                Debug.Log($"카드 드로우 스킵: Hand={_handState.Hand.Count}/{HandState.MaxHandSize}, Draw={beforeDrawCount}"); // 카드가 유실되지 않고 건너뛴 상태 출력
                return false; // 실패 반환
            }

            Debug.Log($"카드 자동 드로우: Hand={_handState.Hand.Count}/{HandState.MaxHandSize}, Draw={_runState.Deck.DrawPile.Count}"); // 성공한 카드 상태 출력
            return true; // 드로우 성공 반환
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            if (!IsBound) // 실제 런 상태가 아직 연결되지 않았으면
            {
                return; // 입력으로 임시 데이터를 만들지 않고 기다림
            }

            if (!CanReceivePlayerInput) // 현재 적 턴 또는 전투 종료 상태라면
            {
                _selectedCard = null; // 적 턴으로 넘어갈 때 남아 있던 카드 선택 해제
                DeselectPiece(); // 남아 있던 기물 선택과 이동/공격 후보 강조도 해제
                DeselectCurrentCell(); // 보드 선택 강조도 해제
                return; // 플레이어의 모든 보드·카드 입력 차단
            }

            HandleCardSelectionInput(); // 16일차: 숫자키 1~0을 현재 손패 슬롯 1~10에 대응해 선택 처리

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) // 마우스가 있고 이번 프레임에 좌클릭했으면
            {
                HandleBoardClick(); // 보드 클릭 처리
            }
        }

        private void HandleCardSelectionInput() // 16일차: 고정 기물 매핑 없이 숫자키로 현재 손패 슬롯을 선택하는 메서드
        {
            if (Keyboard.current == null) // 키보드가 없으면
            {
                return; // 처리할 수 없으므로 종료
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame) TrySelectHandSlot(0); // 1번 키는 현재 손패 첫 번째 카드 선택
            if (Keyboard.current.digit2Key.wasPressedThisFrame) TrySelectHandSlot(1); // 2번 키는 현재 손패 두 번째 카드 선택
            if (Keyboard.current.digit3Key.wasPressedThisFrame) TrySelectHandSlot(2); // 3번 키는 현재 손패 세 번째 카드 선택
            if (Keyboard.current.digit4Key.wasPressedThisFrame) TrySelectHandSlot(3); // 4번 키는 현재 손패 네 번째 카드 선택
            if (Keyboard.current.digit5Key.wasPressedThisFrame) TrySelectHandSlot(4); // 5번 키는 현재 손패 다섯 번째 카드 선택
            if (Keyboard.current.digit6Key.wasPressedThisFrame) TrySelectHandSlot(5); // 6번 키는 현재 손패 여섯 번째 카드 선택
            if (Keyboard.current.digit7Key.wasPressedThisFrame) TrySelectHandSlot(6); // 7번 키는 현재 손패 일곱 번째 카드 선택
            if (Keyboard.current.digit8Key.wasPressedThisFrame) TrySelectHandSlot(7); // 8번 키는 현재 손패 여덟 번째 카드 선택
            if (Keyboard.current.digit9Key.wasPressedThisFrame) TrySelectHandSlot(8); // 9번 키는 현재 손패 아홉 번째 카드 선택
            if (Keyboard.current.digit0Key.wasPressedThisFrame) TrySelectHandSlot(9); // 0번 키는 현재 손패 열 번째 카드 선택
        }

        public bool TrySelectHandSlot(int handIndex) // 16일차: 현재 손패 인덱스를 기준으로 카드를 선택하는 테스트 가능한 진입점
        {
            if (!CanReceivePlayerInput || _handState == null) // 입력 권한이 없거나 손패 상태가 없으면
            {
                return false; // 카드 선택 실패 반환
            }

            if (handIndex < 0 || handIndex >= _handState.Hand.Count) // 요청한 슬롯에 카드가 존재하지 않으면
            {
                Debug.Log($"손패 {handIndex + 1}번 슬롯에는 카드가 없습니다."); // 비어 있는 슬롯임을 개발 로그로 출력
                return false; // 선택 상태를 바꾸지 않고 실패 반환
            }

            ToggleCardSelection(_handState.Hand[handIndex]); // 현재 손패 순서 그대로 해당 카드 선택 토글
            return true; // 유효한 손패 슬롯 입력으로 처리했음을 반환
        }

        private void ToggleCardSelection(PieceDefinition card) // 손패 카드 선택 상태를 켜고 끄는 메서드
        {
            if (!CanReceivePlayerInput || _handState == null || card == null) // 입력 권한·손패 연결·카드 데이터 중 하나라도 없으면
            {
                return; // 처리하지 않고 종료
            }

            if (!_handState.Hand.Contains(card)) // 실제 RunState.Hand에 해당 카드가 없으면
            {
                Debug.Log($"{card.DisplayName} 카드가 손패에 없습니다."); // 사유를 콘솔에 출력
                return; // 선택하지 않고 종료
            }

            DeselectPiece(); // 카드를 선택하면 진행 중이던 기물 이동 선택 취소
            _selectedCard = _selectedCard == card ? null : card; // 이미 선택된 카드면 해제, 아니면 새로 선택
            Debug.Log(_selectedCard != null // 선택 결과에 따라
                ? $"카드 선택: {_selectedCard.DisplayName} (배치할 아군 칸을 클릭하세요)" // 선택됐을 때 안내 로그
                : "카드 선택 해제"); // 해제됐을 때 안내 로그
        }

        private void HandleBoardClick() // 보드 클릭 시 카드 소환·기물 선택·기물 이동을 처리하는 메서드
        {
            if (!CanReceivePlayerInput || _camera == null || _boardView == null) // 입력 권한·카메라·보드 뷰 중 하나라도 없으면
            {
                return; // 클릭 처리를 하지 않고 종료
            }

            var screenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            var ray = _camera.ScreenPointToRay(screenPosition); // 화면 좌표를 카메라 기준 광선으로 변환

            if (Physics.Raycast(ray, out var hit) && _boardView.TryGetCellFromWorldPoint(hit.point, out var cell)) // 광선이 보드 메시에 맞고 유효한 칸으로 변환되면
            {
                if (_selectedCard != null) // 선택된 손패 카드가 있으면
                {
                    TryPlayCardOnCell(cell); // 그 칸에 카드 소환 시도
                    return; // 처리 완료 후 종료
                }

                if (_selectedPiece != null) // 이동을 위해 선택해 둔 기물이 있으면
                {
                    if (_pendingMovement != null && _pendingMovement.MoveTiles.Contains(cell)) // 클릭한 칸이 이동 후보면
                    {
                        ExecuteMove(cell); // 실제 이동 실행
                        return; // 처리 완료 후 종료
                    }

                    if (_pendingMovement != null && _pendingMovement.AttackTiles.Contains(cell)) // 클릭한 칸이 공격 후보면
                    {
                        ExecuteAttack(cell); // 실제 전투 판정 실행
                        return; // 처리 완료 후 종료
                    }
                }

                if (TrySelectPieceAt(cell)) // 클릭한 칸에 내 기물이 있으면 선택 처리
                {
                    return; // 처리 완료 후 종료
                }

                DeselectPiece(); // 후보 칸도 내 기물도 아닌 칸을 클릭했으면 기물 선택 해제
                SelectCell(cell); // 기존 일반 칸 선택 처리
                return; // 처리 완료 후 종료
            }

            DeselectPiece(); // 보드 밖을 클릭했으면 기물 선택도 해제
            DeselectCurrentCell(); // 보드 밖을 클릭했으면 선택 해제
        }

        public bool TrySelectPieceAt(Vector2Int cell) // 지정한 칸에 내 기물이 있으면 선택하고 이동/공격 후보를 계산하는 진입점
        {
            if (!CanReceivePlayerInput) // 적 턴이거나 아직 상태가 연결되지 않았으면
            {
                return false; // 선택할 수 없으므로 실패 반환
            }

            var tileState = _boardView.GetTile(cell); // 클릭한 칸의 실제 타일 데이터 조회
            if (tileState == null || !tileState.IsOccupied || !tileState.OccupyingPiece.IsPlayerPiece) // 유효 칸이 아니거나 내 기물이 없으면
            {
                return false; // 선택하지 않고 실패 반환
            }

            if (_selectedPiece == tileState.OccupyingPiece) // 이미 선택된 같은 기물을 다시 클릭했으면
            {
                DeselectPiece(); // 선택 해제
                return true; // 선택 관련 클릭으로 처리했음을 반환
            }

            _selectedPiece = tileState.OccupyingPiece; // 새로 선택한 기물 저장
            _pendingMovement = MovementResolver.GetReachableTiles( // 이 기물의 이동/공격 후보 칸 계산
                _selectedPiece.Definition.MovementType, // 기물 이동 유형 전달
                _selectedPiece.BoardPosition, // 현재 좌표 전달
                _selectedPiece.IsPlayerPiece, // 아군 여부 전달
                _boardView.State); // 실제 보드 상태 전달
            _boardView.HighlightMoveCandidates(_pendingMovement.MoveTiles, _pendingMovement.AttackTiles); // 계산한 후보 칸을 화면에 강조 표시

            Debug.Log($"{_selectedPiece.Definition.DisplayName} 선택: 이동 {_pendingMovement.MoveTiles.Count}칸 / 공격 {_pendingMovement.AttackTiles.Count}칸"); // 선택 결과 출력
            return true; // 정상적으로 선택했음을 반환
        }

        public bool TryMoveSelectedPieceTo(Vector2Int destination) // 현재 선택된 기물을 지정한 후보 칸으로 이동시키는 진입점
        {
            if (!CanReceivePlayerInput || _selectedPiece == null || _pendingMovement == null || !_pendingMovement.MoveTiles.Contains(destination)) // 유효한 이동 상태가 아니면
            {
                return false; // 이동 실패 반환
            }

            ExecuteMove(destination); // 실제 이동 실행
            return true; // 정상적으로 이동했음을 반환
        }

        private void ExecuteMove(Vector2Int destination) // 선택된 기물을 실제로 이동시키고 턴을 소비하는 메서드
        {
            var piece = _selectedPiece; // 선택을 지우기 전에 이동 기물 참조 저장
            var origin = piece.BoardPosition; // 로그용 원래 좌표 저장

            MovePieceTo(piece, destination); // 보드 점유·좌표·화면 위치를 함께 갱신
            Debug.Log($"{piece.Definition.DisplayName} 이동: {origin} -> {destination}"); // 이동 결과 출력

            DeselectPiece(); // 이동 완료 후 선택과 후보 강조 해제
            _turnManager?.TryCompletePlayerAction(); // 이동을 이번 플레이어 턴 행동으로 처리
        }

        public bool TryAttackSelectedPieceTarget(Vector2Int target) // 현재 선택된 기물로 지정한 공격 후보 칸을 공격하는 진입점
        {
            if (!CanReceivePlayerInput || _selectedPiece == null || _pendingMovement == null || !_pendingMovement.AttackTiles.Contains(target)) // 유효한 공격 상태가 아니면
            {
                return false; // 공격 실패 반환
            }

            ExecuteAttack(target); // 실제 전투 판정 실행
            return true; // 정상적으로 공격을 실행했음을 반환
        }

        private void ExecuteAttack(Vector2Int target) // 공격 후보 칸에 대해 실제 HP·ATK 전투 판정을 실행하는 메서드
        {
            var targetTile = _boardView.GetTile(target); // 공격 대상 칸의 실제 타일 데이터 조회
            if (targetTile == null || !targetTile.IsOccupied) // 유효하지 않거나 이미 비어 있는 칸이면
            {
                DeselectPiece(); // 선택만 해제
                return; // 공격을 실행하지 않고 종료
            }

            var attacker = _selectedPiece; // 현재 선택된 공격자
            var defender = targetTile.OccupyingPiece; // 실제 공격 대상 기물
            var result = CombatResolver.ResolveAttack(attacker, defender); // 고정 ATK 규칙으로 전투 판정 실행

            Debug.Log($"{attacker.Definition.DisplayName} 공격 -> {defender.Definition.DisplayName}: {result.DamageDealt} 피해, 남은 HP {defender.CurrentHp}"); // 판정 결과 출력

            if (result.DefenderDied) // 대상이 사망했으면
            {
                RemovePieceFromBoard(defender); // 대상을 보드와 화면에서 제거
                MovePieceTo(attacker, target); // 공격자가 대상 칸을 점유
                Debug.Log($"{attacker.Definition.DisplayName}이(가) {target} 칸을 점유했습니다."); // 점유 결과 출력
            }
            else // 대상이 생존했으면
            {
                Debug.Log($"{defender.Definition.DisplayName} 생존 — {attacker.Definition.DisplayName}은(는) 원위치를 유지합니다."); // 비치명 결과 출력
            }

            AttackResolved?.Invoke(result); // 외부 시스템에 전투 결과 통지

            DeselectPiece(); // 선택과 후보 강조 해제
            _turnManager?.TryCompletePlayerAction(); // 공격도 플레이어 턴 행동으로 처리
        }

        private void MovePieceTo(PieceRuntimeState piece, Vector2Int destination) // 기물의 보드 좌표와 화면 위치를 함께 갱신하는 공통 메서드
        {
            var originTile = _boardView.GetTile(piece.BoardPosition); // 기물이 원래 있던 칸 조회
            if (originTile != null) // 원래 칸이 유효하면
            {
                originTile.OccupyingPiece = null; // 원래 칸 점유 해제
            }

            piece.BoardPosition = destination; // 기물 실제 좌표 갱신
            var destinationTile = _boardView.GetTile(destination); // 이동할 칸 조회
            if (destinationTile != null) // 대상 칸이 유효하면
            {
                destinationTile.OccupyingPiece = piece; // 새 칸 점유 상태 갱신
            }

            if (_pieceViews.TryGetValue(piece, out var pieceView) && pieceView != null) // 연결된 화면 표시가 있으면
            {
                pieceView.MoveTo(destination, _boardView.TileSize); // 화면 위치도 같은 좌표로 이동
            }
        }

        private void RemovePieceFromBoard(PieceRuntimeState piece) // 사망한 기물을 보드 점유와 화면에서 제거하는 메서드
        {
            var tile = _boardView.GetTile(piece.BoardPosition); // 이 기물이 있던 칸 조회
            if (tile != null && tile.OccupyingPiece == piece) // 아직 그 칸을 이 기물이 점유하면
            {
                tile.OccupyingPiece = null; // 점유 상태 해제
            }

            if (_pieceViews.TryGetValue(piece, out var pieceView) && pieceView != null) // 연결된 화면 표시가 있으면
            {
                Destroy(pieceView.gameObject); // 화면에서 기물 오브젝트 제거
            }

            _pieceViews.Remove(piece); // 화면 연결 정보 정리
        }

        private void DeselectPiece() // 현재 선택된 기물과 이동/공격 후보 강조를 해제하는 메서드
        {
            if (_selectedPiece == null) // 선택된 기물이 없으면
            {
                return; // 할 일이 없으므로 종료
            }

            _selectedPiece = null; // 선택 상태 초기화
            _pendingMovement = null; // 후보 계산 결과 초기화
            if (_boardView != null) // 보드 뷰가 존재하면
            {
                _boardView.ClearMoveCandidates(); // 화면의 이동/공격 후보 강조 해제
            }
        }

        private void TryPlayCardOnCell(Vector2Int cell) // 선택된 손패 카드를 지정한 칸에 소환 시도하는 메서드
        {
            if (!CanReceivePlayerInput || _selectedCard == null || _handState == null) // 필요한 입력·카드·손패 상태가 없으면
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
                Debug.Log($"{tileState.BoardPosition}에는 소환할 수 없습니다 (아군 영역의 빈 칸만 가능)."); // 실패 사유 출력
                return; // 소환하지 않고 종료
            }

            var runtimeState = SpawnPiece(_selectedCard, tileState, isPlayerPiece: true, objectName: "Piece"); // 선택된 카드로 기물을 실제 생성
            _handState.RemoveCard(_selectedCard); // 실제 RunState.Hand에서 사용한 카드 제거

            Debug.Log($"{runtimeState.Definition.DisplayName} 소환: {tileState.BoardPosition} / RunState.Hand={_handState.Hand.Count}장"); // 상태 변경 결과 출력
            _selectedCard = null; // 카드 선택 해제

            SelectCell(cell); // 소환한 칸을 선택 상태로 표시
        }

        public void SpawnTestEnemySquad(Vector2Int anchor) // 폰+룩 2종으로 테스트 적을 배치하는 편의 진입점
        {
            SpawnTestEnemy(_pawnDefinition, anchor); // 기준 좌표에 폰 배치
            SpawnTestEnemy(_rookDefinition, anchor + new Vector2Int(2, 0)); // 기준 좌표 오른쪽 2칸에 룩 배치
        }

        public PieceRuntimeState SpawnTestEnemy(PieceDefinition definition, Vector2Int position) // 테스트용 적 기물을 직접 배치하는 개발용 진입점
        {
            if (!IsBound || definition == null) // 상태가 연결되지 않았거나 기물 정의가 없으면
            {
                return null; // 소환 실패 반환
            }

            var tileState = _boardView.GetTile(position); // 지정 좌표의 실제 타일 데이터 조회
            if (tileState == null || tileState.IsOccupied) // 유효하지 않거나 이미 점유된 칸이면
            {
                Debug.LogWarning($"SpawnTestEnemy: {position}에 배치할 수 없습니다(범위 밖 또는 이미 점유됨)."); // 실패 사유 안내
                return null; // 소환 실패 반환
            }

            return SpawnPiece(definition, tileState, isPlayerPiece: false, objectName: "Piece(Enemy)"); // 적 기물로 생성하고 보드·화면에 등록
        }

        private PieceRuntimeState SpawnPiece(PieceDefinition definition, TileState tileState, bool isPlayerPiece, string objectName) // 기물 런타임 상태와 화면 표시를 함께 만드는 공용 메서드
        {
            var runtimeState = new PieceRuntimeState(definition, tileState.BoardPosition, isPlayerPiece); // 기물 런타임 상태 생성
            tileState.OccupyingPiece = runtimeState; // 실제 RunState.Board의 타일 점유 상태 갱신

            var pieceObject = new GameObject(objectName); // 기물을 담을 빈 오브젝트 생성
            pieceObject.transform.SetParent(_boardView.transform, false); // 보드 뷰 자식으로 배치
            var pieceView = pieceObject.AddComponent<PieceView>(); // 기물 뷰 컴포넌트 부착
            pieceView.Initialize(runtimeState, _boardView.TileSize); // 실제 런타임 데이터와 타일 크기 주입
            _pieceViews[runtimeState] = pieceView; // 이후 이동 시 같은 화면 오브젝트를 찾을 수 있도록 등록

            return runtimeState; // 생성된 런타임 상태 반환
        }

        private void SelectCell(Vector2Int cell) // 칸을 선택 상태로 만드는 메서드
        {
            if (!CanReceivePlayerInput) // 현재 플레이어 입력이 잠긴 상태라면
            {
                return; // 선택 상태를 바꾸지 않음
            }

            if (_selectedCell == cell) // 이미 같은 칸이 선택돼 있으면
            {
                DeselectCurrentCell(); // 선택 해제
                return; // 종료
            }

            _boardView.HighlightCell(cell); // 보드 메시에 강조 표시 반영
            _selectedCell = cell; // 새 칸을 선택 상태로 저장

            var tileState = _boardView.GetTile(cell); // 선택한 칸의 실제 RunState.Board 데이터 조회
            if (tileState != null) // 정상 타일이면
            {
                Debug.Log($"Tile selected: {tileState.BoardPosition} - Occupied: {tileState.IsOccupied}, PlayerArea: {tileState.IsPlayerPlacementArea}"); // 좌표·점유·영역 상태 출력
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

        private void OnGUI() // 화면 좌측에 16일차 개발용 카드 상태 UI를 그리는 메서드
        {
            if (!IsBound) // 실제 RunState가 아직 연결되지 않았으면
            {
                GUI.Label(new Rect(10, 10, 600, 20), "BattleController가 RunState를 연결하는 중입니다."); // 상태 연결 대기 안내
                return; // 카드 UI는 그리지 않음
            }

            for (int i = 0; i < HandState.MaxHandSize; i++) // 손패 최대 10개 슬롯을 순서대로 표시하며
            {
                string keyLabel = i == 9 ? "0" : (i + 1).ToString(); // 열 번째 슬롯은 0번 키로 표시
                PieceDefinition card = i < _handState.Hand.Count ? _handState.Hand[i] : null; // 현재 슬롯의 실제 손패 카드 조회
                GUI.Label(new Rect(10, 10 + (i * 20), 520, 20), BuildHandSlotLabel(keyLabel, card)); // 숫자키·카드 이름·선택 상태 표시
            }

            GUI.Label(new Rect(10, 220, 620, 20), $"Hand {_handState.Hand.Count}/{HandState.MaxHandSize} | Draw {_runState.Deck.DrawPile.Count} | Owned {_runState.Deck.OwnedCardPool.Count} | Dead {_runState.Deck.DeadCardPile.Count}"); // 실제 카드 상태 요약
            GUI.Label(new Rect(10, 240, 620, 20), $"입력: {(CanReceivePlayerInput ? "가능" : "적 턴 잠김")} / 숫자키 1~0 = 현재 손패 슬롯 선택"); // 변경된 숫자키 조작 안내
            GUI.Label(new Rect(10, 260, 620, 20), BuildSelectedPieceLabel()); // 현재 선택된 기물과 이동/공격 후보 수 안내
        }

        private string BuildHandSlotLabel(string keyLabel, PieceDefinition card) // 현재 손패 슬롯을 개발용 텍스트로 만드는 메서드
        {
            if (card == null) // 해당 손패 슬롯이 비어 있으면
            {
                return $"[{keyLabel}] -"; // 비어 있는 슬롯 표시
            }

            string displayName = string.IsNullOrEmpty(card.DisplayName) ? card.name : card.DisplayName; // 표시 이름이 비어 있으면 Unity 오브젝트 이름을 대신 사용
            return _selectedCard == card // 현재 선택 카드와 비교해
                ? $"[{keyLabel}] {displayName} <선택됨>" // 선택된 손패 카드 표시
                : $"[{keyLabel}] {displayName}"; // 일반 손패 카드 표시
        }

        private string BuildSelectedPieceLabel() // 선택된 기물 상태를 안내 문구로 만드는 메서드
        {
            if (_selectedPiece == null || _pendingMovement == null) // 선택된 기물이 없으면
            {
                return "선택된 기물 없음 / 내 기물을 클릭해 이동 후보를 확인하세요."; // 안내 문구 반환
            }

            return $"선택: {_selectedPiece.Definition.DisplayName} @ {_selectedPiece.BoardPosition} / 이동 {_pendingMovement.MoveTiles.Count}칸, 공격 {_pendingMovement.AttackTiles.Count}칸"; // 선택 상태 안내 문구 반환
        }

        private void OnDestroy() // 입력 컨트롤러가 파괴될 때 이벤트 구독을 정리하는 메서드
        {
            if (_turnManager != null) // 연결된 턴 매니저가 남아 있으면
            {
                _turnManager.TurnChanged -= HandleTurnChangedForCardDraw; // 자동 드로우 이벤트 구독 해제
            }
        }
    }
}
