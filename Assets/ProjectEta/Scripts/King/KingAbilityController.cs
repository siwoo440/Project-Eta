using UnityEngine; // MonoBehaviour·GameObject 사용
using UnityEngine.SceneManagement; // Battle 씬 자동 생성
using ProjectEta.Battle; // BattleController·BattleHooks·TurnManager 사용
using ProjectEta.Board; // BoardInputController 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용
using ProjectEta.Run; // RunState·RunFlowPhase 사용

namespace ProjectEta.King
{
    [DefaultExecutionOrder(1060)]
    public sealed class KingAbilityController : MonoBehaviour
    {
        private BattleController _battleController; // 현재 전투 컨트롤러
        private RunState _runState; // 현재 런 상태
        private KingRunState _kingState; // 현재 런 킹 상태
        private BattleHooks _boundHooks; // 현재 구독 전투 훅
        private TurnManager _boundTurnManager; // 현재 구독 턴 매니저
        private StrategyKingSelectionUI _strategySelectionUI; // 전략형 전술적 준비 카드 선택 UI
        private RunFlowPhase _previousFlowPhase; // 직전 런 진행 단계
        private bool _hasPreviousFlowPhase; // 직전 진행 단계 존재 여부

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateForBattleScene()
        {
            if (SceneManager.GetActiveScene().name != "Battle") return; // Battle 씬 외 생성 차단
            if (Object.FindFirstObjectByType<KingAbilityController>() != null) return; // 중복 킹 능력 관리자 차단

            var host = new GameObject("KingAbilityController_Day49"); // 49일차 호스트 이름 유지
            host.AddComponent<KingAbilityController>(); // 공통 킹 능력 훅 관리자 추가
            host.AddComponent<KingSelectionUI>(); // 첫 런 킹 선택·상태 UI 추가
            host.AddComponent<StrategyKingSelectionUI>(); // 50일차 전략형 카드 선택 UI 추가
        }

        private void Update()
        {
            ResolveBattleController(); // 현재 BattleController 탐색
            RebindRunStateIfNeeded(); // RunState 변경 대응
            RebindHooksIfNeeded(); // BattleHooks 변경 대응
            RebindTurnManagerIfNeeded(); // TurnManager 변경 대응
            ResolveStrategySelectionUI(); // 전략형 카드 선택 UI 참조 보장
            TryStartStrategyPreparation(); // 배치 턴 전략형 패시브 자동 발동
            TrackRunFlow(); // 전투 종료 시 전투 한정 킹 상태 초기화
        }

        private void ResolveBattleController()
        {
            if (_battleController != null) return; // 기존 전투 컨트롤러 재사용
            _battleController = Object.FindFirstObjectByType<BattleController>(); // Battle 씬 전투 컨트롤러 탐색
        }

        private void ResolveStrategySelectionUI()
        {
            if (_strategySelectionUI != null) return; // 기존 전략형 선택 UI 재사용
            _strategySelectionUI = GetComponent<StrategyKingSelectionUI>(); // 같은 호스트에서 전략형 선택 UI 탐색
            if (_strategySelectionUI == null) _strategySelectionUI = gameObject.AddComponent<StrategyKingSelectionUI>(); // 누락 시 자동 추가
        }

        private void RebindRunStateIfNeeded()
        {
            RunState current = _battleController != null ? _battleController.RunState : null; // 현재 BattleController 런 조회
            if (current == _runState) return; // 동일 RunState 재연결 차단

            CancelStrategyPreparation(); // 이전 런 전략형 선택 대기 정리
            _runState = current; // 현재 런 상태 교체
            _kingState = KingRunStateService.Get(_runState); // 런 전체 킹 상태 연결
            _hasPreviousFlowPhase = false; // 새 런 진행 단계 추적 초기화
        }

        private void RebindHooksIfNeeded()
        {
            BattleHooks current = _battleController != null ? _battleController.BattleHooks : null; // 현재 전투 훅 조회
            if (current == _boundHooks) return; // 동일 훅 재구독 차단

            UnsubscribeHooks(); // 이전 전투 훅 구독 해제
            _boundHooks = current; // 새 전투 훅 저장

            if (_boundHooks == null) return; // 훅 누락 상태 종료
            _boundHooks.BeforeDamage += HandleBeforeDamage; // 공격형·방어형 피해 전 패시브 연결
            _boundHooks.AfterAttack += HandleAfterAttack; // 공격형 직접 처치 격노 연결
            _boundHooks.AfterMove += HandleAfterMove; // 방어형 킹 이동 여부 추적 연결
        }

