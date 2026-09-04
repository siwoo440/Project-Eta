using System; // StringComparison과 Action을 사용하기 위한 네임스페이스
using System.Collections; // IEnumerator 코루틴을 사용하기 위한 네임스페이스
using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using System.Reflection; // 기존 BattleController 턴 제한 필드와 호환하기 위한 네임스페이스
using UnityEngine; // MonoBehaviour, Resources, Debug, Vector2Int를 사용하기 위한 네임스페이스
using UnityEngine.SceneManagement; // 현재 씬이 Battle인지 확인하기 위한 네임스페이스
using ProjectEta.Battle; // BattleController, TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardInputController, BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // PlayerStartingDeckCatalog를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Round // 라운드 구성·증원 관련 타입을 모아두는 네임스페이스
{
    public sealed class RoundRuntimeController : MonoBehaviour // RoundDefinition을 실제 Battle 씬의 시작 적·증원·턴 제한에 연결하는 36일차 런타임 관리자
    {
        private const string PrototypeRoundResourceName = "PrototypeRound36"; // 별도 Inspector 연결 없이 로드할 기본 라운드 Resources 이름
        private const string PieceCatalogResourceName = "PlayerStartingDeck26"; // 26종 PieceDefinition을 PieceId로 찾기 위한 기존 Resources 카탈로그

        private BattleController _battleController; // 현재 Battle 씬의 전투 상태 소유자
        private BoardInputController _boardInputController; // 적 기물을 실제 보드·화면에 함께 생성할 기존 스폰 진입점
        private TurnManager _turnManager; // 증원 턴과 턴 제한을 읽을 실제 턴 매니저
        private RunState _runState; // 현재 보드와 라운드 번호를 읽을 런 상태
        private RoundDefinition _definition; // 현재 적용 중인 라운드 데이터
        private PlayerStartingDeckCatalog _pieceCatalog; // PieceId를 실제 PieceDefinition으로 변환할 26종 카탈로그
        private readonly HashSet<int> _processedReinforcementIndices = new HashSet<int>(); // 이미 성공·실패 처리한 증원 인덱스 기록
        private RoundSummaryUI _summaryUI; // 상단 중앙 턴 UI 바로 아래에 표시할 라운드 정보 UI
        private bool _isInitialized; // 런타임 연결 완료 여부

        public RoundDefinition Definition => _definition; // 외부 UI·테스트가 읽는 현재 라운드 데이터
        public int TurnLimit => _definition != null ? _definition.TurnLimit : 30; // 데이터가 없을 때 기존 일반 라운드 30턴을 안전한 기본값으로 사용
        public int CurrentEnemyCount => _runState != null ? CountCurrentEnemies(_runState.Board) : 0; // 현재 보드의 실제 생존 적 수
        public event Action RoundStateChanged; // 시작 적·증원·턴 상태가 바뀌어 UI 갱신이 필요할 때 발행하는 이벤트

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] // Battle 씬 로드가 끝난 뒤 자동 실행
        private static void AutoCreateForBattleScene() // 인스펙터 배치 없이 36일차 라운드 관리자를 생성하는 부트스트랩
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬이 아니면 생성하지 않음
            if (UnityEngine.Object.FindFirstObjectByType<RoundRuntimeController>() != null) return; // 이미 존재하면 중복 생성하지 않음

            var controllerObject = new GameObject("RoundRuntimeController_Day36"); // 라운드 관리자 전용 오브젝트 생성
            controllerObject.AddComponent<RoundRuntimeController>(); // Start 코루틴에서 기존 전투 시스템과 연결
        }

        private IEnumerator Start() // BattleController의 자동 생성 순서와 무관하게 연결될 때까지 기다리는 초기화 코루틴
        {
            const int maxWaitFrames = 180; // 약 3초 정도의 충분한 초기화 대기 프레임
            int waitedFrames = 0; // 현재까지 대기한 프레임 수

            while (waitedFrames < maxWaitFrames) // 핵심 전투 객체가 준비될 때까지 반복
            {
                _battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 탐색
                _boardInputController = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 현재 BoardInputController 탐색

                if (_battleController != null && _boardInputController != null && _battleController.RunState != null && _battleController.TurnManager != null && _boardInputController.IsBound) // 모든 핵심 상태가 준비됐으면
                {
                    InitializeRuntime(); // RoundDefinition을 실제 전투에 연결
                    yield break; // 초기화 완료
                }

                waitedFrames++; // 대기 횟수 증가
                yield return null; // 다음 프레임까지 기다림
            }

            Debug.LogError("36일차 RoundRuntimeController 초기화 실패: BattleController 또는 BoardInputController 연결을 찾지 못했습니다."); // 제한 시간 안에 연결되지 않으면 원인 출력
        }

        private void InitializeRuntime() // 라운드 데이터·시작 적·증원·UI를 한 번에 준비하는 메서드
        {
            if (_isInitialized) return; // 중복 초기화를 방지

            _runState = _battleController.RunState; // 실제 런 상태 참조 저장
            _turnManager = _battleController.TurnManager; // 실제 턴 매니저 참조 저장
            _definition = Resources.Load<RoundDefinition>(PrototypeRoundResourceName); // 36일차 기본 RoundDefinition 에셋 로드
            _pieceCatalog = Resources.Load<PlayerStartingDeckCatalog>(PieceCatalogResourceName); // 26종 PieceDefinition 카탈로그 로드

            if (_definition == null) // 라운드 데이터 에셋을 찾지 못했으면
            {
                Debug.LogError($"RoundDefinition Resources/{PrototypeRoundResourceName}를 찾지 못했습니다."); // 누락된 리소스 이름 출력
                return; // 데이터 없이 증원 시스템을 시작하지 않음
            }

            if (_pieceCatalog == null) // 기물 카탈로그를 찾지 못했으면
            {
                Debug.LogError($"PlayerStartingDeckCatalog Resources/{PieceCatalogResourceName}를 찾지 못했습니다."); // 누락된 기존 리소스 안내
                return; // PieceId를 실제 기물로 변환할 수 없으므로 중단
            }

            ApplyRoundTurnLimitToLegacyBattleController(); // 기존 BattleController의 30턴 테스트 필드를 현재 RoundDefinition 값으로 동기화
            EnsureInitialEnemySetup(); // 새 테스트 런이면 RoundDefinition의 시작 적 구성을 보장
            BindTurnEvents(); // PlayerTurn 시작마다 증원과 UI 갱신을 처리하도록 연결
            EnsureSummaryUI(); // 상단 중앙 턴 UI 바로 아래에 라운드 정보 UI 생성

            _isInitialized = true; // 초기화 완료 표시
            RoundStateChanged?.Invoke(); // 첫 UI 표시를 위해 현재 상태 통지

            Debug.Log($"36일차 라운드 연결 완료: {_definition.DisplayName} / TurnLimit={TurnLimit} / InitialEnemies={CurrentEnemyCount} / Reinforcements={_definition.Reinforcements.Count}"); // 현재 적용 결과 출력
        }

        private void ApplyRoundTurnLimitToLegacyBattleController() // 기존 BattleController의 테스트 턴 제한을 데이터 값으로 바꾸는 호환 브리지
        {
            var field = typeof(BattleController).GetField("_turnLimitTestValue", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 private 턴 제한 필드 탐색

            if (field == null) // 이후 BattleController 구조가 바뀌어 필드를 찾지 못하면
            {
                Debug.LogWarning("36일차 턴 제한 호환 연결 실패: BattleController._turnLimitTestValue 필드를 찾지 못했습니다."); // 실패 원인만 로그로 남김
                return; // RoundRuntimeController 자체 증원 기능은 계속 유지
            }

            field.SetValue(_battleController, TurnLimit); // 기존 제한 판정도 RoundDefinition.TurnLimit을 사용하도록 런타임 값 교체
        }

        private void EnsureInitialEnemySetup() // 기존 폰+룩 프로토타입 테스트 부대를 라운드 데이터의 시작 적 구성으로 자연스럽게 확장하는 메서드
        {
            int currentEnemyCount = CountCurrentEnemies(_runState.Board); // 현재 BattleController Awake에서 이미 만들어진 적 수 확인

            if (currentEnemyCount > 0 && !IsLegacyPrototypeSquad(_runState.Board)) // 세이브 로드 등 이미 실제 적 구성이 존재하고 폰+룩 테스트 부대가 아니라면
            {
                Debug.Log($"기존 전투 적 {currentEnemyCount}기를 유지하고 36일차 시작 적 자동 배치를 건너뜁니다."); // 저장 상태 보호 로그
                return; // 기존 런을 덮어쓰지 않음
            }

            for (int i = 0; i < _definition.InitialEnemies.Count; i++) // RoundDefinition의 모든 시작 적 순회
            {
                var spawn = _definition.InitialEnemies[i]; // 현재 시작 적 데이터
                if (spawn == null) continue; // 비어 있는 항목은 건너뜀

                var existingPiece = _runState.Board.GetTile(spawn.Position)?.OccupyingPiece; // 지정 위치의 현재 점유 기물 확인

                if (existingPiece != null) // BattleController가 먼저 폰+룩을 배치했거나 다른 점유가 있으면
                {
                    if (!existingPiece.IsPlayerPiece && existingPiece.Definition != null && string.Equals(existingPiece.Definition.PieceId, spawn.PieceId, StringComparison.OrdinalIgnoreCase)) // 같은 적 기물이 이미 정확한 위치에 있다면
                    {
                        continue; // 중복 생성하지 않고 해당 시작 적은 충족된 것으로 처리
                    }

                    Debug.LogWarning($"시작 적 배치 실패: {spawn.PieceId} @ {spawn.Position} — 이미 다른 기물이 점유 중입니다."); // 다른 점유가 있으면 안전하게 건너뜀
                    continue; // 기존 기물을 덮어쓰지 않음
                }

                TrySpawnEnemy(spawn, "시작 적"); // 빈 칸이면 실제 적 생성 시도
            }
        }

        private bool IsLegacyPrototypeSquad(BoardState board) // 35일차 이전 BattleController가 자동 배치하는 폰+룩 2기인지 확인하는 메서드
        {
            if (board == null) return false; // 보드가 없으면 false
            if (CountCurrentEnemies(board) != 2) return false; // 정확히 2기가 아니면 기존 테스트 부대가 아님

            var pawn = board.GetTile(new Vector2Int(4, 8))?.OccupyingPiece; // 기존 폰 위치 확인
            var rook = board.GetTile(new Vector2Int(6, 8))?.OccupyingPiece; // 기존 룩 위치 확인

            if (pawn == null || rook == null) return false; // 둘 중 하나라도 없으면 기존 구성이 아님
            if (pawn.IsPlayerPiece || rook.IsPlayerPiece) return false; // 둘 다 적이어야 함
            if (pawn.Definition == null || rook.Definition == null) return false; // 정의가 있어야 비교 가능

            bool pawnMatches = string.Equals(pawn.Definition.PieceId, "pawn", StringComparison.OrdinalIgnoreCase); // 첫 위치가 Pawn인지 확인
            bool rookMatches = string.Equals(rook.Definition.PieceId, "rook", StringComparison.OrdinalIgnoreCase); // 둘째 위치가 Rook인지 확인
            return pawnMatches && rookMatches; // 정확한 기존 테스트 부대일 때만 true
        }

        private void BindTurnEvents() // TurnManager 이벤트를 중복 없이 연결하는 메서드
        {
            _turnManager.TurnChanged -= HandleTurnChanged; // 중복 구독 방지를 위해 먼저 해제
            _turnManager.TurnChanged += HandleTurnChanged; // 모든 턴 전환을 구독
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 턴 전환 시 증원·UI 갱신을 처리하는 메서드
        {
            if (state == TurnState.PlayerTurn) // 새 일반 플레이어 턴이 시작됐을 때만
            {
                ProcessDueReinforcements(turnNumber); // 현재 턴까지 도달한 미처리 증원을 한 번씩 처리
            }

            RoundStateChanged?.Invoke(); // 턴 표시와 현재 적 수가 바뀔 수 있으므로 UI에 갱신 통지
        }

        private void ProcessDueReinforcements(int currentTurn) // 지정 턴에 도달한 증원을 처리하는 메서드
        {
            for (int i = 0; i < _definition.Reinforcements.Count; i++) // 모든 증원 데이터 순회
            {
                if (_processedReinforcementIndices.Contains(i)) continue; // 이미 성공·실패 처리한 증원은 다시 시도하지 않음

                var spawn = _definition.Reinforcements[i]; // 현재 증원 데이터
                if (spawn == null) // 비어 있는 증원 항목이면
                {
                    _processedReinforcementIndices.Add(i); // 잘못된 항목도 반복 처리하지 않도록 완료 표시
                    continue; // 다음 증원 검사
                }

                if (!spawn.IsDue(currentTurn)) continue; // 아직 지정 턴에 도달하지 않았으면 대기

                bool success = TrySpawnEnemy(spawn, $"Turn {currentTurn} 증원"); // 실제 보드·화면에 적 생성 시도
                _processedReinforcementIndices.Add(i); // 성공 여부와 관계없이 이번 증원 이벤트는 한 번만 처리

                if (!success) // 목표 칸 점유 등으로 증원에 실패했으면
                {
                    Debug.LogWarning($"증원 실패 처리 완료: {spawn.PieceId} @ {spawn.Position} / 지정 Turn={spawn.SpawnTurn}"); // 실패 정보를 개발 로그에 남김
                }
            }

            RoundStateChanged?.Invoke(); // 증원 처리 후 현재 적 수를 즉시 UI에 반영
        }

        private bool TrySpawnEnemy(EnemySpawnDefinition spawn, string sourceLabel) // PieceId를 실제 PieceDefinition으로 바꿔 기존 BoardInputController 스폰 기능을 호출하는 메서드
        {
            if (spawn == null) return false; // 데이터가 없으면 실패

            var definition = FindPieceDefinition(spawn.PieceId); // 26종 카탈로그에서 PieceDefinition 조회

            if (definition == null) // PieceId를 찾지 못했으면
            {
                Debug.LogWarning($"{sourceLabel} 실패: PieceId '{spawn.PieceId}'를 PlayerStartingDeck26에서 찾지 못했습니다."); // 데이터 오류 안내
                return false; // 스폰 실패
            }

            var runtimePiece = _boardInputController.SpawnTestEnemy(definition, spawn.Position); // 기존 보드·PieceView 등록 로직을 재사용해 실제 적 생성

            if (runtimePiece == null) // 범위 밖·점유 등으로 기존 스폰 진입점이 실패했으면
            {
                Debug.LogWarning($"{sourceLabel} 실패: {spawn.PieceId} @ {spawn.Position}"); // 실패 위치 출력
                return false; // 실패 반환
            }

            Debug.Log($"{sourceLabel}: {definition.DisplayName} @ {spawn.Position} / 현재 적={CountCurrentEnemies(_runState.Board)}"); // 정상 등장 결과 출력
            RoundStateChanged?.Invoke(); // UI가 즉시 현재 적 수를 갱신하도록 통지
            return true; // 정상 생성 성공
        }

        private PieceDefinition FindPieceDefinition(string pieceId) // PlayerStartingDeck26의 26개 참조에서 PieceId로 실제 기물 정의를 찾는 메서드
        {
            if (_pieceCatalog == null || string.IsNullOrWhiteSpace(pieceId)) return null; // 카탈로그나 id가 없으면 찾을 수 없음

            for (int i = 0; i < _pieceCatalog.Cards.Count; i++) // 26종 시작 덱 카탈로그 순회
            {
                var definition = _pieceCatalog.Cards[i]; // 현재 PieceDefinition 참조
                if (definition == null) continue; // 빈 참조 건너뜀

                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) // PieceId가 일치하면
                {
                    return definition; // 실제 PieceDefinition 반환
                }
            }

            return null; // 끝까지 찾지 못하면 null 반환
        }

        private void EnsureSummaryUI() // 사용자 요청의 상단 중앙 라운드 정보를 한 번만 생성하고 연결하는 메서드
        {
            if (_summaryUI == null) // 아직 UI 참조가 없다면
            {
                _summaryUI = GetComponent<RoundSummaryUI>(); // 같은 라운드 관리자 오브젝트에 기존 컴포넌트가 있는지 확인
            }

            if (_summaryUI == null) // 기존 컴포넌트가 없다면
            {
                _summaryUI = gameObject.AddComponent<RoundSummaryUI>(); // 런타임 Canvas UI 컴포넌트 자동 추가
            }

            _summaryUI.Bind(this, _turnManager, _runState); // 현재 라운드·턴·보드 상태를 UI에 연결
        }

        public static int CountCurrentEnemies(BoardState board) // 현재 보드의 살아 있는 적 런타임 기물 수를 중복 없이 계산하는 공통 메서드
        {
            if (board == null) return 0; // 보드가 없으면 적 0기

            var uniqueEnemies = new HashSet<PieceRuntimeState>(); // 향후 2x2 보스가 여러 타일을 점유해도 1기로 세기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 타일 점유 기물 조회
                    if (piece == null || piece.IsPlayerPiece || piece.IsDead) continue; // 빈 칸·플레이어·사망 기물 제외
                    uniqueEnemies.Add(piece); // 동일 런타임 기물은 한 번만 집합에 등록
                }
            }

            return uniqueEnemies.Count; // 현재 생존 적 기물 수 반환
        }

        private void OnDestroy() // 씬 종료나 오브젝트 파괴 시 이벤트 구독을 정리하는 메서드
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // TurnManager 이벤트 구독 해제
        }
    }
}
