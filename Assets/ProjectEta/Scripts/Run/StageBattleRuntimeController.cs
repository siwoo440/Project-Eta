using System; // StringComparison 사용
using System.Collections.Generic; // HashSet<T>·List<T> 사용
using System.Reflection; // 기존 BattleController 턴 제한 필드 호환 사용
using UnityEngine; // MonoBehaviour·Resources·Vector2Int 사용
using ProjectEta.Battle; // BattleController·TurnManager·TurnState 사용
using ProjectEta.Board; // BoardInputController·BoardState·BoardView 사용
using ProjectEta.Boss; // 대형 보스 점유·시각 유틸리티 사용
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용
using ProjectEta.Round; // RoundDefinition·EnemySpawnDefinition 사용

namespace ProjectEta.Run // 선택 스테이지 전투 런타임 네임스페이스
{
    public sealed class StageBattleRuntimeController : MonoBehaviour // StageDefinition의 RoundDefinition을 현재 새 BattleState에 적용하는 런타임
    {
        private const string PieceCatalogResourceName = "PlayerStartingDeck26"; // 26종 PieceDefinition 카탈로그 Resources 이름
        private readonly HashSet<int> _processedReinforcementIndices = new HashSet<int>(); // 이번 스테이지에서 처리한 증원 인덱스
        private BattleController _battleController; // 실제 전투 상태 소유자
        private BoardInputController _boardInputController; // 적 PieceView 생성 진입점
        private BoardView _boardView; // 보스 시각 보정용 보드 뷰
        private RunState _runState; // 현재 런·새 BattleState
        private TurnManager _turnManager; // 현재 전투 턴 상태
        private StageDefinition _stageDefinition; // 선택한 실제 스테이지 설정
        private RoundDefinition _roundDefinition; // 스테이지가 재사용하는 기존 라운드 설정
        private PlayerStartingDeckCatalog _pieceCatalog; // PieceId→PieceDefinition 조회 카탈로그
        private bool _configured; // 중복 설정 방지 상태

        public StageDefinition StageDefinition => _stageDefinition; // 현재 적용 중인 StageDefinition 공개
        public RoundDefinition RoundDefinition => _roundDefinition; // 현재 적용 중인 RoundDefinition 공개
        public int TurnLimit => _roundDefinition != null ? _roundDefinition.TurnLimit : 30; // 현재 스테이지 턴 제한 공개

        public bool Configure(BattleController battleController, StageDefinition stageDefinition) // 선택한 스테이지를 현재 새 BattleState에 실제 적용
        {
            if (_configured) return false; // 같은 런타임 중복 구성 차단
            if (battleController == null || stageDefinition == null || !stageDefinition.RequiresBattle) return false; // 전투형 필수 입력 검사

            _battleController = battleController; // 전투 컨트롤러 저장
            _boardInputController = UnityEngine.Object.FindFirstObjectByType<BoardInputController>(); // 현재 입력 컨트롤러 탐색
            _boardView = UnityEngine.Object.FindFirstObjectByType<BoardView>(); // 현재 보드 뷰 탐색
            _runState = _battleController.RunState; // 새 BattleState를 가진 런 상태 저장
            _turnManager = _battleController.TurnManager; // 재사용 TurnManager 저장
            _stageDefinition = stageDefinition; // 선택 StageDefinition 저장
            _roundDefinition = stageDefinition.RoundDefinition; // 연결된 기존 RoundDefinition 저장
            _pieceCatalog = Resources.Load<PlayerStartingDeckCatalog>(PieceCatalogResourceName); // PieceDefinition 카탈로그 로드

            if (_runState == null || _turnManager == null || _boardInputController == null || _boardView == null) // 필수 런타임 객체 확인
            {
                Debug.LogError("73일차 StageBattleRuntimeController 구성 실패: RunState·TurnManager·BoardInputController·BoardView를 확인하세요."); // 누락 객체 기록
                return false; // 구성 실패 반환
            }

            if (_roundDefinition == null) // 전투형 스테이지에 RoundDefinition이 없으면
            {
                Debug.LogError($"73일차 스테이지 전투 구성 실패: {stageDefinition.StageId}의 RoundDefinition을 찾지 못했습니다."); // 데이터 누락 기록
                return false; // 구성 실패 반환
            }

            if (_pieceCatalog == null) // 카탈로그 리소스가 없으면
            {
                Debug.LogError($"73일차 스테이지 전투 구성 실패: Resources/{PieceCatalogResourceName}를 찾지 못했습니다."); // 카탈로그 누락 기록
                return false; // 구성 실패 반환
            }

            ApplyTurnLimit(); // StageDefinition의 RoundDefinition 턴 제한을 기존 BattleController에 적용

            if (_stageDefinition.StageType == StageType.Battle || _stageDefinition.StageType == StageType.Elite)
            {
                SpawnGeneratedEncounter(); // 일반·Elite 전투는 Seed 기반 Encounter 적용
            }
            else
            {
                SpawnInitialEnemies(); // Boss 전투는 기존 수동 적 편성 유지
            }

            EnsureConfiguredBoss(); // 보스 스테이지면 2×2 보스 적용
            BindTurnEvents(); // 증원·킹 HP 동기화 이벤트 연결
            _configured = true; // 구성 완료 기록

            Debug.Log($"73일차 스테이지 전투 적용: {_stageDefinition.DisplayName} / Type={_stageDefinition.StageType} / TurnLimit={TurnLimit} / Enemy={CountCurrentEnemies(_runState.Board)}"); // 적용 결과 기록
            return true; // 정상 구성 반환
        }

