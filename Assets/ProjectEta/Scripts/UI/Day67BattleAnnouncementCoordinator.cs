using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성 판정
using ProjectEta.Battle; // BattleController·TurnManager 사용
using ProjectEta.Boss; // BossPhaseStatusUI 사용
using ProjectEta.King; // KingRunStateService 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.UI
{
    [DefaultExecutionOrder(1580)]
    public sealed class Day67BattleAnnouncementCoordinator : MonoBehaviour
    {
        private BattleController _battleController; // 현재 전투 컨트롤러
        private TurnManager _turnManager; // 현재 턴 매니저
        private RunState _runState; // 현재 런 상태
        private Day67BattleAnnouncementUI _announcementUI; // 공통 전투 알림 UI
        private RunFlowPhase _lastFlowPhase; // 이전 상위 흐름 단계
        private bool _hasFlowPhase; // 최초 흐름 기준 생성 여부
        private int _lastBattleStartRound = -1; // 전투 시작 알림 중복 방지 라운드
        private bool _passiveSeeded; // 킹 패시브 이전 상태 초기화 여부
        private int _lastRageStacks; // 이전 공격형 격노 스택
        private bool _lastBarrierActive; // 이전 방어형 방벽 활성 상태
        private bool _lastStrategyPending; // 이전 전략형 준비 상태
        private bool _bossPhase2Shown; // 현재 전투 보스 2페이즈 알림 표시 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<Day67BattleAnnouncementCoordinator>() != null) return; // 중복 관리자 생성 차단

            GameObject host = new GameObject("Day67BattleAnnouncementCoordinator"); // 67일차 전투 알림 호스트 생성
            host.AddComponent<Day67BattleAnnouncementUI>(); // 공통 알림 Canvas 추가
            host.AddComponent<Day67BattleAnnouncementCoordinator>(); // 상태 연결 관리자 추가
        }

        private void Awake()
        {
            _announcementUI = GetComponent<Day67BattleAnnouncementUI>(); // 같은 호스트 알림 UI 연결
        }

        private void Update()
        {
            ResolveBindings(); // 전투·런·턴 참조 최신화
            if (_runState == null || _announcementUI == null) return; // 필수 상태 준비 전 종료

            RefreshFlowPhase(); // Map·Completed·Failed 등 상위 흐름 변화 알림
            PollKingPassive(); // 실제 킹 패시브 상태 변화 감지
            PollBossPhase(); // 실제 보스 Phase 2 UI 활성 상태 감지
        }

        private void ResolveBindings()
        {
            if (_battleController == null) _battleController = Object.FindFirstObjectByType<BattleController>(); // BattleController 지연 탐색
            if (_battleController == null || _battleController.RunState == null || _battleController.TurnManager == null) return; // 핵심 상태 준비 전 대기

            if (_runState != _battleController.RunState)
            {
                _runState = _battleController.RunState; // 새 RunState 연결
                _hasFlowPhase = false; // 새 런 흐름 기준 재설정
                _lastBattleStartRound = -1; // 새 런 전투 시작 알림 허용
                ResetBattleScopedPresentation(); // 이전 전투 패시브·보스 표시 기준 제거
            }

            if (_turnManager != _battleController.TurnManager)
            {
                if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 이전 턴 이벤트 해제
                _turnManager = _battleController.TurnManager; // 새 턴 매니저 연결
                _turnManager.TurnChanged += HandleTurnChanged; // 턴 전환 알림 구독
                SeedCurrentBattlePresentation(); // 현재 전투 첫 화면 알림 구성
            }
        }

        private void RefreshFlowPhase()
        {
            RunFlowPhase currentPhase = _runState.CurrentFlowPhase; // 현재 상위 진행 단계 조회

            if (!_hasFlowPhase)
            {
                _lastFlowPhase = currentPhase; // 최초 흐름 기준 저장
                _hasFlowPhase = true; // 기준 생성 완료
                if (currentPhase == RunFlowPhase.Battle) EnsureBattleStartAnnouncement(); // 씬 시작 전투 알림 보장
                return; // 최초 프레임 전환 판정 생략
            }

            if (currentPhase == _lastFlowPhase) return; // 흐름 변화 없음 처리 생략

            RunFlowPhase previousPhase = _lastFlowPhase; // 이전 흐름 저장
            _lastFlowPhase = currentPhase; // 새 흐름 기준 갱신

            if (currentPhase == RunFlowPhase.Battle)
            {
                ResetBattleScopedPresentation(); // 새 전투 패시브·보스 표시 기준 초기화
                EnsureBattleStartAnnouncement(); // 새 스테이지 전투 시작 알림
                return; // 전투 진입 처리 완료
            }

            if (currentPhase == RunFlowPhase.Completed)
            {
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreateRunResult(true)); // 전체 런 완료 알림 등록
                return; // 런 완료 처리 종료
            }

            if (currentPhase == RunFlowPhase.Failed)
            {
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreateRunResult(false)); // 전체 런 실패 알림 등록
                return; // 런 실패 처리 종료
            }

            if (previousPhase == RunFlowPhase.Battle)
            {
                _bossPhase2Shown = false; // 전투 이탈 시 보스 페이즈 기준 초기화
            }
        }

        private void SeedCurrentBattlePresentation()
        {
            if (_runState == null || _turnManager == null) return; // 런·턴 누락 방어
            if (_runState.CurrentFlowPhase != RunFlowPhase.Battle) return; // 전투 외 초기 알림 차단

            EnsureBattleStartAnnouncement(); // 현재 스테이지 전투 시작 알림 보장
            Day67BattleAnnouncement turnAnnouncement = Day67BattleAnnouncement.CreateTurn(_turnManager.CurrentState, _turnManager.TurnNumber, _turnManager.IsInitialDeployment); // 현재 턴 알림 생성
            _announcementUI.Enqueue(turnAnnouncement); // 배치·플레이어·적 턴 표시
        }

        private void EnsureBattleStartAnnouncement()
        {
            if (_runState == null || _announcementUI == null) return; // 필수 상태 누락 방어
            if (_lastBattleStartRound == _runState.CurrentRound) return; // 같은 스테이지 시작 알림 중복 차단

            _lastBattleStartRound = _runState.CurrentRound; // 현재 스테이지 시작 표시 기록
            _announcementUI.Enqueue(Day67BattleAnnouncement.CreateBattleStart(_runState.CurrentRound, _runState.IsBossRound)); // 일반·보스 전투 시작 알림 등록
        }

        private void HandleTurnChanged(TurnState state, int turnNumber)
        {
            if (_runState == null || _announcementUI == null || _turnManager == null) return; // 필수 상태 누락 방어

            if (state == TurnState.BattleEnded)
            {
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreateBattleResult(_turnManager.Outcome)); // 승리·패배 결과 알림 등록
                return; // 종료 상태 일반 턴 알림 제외
            }

            if (_turnManager.IsInitialDeployment && state == TurnState.DeploymentTurn)
            {
                EnsureBattleStartAnnouncement(); // 새 전투 초기 배치에서 시작 알림 보장
                ResetBattleScopedPresentation(); // 새 전투 패시브·보스 기준 초기화
            }

            Day67BattleAnnouncement announcement = Day67BattleAnnouncement.CreateTurn(state, turnNumber, _turnManager.IsInitialDeployment); // 현재 턴 문구 생성
            _announcementUI.Enqueue(announcement); // 순차 턴 알림 등록
        }

        private void PollKingPassive()
        {
            if (_runState.CurrentFlowPhase != RunFlowPhase.Battle) return; // 전투 외 패시브 표시 차단
            if (!KingRunStateService.TryGet(_runState, out KingRunState state) || state == null) return; // 선택 킹 런 상태 누락 방어

            if (!_passiveSeeded)
            {
                _lastRageStacks = state.RageStacks; // 최초 격노 기준 저장
                _lastBarrierActive = state.BarrierActive; // 최초 방벽 기준 저장
                _lastStrategyPending = state.StrategyPreparationPending; // 최초 전략 준비 기준 저장
                _passiveSeeded = true; // 이후 변화부터 알림 허용
                return; // 초기 상태를 발동으로 오인하지 않음
            }

            if (state.RageStacks > _lastRageStacks)
            {
                int gained = state.RageStacks - _lastRageStacks; // 이번 프레임 신규 격노 수 계산
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreatePassive($"격노 +{gained}")); // 공격형 패시브 발동 표시
            }

            if (state.BarrierActive && !_lastBarrierActive)
            {
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreatePassive("왕의 요새 · 방벽 활성")); // 방어형 패시브 발동 표시
            }

            if (state.StrategyPreparationPending && !_lastStrategyPending)
            {
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreatePassive("전술적 준비")); // 전략형 패시브 발동 표시
            }

            _lastRageStacks = state.RageStacks; // 최신 격노 기준 갱신
            _lastBarrierActive = state.BarrierActive; // 최신 방벽 기준 갱신
            _lastStrategyPending = state.StrategyPreparationPending; // 최신 전략 준비 기준 갱신
        }

        private void PollBossPhase()
        {
            if (_runState.CurrentFlowPhase != RunFlowPhase.Battle) return; // 전투 외 보스 페이즈 표시 차단

            BossPhaseStatusUI statusUI = Object.FindFirstObjectByType<BossPhaseStatusUI>(); // 현재 보스 페이즈 UI 탐색
            if (statusUI == null) return; // 일반 전투·보스 시스템 미생성 시 종료

            Transform canvasTransform = statusUI.transform.Find("BossPhaseStatusCanvas"); // 실제 Phase 2 Canvas 조회
            Canvas phaseCanvas = canvasTransform != null ? canvasTransform.GetComponent<Canvas>() : null; // 활성 상태 판정용 Canvas 확보
            bool phase2Active = phaseCanvas != null && phaseCanvas.enabled && statusUI.DisplayText.StartsWith("BOSS PHASE 2", System.StringComparison.Ordinal); // 실제 Phase 2 표시 여부 계산

            if (phase2Active && !_bossPhase2Shown)
            {
                _bossPhase2Shown = true; // 현재 전투 2페이즈 알림 표시 기록
                _announcementUI.Enqueue(Day67BattleAnnouncement.CreateBossPhase2()); // 보스 2페이즈 경고 등록
            }
        }

        private void ResetBattleScopedPresentation()
        {
            _passiveSeeded = false; // 다음 킹 상태를 새 기준으로 사용
            _lastRageStacks = 0; // 격노 비교 기준 초기화
            _lastBarrierActive = false; // 방벽 비교 기준 초기화
            _lastStrategyPending = false; // 전략 준비 비교 기준 초기화
            _bossPhase2Shown = false; // 새 전투 보스 2페이즈 알림 허용
        }

        private void OnDestroy()
        {
            if (_turnManager != null) _turnManager.TurnChanged -= HandleTurnChanged; // 오브젝트 제거 시 턴 이벤트 해제
        }
    }
}
