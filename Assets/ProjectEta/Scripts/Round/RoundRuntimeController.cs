using System; // StringComparison과 Action을 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using System.Reflection; // 기존 BattleController 턴 제한 필드와 호환하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Resources, Debug, Vector2Int를 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController, BoardState, BoardView를 사용하기 위한 네임스페이스
using ProjectEta.Boss; // 2x2 보스 점유·시각 유틸리티를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // PlayerStartingDeckCatalog를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Round // 라운드 구성·증원 관련 타입을 모아두는 네임스페이스
{
    public sealed class RoundRuntimeController : MonoBehaviour // RoundDefinition을 실제 Battle 씬의 일반 적·증원·보스에 연결하는 런타임 관리자
    {
        private const string PrototypeRoundResourceName = "PrototypeRound36"; // 일반 테스트 라운드 Resources 이름
        private const string PrototypeBossRoundResourceName = "PrototypeBossRound40"; // 40일차 보스 통합 라운드 Resources 이름
        private const string PieceCatalogResourceName = "PlayerStartingDeck26"; // 26종 PieceDefinition 카탈로그 Resources 이름

        private BattleController _battleController; // 현재 Battle 씬 전투 상태 소유자
        private BoardInputController _boardInputController; // 일반 적·보스의 실제 PieceView 생성 진입점
        private BoardView _boardView; // 대형 보스 시각 중심과 타일 크기 접근용 보드 뷰
        private TurnManager _turnManager; // 증원 턴과 턴 제한을 읽을 턴 매니저
        private RunState _runState; // 현재 보드와 라운드 번호를 읽을 런 상태
        private RoundDefinition _definition; // 현재 적용 중인 라운드 데이터
        private PlayerStartingDeckCatalog _pieceCatalog; // PieceId를 PieceDefinition으로 변환할 기물 카탈로그
        private readonly HashSet<int> _processedReinforcementIndices = new HashSet<int>(); // 이미 처리한 증원 인덱스 집합
        private RoundSummaryUI _summaryUI; // 상단 라운드 정보 UI
        private bool _isInitialized; // 런타임 연결 완료 여부

        public RoundDefinition Definition => _definition; // 외부 UI·보스 스포너·테스트가 읽는 현재 라운드 데이터
        public int TurnLimit => _definition != null ? _definition.TurnLimit : 30; // 라운드 데이터가 없을 때 30턴 기본값
        public int CurrentEnemyCount => _runState != null ? CountCurrentEnemies(_runState.Board) : 0; // 실제 생존 적 수
        public event Action RoundStateChanged; // 적 구성·턴 상태 변경 통지 이벤트

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드 직후 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 연결 없이 런타임 관리자 생성
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 실행 방지
            if (UnityEngine.Object.FindFirstObjectByType<RoundRuntimeController>() != null) return; // 중복 생성 방지

            var controllerObject = new GameObject("RoundRuntimeController_Day40"); // 40일차 통합 라운드 관리자 오브젝트 생성
            controllerObject.AddComponent<RoundRuntimeController>(); // Start 코루틴 연결
        }

        private IEnumerator Start() // BattleController 자동 초기화 완료를 기다리는 코루틴
        {
            const int maxWaitFrames = 180; // 최대 대기 프레임
            int waitedFrames = 0; // 현재 대기 프레임

            while (waitedFrames < maxWaitFrames) // 필요한 런타임 객체 준비 대기
            {
                _battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // BattleController 탐색
                _boardInputController = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // BoardInputController 탐색
                _boardView = UnityEngine.Object.FindFirstObjectByType<BoardView>(); // BoardView 탐색

                if (_battleController != null &&
                    _boardInputController != null &&
                    _boardView != null &&
                    _battleController.RunState != null &&
                    _battleController.TurnManager != null &&
                    _boardInputController.IsBound &&
                    _boardView.IsBound) // 전투·보드 상태 연결 완료 여부
                {
                    InitializeRuntime(); // 실제 라운드 연결 실행
                    yield break; // 초기화 코루틴 종료
                }

                waitedFrames++; // 대기 프레임 증가
                yield return null; // 다음 프레임 대기
            }

            Debug.LogError("40일차 RoundRuntimeController 초기화 실패: BattleController, BoardInputController 또는 BoardView 연결을 찾지 못했습니다."); // 초기화 실패 로그
        }

        private void InitializeRuntime() // 라운드 데이터·초기 적·보스·증원·UI를 연결하는 메서드
        {
            if (_isInitialized) return; // 중복 초기화 방지

            _runState = _battleController.RunState; // 현재 런 상태 저장
            _turnManager = _battleController.TurnManager; // 현재 턴 매니저 저장
            string roundResourceName = ResolveRoundResourceName(_runState.CurrentRound); // 현재 라운드 번호로 데이터 에셋 선택
            _definition = Resources.Load<RoundDefinition>(roundResourceName); // 선택된 RoundDefinition 로드
            _pieceCatalog = Resources.Load<PlayerStartingDeckCatalog>(PieceCatalogResourceName); // 26종 기물 카탈로그 로드

            if (_definition == null && !string.Equals(roundResourceName, PrototypeRoundResourceName, StringComparison.Ordinal)) // 보스 테스트 데이터가 누락됐으면
            {
                Debug.LogWarning($"RoundDefinition Resources/{roundResourceName}를 찾지 못해 {PrototypeRoundResourceName}으로 fallback합니다."); // fallback 로그
                _definition = Resources.Load<RoundDefinition>(PrototypeRoundResourceName); // 일반 라운드 데이터 fallback
            }

            if (_definition == null) // 라운드 데이터가 끝까지 없으면
            {
                Debug.LogError($"RoundDefinition Resources/{roundResourceName} 또는 {PrototypeRoundResourceName}를 찾지 못했습니다."); // 리소스 누락 로그
                return; // 초기화 중단
            }

            if (_pieceCatalog == null) // 기물 카탈로그가 없으면
            {
                Debug.LogError($"PlayerStartingDeckCatalog Resources/{PieceCatalogResourceName}를 찾지 못했습니다."); // 카탈로그 누락 로그
                return; // 초기화 중단
            }

            ApplyRoundTurnLimitToLegacyBattleController(); // 기존 BattleController 턴 제한 동기화
            EnsureInitialEnemySetup(); // 일반 시작 적 구성 보장
            EnsureConfiguredBoss(); // 보스 라운드면 RoundDefinition 데이터로 2x2 보스 생성
            BindTurnEvents(); // 턴별 증원 처리 연결
            EnsureSummaryUI(); // 라운드 정보 UI 연결

            _isInitialized = true; // 초기화 완료 기록
            RoundStateChanged?.Invoke(); // 초기 UI 갱신 통지

            Debug.Log($"40일차 라운드 통합 완료: Round={_runState.CurrentRound} / {_definition.DisplayName} / Boss={_definition.IsBossRound} / TurnLimit={TurnLimit} / Enemies={CurrentEnemyCount} / Reinforcements={_definition.Reinforcements.Count}"); // 통합 결과 로그
        }

        public static string ResolveRoundResourceName(int currentRound) // 5·10라운드 보스 회귀 테스트용 데이터 선택 규칙
        {
            return currentRound == 5 || currentRound == 10 ? PrototypeBossRoundResourceName : PrototypeRoundResourceName; // 5·10라운드만 보스 통합 데이터 사용
        }

        public bool EnsureConfiguredBoss() // 현재 RoundDefinition에 지정된 보스를 실제 보드에 한 번만 준비하는 진입점
        {
            if (_definition == null || !_definition.HasBossConfiguration) return false; // 보스 라운드가 아니면 처리 없음
            if (_runState?.Board == null || _boardInputController == null || _boardView == null) return false; // 필수 런타임 객체 검사

            var bossDefinition = Resources.Load<PieceDefinition>(_definition.BossResourceName); // RoundDefinition이 지정한 보스 데이터 로드

            if (bossDefinition == null) // 보스 리소스가 없으면
            {
                Debug.LogError($"보스 생성 실패: Resources/{_definition.BossResourceName} PieceDefinition을 찾지 못했습니다."); // 누락 리소스 로그
                return false; // 생성 실패
            }

            var existingBoss = FindLivingEnemyByPieceId(_runState.Board, bossDefinition.PieceId); // 저장 복원 또는 선행 스포너가 만든 동일 보스 탐색

            if (existingBoss != null) // 동일 보스가 이미 있으면
            {
                if (!LargePieceBoardUtility.IsFootprintComplete(_runState.Board, existingBoss)) // 1x1 기준점만 복원된 상태면
                {
                    if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(_runState.Board, existingBoss)) // 전체 점유 복구 시도
                    {
                        Debug.LogWarning($"보스 기존 점유 복구 실패: {bossDefinition.DisplayName} @ {existingBoss.BoardPosition}"); // 점유 충돌 로그
                        return false; // 잘못된 점유 상태 반환
                    }
                }

                ApplyBossVisual(existingBoss); // 기존 보스 화면 중심·콜라이더 보정
                Debug.Log($"보스 기존 상태 재사용: {bossDefinition.DisplayName} @ {existingBoss.BoardPosition} / HP={existingBoss.CurrentHp}"); // 중복 방지 로그
                return true; // 기존 보스 재사용 성공
            }

            Vector2Int bossAnchor = _definition.BossAnchor; // 데이터에 지정된 보스 기준 좌표

            if (!LargePieceBoardUtility.CanPlace(_runState.Board, bossDefinition, bossAnchor)) // 2x2 전체 배치 가능 여부 검사
            {
                Debug.LogWarning($"보스 배치 실패: {bossDefinition.DisplayName} @ {bossAnchor} / Size={LargePieceBoardUtility.GetFootprint(bossDefinition)}"); // 충돌 또는 경계 로그
                return false; // 다른 기물 덮어쓰기 방지
            }

            PieceRuntimeState boss = _boardInputController.SpawnTestEnemy(bossDefinition, bossAnchor); // 기존 PieceView 생성 경로로 기준 칸 생성

            if (boss == null) // 기준 칸 생성 실패면
            {
                Debug.LogWarning($"보스 기준 칸 스폰 실패: {bossDefinition.DisplayName} @ {bossAnchor}"); // 실패 로그
                return false; // 추가 점유 중단
            }

            if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(_runState.Board, boss)) // 기준 칸을 전체 점유 영역으로 확장
            {
                _runState.Board.ClearPiece(boss); // 부분 생성 잔여 점유 정리
                Debug.LogError($"보스 2x2 점유 확장 실패: {bossDefinition.DisplayName} @ {bossAnchor}"); // 원자성 실패 로그
                return false; // 생성 실패 반환
            }

            ApplyBossVisual(boss); // 보스 3D 위치·크기·클릭 콜라이더 보정
            RoundStateChanged?.Invoke(); // 현재 적 수 UI 갱신
            Debug.Log($"RoundDefinition 보스 생성: {bossDefinition.DisplayName} / Anchor={bossAnchor} / Size={LargePieceBoardUtility.GetFootprint(bossDefinition)} / HP={boss.CurrentHp}"); // 생성 결과 로그
            return true; // 데이터 기반 보스 생성 성공
        }

        private void ApplyBossVisual(PieceRuntimeState boss) // 대형 보스 시각 중심과 콜라이더를 점유 크기에 맞추는 메서드
        {
            if (boss == null || _boardView == null) return; // 필수 객체 검사
            var pieceView = LargePieceVisualUtility.FindPieceView(boss); // 현재 보스 PieceView 탐색
            if (pieceView != null) LargePieceVisualUtility.ApplyFootprint(pieceView, _boardView.TileSize); // 점유 중앙·스케일·콜라이더 적용
        }

        private static PieceRuntimeState FindLivingEnemyByPieceId(BoardState board, string pieceId) // 보드에서 동일 PieceId의 살아 있는 적 한 기를 찾는 메서드
        {
            if (board == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 필수 데이터 검사

            var visited = new HashSet<PieceRuntimeState>(); // 대형 기물 중복 순회 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || !visited.Add(piece)) continue; // 빈 칸·중복 기물 제외
                    if (piece.IsPlayerPiece || piece.IsDead || piece.Definition == null) continue; // 아군·사망·정의 누락 제외

                    if (string.Equals(piece.Definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) // PieceId 일치 여부
                    {
                        return piece; // 동일 살아 있는 보스 반환
                    }
                }
            }

            return null; // 동일 보스 없음
        }

        private void ApplyRoundTurnLimitToLegacyBattleController() // 기존 BattleController의 테스트 턴 제한을 RoundDefinition 값으로 동기화
        {
            var field = typeof(BattleController).GetField("_turnLimitTestValue", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 private 필드 탐색

            if (field == null) // 필드 구조가 바뀌었으면
            {
                Debug.LogWarning("40일차 턴 제한 호환 연결 실패: BattleController._turnLimitTestValue 필드를 찾지 못했습니다."); // 호환 경고 로그
                return; // 나머지 라운드 기능 유지
            }

            field.SetValue(_battleController, TurnLimit); // 실제 턴 제한 값 반영
        }

        private void EnsureInitialEnemySetup() // RoundDefinition의 일반 시작 적 구성을 실제 보드에 보장
        {
            int currentEnemyCount = CountCurrentEnemies(_runState.Board); // 현재 적 수 확인

            if (currentEnemyCount > 0 && !IsLegacyPrototypeSquad(_runState.Board)) // 저장 복원 등 실제 적 상태가 이미 있으면
            {
                Debug.Log($"기존 전투 적 {currentEnemyCount}기를 유지하고 시작 적 자동 배치를 건너뜁니다."); // 저장 상태 보호 로그
                return; // 기존 상태 유지
            }

            for (int i = 0; i < _definition.InitialEnemies.Count; i++) // 모든 시작 적 순회
            {
                var spawn = _definition.InitialEnemies[i]; // 현재 스폰 데이터
                if (spawn == null) continue; // 빈 데이터 제외

                var existingPiece = _runState.Board.GetTile(spawn.Position)?.OccupyingPiece; // 지정 위치 현재 점유 확인

                if (existingPiece != null) // 이미 점유 중이면
                {
                    if (!existingPiece.IsPlayerPiece &&
                        existingPiece.Definition != null &&
                        string.Equals(existingPiece.Definition.PieceId, spawn.PieceId, StringComparison.OrdinalIgnoreCase)) // 같은 적이 이미 정확히 있으면
                    {
                        continue; // 중복 생성 방지
                    }

                    Debug.LogWarning($"시작 적 배치 실패: {spawn.PieceId} @ {spawn.Position} — 이미 다른 기물이 점유 중입니다."); // 점유 충돌 로그
                    continue; // 기존 기물 보호
                }

                TrySpawnEnemy(spawn, "시작 적"); // 빈 칸에 실제 적 생성
            }
        }

        private bool IsLegacyPrototypeSquad(BoardState board) // 기존 BattleController가 자동 배치한 Pawn+Rook 두 기인지 확인
        {
            if (board == null) return false; // 보드 누락 검사
            if (CountCurrentEnemies(board) != 2) return false; // 정확히 두 기만 허용

            var pawn = board.GetTile(new Vector2Int(4, 8))?.OccupyingPiece; // 기존 Pawn 위치 조회
            var rook = board.GetTile(new Vector2Int(6, 8))?.OccupyingPiece; // 기존 Rook 위치 조회

            if (pawn == null || rook == null) return false; // 둘 중 하나 누락 검사
            if (pawn.IsPlayerPiece || rook.IsPlayerPiece) return false; // 적 진영 검사
            if (pawn.Definition == null || rook.Definition == null) return false; // 정의 누락 검사

            bool pawnMatches = string.Equals(pawn.Definition.PieceId, "pawn", StringComparison.OrdinalIgnoreCase); // Pawn 일치 여부
            bool rookMatches = string.Equals(rook.Definition.PieceId, "rook", StringComparison.OrdinalIgnoreCase); // Rook 일치 여부
            return pawnMatches && rookMatches; // 기존 테스트 부대 여부 반환
        }

        private void BindTurnEvents() // TurnManager 이벤트를 중복 없이 연결
        {
            _turnManager.TurnChanged -= HandleTurnChanged; // 기존 중복 구독 제거
            _turnManager.TurnChanged += HandleTurnChanged; // 새 턴 이벤트 구독
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 전환 시 증원과 UI를 갱신
        {
            if (state == TurnState.PlayerTurn) // 일반 플레이어 턴 시작 여부
            {
                ProcessDueReinforcements(turnNumber); // 현재 턴까지의 미처리 증원 실행
            }

            RoundStateChanged?.Invoke(); // 턴·적 수 UI 갱신 통지
        }

        private void ProcessDueReinforcements(int currentTurn) // 지정 턴에 도달한 증원을 한 번씩 처리
        {
            if (_definition == null) return; // 데이터 누락 검사

            for (int i = 0; i < _definition.Reinforcements.Count; i++) // 모든 증원 순회
            {
                if (_processedReinforcementIndices.Contains(i)) continue; // 처리 완료 증원 제외

                var spawn = _definition.Reinforcements[i]; // 현재 증원 데이터

                if (spawn == null) // 빈 증원 데이터면
                {
                    _processedReinforcementIndices.Add(i); // 반복 방지 완료 표시
                    continue; // 다음 증원 검사
                }

                if (!spawn.IsDue(currentTurn)) continue; // 아직 등장 턴 전이면 대기

                bool success = TrySpawnEnemy(spawn, $"Turn {currentTurn} 증원"); // 실제 증원 생성 시도
                _processedReinforcementIndices.Add(i); // 성공·실패 모두 이벤트 한 번 처리

                if (!success) // 생성 실패면
                {
                    Debug.LogWarning($"증원 실패 처리 완료: {spawn.PieceId} @ {spawn.Position} / 지정 Turn={spawn.SpawnTurn}"); // 실패 기록
                }
            }

            RoundStateChanged?.Invoke(); // 증원 처리 후 UI 갱신
        }

        private bool TrySpawnEnemy(EnemySpawnDefinition spawn, string sourceLabel) // 일반 PieceId 기반 적 스폰 경로
        {
            if (spawn == null) return false; // 데이터 누락 검사

            var definition = FindPieceDefinition(spawn.PieceId); // 기물 카탈로그 조회

            if (definition == null) // PieceId 미등록이면
            {
                Debug.LogWarning($"{sourceLabel} 실패: PieceId '{spawn.PieceId}'를 PlayerStartingDeck26에서 찾지 못했습니다."); // 데이터 오류 로그
                return false; // 생성 실패
            }

            var runtimePiece = _boardInputController.SpawnTestEnemy(definition, spawn.Position); // 기존 보드·PieceView 스폰 경로 사용

            if (runtimePiece == null) // 범위·점유 문제면
            {
                Debug.LogWarning($"{sourceLabel} 실패: {spawn.PieceId} @ {spawn.Position}"); // 위치 실패 로그
                return false; // 생성 실패
            }

            Debug.Log($"{sourceLabel}: {definition.DisplayName} @ {spawn.Position} / 현재 적={CountCurrentEnemies(_runState.Board)}"); // 정상 생성 로그
            RoundStateChanged?.Invoke(); // 현재 적 수 갱신 통지
            return true; // 생성 성공
        }

        private PieceDefinition FindPieceDefinition(string pieceId) // PlayerStartingDeck26에서 PieceId로 기물 정의 조회
        {
            if (_pieceCatalog == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 필수 데이터 검사

            for (int i = 0; i < _pieceCatalog.Cards.Count; i++) // 기물 카탈로그 순회
            {
                var definition = _pieceCatalog.Cards[i]; // 현재 기물 정의
                if (definition == null) continue; // 빈 항목 제외

                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) // PieceId 일치 여부
                {
                    return definition; // 기물 정의 반환
                }
            }

            return null; // 미등록 PieceId
        }

        private void EnsureSummaryUI() // 라운드 요약 UI를 현재 런타임에 연결
        {
            if (_summaryUI == null) _summaryUI = GetComponent<RoundSummaryUI>(); // 같은 오브젝트의 기존 UI 탐색
            if (_summaryUI == null) _summaryUI = gameObject.AddComponent<RoundSummaryUI>(); // UI가 없으면 자동 생성
            _summaryUI.Bind(this, _turnManager, _runState); // 현재 라운드·턴·런 상태 연결
        }

        public static int CountCurrentEnemies(BoardState board) // 살아 있는 적 런타임 기물을 점유 칸과 무관하게 한 번씩 계산
        {
            if (board == null) return 0; // 보드 누락 시 0

            var uniqueEnemies = new HashSet<PieceRuntimeState>(); // 2x2 대형 기물 중복 카운트 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || piece.IsPlayerPiece || piece.IsDead) continue; // 빈 칸·아군·사망 제외
                    uniqueEnemies.Add(piece); // 동일 런타임 기물 한 번만 등록
                }
            }

            return uniqueEnemies.Count; // 실제 생존 적 수 반환
        }

        private void OnDestroy() // 씬 종료 시 이벤트 구독 정리
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 해제
        }
    }
}