        private void ApplyTurnLimit() // 기존 BattleController private 테스트 턴 제한과 현재 StageDefinition 동기화
        {
            var field = typeof(BattleController).GetField("_turnLimitTestValue", BindingFlags.Instance | BindingFlags.NonPublic); // 기존 private 턴 제한 필드 탐색
            if (field != null) field.SetValue(_battleController, TurnLimit); // 찾은 경우 현재 RoundDefinition 턴 제한 적용
        }

        private void SpawnInitialEnemies() // 현재 RoundDefinition의 시작 적을 새 보드에 생성
        {
            for (int i = 0; i < _roundDefinition.InitialEnemies.Count; i++) // 시작 적 목록 순회
            {
                EnemySpawnDefinition spawn = _roundDefinition.InitialEnemies[i]; // 현재 스폰 데이터 조회
                if (spawn == null) continue; // 빈 항목 제외
                TrySpawnEnemy(spawn, "스테이지 시작 적"); // 기존 PieceView 스폰 경로 재사용
            }
        }

        private void SpawnGeneratedEncounter() // 일반·Elite 전투의 적 편성을 진행도와 Seed 기반으로 생성
        {
            List<PieceDefinition> sourcePool = BuildEnemySourcePool(); // 현재 적 후보 Pool 생성
            int phase = RunPhaseProgressService.GetCurrentPhase(_runState); // 현재 RouteMap Phase 조회
            int stage = Mathf.Clamp(_runState.CurrentRound, RoundState.FirstRound, RoundState.FinalRound); // 현재 Stage 번호 보정
            int mapSeed = _runState.RouteMap != null ? _runState.RouteMap.MapSeed : 0; // 현재 RouteMap Seed 조회
            string nodeId = _runState.RouteMap != null ? _runState.RouteMap.CurrentNodeId : string.Empty; // 현재 RouteMap 노드 ID 조회

            EnemyEncounterResult encounter = EnemyEncounterGenerator.Generate(
                sourcePool,
                _roundDefinition,
                _stageDefinition.StageType,
                mapSeed,
                phase,
                stage,
                nodeId); // 일반·Elite Encounter 생성

            IReadOnlyList<EnemyEncounterContentIssue> issues = EnemyEncounterContentValidator.Validate(encounter); // Encounter 무결성 검사
            if (issues.Count > 0)
            {
                Debug.LogWarning($"73일차 Encounter 검증 실패: Issues={issues.Count} / Type={_stageDefinition.StageType} / Node={nodeId}"); // 검증 실패 기록
                SpawnInitialEnemies(); // 기존 RoundDefinition 적 편성으로 안전 대체
                return; // 생성 Encounter 적용 중단
            }

            int spawned = 0; // 실제 생성 적 수 초기화
            for (int i = 0; i < encounter.Spawns.Count; i++)
            {
                EnemyEncounterSpawn spawn = encounter.Spawns[i]; // 현재 Encounter 배치 조회
                if (spawn == null || spawn.Piece == null) continue; // 빈 배치 제외

                PieceRuntimeState runtimePiece = _boardInputController.SpawnTestEnemy(spawn.Piece, spawn.Position); // 기존 보드·PieceView 생성 경로 사용
                if (runtimePiece == null) continue; // 점유·경계 실패 제외
                spawned++; // 실제 생성 수 누적
                Debug.Log($"73일차 Encounter 적: {spawn.Piece.DisplayName} @ {spawn.Position}"); // 생성 결과 기록
            }

            if (spawned <= 0)
            {
                Debug.LogWarning("73일차 Encounter 적 생성 실패: 기존 RoundDefinition 편성으로 대체합니다."); // 전체 생성 실패 기록
                SpawnInitialEnemies(); // 기존 편성으로 안전 대체
                return; // 후속 로그 생략
            }

            Debug.Log($"73일차 Enemy Encounter: Type={_stageDefinition.StageType} / Phase={phase} / Stage={stage} / Seed={encounter.Seed} / Requested={encounter.Spawns.Count} / Spawned={spawned} / Threat={encounter.ThreatScore}"); // Encounter 재현 정보 기록
        }