        private void RebindTurnManagerIfNeeded()
        {
            TurnManager current = _battleController != null ? _battleController.TurnManager : null; // 현재 턴 매니저 조회
            if (current == _boundTurnManager) return; // 동일 턴 매니저 재구독 차단

            UnsubscribeTurnManager(); // 이전 턴 매니저 구독 해제
            _boundTurnManager = current; // 새 턴 매니저 저장

            if (_boundTurnManager == null) return; // 턴 매니저 누락 상태 종료
            _boundTurnManager.TurnChanged += HandleTurnChanged; // 방어형 턴 시작·종료 조건 추적 연결
        }

        private void HandleBeforeDamage(DamageContext context)
        {
            int attackBonus = AttackKingAbility.ApplyBeforeDamage(_kingState, context); // 공격형 킹 격노 피해 적용
            if (attackBonus > 0) Debug.Log($"공격형 킹 처형의 연쇄: 격노 {attackBonus}스택 소비 / 피해 +{attackBonus}"); // 공격형 격노 소비 결과 출력

            bool barrierConsumed = DefenseKingAbility.ApplyBeforeDamage(_kingState, context, out int reducedAmount); // 방어형 킹 방벽 피해 경감 적용
            if (barrierConsumed) Debug.Log($"방어형 킹 왕의 요새: 방벽 소비 / 피해 -{reducedAmount} / 최종 피해 {context.Amount}"); // 방어형 방벽 소비 결과 출력
        }

        private void HandleAfterAttack(CombatResult result)
        {
            bool granted = AttackKingAbility.HandleAfterAttack(_kingState, result); // 공격형 킹 직접 처치 격노 획득 처리
            if (granted) Debug.Log($"공격형 킹 처형의 연쇄: 격노 {_kingState.RageStacks}/{KingRunState.AttackRageMaxStacks}"); // 격노 획득 결과 출력
        }

        private void HandleAfterMove(PieceRuntimeState piece, Vector2Int origin, Vector2Int destination)
        {
            bool moved = DefenseKingAbility.HandleAfterMove(_kingState, piece, origin, destination); // 방어형 킹 현재 턴 이동 기록
            if (moved) Debug.Log("방어형 킹 왕의 요새: 이번 플레이어 턴 킹 이동 감지"); // 이동으로 신규 방벽 획득이 막힘을 출력
        }

        private void HandleTurnChanged(TurnState state, int turnNumber)
        {
            if (_kingState == null) return; // 킹 상태 준비 전 턴 이벤트 무시

            if (state == TurnState.PlayerTurn)
            {
                DefenseKingAbility.BeginPlayerTurn(_kingState); // 새 플레이어 턴 방어형 이동 기록 초기화
                return; // PlayerTurn 처리 종료
            }

            if (state == TurnState.EnemyTurn)
            {
                bool gained = DefenseKingAbility.HandleEnemyTurnStarting(_kingState); // 적 턴 직전 방어형 정지 여부 판정
                if (gained) Debug.Log("방어형 킹 왕의 요새: 방벽 획득"); // 신규 방벽 획득 결과 출력
            }
        }

        private void TryStartStrategyPreparation()
        {
            if (_runState == null || _kingState == null || _boundTurnManager == null) return; // 필수 런·턴 상태 누락 방어
            if (_runState.CurrentFlowPhase != RunFlowPhase.Battle) return; // 전투 외 지도·상점·이벤트에서 발동 차단
            if (_boundTurnManager.CurrentState != TurnState.DeploymentTurn) return; // 배치 턴 외 발동 차단
            if (_kingState.Archetype != KingArchetype.Strategy) return; // 전략형 킹 외 발동 차단
            if (_kingState.StrategyPreparationPending) return; // 이미 카드 선택 중이면 중복 발동 차단
            if (_kingState.StrategyPreparationTurnNumber == _boundTurnManager.TurnNumber) return; // 같은 배치 턴 재발동 차단

            var candidates = StrategyKingAbility.BuildCandidates(_kingState, _runState.Deck, _runState.Hand); // 덱 위 최대 3장 후보 생성

            if (candidates.Count == 0)
            {
                _kingState.MarkStrategyPreparationSkipped(_boundTurnManager.TurnNumber); // 손패 Full·덱 비어 있음 상태 해당 턴 처리 완료
                return; // 선택 UI 없이 종료
            }

            if (!_kingState.TryBeginStrategyPreparation(_boundTurnManager.TurnNumber)) return; // 전략형 상태 시작 실패 차단

            _boundTurnManager.SetDeploymentChoicePending(true); // 선택 완료 전 배치·종료 입력 차단
            ResolveStrategySelectionUI(); // 전략형 선택 UI 생성 보장
            _strategySelectionUI.Show(candidates, index => ResolveStrategyChoice(candidates, index)); // 카드 3장 선택 화면 표시
            Debug.Log($"전략형 킹 전술적 준비: 덱 위 {candidates.Count}장 중 1장 선택"); // 전략형 패시브 발동 출력
        }

