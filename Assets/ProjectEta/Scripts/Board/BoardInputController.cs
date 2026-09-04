using System; // Action 이벤트와 Math를 사용하기 위한 네임스페이스
using System.Collections.Generic; // Dictionary를 사용하기 위한 네임스페이스
using System.Linq; // IReadOnlyList에 대한 Contains 확장 메서드를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Physics 등을 사용하기 위한 네임스페이스
using UnityEngine.EventSystems; // 버그 수정: 클릭이 UI(카드 더미 버튼·패널) 위에서 발생했는지 확인하기 위한 네임스페이스
using UnityEngine.InputSystem; // 새 Input System(Mouse, Keyboard)을 사용하기 위한 네임스페이스
using UnityEngine.Rendering; // 드래그 중 기물 고스트 머티리얼의 투명 블렌딩을 설정하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState, DeckState를 사용하기 위한 네임스페이스
using ProjectEta.Fusion; // 21일차: FusionRecipe, FusionRecipeDatabase를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceView를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public class BoardInputController : MonoBehaviour // 마우스/키보드로 일반 전투 입력과 배치 턴 카드 입력을 분리해 처리하는 컴포넌트
    {
        [SerializeField] private Camera _camera; // 레이캐스트에 사용할 카메라
        [SerializeField] private BoardView _boardView; // 좌표 변환·컨테이너로 쓸 보드 뷰
        [SerializeField] private PieceDefinition _kingDefinition; // 프로토타입 시작 풀에 넣을 킹 카드 데이터
        [SerializeField] private PieceDefinition _pawnDefinition; // 프로토타입 시작 풀에 넣을 폰 카드 데이터
        [SerializeField] private PieceDefinition _knightDefinition; // 프로토타입 시작 풀에 넣을 나이트 카드 데이터
        [SerializeField] private PieceDefinition _bishopDefinition; // 프로토타입 시작 풀에 넣을 비숍 카드 데이터
        [SerializeField] private PieceDefinition _rookDefinition; // 프로토타입 시작 풀에 넣을 룩 카드 데이터
        [SerializeField] private PieceDefinition _queenDefinition; // 프로토타입 시작 풀에 넣을 퀸 카드 데이터
        [SerializeField] private FusionRecipeDatabase _fusionRecipeDatabase; // 21일차: 재료 2장으로 합성 레시피를 조회할 데이터베이스

        public RunState RunState => _runState; // 현재 입력이 변경하는 실제 런 상태
        public HandState HandState => _handState; // 현재 입력이 변경하는 실제 플레이어 손패 상태
        public HandState EnemyHandState => _enemyHandState; // 17일차 추가: 적 턴 자동 소환에 사용하는 프로토타입 적 손패
        public DeckState EnemyDeck => _enemyDeck; // 20일차 추가: 플레이어와 동일한 구조의 적 보유 풀·드로우·죽은 카드 더미
        public TurnManager TurnManager => _turnManager; // 현재 입력 권한을 판단하는 실제 턴 매니저
        public BattleHooks BattleHooks => _battleHooks; // 31일차: 정보 패널 UI 등이 피해·턴 훅을 구독할 수 있도록 공개하는 접근자
        public bool IsBound => _runState != null && _handState != null && _boardView != null && _boardView.IsBound; // 전투 상태 연결 여부
        public bool CanReceivePlayerInput => IsBound && (_turnManager == null || _turnManager.CanPlayerInput); // 일반 턴 또는 배치 턴에서 플레이어 입력을 받을 수 있는지 여부
        public bool CanUseCombatInput => IsBound && (_turnManager == null || _turnManager.CanPlayerAct); // 기물 선택·이동·공격 같은 일반 전투 입력 가능 여부
        public bool CanUseDeploymentInput => IsBound && (_turnManager == null || _turnManager.CanDeploy); // 자유 배치 턴 입력 가능 여부
        public bool CanUseCardSummonInput => IsBound && (_turnManager == null || _turnManager.CurrentState == TurnState.DeploymentTurn || _turnManager.CanPlayerAct); // 배치 턴 또는 일반 플레이어 턴의 카드 소환 입력 가능 여부
        public bool CanUseFusionInput => IsBound && (_turnManager == null || _turnManager.CanDeploy); // 21일차: 합성은 배치 턴에만 가능(확정 규칙)
        public bool IsFusionModeActive => _isFusionModeActive; // 21일차: 합성 버튼으로 진입한 재료 선택 모드인지 여부
        public IReadOnlyList<PieceDefinition> FusionMaterials => _fusionMaterials; // 21일차: 현재 합성 재료로 선택된 손패 카드 목록(최대 2장)
        public FusionRecipe CurrentFusionRecipe => _currentFusionRecipe; // 21일차: 현재 선택된 재료 2장으로 미리 계산된 합성 결과 레시피(없으면 null)
        public FusionBlockReason CurrentFusionBlockReason => _currentFusionBlockReason; // 22일차: 현재 재료 조합이 합성 불가라면 그 구체적인 사유
        public bool IsCurrentFusionRecipeUndiscovered => _currentFusionRecipe != null && _runState != null && !_runState.FusionDiscovery.IsDiscovered(_currentFusionRecipe); // 22일차: 아직 발견하지 못한 숨김 레시피라서 결과를 가려야 하는지 여부
        public PieceDefinition SelectedCard => _selectedCard; // 현재 손패에서 숫자키로 선택된 카드
        public PieceRuntimeState SelectedPiece => _selectedPiece; // 현재 선택된 이동 대기 기물
        public MovementResult PendingMovement => _pendingMovement; // 현재 선택된 기물의 이동/공격 후보 칸
        public bool HasCardDropGhost => _cardDropGhost != null; // 카드 드래그 중 실제 기물 형태의 3D 고스트가 존재하는지 여부
        public Vector2Int CardDropPreviewCell => _cardDropPreviewCell ?? new Vector2Int(-1, -1); // 현재 고스트가 가리키는 보드 셀
        public bool IsCardDropPreviewValid => _isCardDropPreviewValid; // 현재 고스트 위치가 실제 소환 가능한 칸인지 여부

        public event Action<CombatResult> AttackResolved; // 공격 판정이 끝날 때마다 외부 전투 시스템에 알리는 이벤트
        public event Action HandChanged; // 18일차: Draw·소환 등 실제 플레이어 손패가 바뀔 때 카드 UI에 알리는 이벤트
        public event Action DeckChanged; // 19일차: 보유 풀·드로우 더미·죽은 카드 더미 구성이 바뀔 때 덱/무덤 패널 UI에 알리는 이벤트
        public event Action FusionSelectionChanged; // 21일차: 합성 모드 On/Off, 재료 선택, 결과 미리보기가 바뀔 때 합성 패널 UI에 알리는 이벤트
        public event Action<FusionRecipe> HiddenRecipeDiscovered; // 22일차: 숨김 합성식을 이번 합성으로 처음 발견했을 때 알리는 이벤트
        public event Action<PieceRuntimeState> SelectionChanged; // 31일차: 보드 위 기물 선택이 바뀌거나 해제될 때(null) 정보 패널 UI에 알리는 이벤트

        private const int PrototypeInitialHandSize = 5; // 기본 6종 중 5장을 초기 손패로 뽑는 테스트 값
        private const int EnemyInitialHandSize = 3; // 20일차: 적 카드 5종 중 3장만 먼저 손패로 뽑고 나머지는 드로우 더미에 남겨 이후 다시 뽑히게 하는 테스트 값
        private RunState _runState; // BattleController가 소유하는 실제 런 상태
        private HandState _handState; // RunState.Hand 참조
        private readonly HandState _enemyHandState = new HandState(); // 적 AI가 자기 턴에 소비해 소환할 프로토타입 손패
        private readonly DeckState _enemyDeck = new DeckState(); // 20일차: 플레이어와 동일한 구조로 적의 보유 풀·드로우·죽은 카드 더미를 관리
        private readonly List<TileState> _freeEnemyTileBuffer = new List<TileState>(); // 20일차: 적 무작위 소환 위치를 고를 때 재사용하는 빈 칸 후보 목록
        private TurnManager _turnManager; // BattleController가 소유하는 실제 턴 매니저
        private BattleHooks _battleHooks; // 29일차: BattleController가 소유하는 실제 전투 훅 버스
        private Vector2Int? _selectedCell; // 현재 선택된 칸 좌표
        private PieceDefinition _selectedCard; // 현재 선택된 손패 카드
        private PieceRuntimeState _selectedPiece; // 현재 이동을 위해 선택된 보드 위 기물
        private MovementResult _pendingMovement; // 선택된 기물의 이동/공격 후보 칸 계산 결과
        private readonly Dictionary<PieceRuntimeState, PieceView> _pieceViews = new Dictionary<PieceRuntimeState, PieceView>(); // 기물 데이터와 화면 표시 연결 목록
        private GameObject _cardDropGhost; // 카드 드래그 중 목표 셀에 표시하는 실제 기물 실루엣 고스트 루트
        private PieceView _cardDropGhostView; // 기존 PieceView 모델링을 재사용하는 고스트 뷰
        private Material _cardDropGhostMaterial; // 유효/무효 위치에 따라 색을 바꾸는 투명 고스트 머티리얼
        private PieceDefinition _cardDropGhostDefinition; // 현재 고스트가 표현 중인 카드 정의
        private Vector2Int? _cardDropPreviewCell; // 현재 마우스가 가리키는 보드 셀
        private bool _isCardDropPreviewValid; // 현재 프리뷰 셀의 실제 소환 가능 여부
        private bool _isFusionModeActive; // 21일차: 합성 버튼으로 진입한 재료 선택 모드 여부
        private readonly List<PieceDefinition> _fusionMaterials = new List<PieceDefinition>(2); // 21일차: 현재 합성 재료로 클릭 선택된 손패 카드(최대 2장)
        private FusionRecipe _currentFusionRecipe; // 21일차: 현재 선택된 재료 2장에 대한 미리보기 레시피(없으면 null)
        private FusionBlockReason _currentFusionBlockReason = FusionBlockReason.NotEnoughMaterials; // 22일차: 현재 재료 조합의 합성 차단 사유(재료 미선택이 기본값)

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

        public void Bind(RunState runState, BoardView boardView = null, TurnManager turnManager = null, BattleHooks battleHooks = null) // BattleController가 실제 RunState와 턴 매니저를 입력 시스템에 연결하는 메서드(29일차: 전투 훅 버스 매개변수 추가)
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

            if (_battleHooks != null) // 29일차: 이전 훅 버스가 연결돼 있었다면
            {
                _battleHooks.TurnEnd -= HandleBattleHooksTurnEnd; // 재연결 전에 턴 종료 정산 구독 해제
                _battleHooks.AfterAttack -= HandleBattleHooksAfterAttackVisual; // 30일차: 재연결 전에 공격 연출 구독 해제
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
            _battleHooks = battleHooks; // 29일차: 전달받은 전투 훅 버스를 이동·공격·상태 정산에 사용
            _selectedCard = null; // 상태 교체 시 이전 카드 선택 제거
            DeselectPiece(); // 상태 교체 시 이전 기물 선택도 제거
            DeselectCurrentCell(); // 이전 선택 칸 강조 제거

            if (_turnManager != null) // 턴 매니저가 실제로 전달됐다면
            {
                _turnManager.TurnChanged -= HandleTurnChangedForCardDraw; // 중복 구독을 방지하기 위해 먼저 해제
                _turnManager.TurnChanged += HandleTurnChangedForCardDraw; // 2턴 이후 플레이어 턴 시작 자동 드로우 구독
            }

            if (_battleHooks != null) // 29일차: 훅 버스가 실제로 전달됐다면
            {
                _battleHooks.TurnEnd -= HandleBattleHooksTurnEnd; // 중복 구독을 방지하기 위해 먼저 해제
                _battleHooks.TurnEnd += HandleBattleHooksTurnEnd; // 턴 종료 훅에 28일차 상태 이상 정산을 구독
                _battleHooks.AfterAttack -= HandleBattleHooksAfterAttackVisual; // 30일차: 중복 구독을 방지하기 위해 먼저 해제
                _battleHooks.AfterAttack += HandleBattleHooksAfterAttackVisual; // 30일차: 공격 결과 훅에 연출 재생을 구독
            }
        }

        private void HandleBattleHooksTurnEnd(TurnState state, int turnNumber) // 29일차: TurnEnd 훅을 구독해 28일차 상태 이상 정산을 실행하는 메서드
        {
            ApplyTurnEndStatusEffects(); // 독·화상 피해 적용과 지속 턴 감소를 실제로 수행
        }

        private void HandleBattleHooksAfterAttackVisual(CombatResult result) // 30일차: 판정 결과에 따라 생존 시 공격자·방어자 연출을 재생하는 메서드(치명타는 RemovePieceFromBoard·MovePieceTo가 이미 처리)
        {
            if (result == null || result.DefenderDied) // 결과가 없거나 이미 치명타로 처리됐으면
            {
                return; // 생존 케이스에서만 별도 연출이 필요함(치명 처치의 전진·쓰러짐은 다른 경로에서 이미 재생)
            }

            bool attackerIsMelee = result.Attacker.Definition != null && (result.Attacker.Definition.RoleTags & PieceRoleTag.Ranged) == 0; // 원거리 공격자는 오늘 범위에서 접근 연출을 재생하지 않음(투사체 연출은 이후 일차)

            if (attackerIsMelee && _pieceViews.TryGetValue(result.Attacker, out var attackerView) && attackerView != null) // 근접 공격자의 화면 표시를 찾았으면
            {
                attackerView.PlayNonLethalStrikeAndReturn(result.Defender.BoardPosition, _boardView.TileSize); // 목표 쪽으로 다가가 타격한 뒤 원위치로 복귀
            }

            if (_pieceViews.TryGetValue(result.Defender, out var defenderView) && defenderView != null) // 생존한 대상의 화면 표시를 찾았으면
            {
                defenderView.PlayHitReaction(); // 짧게 흔들리는 피격 반응 재생
            }
        }

        public void EnsurePrototypeStartingHand() // 기존 호출부 호환을 유지하면서 실제 DeckState→HandState 시작 흐름을 구성하는 메서드
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

            if (_kingDefinition == null || !deck.TryMoveSpecificToHand(_kingDefinition, _handState)) // 초기 배치가 막히지 않도록 킹을 반드시 손패에 먼저 넣음
            {
                Debug.LogError("시작 손패에 킹을 포함하지 못했습니다. King PieceDefinition 연결을 확인하세요."); // 킹 누락은 진행 불가 원인이므로 명확히 오류 출력
                return; // 킹 없는 상태로 전투를 시작하지 않음
            }

            while (_handState.Hand.Count < PrototypeInitialHandSize && deck.TryDrawToHand(_handState)) // 킹 1장 + 나머지 랜덤 카드로 초기 손패 5장을 채움
            {
                // TryDrawToHand가 실제 카드 이동을 수행하므로 반복문 본문에서 추가 처리하지 않음
            }

            Debug.Log($"시작 덱 구성: 킹 포함 / Owned={deck.OwnedCardPool.Count}, Hand={_handState.Hand.Count}, Draw={deck.DrawPile.Count}"); // 실제 초기 카드 상태 로그
            HandChanged?.Invoke(); // 시작 손패 5장이 완성됐음을 이미지 손패 UI에 즉시 알림
            DeckChanged?.Invoke(); // 19일차: 드로우 더미가 처음 구성됐음을 덱/무덤 패널 UI에 알림
        }

        public void EnsurePrototypeEnemyStartingHand() // 20일차: 플레이어와 동일하게 보유 풀→드로우 더미→손패 구조로 적 시작 손패를 구성하는 메서드
        {
            if (_enemyDeck.OwnedCardPool.Count > 0 || _enemyHandState.Hand.Count > 0) // 이미 적 덱·손패가 구성돼 있으면
            {
                return; // 중복 카드 추가를 방지하고 종료
            }

            var enemyCards = new[] // 현재 단계에서는 킹을 제외한 기본 전투 기물 5종을 적 카드로 사용
            {
                _pawnDefinition, // 적 폰 카드
                _knightDefinition, // 적 나이트 카드
                _bishopDefinition, // 적 비숍 카드
                _rookDefinition, // 적 룩 카드
                _queenDefinition // 적 퀸 카드
            };

            foreach (var card in enemyCards) // 기본 적 카드 목록을 순회하며
            {
                if (card != null) // 실제 PieceDefinition이 연결된 카드만
                {
                    _enemyDeck.AddToOwnedPool(card); // 손패가 아니라 먼저 적의 보유 카드 풀에 등록
                }
            }

            _enemyDeck.RebuildDrawPileFromOwnedPool(); // 보유 카드 풀 전체를 복사하고 실제 드로우 순서로 셔플

            while (_enemyHandState.Hand.Count < EnemyInitialHandSize && _enemyDeck.TryDrawToHand(_enemyHandState)) // 적 시작 손패를 정해진 장수만큼 채움(나머지는 드로우 더미에 남아 이후 다시 뽑힘)
            {
                // TryDrawToHand가 실제 카드 이동을 수행하므로 반복문 본문에서 추가 처리하지 않음
            }

            Debug.Log($"적 시작 덱 구성: Owned={_enemyDeck.OwnedCardPool.Count}, Hand={_enemyHandState.Hand.Count}, Draw={_enemyDeck.DrawPile.Count}"); // 적 카드 준비 상태를 개발 로그로 출력
        }

        private PieceDefinition[] GetPrototypeStartingCards() // 프로토타입 시작 풀 6종을 한 곳에서 반환하는 메서드
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

        private void HandleTurnChangedForCardDraw(TurnState state, int turnNumber) // 새로운 플레이어 일반 턴에 자동 드로우를 연결하는 이벤트 처리 메서드
        {
            if (state != TurnState.PlayerTurn || turnNumber <= 1) // 첫 턴은 초기 손패와 중복되므로 2턴 이후 플레이어 턴만 처리
            {
                return; // 자동 드로우 없이 종료
            }

            TryDrawOneCard(); // 실제 DeckState에서 손패로 카드 1장 이동 시도
        }

        public bool TryDrawOneCard() // 테스트와 턴 이벤트에서 함께 사용하는 일반 드로우 진입점
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
            HandChanged?.Invoke(); // 자동 드로우로 손패가 바뀌었음을 카드 UI에 알림
            DeckChanged?.Invoke(); // 19일차: 드로우 더미 장수가 줄었음을 덱 패널 UI에 알림
            return true; // 드로우 성공 반환
        }

        public bool TryDiscardHandCardToBottom(PieceDefinition card) // 19일차: 배치 턴에 손패 카드 1장을 드로우 더미 맨 아래로 정리하는 진입점
        {
            if (!CanUseDeploymentInput || _runState == null || _handState == null || card == null) // 배치 턴이 아니거나 필수 정보가 없으면
            {
                return false; // 정리를 실행하지 않고 실패 반환
            }

            if (!_runState.Deck.DiscardToBottom(card, _handState)) // 실제 손패→드로우 더미 이동 시도
            {
                return false; // 손패에 없는 카드 등으로 실패
            }

            if (_selectedCard == card) _selectedCard = null; // 정리한 카드가 선택돼 있었다면 선택 해제

            Debug.Log($"손패 정리: {card.DisplayName} -> 드로우 더미 맨 아래 / Hand={_handState.Hand.Count}, Draw={_runState.Deck.DrawPile.Count}"); // 정리 결과 출력
            HandChanged?.Invoke(); // 손패 수가 줄었음을 카드 UI에 알림
            DeckChanged?.Invoke(); // 19일차: 드로우 더미 구성이 바뀌었음을 덱 패널 UI에 알림
            return true; // 정리 성공 반환
        }

        public void ReturnDeadPileToOwnedPool() // 19일차: 승리 등으로 라운드가 끝났을 때 죽은 카드 더미를 보유 풀로 복귀시키는 진입점
        {
            if (_runState == null) return; // 연결된 런 상태가 없으면 처리하지 않음
            if (_runState.Deck.DeadCardPile.Count == 0) return; // 되돌릴 죽은 카드가 없으면 종료

            int returned = _runState.Deck.DeadCardPile.Count; // 로그용으로 복귀 전 죽은 카드 수 저장
            _runState.Deck.ReturnDeadPileToOwnedPool(); // 실제 죽은 카드 더미를 보유 풀로 복귀
            Debug.Log($"라운드 클리어: 죽은 카드 {returned}장이 보유 풀로 복귀했습니다."); // 복귀 결과 출력
            DeckChanged?.Invoke(); // 19일차: 죽은 카드 더미가 비워졌음을 덱/무덤 패널 UI에 알림
        }

        public bool TryFindFusionRecipe(PieceDefinition materialA, PieceDefinition materialB, out FusionRecipe recipe) // 21일차: 재료 2장(순서 무관)으로 매칭되는 합성 레시피를 조회하는 진입점
        {
            recipe = null; // 기본값은 매칭 없음
            if (_fusionRecipeDatabase == null || materialA == null || materialB == null) // 데이터베이스나 재료가 없으면
            {
                return false; // 조회하지 않고 실패 반환
            }

            return _fusionRecipeDatabase.TryFindRecipe(materialA, materialB, out recipe); // 실제 레시피 데이터베이스에 위임
        }

        public FusionBlockReason EvaluateFusion(PieceDefinition materialA, PieceDefinition materialB, out FusionRecipe recipe) // 22일차: 등급 규칙·재료 분류·손패 보유·수량 제한을 한 번에 판정해 합성 가능 여부와 사유를 돌려주는 메서드
        {
            recipe = null; // 기본값은 매칭 결과 없음

            if (!CanUseFusionInput || _handState == null) // 배치 턴이 아니거나 손패가 연결되지 않았으면
            {
                return FusionBlockReason.NotDeploymentTurn; // 턴 조건 위반으로 차단
            }

            if (materialA == null || materialB == null) // 재료가 아직 2장 모이지 않았으면
            {
                return FusionBlockReason.NotEnoughMaterials; // 재료 부족으로 차단
            }

            if (!TryFindFusionRecipe(materialA, materialB, out recipe)) // 일치하는 레시피가 없으면
            {
                return FusionBlockReason.NoRecipe; // 조합 없음으로 차단
            }

            var recipeReason = FusionRuleValidator.ValidateRecipe(recipe); // 레시피 자체의 재료 분류·등급 상승 규칙 검증
            if (recipeReason != FusionBlockReason.None) // 규칙을 위반한 레시피면
            {
                return recipeReason; // 위반 사유를 그대로 반환해 매칭을 거부
            }

            if (!HasCardsAvailableForFusion(materialA, materialB)) // 실제 손패에 재료 2장(동일 카드면 2장 모두)이 없으면
            {
                return FusionBlockReason.MaterialsMissingInHand; // 손패 부족으로 차단
            }

            int ownedCount = _runState != null ? _runState.CountOwnedCopies(recipe.Result) : 0; // 결과 기물을 이미 몇 개 보유 중인지 확인
            return FusionRuleValidator.ValidateOwnedLimit(recipe.Result, ownedCount); // 4·5성 보유 수량 제한까지 판정한 최종 결과 반환
        }

        public bool TryFuseCards(PieceDefinition materialA, PieceDefinition materialB) // 21일차: 손패의 재료 카드 2장을 실제로 합성해 결과 카드를 손패에 넣는 진입점
        {
            var blockReason = EvaluateFusion(materialA, materialB, out var recipe); // 22일차: 턴·레시피·등급·손패·수량 규칙을 한 번에 검증
            if (blockReason != FusionBlockReason.None) // 하나라도 위반했으면
            {
                Debug.Log($"합성 불가: {FusionRuleValidator.DescribeBlockReason(blockReason)}"); // 차단 사유를 개발 로그로 안내
                return false; // 카드 상태를 바꾸지 않고 실패 반환
            }

            _handState.RemoveCard(materialA); // 재료 A를 손패에서 제거
            _handState.RemoveCard(materialB); // 재료 B를 손패에서 제거(동일 카드면 남은 두 번째 장이 제거됨)
            _handState.TryAddCard(recipe.Result); // 결과 카드를 손패에 추가(같은 배치 턴에 바로 배치 가능)

            if (_runState != null) // 22일차: 보유 카드 풀이 4·5성 수량 제한의 단일 기준이므로 합성 결과를 여기에도 반영
            {
                _runState.Deck.RemoveFromOwnedPool(materialA); // 소모한 재료 A를 보유 풀에서 제거(다음 라운드에 되살아나지 않도록)
                _runState.Deck.RemoveFromOwnedPool(materialB); // 소모한 재료 B를 보유 풀에서 제거
                _runState.Deck.AddToOwnedPool(recipe.Result); // 새로 얻은 합성 결과를 보유 풀에 등록
            }

            if (_selectedCard == materialA || _selectedCard == materialB) _selectedCard = null; // 합성에 쓰인 카드가 숫자키로 선택돼 있었다면 선택 해제

            Debug.Log($"합성: {materialA.DisplayName} + {materialB.DisplayName} -> {recipe.Result.DisplayName}"); // 합성 결과를 콘솔에 출력

            if (_runState != null && _runState.FusionDiscovery.TryMarkDiscovered(recipe)) // 22일차: 숨김 레시피를 이번 합성으로 처음 성공시켰으면
            {
                Debug.Log($"숨김 합성식 발견: {recipe.RecipeId} ({materialA.DisplayName} + {materialB.DisplayName} -> {recipe.Result.DisplayName})"); // 발견 사실을 개발 로그로 기록
                HiddenRecipeDiscovered?.Invoke(recipe); // 발견 알림을 UI에 전달
            }

            HandChanged?.Invoke(); // 손패 구성이 바뀌었음을 카드 UI에 알림
            return true; // 합성 성공 반환
        }

        private bool HasCardsAvailableForFusion(PieceDefinition materialA, PieceDefinition materialB) // 손패에 재료 2장을 실제로 소모할 수 있는지 확인하는 메서드(동일 카드 합성 시 2장 보유 여부까지 확인)
        {
            if (_handState == null) return false; // 손패가 없으면 불가

            if (materialA != materialB) // 서로 다른 재료면
            {
                return _handState.Hand.Contains(materialA) && _handState.Hand.Contains(materialB); // 두 카드가 각각 손패에 있는지만 확인
            }

            int count = 0; // 동일 카드가 손패에 몇 장 있는지 셀 변수
            foreach (var card in _handState.Hand) // 손패를 순회하며
            {
                if (card == materialA) count++; // 같은 카드일 때마다 증가
            }

            return count >= 2; // 동일 카드 합성은 최소 2장이 있어야 가능
        }

        public bool SetFusionModeActive(bool active) // 21일차: 합성 버튼으로 재료 선택 모드를 켜고 끄는 진입점
        {
            if (active && !CanUseFusionInput) // 배치 턴이 아닐 때 켜려고 하면
            {
                return false; // 합성 모드 진입을 거부
            }

            if (_isFusionModeActive == active) // 이미 같은 상태면
            {
                return true; // 추가 처리 없이 성공 반환(멱등)
            }

            _isFusionModeActive = active; // 합성 모드 상태 갱신
            ClearFusionMaterialsInternal(); // 모드 전환 시 이전에 남아 있던 재료 선택은 항상 비움

            if (active) // 합성 모드로 진입하는 경우
            {
                _selectedCard = null; // 숫자키 보조 소환 선택과 동시에 활성화되지 않도록 해제
                DeselectPiece(); // 기물 이동/공격 선택도 함께 해제
                DeselectCurrentCell(); // 일반 칸 강조도 함께 해제
            }

            FusionSelectionChanged?.Invoke(); // 합성 패널 UI에 모드 전환을 알림
            return true; // 정상 전환 반환
        }

        public bool TryToggleFusionMaterial(PieceDefinition card) // 21일차: 합성 모드에서 손패 카드를 재료로 선택/해제하는 진입점(카드 좌클릭에서 호출)
        {
            if (!_isFusionModeActive || !CanUseFusionInput || _handState == null || card == null) // 합성 모드가 아니거나 배치 턴이 아니거나 데이터가 없으면
            {
                return false; // 처리하지 않고 실패 반환
            }

            if (!_handState.Hand.Contains(card)) // 실제 손패에 없는 카드면
            {
                return false; // 선택하지 않고 실패 반환
            }

            if (_fusionMaterials.Contains(card)) // 이미 재료로 선택된 카드를 다시 클릭했으면
            {
                _fusionMaterials.Remove(card); // 선택 해제(토글 동작)
            }
            else // 아직 선택되지 않은 카드면
            {
                if (_fusionMaterials.Count >= 2) // 이미 재료 2장이 다 찼으면
                {
                    return false; // 세 번째 재료는 받지 않고 실패 반환
                }

                _fusionMaterials.Add(card); // 새 재료로 추가
            }

            RecomputeFusionPreview(); // 재료 2장이 모였는지 확인해 결과 미리보기 갱신
            FusionSelectionChanged?.Invoke(); // 합성 패널 UI에 선택 변경을 알림
            return true; // 선택 처리 성공 반환
        }

        private void RecomputeFusionPreview() // 21일차: 현재 선택된 재료로 결과 레시피를 다시 계산하는 메서드(22일차에 규칙 검증과 차단 사유 계산을 추가)
        {
            _currentFusionRecipe = null; // 기본값은 미리보기 없음

            if (_fusionMaterials.Count != 2) // 재료가 아직 2장 모이지 않았으면
            {
                _currentFusionBlockReason = FusionBlockReason.NotEnoughMaterials; // 재료 부족 상태로 표시하고
                return; // 미리보기 계산을 종료
            }

            _currentFusionBlockReason = EvaluateFusion(_fusionMaterials[0], _fusionMaterials[1], out var previewRecipe); // 등급·수량 규칙까지 포함해 합성 가능 여부 판정
            _currentFusionRecipe = _currentFusionBlockReason == FusionBlockReason.None ? previewRecipe : null; // 규칙을 통과한 경우에만 결과를 미리보기로 노출(위반 레시피는 매칭 거부)
        }

        public bool TryConfirmFusionSelection() // 21일차: 합성 패널의 "합성" 버튼이 호출하는 실제 확정 진입점
        {
            if (!_isFusionModeActive || _fusionMaterials.Count != 2 || _currentFusionRecipe == null || _currentFusionBlockReason != FusionBlockReason.None) // 모드가 꺼져 있거나 재료가 덜 모였거나 규칙 위반으로 미리보기가 없으면
            {
                return false; // 합성을 실행하지 않고 실패 반환
            }

            bool fused = TryFuseCards(_fusionMaterials[0], _fusionMaterials[1]); // 기존 합성 실행 로직에 위임(카드 제거·결과 추가·HandChanged까지 처리)
            if (!fused) // 그 사이 손패 상태가 바뀌는 등의 이유로 실패했으면
            {
                ClearFusionMaterialsInternal(); // 더 이상 유효하지 않은 선택을 정리
                FusionSelectionChanged?.Invoke(); // 합성 패널 UI에도 반영
                return false; // 실패 반환
            }

            ClearFusionMaterialsInternal(); // 합성에 사용된 재료 선택을 비움(합성 모드 자체는 유지해 연속 합성 가능)
            FusionSelectionChanged?.Invoke(); // 합성 패널 UI를 빈 재료 상태로 갱신
            return true; // 합성 확정 성공 반환
        }

        private void ClearFusionMaterialsInternal() // 21일차: 합성 재료 선택과 미리보기 결과를 함께 비우는 내부 메서드
        {
            _fusionMaterials.Clear(); // 선택된 재료 목록 비움
            _currentFusionRecipe = null; // 미리보기 결과도 함께 초기화
            _currentFusionBlockReason = FusionBlockReason.NotEnoughMaterials; // 22일차: 차단 사유도 재료 미선택 상태로 되돌림
        }

        private void Update() // 매 프레임 자동 호출되는 메서드
        {
            if (!IsBound) // 실제 런 상태가 아직 연결되지 않았으면
            {
                return; // 입력으로 임시 데이터를 만들지 않고 기다림
            }

            if (_isFusionModeActive && !CanUseFusionInput) // 21일차: 배치 턴을 벗어났는데 합성 모드가 여전히 켜져 있으면
            {
                SetFusionModeActive(false); // 합성 모드를 자동으로 종료(패널 UI도 이벤트로 함께 닫힘)
            }

            if (!CanReceivePlayerInput) // 현재 적 턴 또는 전투 종료 상태라면
            {
                ClearInteractiveSelection(); // 적 턴으로 넘어갈 때 남아 있던 카드·기물·칸 선택을 모두 해제
                return; // 플레이어의 모든 보드·카드 입력 차단
            }

            HandleCardSelectionInput(); // 배치 턴뿐 아니라 일반 PlayerTurn에서도 숫자키 1~0으로 손패 카드를 선택할 수 있게 처리

            if (_turnManager == null || _turnManager.CurrentState == TurnState.DeploymentTurn) // 자유 배치 턴 또는 테스트용 턴 매니저 미연결 상태라면
            {
                DeselectPiece(); // 배치 턴에서는 기존 기물 이동 후보가 남지 않도록 해제
                DeselectCurrentCell(); // 일반 칸 선택 강조도 해제

                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverInteractiveUI()) // 배치 턴에 마우스 좌클릭이 들어오고 UI 위가 아니면
                {
                    HandleBoardClick(); // 선택된 손패 카드의 자유 배치만 처리
                }

                return; // 배치 턴에서는 일반 기물 선택·이동·공격 흐름으로 내려가지 않음
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverInteractiveUI()) // 일반 PlayerTurn에 마우스 좌클릭이 들어오고 UI 위가 아니면
            {
                HandleBoardClick(); // 선택 카드가 있으면 소환, 없으면 기물 이동·공격 처리
            }
        }

        private static bool IsPointerOverInteractiveUI() // 버그 수정: 카드 더미 버튼·패널 등 화면 UI를 클릭했을 때 같은 클릭이 3D 보드로도 전달되지 않도록 확인하는 메서드
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(); // 현재 클릭이 UI Raycast에 먼저 잡혔는지 확인
        }

        private void HandleCardSelectionInput() // 숫자키로 현재 손패 슬롯을 선택하는 메서드
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

        public bool TrySelectHandSlot(int handIndex) // 현재 손패 인덱스를 기준으로 숫자키 보조 선택을 처리하는 테스트 가능한 진입점
        {
            if (_isFusionModeActive) // 21일차: 합성 재료 선택 중에는 일반 소환용 숫자키 선택과 섞이지 않도록 차단
            {
                return false; // 선택하지 않고 실패 반환
            }

            if (_handState == null || handIndex < 0 || handIndex >= _handState.Hand.Count) // 손패가 없거나 요청 슬롯이 범위를 벗어나면
            {
                Debug.Log($"손패 {handIndex + 1}번 슬롯에는 카드가 없습니다."); // 비어 있는 슬롯임을 개발 로그로 출력
                return false; // 선택 실패 반환
            }

            var card = _handState.Hand[handIndex]; // 현재 손패 순서 그대로 선택 후보 카드 조회
            if (!CanSummonCard(card)) // 현재 턴·킹 필수·손패 규칙상 이 카드를 사용할 수 없으면
            {
                Debug.Log("현재 턴에는 해당 손패 카드를 소환할 수 없습니다."); // 사용 불가 이유를 개발 로그로 안내
                return false; // 카드 선택 거부
            }

            ToggleCardSelection(card); // 기존 숫자키 보조 선택 상태 토글
            return true; // 유효한 카드 선택 성공 반환
        }

        public bool CanSummonCard(PieceDefinition card) // 18일차 CardView가 현재 카드를 드래그할 수 있는지 확인하는 공통 규칙 메서드
        {
            if (!CanUseCardSummonInput || _handState == null || card == null) return false; // 현재 소환 가능한 턴이 아니거나 데이터가 없으면 실패
            if (!_handState.Hand.Contains(card)) return false; // 실제 플레이어 손패에 없는 카드는 사용할 수 없음
            if (_turnManager != null && _turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced && card.MovementType != PieceMovementType.King) return false; // 첫 배치에서는 킹만 허용
            if (!IsWithinDeployLimit(card)) return false; // 22일차: 4·5성 기물은 보드에 동시에 배치할 수 있는 수가 제한됨
            return true; // 모든 소환 사전 조건을 만족하면 사용 가능
        }

        public bool IsWithinDeployLimit(PieceDefinition card) // 22일차: 4·5성 기물의 동시 배치 수량 제한을 만족하는지 확인하는 메서드
        {
            if (card == null || _runState == null) return true; // 판정에 필요한 데이터가 없으면 제한을 걸지 않음
            if (!FusionRuleValidator.HasOwnedLimit(card.Grade)) return true; // 1~3성은 배치 수량 제한이 없음
            return _runState.CountDeployedCopies(card) < FusionRuleValidator.GetOwnedLimit(card.Grade); // 보유 상한과 같은 값을 보드 동시 배치 상한으로 사용
        }

        public bool TryGetBoardCellFromScreenPoint(Vector2 screenPosition, out Vector2Int cell) // UI 드래그 화면 좌표를 실제 3D 보드 셀로 변환하는 메서드
        {
            cell = default; // 실패 시 기본 좌표를 반환하도록 초기화
            if (_camera == null || _boardView == null) return false; // 카메라나 보드가 없으면 변환 불가
            if (float.IsNaN(screenPosition.x) || float.IsNaN(screenPosition.y)) return false; // 버그 수정: 유효하지 않은 좌표로 ScreenPointToRay 경고를 만들지 않음
            var ray = _camera.ScreenPointToRay(screenPosition); // 화면 좌표에서 월드 Ray 생성
            if (!Physics.Raycast(ray, out var hit)) return false; // 월드 오브젝트에 맞지 않으면 보드 밖으로 처리
            return _boardView.TryGetCellFromWorldPoint(hit.point, out cell); // 맞은 월드 위치를 보드 좌표로 변환해 반환
        }

        public bool PreviewCardDrop(PieceDefinition card, Vector2 screenPosition) // 카드를 드래그하는 동안 커서 아래 보드 칸에 실제 기물 실루엣 고스트를 표시하는 메서드
        {
            if (_selectedPiece != null) DeselectPiece(); // 카드 드래그를 시작하면 이동/공격 후보 선택을 취소해 행동 종류를 명확히 분리
            if (!CanSummonCard(card) || !TryGetBoardCellFromScreenPoint(screenPosition, out var cell)) // 카드 사용 불가 또는 보드 밖이면
            {
                ClearCardDropPreview(); // 보드 밖에서는 고스트와 강조를 모두 제거
                return false; // 유효 Drop 아님
            }

            return PreviewCardDropAtCell(card, cell); // 테스트 가능한 셀 기반 공통 프리뷰 경로 사용
        }

        public bool PreviewCardDropAtCell(PieceDefinition card, Vector2Int cell) // 특정 셀에 카드 기물 고스트를 표시하는 테스트 가능한 프리뷰 메서드
        {
            if (!CanSummonCard(card) || _boardView == null) // 현재 카드 사용 권한이나 보드 연결이 없으면
            {
                ClearCardDropPreview(); // 남아 있던 프리뷰를 정리
                return false; // 프리뷰 실패 반환
            }

            var tile = _boardView.GetTile(cell); // 현재 목표 셀의 실제 TileState 조회
            if (tile == null) // 보드 범위를 벗어난 좌표면
            {
                ClearCardDropPreview(); // 고스트와 강조 제거
                return false; // 프리뷰 불가 반환
            }

            bool isValid = tile.IsPlayerPlacementArea && !tile.IsOccupied; // 실제 소환 조건인 아군 영역 + 빈 칸 여부 계산
            _cardDropPreviewCell = cell; // 현재 목표 셀 저장
            _isCardDropPreviewValid = isValid; // 유효/무효 상태 저장

            if (isValid) _boardView.HighlightCell(cell); // 실제 소환 가능한 칸은 기존 보드 강조도 함께 표시
            else _boardView.ClearHighlight(); // 적 영역·점유 칸은 파란 강조 대신 붉은 고스트만 보여 혼동 방지

            ShowCardDropGhost(card, cell, isValid); // 실제 기물 모델 실루엣을 목표 셀에 표시
            return isValid; // 실제 Drop 가능 여부 반환
        }

        public void ClearCardDropPreview() // 카드 드래그이 끝나거나 보드 밖으로 나갔을 때 모든 프리뷰를 정리하는 메서드
        {
            _boardView?.ClearHighlight(); // 기존 선택 셀 하이라이트 제거
            _selectedCell = null; // 일반 셀 선택 상태도 함께 초기화
            _cardDropPreviewCell = null; // 고스트 목표 셀 초기화
            _isCardDropPreviewValid = false; // 유효 상태 초기화
            DestroyCardDropGhost(); // 실제 기물 고스트 오브젝트와 머티리얼 제거
        }

        private void ShowCardDropGhost(PieceDefinition card, Vector2Int cell, bool isValid) // 현재 카드 종류와 목표 셀을 실제 3D 기물 윤곽으로 보여주는 메서드
        {
            if (_cardDropGhost == null || _cardDropGhostDefinition != card) // 카드 종류가 바뀌었거나 아직 고스트가 없으면
            {
                DestroyCardDropGhost(); // 이전 카드 고스트 정리
                _cardDropGhost = new GameObject("CardDropGhost"); // 새 고스트 루트 생성
                _cardDropGhost.transform.SetParent(_boardView.transform, false); // 실제 기물과 같은 보드 좌표계를 사용하도록 BoardView 자식으로 연결
                _cardDropGhostView = _cardDropGhost.AddComponent<PieceView>(); // 기존 기물별 프리미티브 모델링을 그대로 재사용
                var previewState = new PieceRuntimeState(card, cell, true); // 실제 보드에는 등록하지 않는 미리보기 전용 런타임 상태 생성
                _cardDropGhostView.Initialize(previewState, _boardView.TileSize); // 킹·폰·나이트 등 카드 종류에 맞는 3D 모델 생성
                _cardDropGhostDefinition = card; // 현재 고스트 카드 정의 저장

                foreach (var collider in _cardDropGhost.GetComponentsInChildren<Collider>(true)) collider.enabled = false; // 고스트가 Raycast를 가로채지 않도록 모든 콜라이더 비활성화
                ReplaceGhostMaterials(); // 실제 기물 머티리얼을 투명 프리뷰 머티리얼로 교체
            }
            else // 같은 카드 고스트를 계속 이동시키는 경우
            {
                _cardDropGhostView.SnapTo(cell, _boardView.TileSize); // 연출 없이 즉시 마우스를 따라가도록 고스트만 이동
            }

            if (_cardDropGhost != null) _cardDropGhost.name = $"CardDropGhost_{card.DisplayName}_{cell.x}_{cell.y}"; // Hierarchy에서 목표 카드와 셀을 쉽게 확인할 수 있게 이름 지정
            UpdateGhostColor(isValid); // 유효 위치는 청록, 무효 위치는 붉은색으로 즉시 구분
        }

        private void ReplaceGhostMaterials() // PieceView가 만든 실제 기물 머티리얼을 하나의 투명 고스트 머티리얼로 교체하는 메서드
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Sprites/Default"); // URP 우선으로 투명 표현 가능한 셰이더 탐색
            if (shader == null) return; // 사용할 셰이더를 찾지 못하면 기존 기물 머티리얼 상태로라도 모델을 유지
            _cardDropGhostMaterial = new Material(shader); // 프리뷰 전용 머티리얼 생성
            ConfigureGhostMaterialTransparency(_cardDropGhostMaterial); // URP 투명 Surface 설정 적용

            var renderers = _cardDropGhost.GetComponentsInChildren<Renderer>(true); // 고스트의 모든 모델 파츠 렌더러 수집
            foreach (var renderer in renderers) // 모든 파츠를 순회하며
            {
                renderer.sharedMaterial = _cardDropGhostMaterial; // 하나의 고스트 머티리얼을 공유해 일관된 실루엣 색 적용
                renderer.shadowCastingMode = ShadowCastingMode.Off; // 고스트가 실제 기물처럼 그림자를 만들지 않도록 차단
                renderer.receiveShadows = false; // 고스트가 어둡게 변하지 않도록 그림자 수신 차단
            }
        }

        private static void ConfigureGhostMaterialTransparency(Material material) // URP 머티리얼을 반투명 고스트용으로 설정하는 메서드
        {
            if (material == null) return; // 머티리얼이 없으면 종료
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f); // URP Surface Type을 Transparent로 변경
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f); // Alpha 블렌딩 사용
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); // 소스 알파 기반 블렌드 설정
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha); // 배경과 알파 반전 혼합
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f); // 투명 고스트가 깊이 버퍼를 가리지 않도록 설정
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); // URP 투명 Surface 키워드 활성화
            material.renderQueue = (int)RenderQueue.Transparent; // 실제 기물 뒤/앞 관계에서 투명 오브젝트 큐 사용
        }

        private void UpdateGhostColor(bool isValid) // 프리뷰 위치의 유효성에 따라 고스트 실루엣 색을 변경하는 메서드
        {
            if (_cardDropGhostMaterial == null) return; // 교체 머티리얼이 없으면 종료
            Color ghostColor = isValid ? new Color(0.15f, 0.92f, 0.88f, 0.42f) : new Color(0.95f, 0.18f, 0.16f, 0.42f); // 유효 청록 / 무효 붉은색 반투명 색상 선택
            if (_cardDropGhostMaterial.HasProperty("_BaseColor")) _cardDropGhostMaterial.SetColor("_BaseColor", ghostColor); // URP Lit/Unlit 기본 색상 적용
            _cardDropGhostMaterial.color = ghostColor; // 일반 color 프로퍼티에도 동일 색 적용
        }

        private void DestroyCardDropGhost() // 카드 드래그용 고스트 오브젝트와 생성 머티리얼을 안전하게 정리하는 메서드
        {
            if (_cardDropGhost != null) // 고스트 오브젝트가 존재하면
            {
                if (Application.isPlaying) Destroy(_cardDropGhost); // Play Mode에서는 프레임 종료 시 안전하게 파괴
                else DestroyImmediate(_cardDropGhost); // EditMode 테스트에서는 즉시 파괴
            }

            if (_cardDropGhostMaterial != null) // 프리뷰 전용 머티리얼이 존재하면
            {
                if (Application.isPlaying) Destroy(_cardDropGhostMaterial); // Play Mode에서는 안전하게 예약 파괴
                else DestroyImmediate(_cardDropGhostMaterial); // EditMode에서는 즉시 파괴
            }

            _cardDropGhost = null; // 고스트 루트 참조 초기화
            _cardDropGhostView = null; // PieceView 참조 초기화
            _cardDropGhostMaterial = null; // 머티리얼 참조 초기화
            _cardDropGhostDefinition = null; // 카드 정의 참조 초기화
        }

        private void ToggleCardSelection(PieceDefinition card) // 배치 턴의 손패 카드 선택 상태를 켜고 끄는 메서드
        {
            if (!CanUseCardSummonInput || _handState == null || card == null) // 카드 소환 권한·손패 연결·카드 데이터 중 하나라도 없으면
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
                ? $"배치 카드 선택: {_selectedCard.DisplayName} (아군 10×5 영역의 빈 칸을 클릭하세요)" // 선택됐을 때 안내 로그
                : "배치 카드 선택 해제"); // 해제됐을 때 안내 로그
        }

        private void HandleBoardClick() // 현재 턴 종류에 따라 카드 배치 또는 일반 기물 행동을 처리하는 메서드
        {
            if (!CanReceivePlayerInput || _camera == null || _boardView == null) // 입력 권한·카메라·보드 뷰 중 하나라도 없으면
            {
                return; // 클릭 처리를 하지 않고 종료
            }

            var screenPosition = Mouse.current.position.ReadValue(); // 현재 마우스 화면 좌표 읽기
            if (float.IsNaN(screenPosition.x) || float.IsNaN(screenPosition.y)) // 버그 수정: 입력 장치가 이번 프레임에 유효하지 않은 좌표를 보고하면
            {
                return; // NaN 좌표로 ScreenPointToRay를 호출해 경고를 만들지 않고 이번 클릭을 무시
            }

            var ray = _camera.ScreenPointToRay(screenPosition); // 화면 좌표를 카메라 기준 광선으로 변환

            if (!Physics.Raycast(ray, out var hit) || !_boardView.TryGetCellFromWorldPoint(hit.point, out var cell)) // 광선이 보드에 맞지 않거나 유효 칸으로 변환되지 않으면
            {
                if (CanUseCombatInput) // 일반 전투 턴에서 보드 밖을 클릭했다면
                {
                    DeselectPiece(); // 기물 선택 해제
                    DeselectCurrentCell(); // 칸 선택 해제
                }

                return; // 보드 클릭 처리를 종료
            }

            if (_selectedCard != null && CanUseCardSummonInput) // 배치 턴 또는 일반 PlayerTurn에서 손패 카드가 선택돼 있으면
            {
                TryDeploySelectedCardTo(cell); // 아군 영역의 빈 칸에 카드 소환 시도
                return; // 카드 소환 입력은 기물 이동·공격과 동시에 처리하지 않음
            }

            if (CanUseDeploymentInput) // 자유 배치 턴인데 선택 카드가 없다면
            {
                return; // 기물 이동·공격 입력을 완전히 차단하고 카드 선택을 기다림
            }

            if (!CanUseCombatInput) // 일반 전투 행동 권한이 없다면
            {
                return; // 보드 클릭을 더 이상 처리하지 않음
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
        }

        public bool TrySelectPieceAt(Vector2Int cell) // 지정한 칸에 내 기물이 있으면 선택하고 이동/공격 후보를 계산하는 진입점
        {
            if (!CanUseCombatInput) // 배치 턴·적 턴·종료 상태거나 아직 연결되지 않았으면
            {
                return false; // 기물 선택을 금지하고 실패 반환
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

            _selectedCard = null; // 일반 턴에 기물을 선택하면 손패 소환 선택을 취소해 행동 종류를 명확히 분리
            _selectedPiece = tileState.OccupyingPiece; // 새로 선택한 기물 저장
            _pendingMovement = MovementResolver.GetReachableTiles(_selectedPiece, _boardView.State); // 31일차: 런타임 상태 기반 오버로드로 교체 — 기절·속박 게이팅과 카멜레온 순환 단계를 실제 하이라이트에 반영
            _boardView.HighlightMoveCandidates(_pendingMovement.MoveTiles, _pendingMovement.AttackTiles); // 계산한 후보 칸을 화면에 강조 표시
            SelectionChanged?.Invoke(_selectedPiece); // 31일차: 정보 패널 UI에 새로 선택된 기물을 통지

            Debug.Log($"{_selectedPiece.Definition.DisplayName} 선택: 이동 {_pendingMovement.MoveTiles.Count}칸 / 공격 {_pendingMovement.AttackTiles.Count}칸"); // 선택 결과 출력
            return true; // 정상적으로 선택했음을 반환
        }

        public bool TryMoveSelectedPieceTo(Vector2Int destination) // 현재 선택된 기물을 지정한 후보 칸으로 이동시키는 진입점
        {
            if (!CanUseCombatInput || _selectedPiece == null || _pendingMovement == null || !_pendingMovement.MoveTiles.Contains(destination)) // 일반 행동 권한이 없거나 유효한 이동 후보가 아니면
            {
                return false; // 이동 실패 반환
            }

            ExecuteMove(destination); // 실제 이동 실행
            return true; // 정상적으로 이동했음을 반환
        }

        private void ExecuteMove(Vector2Int destination) // 선택된 기물을 실제로 이동시키고 일반 턴 행동을 소비하는 메서드
        {
            var piece = _selectedPiece; // 선택을 지우기 전에 이동 기물 참조 저장
            var origin = piece.BoardPosition; // 로그용 원래 좌표 저장

            MovePieceTo(piece, destination); // 보드 점유·좌표·화면 위치를 함께 갱신
            Debug.Log($"{piece.Definition.DisplayName} 이동: {origin} -> {destination}"); // 이동 결과 출력

            DeselectPiece(); // 이동 완료 후 선택과 후보 강조 해제
            _turnManager?.TryCompletePlayerAction(); // 이동을 이번 플레이어 일반 턴 행동으로 처리
        }

        public bool TryAttackSelectedPieceTarget(Vector2Int target) // 현재 선택된 기물로 지정한 공격 후보 칸을 공격하는 진입점
        {
            if (!CanUseCombatInput || _selectedPiece == null || _pendingMovement == null || !_pendingMovement.AttackTiles.Contains(target)) // 일반 행동 권한이 없거나 유효한 공격 후보가 아니면
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

            _battleHooks?.RaiseBeforeAttack(attacker, defender); // 29일차: 판정 직전 통지

            var result = CombatResolver.ResolveAttack(attacker, defender, _battleHooks); // 고정 ATK 규칙으로 전투 판정 실행(훅을 전달해 BeforeDamage/AfterDamage 발행)

            Debug.Log($"{attacker.Definition.DisplayName} 공격 -> {defender.Definition.DisplayName}: {result.DamageDealt} 피해, 남은 HP {defender.CurrentHp}"); // 판정 결과 출력

            if (result.DefenderDied) // 대상이 사망했으면
            {
                RemovePieceFromBoard(defender); // 사망한 대상을 보드와 화면에서 제거

                if (CombatMovementPolicy.ShouldOccupyDefenderTileAfterKill(attacker.Definition)) // 근접 계열이면 처치한 칸을 점유
                {
                    MovePieceTo(attacker, target); // 공격자를 대상 칸으로 이동
                    Debug.Log($"{attacker.Definition.DisplayName}이(가) {target} 칸을 점유했습니다."); // 근접 처치 결과 출력
                }
                else // Cannon 같은 원거리 기물이면
                {
                    Debug.Log($"{attacker.Definition.DisplayName} 원거리 처치 — 원위치를 유지합니다."); // 원거리 공격자는 이동하지 않음
                }
            }

            else // 대상이 생존했으면
            {
                Debug.Log($"{defender.Definition.DisplayName} 생존 — {attacker.Definition.DisplayName}은(는) 원위치를 유지합니다."); // 비치명 결과 출력
            }

            _battleHooks?.RaiseAfterAttack(result); // 29일차: 사망 처리·전진까지 모두 끝난 뒤 통지

            AttackResolved?.Invoke(result); // 외부 시스템에 전투 결과 통지

            DeselectPiece(); // 선택과 후보 강조 해제
            _turnManager?.TryCompletePlayerAction(); // 공격도 플레이어 일반 턴 행동으로 처리
        }

        private void MovePieceTo(PieceRuntimeState piece, Vector2Int destination) // 기물의 보드 좌표와 화면 위치를 함께 갱신하는 공통 메서드
        {
            var origin = piece.BoardPosition; // 29일차: 훅 전달용으로 원래 좌표를 먼저 저장
            _battleHooks?.RaiseBeforeMove(piece, origin, destination); // 29일차: 실제 이동 직전 통지

            var originTile = _boardView.GetTile(origin); // 기물이 원래 있던 칸 조회
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

            _battleHooks?.RaiseAfterMove(piece, origin, destination); // 29일차: 보드·화면 반영이 모두 끝난 뒤 통지
        }

        public void ApplyTurnEndStatusEffects() // 28일차: 1턴(플레이어+적 행동)이 끝날 때 보드 위 모든 기물의 독·화상 피해와 상태 지속 턴을 정산하는 메서드
        {
            if (_boardView == null || !_boardView.IsBound) // 보드가 아직 연결되지 않았으면
            {
                return; // 정산할 대상이 없으므로 종료
            }

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var tile = _boardView.GetTile(new Vector2Int(x, y)); // 현재 칸 조회
                    var piece = tile?.OccupyingPiece; // 점유 기물 조회
                    if (piece == null) continue; // 빈 칸은 건너뜀

                    int damage = StatusEffectTickResolver.ResolveTurnEndDamage(piece, _battleHooks); // 독·화상 등 이번 턴 틱 피해 정산(29일차: BeforeDamage/AfterDamage 훅과 함께)
                    if (damage > 0) // 실제 피해가 있었다면
                    {
                        Debug.Log($"{piece.Definition.DisplayName} 상태 이상 피해 {damage}, 남은 HP {piece.CurrentHp}"); // 결과 출력
                    }

                    piece.TickStatusEffects(); // 지속 턴 감소 및 만료된 상태 제거(기절·속박 해제 포함)

                    if (piece.IsDead) // 틱 피해로 사망했다면
                    {
                        RemovePieceFromBoard(piece); // 기존 사망 처리(보드 해제·화면 제거·죽은 카드 더미 이동) 재사용
                    }
                }
            }
        }

        private void RemovePieceFromBoard(PieceRuntimeState piece) // 사망한 기물을 보드 점유와 화면에서 제거하는 메서드
        {
            if (_selectedPiece == piece) // 31일차: 지금 선택 중인 기물이 죽었으면
            {
                DeselectPiece(); // 정보 패널이 죽은 기물을 계속 보여주지 않도록 선택도 함께 해제
            }

            var tile = _boardView.GetTile(piece.BoardPosition); // 이 기물이 있던 칸 조회
            if (tile != null && tile.OccupyingPiece == piece) // 아직 그 칸을 이 기물이 점유하면
            {
                tile.OccupyingPiece = null; // 점유 상태 해제
            }

            if (_pieceViews.TryGetValue(piece, out var pieceView) && pieceView != null) // 연결된 화면 표시가 있으면
            {
                pieceView.PlayDeathTogglingThenDestroy(() => Destroy(pieceView.gameObject)); // 30일차: 즉시 제거 대신 무작위 방향으로 쓰러진 뒤 제거(전투 사망·상태 이상 사망 모두 공통 적용)
            }

            _pieceViews.Remove(piece); // 화면 연결 정보는 지금 정리(실제 오브젝트 파괴는 연출 완료 후 콜백에서 수행)

            if (piece.IsPlayerPiece && _runState != null) // 19일차: 아군 기물이 죽었으면
            {
                _runState.Deck.MoveToDeadPile(piece.Definition); // 해당 카드를 죽은 카드 더미로 이동해 같은 전투에서 재사용 차단
                DeckChanged?.Invoke(); // 죽은 카드 더미 구성 변경을 덱/무덤 패널 UI에 알림
            }
            else if (!piece.IsPlayerPiece) // 20일차: 적 기물이 죽었으면
            {
                _enemyDeck.MoveToDeadPile(piece.Definition); // 플레이어와 동일하게 적 죽은 카드 더미로 이동
            }
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

            SelectionChanged?.Invoke(null); // 31일차: 정보 패널 UI에 선택 해제를 통지
        }

        public bool TryDeploySelectedCardTo(Vector2Int cell) // 기존 숫자키 선택→보드 클릭 소환 방식을 유지하는 호환 진입점
        {
            if (_selectedCard == null) return false; // 숫자키로 선택된 카드가 없으면 소환 실패
            return TrySummonCardFromUI(_selectedCard, cell); // 18일차 드래그 Drop과 같은 공통 카드 소환 경로 사용
        }

        public bool TrySummonCardFromUI(PieceDefinition card, Vector2Int cell) // CardView 드래그 Drop이 직접 호출하는 실제 플레이어 카드 소환 메서드
        {
            if (!CanSummonCard(card) || _handState == null) return false; // 현재 턴·손패·킹 필수 규칙을 만족하지 못하면 실패

            var tileState = _boardView.GetTile(cell); // Drop된 보드 좌표의 실제 TileState 조회
            if (tileState == null || !tileState.IsPlayerPlacementArea || tileState.IsOccupied) // 아군 10×5 영역의 빈 칸이 아니면
            {
                Debug.Log($"{cell}에는 카드를 소환할 수 없습니다 (아군 영역의 빈 칸만 가능)."); // Drop 실패 이유 출력
                return false; // 카드 소비 없이 실패 반환
            }

            var runtimeState = SpawnPiece(card, tileState, isPlayerPiece: true, objectName: "Piece"); // 카드 데이터로 실제 아군 기물 생성
            _handState.RemoveCard(card); // 사용한 실제 카드 1장을 손패에서 제거
            _selectedCard = null; // 숫자키 보조 선택 상태도 함께 해제
            DeselectPiece(); // 일반 기물 선택과 카드 소환 행동이 겹치지 않도록 선택 해제
            DeselectCurrentCell(); // 남은 일반 셀 강조 정리

            Debug.Log($"{runtimeState.Definition.DisplayName} 카드 소환: {tileState.BoardPosition} / Hand={_handState.Hand.Count}장"); // 소환 결과 출력

            if (_turnManager != null) // 실제 턴 매니저가 연결돼 있으면
            {
                if (_turnManager.CurrentState == TurnState.DeploymentTurn) // 시작/주기 자유 배치 턴이면
                {
                    if (_turnManager.IsInitialDeployment && runtimeState.Definition.MovementType == PieceMovementType.King) _turnManager.MarkInitialKingPlaced(); // 시작 킹 필수 조건 충족
                    _turnManager.RegisterDeployment(); // 자유 배치 수만 누적하고 배치 턴은 유지
                }
                else if (_turnManager.CurrentState == TurnState.PlayerTurn) // 일반 플레이어 턴이면
                {
                    _turnManager.TryCompletePlayerAction(); // 카드 소환 자체를 이번 턴 행동 1회로 처리해 즉시 EnemyTurn으로 전환
                }
            }

            HandChanged?.Invoke(); // 실제 손패에서 카드가 빠졌음을 이미지 손패 UI에 알림
            return true; // 카드 소환 성공 반환
        }

        public bool TryEnemySummonOneCard() // EnemyTurn에 적 손패 카드 1장을 적 진영에 자동 소환하고 즉시 턴을 끝내는 메서드
        {
            if (!IsBound || _turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) // 실제 전투가 연결되지 않았거나 적 턴이 아니면
            {
                return false; // 적 소환을 실행하지 않음
            }

            if (_enemyHandState.Hand.Count == 0) // 적이 사용할 카드가 손패에 없으면
            {
                _enemyDeck.TryDrawToHand(_enemyHandState); // 20일차: 플레이어처럼 드로우 더미에서 카드 1장을 다시 채워봄
            }

            if (_enemyHandState.Hand.Count == 0) // 드로우 더미까지 비어 더 이상 뽑을 카드가 없으면
            {
                return false; // 이번 적 턴에는 소환 행동을 할 수 없음
            }

            var targetTile = FindRandomFreeEnemyPlacementTile(); // 20일차: 적 진영 10×5의 빈 칸 중 하나를 무작위로 선택
            if (targetTile == null) // 적 진영에 빈 칸이 하나도 없으면
            {
                return false; // 소환할 공간이 없으므로 실패 반환
            }

            var card = _enemyHandState.Hand[0]; // 프로토타입 AI는 현재 손패의 첫 카드를 사용
            var runtimeState = SpawnPiece(card, targetTile, isPlayerPiece: false, objectName: "Piece(EnemySummoned)"); // 적 기물로 실제 소환
            _enemyHandState.RemoveCard(card); // 사용한 적 카드 1장을 실제 손패에서 소비

            Debug.Log($"적 카드 소환: {runtimeState.Definition.DisplayName} @ {targetTile.BoardPosition} / EnemyHand={_enemyHandState.Hand.Count}, EnemyDraw={_enemyDeck.DrawPile.Count}"); // 적 소환 결과 출력
            _turnManager.CompleteEnemyTurn(); // 소환 1회를 적 턴의 행동으로 간주하고 즉시 다음 상태로 진행
            return true; // 적 카드 소환 성공 반환
        }

        private TileState FindRandomFreeEnemyPlacementTile() // 20일차: 적 진영에서 자동 소환에 사용할 빈 칸 하나를 무작위로 고르는 메서드
        {
            _freeEnemyTileBuffer.Clear(); // 이전 호출에서 남은 목록 초기화(매 호출마다 새 GC 할당을 피하기 위해 필드 재사용)

            for (int y = BoardState.Height - 1; y >= BoardState.Height / 2; y--) // 적 진영 전체 행을 순회하며
            {
                for (int x = 0; x < BoardState.Width; x++) // 각 행의 모든 칸을 순회하며
                {
                    var tile = _boardView.GetTile(new Vector2Int(x, y)); // 현재 적 진영 칸 조회
                    if (tile != null && tile.IsEnemyPlacementArea && !tile.IsOccupied) // 적 배치 영역의 빈 칸이면
                    {
                        _freeEnemyTileBuffer.Add(tile); // 무작위 선택 후보 목록에 추가
                    }
                }
            }

            if (_freeEnemyTileBuffer.Count == 0) // 모든 적 진영 칸이 점유돼 있으면
            {
                return null; // 소환 불가 반환
            }

            return _freeEnemyTileBuffer[UnityEngine.Random.Range(0, _freeEnemyTileBuffer.Count)]; // 빈 칸 후보 중 하나를 무작위로 선택해 반환
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

        private void SelectCell(Vector2Int cell) // 일반 전투 턴에서 칸을 선택 상태로 만드는 메서드
        {
            if (!CanUseCombatInput) // 현재 일반 전투 입력이 잠긴 상태라면
            {
                return; // 배치 턴에서는 일반 선택 강조를 만들지 않음
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

        private void ClearInteractiveSelection() // 턴이 잠겼을 때 카드·기물·칸·합성 선택을 한 번에 정리하는 메서드
        {
            _selectedCard = null; // 배치 카드 선택 해제
            DeselectPiece(); // 기물 선택과 이동/공격 후보 해제
            DeselectCurrentCell(); // 일반 칸 선택 강조 해제
            if (_isFusionModeActive) SetFusionModeActive(false); // 21일차: 입력이 잠기면 합성 모드도 함께 종료
        }

        private void OnGUI() // 18일차 이후 실제 카드 손패는 Canvas로 표시하고 좌상단에는 최소 디버그 정보만 남기는 메서드
        {
            if (!IsBound) // 실제 RunState가 아직 연결되지 않았으면
            {
                GUI.Label(new Rect(10, 10, 600, 20), "BattleController가 RunState를 연결하는 중입니다."); // 상태 연결 대기 안내
                return; // 추가 디버그 UI는 그리지 않음
            }

            GUI.Label(new Rect(10, 10, 960, 20), $"Debug | PlayerHand {_handState.Hand.Count}/{HandState.MaxHandSize} | Draw {_runState.Deck.DrawPile.Count} | Dead {_runState.Deck.DeadCardPile.Count} | EnemyHand {_enemyHandState.Hand.Count} | EnemyDraw {_enemyDeck.DrawPile.Count} | EnemyDead {_enemyDeck.DeadCardPile.Count}"); // 카드 상태 한 줄 요약(20일차: 적 덱 정보 추가)
            GUI.Label(new Rect(10, 30, 920, 20), BuildTurnInputLabel()); // 현재 턴 조작 안내 표시
            GUI.Label(new Rect(10, 50, 920, 20), BuildSelectedPieceLabel()); // 선택 기물 디버그 상태 표시
        }

        private string BuildTurnInputLabel() // 현재 턴 종류에 맞는 개발용 조작 안내 문구를 만드는 메서드
        {
            if (_turnManager == null) // 테스트용으로 턴 매니저가 연결되지 않았다면
            {
                return "입력: 테스트 모드 / 숫자키 1~0 = 손패 슬롯 선택"; // 테스트용 카드 입력 안내 반환
            }

            if (_turnManager.CurrentState == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment && !_turnManager.IsInitialKingPlaced) // 시작 배치에서 아직 킹이 없다면
            {
                return "시작 배치 턴: 먼저 킹을 반드시 배치하세요. 이후 원하는 카드를 자유롭게 추가 배치하고 Space로 턴 종료"; // 킹 필수 + 자유 배치 안내
            }

            if (_turnManager.CurrentState == TurnState.DeploymentTurn && _turnManager.IsInitialDeployment) // 킹을 이미 놓은 시작 배치 턴이면
            {
                return $"시작 배치 턴: 킹 배치 완료 / 자유 배치 중 {_turnManager.DeployedCardCount}장 / 카드 우클릭 = 손패 정리 / Space = 배치 턴 종료"; // 추가 자유 배치와 종료 안내
            }

            if (_turnManager.CurrentState == TurnState.DeploymentTurn) // 5턴마다 열리는 일반 배치 턴이면
            {
                return $"배치 턴: 원하는 만큼 자유 배치 가능 / 현재 {_turnManager.DeployedCardCount}장 배치 / 카드 우클릭 = 손패 정리 / Space = 배치 턴 종료"; // 주기 배치 전용 조작 안내
            }

            if (_turnManager.CurrentState == TurnState.PlayerTurn) // 일반 플레이어 턴이면
            {
                return "플레이어 턴: 기물 이동/공격 또는 숫자키 1~0 카드 선택→아군 빈 칸 소환 / 소환 성공 즉시 적 턴"; // 일반 전투와 카드 소환 중 하나를 선택하는 조작 안내
            }

            return $"입력 잠김: {_turnManager.CurrentState}"; // 적 턴 또는 전투 종료 상태 안내 반환
        }

        private string BuildSelectedPieceLabel() // 선택된 기물 상태를 안내 문구로 만드는 메서드
        {
            if (_selectedPiece == null || _pendingMovement == null) // 선택된 기물이 없으면
            {
                return "선택된 기물 없음 / 플레이어 턴에 내 기물을 클릭해 이동 후보를 확인하세요."; // 안내 문구 반환
            }

            return $"선택: {_selectedPiece.Definition.DisplayName} @ {_selectedPiece.BoardPosition} / 이동 {_pendingMovement.MoveTiles.Count}칸, 공격 {_pendingMovement.AttackTiles.Count}칸"; // 선택 상태 안내 문구 반환
        }

        private void OnDestroy() // 입력 컨트롤러가 파괴될 때 이벤트와 드래그 프리뷰를 정리하는 메서드
        {
            ClearCardDropPreview(); // 남아 있을 수 있는 기물 고스트와 머티리얼을 먼저 제거
            if (_turnManager != null) // 연결된 턴 매니저가 남아 있으면
            {
                _turnManager.TurnChanged -= HandleTurnChangedForCardDraw; // 자동 드로우 이벤트 구독 해제
            }

            if (_battleHooks != null) // 29일차: 연결된 훅 버스가 남아 있으면
            {
                _battleHooks.TurnEnd -= HandleBattleHooksTurnEnd; // 턴 종료 정산 구독 해제
                _battleHooks.AfterAttack -= HandleBattleHooksAfterAttackVisual; // 30일차: 공격 연출 구독 해제
            }
        }
    }
}