        private List<PieceDefinition> BuildEnemySourcePool() // 카탈로그·Resources에서 일반 적 후보 수집
        {
            var pool = new List<PieceDefinition>(); // 적 후보 Pool 생성

            for (int i = 0; i < _pieceCatalog.Cards.Count; i++)
            {
                AddEnemyCandidate(pool, _pieceCatalog.Cards[i]); // 시작 덱 카탈로그 후보 등록
            }

            PieceDefinition[] resources = Resources.LoadAll<PieceDefinition>(string.Empty); // 전체 PieceDefinition Resources 조회
            for (int i = 0; i < resources.Length; i++)
            {
                AddEnemyCandidate(pool, resources[i]); // 독립 적 기물 후보 등록
            }

            return pool; // 일반 적 후보 Pool 반환
        }

        private static void AddEnemyCandidate(List<PieceDefinition> pool, PieceDefinition definition) // 일반 Enemy Pool 후보 등록
        {
            if (pool == null || !EnemyEncounterRules.CanUsePiece(definition)) return; // 빈 Pool·금지 기물 제외
            if (pool.Contains(definition)) return; // 같은 에셋 중복 제외
            pool.Add(definition); // 일반 적 후보 등록
        }

        private void EnsureConfiguredBoss() // MidBoss·FinalBoss RoundDefinition의 대형 보스 실제 생성
        {
            if (!_roundDefinition.HasBossConfiguration) return; // 보스 설정 없는 일반 전투 제외

            PieceDefinition bossDefinition = Resources.Load<PieceDefinition>(_roundDefinition.BossResourceName); // 보스 PieceDefinition 로드
            if (bossDefinition == null) // 보스 리소스 누락 확인
            {
                Debug.LogError($"45일차 보스 생성 실패: Resources/{_roundDefinition.BossResourceName}를 찾지 못했습니다."); // 보스 리소스 누락 기록
                return; // 보스 생성 중단
            }

            Vector2Int anchor = _roundDefinition.BossAnchor; // 기존 RoundDefinition 보스 기준 좌표 사용
            if (!LargePieceBoardUtility.CanPlace(_runState.Board, bossDefinition, anchor)) // 전체 점유 영역 충돌 검사
            {
                Debug.LogWarning($"45일차 보스 배치 실패: {bossDefinition.DisplayName} @ {anchor}"); // 배치 실패 기록
                return; // 기존 기물 보호
            }

            PieceRuntimeState boss = _boardInputController.SpawnTestEnemy(bossDefinition, anchor); // 기존 1칸 기준 스폰 실행
            if (boss == null) return; // 기준 칸 생성 실패 처리

            if (!LargePieceBoardUtility.ExpandExistingAnchorOccupancy(_runState.Board, boss)) // OccupancySize 전체 점유 확장
            {
                _runState.Board.ClearPiece(boss); // 실패 시 부분 점유 정리
                Debug.LogError($"45일차 보스 점유 확장 실패: {bossDefinition.DisplayName} @ {anchor}"); // 원자성 실패 기록
                return; // 보스 구성 종료
            }

            PieceView bossView = LargePieceVisualUtility.FindPieceView(boss); // 생성된 보스 화면 오브젝트 조회
            if (bossView != null) LargePieceVisualUtility.ApplyFootprint(bossView, _boardView.TileSize); // 2×2 중앙·스케일·콜라이더 적용
        }

        private void BindTurnEvents() // 새 스테이지 턴 이벤트 연결
        {
            _turnManager.TurnChanged -= HandleTurnChanged; // 중복 이벤트 구독 제거
            _turnManager.TurnChanged += HandleTurnChanged; // 증원·킹 HP 동기화 이벤트 구독
        }

        private void HandleTurnChanged(TurnState state, int turnNumber) // 새 전투 턴 변경 후 스테이지 후속 처리
        {
            if (state == TurnState.PlayerTurn) ProcessDueReinforcements(turnNumber); // 플레이어 일반 턴 시작 시 도달한 증원 처리
            if (state == TurnState.DeploymentTurn && _turnManager.IsInitialKingPlaced) SyncPlayerKingHp(); // 시작 킹 배치 직후 런 HP를 새 기물에 동기화
        }

