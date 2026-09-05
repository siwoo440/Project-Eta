using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // Dictionary<T,T>, HashSet<T>, IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Debug, Resources, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController, BattleHooks, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController, BoardState, BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // PlayerStartingDeckCatalog를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceRuntimeState, PieceCategory를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossPhase2Controller : MonoBehaviour // 38일차 보스 행동 위에 Phase 2 전환·예고·다음 턴 범위 공격·1회 증원을 연결하는 39일차 관리자
    {
        private const string PieceCatalogResourceName = "PlayerStartingDeck26"; // Phase 2 증원 PieceDefinition을 찾기 위한 기존 26종 카탈로그 이름
        private const int KingLaneLength = 3; // King 직선 패턴의 기본 공격 길이

        private static readonly Vector2Int[] ReinforcementCandidateCells = // 현재 10x10 적 진영에서 빈 칸을 순서대로 찾을 증원 후보 좌표
        {
            new Vector2Int(9, 9), // 우상단 첫 후보
            new Vector2Int(8, 9), // 우상단 인접 후보
            new Vector2Int(9, 8), // 우측 두 번째 줄 후보
            new Vector2Int(8, 8), // 우측 내부 후보
            new Vector2Int(7, 9), // 우상단 여유 후보
            new Vector2Int(2, 9), // 좌상단 여유 후보
            new Vector2Int(2, 8), // 좌측 두 번째 줄 후보
            new Vector2Int(7, 8), // 우측 두 번째 줄 여유 후보
            new Vector2Int(1, 9), // 최종 좌측 후보
            new Vector2Int(6, 9) // 최종 우측 후보
        };

        private BattleController _battleController; // 현재 전투 상태 소유자
        private BoardInputController _boardInput; // 기존 적 스폰 진입점을 재사용하기 위한 보드 입력 컨트롤러
        private BoardView _boardView; // 위험 칸 표시와 보스 전투 화면 위치에 사용할 보드 뷰
        private RunState _runState; // 현재 보드와 덱 상태를 가진 런 상태
        private TurnManager _turnManager; // EnemyTurn 소비와 페이즈 공격 실행에 사용할 턴 매니저
        private BattleHooks _battleHooks; // 보스 피해를 감지하고 기존 전투 이벤트를 유지할 훅 버스
        private PlayerStartingDeckCatalog _pieceCatalog; // Knight/Pawn 증원 정의를 찾을 기존 카탈로그
        private BossTelegraphOverlay _overlay; // 보드 위 실제 위험 칸 표시기
        private BossPhaseStatusUI _statusUI; // 상단 Phase 2·예고 상태 표시 UI
        private readonly Dictionary<PieceRuntimeState, BossPhaseRuntimeState> _states = new Dictionary<PieceRuntimeState, BossPhaseRuntimeState>(); // 보스 런타임별 페이즈 상태 저장
        private bool _isBound; // 실제 Battle 객체와 연결됐는지 여부

        public void Bind(BattleController battleController, BoardInputController boardInput, BoardView boardView) // EnemyAITurnDriver에서 현재 전투 객체를 직접 연결하는 메서드
        {
            if (_isBound && _battleController == battleController && _boardInput == boardInput && _boardView == boardView) return; // 같은 전투에 이미 연결됐으면 중복 구독 금지
            UnbindHooks(); // 다른 전투에 재사용될 경우 기존 훅 정리

            _battleController = battleController; // 전투 컨트롤러 저장
            _boardInput = boardInput; // 적 스폰 컨트롤러 저장
            _boardView = boardView; // 실제 보드 뷰 저장
            _runState = battleController != null ? battleController.RunState : null; // 현재 런 상태 저장
            _turnManager = battleController != null ? battleController.TurnManager : null; // 현재 턴 매니저 저장
            _battleHooks = battleController != null ? battleController.BattleHooks : null; // 현재 전투 훅 저장
            _pieceCatalog = Resources.Load<PlayerStartingDeckCatalog>(PieceCatalogResourceName); // 기존 26종 카탈로그 로드

            _overlay = GetComponent<BossTelegraphOverlay>(); // 같은 오브젝트의 위험 칸 표시기 조회
            if (_overlay == null) _overlay = gameObject.AddComponent<BossTelegraphOverlay>(); // 없으면 자동 추가
            _statusUI = GetComponent<BossPhaseStatusUI>(); // 같은 오브젝트의 보스 상태 UI 조회
            if (_statusUI == null) _statusUI = gameObject.AddComponent<BossPhaseStatusUI>(); // 없으면 자동 추가

            if (_battleHooks != null) _battleHooks.AfterDamage += HandleAfterDamage; // 플레이어 공격으로 보스 HP가 50%를 넘는 순간을 즉시 감지
            _isBound = _runState != null && _turnManager != null; // 핵심 전투 상태가 있으면 연결 완료

            ScanBossesAndApplyTransitions(); // 이미 절반 HP 이하인 상태로 로드된 보스도 즉시 상태 복원
        }

        public bool TryHandleEnemyTurn() // Phase 2 보스가 존재하면 일반 AI보다 먼저 예고 또는 예고 공격을 한 턴 행동으로 처리하는 메서드
        {
            if (!_isBound || _turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn || _runState == null) return false; // 실제 EnemyTurn이 아니면 일반 AI로 넘김

            ScanBossesAndApplyTransitions(); // 현재 보드의 새 보스·HP 변화를 먼저 동기화
            PieceRuntimeState boss = FindActivePhase2Boss(); // 살아 있는 Phase 2 보스 한 기 선택
            if (boss == null) return false; // Phase 2 보스가 없으면 38일차 일반 보스 AI를 그대로 사용

            BossPhaseRuntimeState state = GetOrCreateState(boss); // 선택 보스의 페이즈 런타임 상태 확보

            if (state.PendingTelegraph != null) // 이전 EnemyTurn에서 이미 위험 칸을 예고했다면
            {
                BossTelegraphState pending = state.PendingTelegraph; // 실행할 예고 스냅샷 확보
                state.ClearPendingTelegraph(); // 실행 중 중복 소비되지 않도록 먼저 상태 제거
                _overlay?.Clear(); // 플레이어가 보던 위험 칸 표시 제거
                _statusUI?.SetState(BossPhase.Phase2, string.Empty, false); // 공격 실행 후 다음 패턴 준비 상태 표시

                bool executed = BossActionExecutor.TryExecuteTelegraphedAreaAttack( // 예고 순간 저장한 동일 TargetCells로 실제 범위 피해 실행
                    boss, // 공격 주체 보스
                    pending.TargetCells, // UI가 보여 준 바로 그 위험 칸
                    _runState, // 현재 보드·덱 상태
                    _turnManager, // EnemyTurn 종료에 사용할 매니저
                    _battleHooks, // 기존 피해·로그·상태 훅
                    _boardView, // 사망 기물 화면 처리에 사용할 뷰
                    out int hitCount); // 실제 적중 플레이어 수

                Debug.Log($"39일차 보스 예고 공격 실행: {pending.DisplayName} / 위험칸={pending.TargetCells.Count} / 적중={hitCount}"); // 실제 실행 결과 로그

                if (!executed && _turnManager.CurrentState == TurnState.EnemyTurn) CompleteEnemyTurnWithoutAction(); // 실행 실패 시 교착 방지
                return true; // Phase 2 보스가 이번 EnemyTurn을 소비했음을 알림
            }

            BossPatternType patternType = state.ConsumeNextPatternType(); // 주변 강타와 King 직선 패턴을 교대로 선택
            IReadOnlyList<Vector2Int> targetCells = BuildTargetCells(patternType, boss); // 패턴별 실제 위험 칸 계산

            if (targetCells.Count == 0) // King 부재나 보드 경계로 선택 패턴이 빈 경우
            {
                patternType = BossPatternType.SlamRing; // 항상 계산 가능한 주변 강타로 폴백
                targetCells = BossPatternLibrary.BuildSlamRing(_runState.Board, boss); // 같은 위험 칸 계산 함수 사용
            }

            string displayName = BossPatternLibrary.GetDisplayName(patternType); // 플레이어용 패턴 이름 결정
            var telegraph = new BossTelegraphState(boss, patternType, displayName, targetCells, _turnManager.TurnNumber); // 이번 턴 위험 칸을 고정 스냅샷으로 저장
            state.SetPendingTelegraph(telegraph); // 다음 EnemyTurn까지 피해 없이 예고 상태만 유지
            _overlay?.Show(_boardView, telegraph.TargetCells, telegraph.PatternType); // 실제 TargetCells와 동일한 칸을 붉은/주황 경고 타일로 표시
            _statusUI?.SetState(BossPhase.Phase2, telegraph.DisplayName, false); // 상단 UI에 현재 예고 패턴 표시

            Debug.Log($"39일차 보스 텔레그래프: {telegraph.DisplayName} / 위험칸={telegraph.TargetCells.Count} / 다음 EnemyTurn 실행"); // 개발 로그
            CompleteEnemyTurnWithoutAction(); // 예고 자체가 이번 EnemyTurn 행동이며 실제 피해 없이 PlayerTurn을 보장
            return true; // 일반 AI가 같은 턴에 추가 행동하지 않게 차단
        }

        private IReadOnlyList<Vector2Int> BuildTargetCells(BossPatternType patternType, PieceRuntimeState boss) // 현재 패턴 종류의 실제 위험 칸을 만드는 메서드
        {
            if (patternType == BossPatternType.KingLane) // King 방향 직선 패턴이면
            {
                PieceRuntimeState king = BossPatternLibrary.FindPlayerKing(_runState.Board); // 현재 살아 있는 플레이어 King 탐색
                if (king != null) return BossPatternLibrary.BuildKingLane(_runState.Board, boss, king, KingLaneLength); // King 방향 2칸 폭 x 3칸 길이 위험선 반환
            }

            return BossPatternLibrary.BuildSlamRing(_runState.Board, boss); // 기본은 2x2 주변 강타 링 반환
        }

        private void HandleAfterDamage(PieceRuntimeState target, PieceRuntimeState source, int appliedAmount) // 보스 HP 피해 직후 Phase 2 전환 또는 사망을 처리하는 훅
        {
            if (target?.Definition == null || target.IsPlayerPiece || target.Definition.Category != PieceCategory.Boss) return; // 적 Boss 피해만 처리

            BossPhaseRuntimeState state = GetOrCreateState(target); // 대상 보스의 페이즈 상태 확보

            if (target.IsDead) // Phase 2 보스가 사망했다면
            {
                state.ClearPendingTelegraph(); // 실행 대기 공격 제거
                _overlay?.Clear(); // 남은 위험 칸 표시 제거
                _statusUI?.SetState(BossPhase.Phase1, string.Empty, false); // 별도 보스 상태 줄 숨김
                return; // 사망 뒤 페이즈 전환 금지
            }

            TryApplyPhase2Transition(target, state); // 이번 피해로 50% 조건에 들어갔는지 검사
        }

        private void ScanBossesAndApplyTransitions() // 보드에 새로 생성되거나 세이브로 복원된 보스를 한 번씩 찾아 페이즈 상태를 동기화하는 메서드
        {
            if (_runState?.Board == null) return; // 보드가 없으면 검사할 수 없음
            var visited = new HashSet<PieceRuntimeState>(); // 2x2 네 칸의 같은 보스를 한 번만 검사하기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 전체 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 전체 순회
                {
                    var piece = _runState.Board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 기물 조회
                    if (piece?.Definition == null || piece.IsPlayerPiece || piece.IsDead || !visited.Add(piece)) continue; // 살아 있는 적 기물만 한 번 검사
                    if (piece.Definition.Category != PieceCategory.Boss) continue; // Boss 카테고리만 페이즈 상태 관리

                    BossPhaseRuntimeState state = GetOrCreateState(piece); // 상태가 없으면 Phase 1 상태 생성
                    TryApplyPhase2Transition(piece, state); // 현재 HP가 이미 절반 이하면 즉시 Phase 2 적용
                }
            }
        }

        private void TryApplyPhase2Transition(PieceRuntimeState boss, BossPhaseRuntimeState state) // 보스 하나의 50% HP 전환·증원·UI를 한 번에 처리하는 메서드
        {
            if (boss?.Definition == null || state == null) return; // 필수 데이터가 없으면 종료
            if (!state.TryEnterPhase2(boss.CurrentHp, boss.Definition.BaseHp)) return; // 이번 호출에서 새로 Phase 2가 된 경우만 아래 처리

            int reinforcementCount = 0; // 실제 생성된 증원 수
            if (state.TryMarkReinforcementCalled()) reinforcementCount = SpawnPhase2Reinforcements(); // 같은 보스에서 단 한 번만 증원 처리

            _statusUI?.SetState(BossPhase.Phase2, string.Empty, reinforcementCount > 0); // 상단에 Phase 2와 증원 발생 안내 표시
            Debug.Log($"BOSS PHASE 2 진입: {boss.Definition.DisplayName} / HP={boss.CurrentHp}/{boss.Definition.BaseHp} / 증원={reinforcementCount}"); // 전환 결과 로그
        }

        private int SpawnPhase2Reinforcements() // 기존 PlayerStartingDeck26 카탈로그와 SpawnTestEnemy를 재사용해 Phase 2 증원 1~2기를 생성하는 메서드
        {
            if (_boardInput == null || _runState?.Board == null || _pieceCatalog == null) // 증원에 필요한 기존 시스템이 없으면
            {
                Debug.LogWarning("39일차 Phase 2 증원 생략: BoardInput 또는 PlayerStartingDeck26 카탈로그가 준비되지 않았습니다."); // 원인 출력
                return 0; // 전투 자체는 계속 진행
            }

            string[] reinforcementIds = { "knight", "pawn" }; // Phase 2 프로토타입 압박용 기본 증원 두 종류
            int spawnedCount = 0; // 실제 성공 수
            int candidateStart = 0; // 다음 증원이 앞 증원과 같은 칸을 다시 검사하지 않도록 시작 인덱스

            for (int i = 0; i < reinforcementIds.Length; i++) // Knight와 Pawn을 순서대로 시도
            {
                PieceDefinition definition = FindDefinition(reinforcementIds[i]); // 기존 26종 카탈로그에서 실제 정의 찾기
                if (definition == null) continue; // 카탈로그에 없으면 해당 증원만 건너뜀

                for (int candidateIndex = candidateStart; candidateIndex < ReinforcementCandidateCells.Length; candidateIndex++) // 안전 후보 칸을 순서대로 탐색
                {
                    Vector2Int cell = ReinforcementCandidateCells[candidateIndex]; // 현재 후보 좌표
                    var tile = _runState.Board.GetTile(cell); // 실제 타일 조회
                    if (tile == null || tile.IsBlockedByObstacle || tile.OccupyingPiece != null) continue; // 보드 밖·장애물·점유 칸 제외

                    PieceRuntimeState spawned = _boardInput.SpawnTestEnemy(definition, cell); // 기존 적 스폰 경로를 그대로 재사용
                    candidateStart = candidateIndex + 1; // 다음 증원은 다음 후보부터 검색

                    if (spawned != null) // 실제 보드·화면 생성에 성공했으면
                    {
                        spawnedCount++; // 성공 수 증가
                        Debug.Log($"Phase 2 증원: {definition.DisplayName} @ {cell}"); // 생성 결과 로그
                    }

                    break; // 한 종류는 한 번만 스폰 시도
                }
            }

            return spawnedCount; // 실제 생성된 총 증원 수 반환
        }

        private PieceDefinition FindDefinition(string pieceId) // PlayerStartingDeck26 카탈로그에서 PieceId로 실제 PieceDefinition을 찾는 메서드
        {
            if (_pieceCatalog == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 잘못된 입력이면 찾지 않음

            for (int i = 0; i < _pieceCatalog.Cards.Count; i++) // 카탈로그 전체 순회
            {
                PieceDefinition definition = _pieceCatalog.Cards[i]; // 현재 정의 조회
                if (definition == null) continue; // 빈 슬롯 건너뜀
                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) return definition; // PieceId 일치 시 반환
            }

            return null; // 찾지 못하면 null
        }

        private PieceRuntimeState FindActivePhase2Boss() // 현재 보드에서 살아 있는 Phase 2 보스 한 기를 결정론적으로 찾는 메서드
        {
            if (_runState?.Board == null) return null; // 보드가 없으면 null
            var visited = new HashSet<PieceRuntimeState>(); // 2x2 중복 방지

            for (int y = 0; y < BoardState.Height; y++) // Y 우선 고정 순서로 탐색해 같은 보드에서 항상 같은 보스 선택
            {
                for (int x = 0; x < BoardState.Width; x++) // X 순회
                {
                    var piece = _runState.Board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 적 조회
                    if (piece?.Definition == null || piece.IsPlayerPiece || piece.IsDead || !visited.Add(piece)) continue; // 유효한 살아 있는 적만 한 번 확인
                    if (piece.Definition.Category != PieceCategory.Boss) continue; // Boss만 사용
                    if (GetOrCreateState(piece).Phase == BossPhase.Phase2) return piece; // Phase 2 보스면 즉시 반환
                }
            }

            return null; // Phase 2 보스 없음
        }

        private BossPhaseRuntimeState GetOrCreateState(PieceRuntimeState boss) // 보스별 런타임 페이즈 상태를 가져오거나 처음 만드는 메서드
        {
            if (boss == null) return null; // 보스가 없으면 상태도 없음
            if (_states.TryGetValue(boss, out var state)) return state; // 기존 상태가 있으면 반환
            state = new BossPhaseRuntimeState(); // 새 Phase 1 상태 생성
            _states.Add(boss, state); // 같은 런타임 보스에 연결
            return state; // 새 상태 반환
        }

        private void CompleteEnemyTurnWithoutAction() // 텔레그래프 예고처럼 피해 없는 보스 행동도 정상적으로 한 EnemyTurn을 소비하게 하는 메서드
        {
            if (_turnManager == null || _turnManager.CurrentState != TurnState.EnemyTurn) return; // 이미 다른 상태면 중복 종료 금지
            if (_turnManager.CompleteEnemyTurn()) _battleHooks?.RaiseTurnEnd(_turnManager.CurrentState, _turnManager.TurnNumber); // 기존 턴 종료·상태 효과 훅 유지
        }

        private void UnbindHooks() // 재바인딩 또는 파괴 시 기존 전투 훅 구독을 해제하는 메서드
        {
            if (_battleHooks != null) _battleHooks.AfterDamage -= HandleAfterDamage; // 보스 피해 이벤트 구독 해제
            _isBound = false; // 연결 상태 초기화
        }

        private void OnDestroy() // Battle 씬 종료 시 런타임 표시와 이벤트를 정리하는 메서드
        {
            UnbindHooks(); // 전투 훅 해제
            _overlay?.Clear(); // 남은 위험 칸 표시 제거
        }
    }
}