        private bool ResolveStrategyChoice(System.Collections.Generic.IReadOnlyList<PieceDefinition> candidates, int selectedIndex)
        {
            if (_runState == null || _kingState == null) return false; // 런·킹 상태 누락 시 선택 유지

            bool success = StrategyKingAbility.TryChoose(
                _kingState,
                _runState.Deck,
                _runState.Hand,
                candidates,
                selectedIndex,
                out PieceDefinition selected); // 선택 카드 손패 이동·나머지 덱 아래 이동 처리

            if (!success)
            {
                Debug.LogWarning("전략형 킹 전술적 준비: 카드 선택 처리에 실패했습니다. 현재 덱·손패 상태를 확인하세요."); // 실패 원인 확인용 경고
                return false; // UI를 닫지 않고 재선택 허용
            }

            _boundTurnManager?.SetDeploymentChoicePending(false); // 카드 선택 완료 후 배치 입력 복구
            RefreshCardUI(); // 직접 변경된 Hand·DrawPile UI 즉시 갱신
            Debug.Log($"전략형 킹 전술적 준비: {selected?.DisplayName ?? "카드"} 선택 / 나머지 후보 덱 아래 이동"); // 카드 선택 결과 출력
            return true; // UI 닫기 허용
        }

        private void RefreshCardUI()
        {
            if (_battleController == null) return; // BattleController 누락 방어
            BoardInputController boardInput = Object.FindFirstObjectByType<BoardInputController>(); // 현재 전투 카드 입력 컨트롤러 탐색
            if (boardInput == null) return; // 카드 입력 컨트롤러 누락 방어
            _battleController.HandUI?.Bind(boardInput); // 현재 HandState 기준 손패 UI 즉시 재구성
            _battleController.DeckPanelUI?.Bind(boardInput); // 현재 DrawPile 기준 덱 장수·목록 즉시 갱신
        }

        private void TrackRunFlow()
        {
            if (_runState == null || _kingState == null) return; // 런·킹 상태 누락 방어
            RunFlowPhase current = _runState.CurrentFlowPhase; // 현재 런 진행 단계 조회

            if (_hasPreviousFlowPhase && _previousFlowPhase == RunFlowPhase.Battle && current != RunFlowPhase.Battle)
            {
                CancelStrategyPreparation(); // 전투 종료 중 남은 전략형 선택 UI 정리
                _kingState.ResetBattleScopedState(); // 격노·방벽·전술적 준비 전투 상태 초기화
            }

            _previousFlowPhase = current; // 현재 진행 단계 저장
            _hasPreviousFlowPhase = true; // 진행 단계 추적 활성화
        }

        private void CancelStrategyPreparation()
        {
            _boundTurnManager?.SetDeploymentChoicePending(false); // 남은 배치 선택 입력 차단 해제
            _strategySelectionUI?.Hide(); // 전략형 선택 UI 숨김
            if (_kingState != null && _kingState.StrategyPreparationPending) _kingState.CompleteStrategyPreparation(); // 이전 선택 대기 상태 정리
        }

        private void UnsubscribeHooks()
        {
            if (_boundHooks == null) return; // 구독 훅 누락 방어
            _boundHooks.BeforeDamage -= HandleBeforeDamage; // 피해 훅 구독 해제
            _boundHooks.AfterAttack -= HandleAfterAttack; // 공격 종료 훅 구독 해제
            _boundHooks.AfterMove -= HandleAfterMove; // 이동 훅 구독 해제
        }

        private void UnsubscribeTurnManager()
        {
            if (_boundTurnManager == null) return; // 구독 턴 매니저 누락 방어
            _boundTurnManager.TurnChanged -= HandleTurnChanged; // 턴 상태 이벤트 구독 해제
        }

        private void OnDestroy()
        {
            CancelStrategyPreparation(); // 제거 시 전략형 선택 대기 정리
            UnsubscribeHooks(); // 전투 훅 구독 정리
            UnsubscribeTurnManager(); // 턴 매니저 구독 정리
        }
    }
}