        private void ProcessDueReinforcements(int currentTurn) // RoundDefinition의 지정 턴 증원 한 번씩 처리
        {
            for (int i = 0; i < _roundDefinition.Reinforcements.Count; i++) // 증원 목록 순회
            {
                if (_processedReinforcementIndices.Contains(i)) continue; // 이미 처리한 증원 제외
                EnemySpawnDefinition spawn = _roundDefinition.Reinforcements[i]; // 현재 증원 데이터 조회
                if (spawn == null) // 빈 데이터 처리
                {
                    _processedReinforcementIndices.Add(i); // 반복 방지 완료 표시
                    continue; // 다음 항목 이동
                }

                if (!spawn.IsDue(currentTurn)) continue; // 아직 지정 턴 전이면 대기
                TrySpawnEnemy(spawn, $"Turn {currentTurn} 스테이지 증원"); // 현재 보드에 실제 증원 생성
                _processedReinforcementIndices.Add(i); // 성공·실패 모두 한 번만 처리
            }
        }

        private bool TrySpawnEnemy(EnemySpawnDefinition spawn, string sourceLabel) // PieceId 기반 일반 적 스폰
        {
            PieceDefinition definition = FindPieceDefinition(spawn.PieceId); // 카탈로그에서 기물 데이터 조회
            if (definition == null) // 미등록 PieceId 처리
            {
                Debug.LogWarning($"{sourceLabel} 실패: PieceId '{spawn.PieceId}'를 찾지 못했습니다."); // 데이터 오류 기록
                return false; // 생성 실패 반환
            }

            PieceRuntimeState runtimePiece = _boardInputController.SpawnTestEnemy(definition, spawn.Position); // 기존 보드·PieceView 생성 경로 사용
            if (runtimePiece == null) return false; // 점유·경계 실패 반환
            Debug.Log($"{sourceLabel}: {definition.DisplayName} @ {spawn.Position}"); // 정상 생성 기록
            return true; // 생성 성공 반환
        }

        private PieceDefinition FindPieceDefinition(string pieceId) // PlayerStartingDeck26와 Resources에서 PieceId 조회
        {
            if (string.IsNullOrWhiteSpace(pieceId)) return null; // 빈 PieceId 차단

            for (int i = 0; i < _pieceCatalog.Cards.Count; i++) // 카탈로그 순회
            {
                PieceDefinition definition = _pieceCatalog.Cards[i]; // 현재 기물 데이터 조회
                if (definition == null) continue; // 빈 항목 제외
                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) return definition; // ID 일치 기물 반환
            }

            PieceDefinition[] resources = Resources.LoadAll<PieceDefinition>(string.Empty); // 독립 Resources 기물 fallback 조회
            for (int i = 0; i < resources.Length; i++) // 전체 기물 리소스 순회
            {
                PieceDefinition definition = resources[i]; // 현재 리소스 조회
                if (definition == null) continue; // 빈 리소스 제외
                if (string.Equals(definition.PieceId, pieceId, StringComparison.OrdinalIgnoreCase)) return definition; // ID 일치 리소스 반환
            }

            return null; // 일치 기물 없음
        }

        private void SyncPlayerKingHp() // 새 전투에 다시 배치한 킹 기물 HP를 런 전체 KingHp와 동기화
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    PieceRuntimeState piece = _runState.Board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.Definition == null) continue; // 빈 칸·적·정의 누락 제외
                    if (piece.Definition.MovementType != PieceMovementType.King) continue; // 킹 외 기물 제외
                    piece.CurrentHp = _runState.KingHp; // 런 전체 킹 체력을 새 전투 기물에 적용
                    Debug.Log($"45일차 킹 HP 동기화: {piece.CurrentHp}"); // 동기화 결과 기록
                    return; // 플레이어 킹 한 기 처리 후 종료
                }
            }
        }

        private static int CountCurrentEnemies(BoardState board) // 대형 기물 중복 없이 현재 적 기물 수 계산
        {
            if (board == null) return 0; // 보드 누락 처리
            var uniqueEnemies = new HashSet<PieceRuntimeState>(); // 동일 런타임 기물 중복 방지 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    PieceRuntimeState piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || piece.IsPlayerPiece || piece.IsDead) continue; // 빈 칸·아군·사망 제외
                    uniqueEnemies.Add(piece); // 같은 대형 기물은 한 번만 등록
                }
            }

            return uniqueEnemies.Count; // 실제 적 기물 수 반환
        }

        private void OnDestroy() // 스테이지 전투 런타임 제거 시 이벤트 정리
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 턴 이벤트 구독 해제
        }
    }
}
