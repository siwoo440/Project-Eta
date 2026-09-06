using System; // Action 이벤트를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle
{
    public class TurnManager
    {
        public const int DeploymentInterval = 5; // 일반 턴 5회가 끝날 때마다 주기 배치 턴을 여는 고정 주기

        public event Action<TurnState, int> TurnChanged; // 턴 상태나 배치 조건이 바뀔 때 UI와 카드 시스템 등에 알리는 이벤트
        public event Action PlayerActionTransitionPending; // 플레이어 연출 완료 뒤 적 턴 전환이 필요함을 알리는 이벤트

        public TurnState CurrentState { get; private set; } // 현재 턴 상태
        public int TurnNumber { get; private set; } // 현재 일반 전투 턴 번호
        public bool IsInitialDeployment { get; private set; } // 현재 배치 턴이 전투 시작 배치인지 여부
        public bool IsInitialKingPlaced { get; private set; } // 시작 배치에서 필수 킹이 실제 보드에 놓였는지 여부
        public bool HasPlayerActed { get; private set; } // 이번 플레이어 턴에 일반 행동을 이미 완료했는지 여부
        public int DeployedCardCount { get; private set; } // 현재 배치 턴에서 자유롭게 배치한 카드 수
        public bool IsDeploymentChoicePending { get; private set; } // 전략형 킹 등 외부 배치 선택 UI 진행 여부
        public bool HasDeployedThisTurn => DeployedCardCount > 0; // 기존 호출부 호환용 배치 여부 프로퍼티
        public bool CanPlayerInput => CurrentState == TurnState.PlayerTurn || CurrentState == TurnState.DeploymentTurn; // 플레이어 입력 가능 여부
        public bool CanPlayerAct => CurrentState == TurnState.PlayerTurn && !HasPlayerActed; // 일반 기물 행동 가능 여부
        public bool CanDeploy => CurrentState == TurnState.DeploymentTurn && !IsDeploymentChoicePending; // 전략형 선택 중 보드·카드 배치 입력 차단
        public bool CanEndDeploymentTurn => CurrentState == TurnState.DeploymentTurn && !IsDeploymentChoicePending && (!IsInitialDeployment || IsInitialKingPlaced); // 필수 선택 완료 후 배치 종료 허용
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None; // 전투 종료 결과
        public bool IsPlayerActionTransitionPending { get; private set; } // 플레이어 행동 연출 완료 대기 상태
        public bool IsPlayerActionTransitionDeferred { get; private set; } // 연출 완료 후 적 턴 진입 사용 여부

        public TurnManager()
        {
            CurrentState = TurnState.DeploymentTurn; // 전투는 시작 배치 턴부터 시작
            TurnNumber = 1; // 시작 배치는 일반 턴을 소비하지 않으므로 1턴 대기
            IsInitialDeployment = true; // 현재 배치가 시작 배치임을 기록
            IsInitialKingPlaced = false; // 아직 필수 킹이 보드에 없음
            HasPlayerActed = false; // 일반 턴 미시작 상태
            DeployedCardCount = 0; // 시작 배치에서 아직 아무 카드도 놓지 않음
            IsDeploymentChoicePending = false; // 외부 카드 선택 대기 없음
            IsPlayerActionTransitionPending = false; // 연출 대기 상태 초기화
            IsPlayerActionTransitionDeferred = false; // 순수 상태 테스트 호환 기본 즉시 전환
        }

        public bool TryCompletePlayerAction()
        {
            if (!CanPlayerAct) return false; // 잘못된 행동 완료 거부

            HasPlayerActed = true; // 이번 턴 행동 완료 기록

            if (IsPlayerActionTransitionDeferred)
            {
                IsPlayerActionTransitionPending = true; // 적 턴 전환 연출 완료까지 보류
                PlayerActionTransitionPending?.Invoke(); // 런타임 연출 대기 알림
                return true; // 행동 자체 정상 완료 반환
            }

            CurrentState = TurnState.EnemyTurn; // 즉시 적 턴 전환
            NotifyTurnChanged(); // 상태 변경 알림
            return true; // 성공 반환
        }

        public void SetPlayerActionTransitionDeferred(bool deferred)
        {
            IsPlayerActionTransitionDeferred = deferred; // 지연 모드 상태 저장
        }

        public bool ReleaseDeferredPlayerActionTransition()
        {
            if (!IsPlayerActionTransitionPending || CurrentState != TurnState.PlayerTurn) return false; // 대기 상태·현재 턴 검증

            IsPlayerActionTransitionPending = false; // 연출 대기 상태 해제
            CurrentState = TurnState.EnemyTurn; // 적 턴 실제 전환
            NotifyTurnChanged(); // 적 턴 시작 알림
            return true; // 전환 성공 반환
        }

        public void SetDeploymentChoicePending(bool pending)
        {
            if (pending && CurrentState != TurnState.DeploymentTurn) return; // 배치 턴 외 새 선택 대기 설정 차단
            IsDeploymentChoicePending = pending; // 외부 선택 대기 상태만 변경하고 TurnChanged는 재발행하지 않음
        }

        public void ResetForNewBattle()
        {
            CurrentState = TurnState.DeploymentTurn; // 새 전투 시작 배치 턴 복구
            TurnNumber = 1; // 새 전투 턴 번호 초기화
            IsInitialDeployment = true; // 새 전투 시작 배치 지정
            IsInitialKingPlaced = false; // 플레이어 킹 재배치 필요
            HasPlayerActed = false; // 플레이어 행동권 초기화
            DeployedCardCount = 0; // 배치 수 초기화
            IsDeploymentChoicePending = false; // 이전 전략 선택 대기 제거
            Outcome = BattleOutcome.None; // 이전 전투 승패 제거
            IsPlayerActionTransitionPending = false; // 이전 연출 대기 제거
            NotifyTurnChanged(); // 새 전투 시작 상태 통지
        }

        public bool CompleteEnemyTurn()
        {
            if (CurrentState != TurnState.EnemyTurn) return false; // 잘못된 호출 거부

            if (TurnNumber % DeploymentInterval == 0)
            {
                CurrentState = TurnState.DeploymentTurn; // 일반 턴 번호 증가 전 배치 턴 진입
                IsInitialDeployment = false; // 시작 배치가 아닌 주기 배치
                IsInitialKingPlaced = true; // 시작 킹 조건 유지
                DeployedCardCount = 0; // 새 배치 턴 배치 수 초기화
                IsDeploymentChoicePending = false; // 새 배치 턴 선택 대기 기본 해제
                IsPlayerActionTransitionPending = false; // 이전 연출 대기 제거
                NotifyTurnChanged(); // 배치 턴 진입 알림
                return true; // 성공 반환
            }

            StartNextPlayerTurn(); // 배치 시점이 아니면 다음 일반 턴 시작
            return true; // 성공 반환
        }

        public void MarkInitialKingPlaced()
        {
            if (CurrentState != TurnState.DeploymentTurn || !IsInitialDeployment || IsInitialKingPlaced) return; // 잘못된 상태·중복 변경 차단

            IsInitialKingPlaced = true; // 필수 킹 조건 충족
            NotifyTurnChanged(); // UI 즉시 갱신
        }

        public void RegisterDeployment()
        {
            if (!CanDeploy) return; // 배치 불가 상태 변경 차단

            DeployedCardCount++; // 카드 배치 수 증가
            NotifyTurnChanged(); // UI 현재 배치 수 반영
        }

        public bool TryEndDeploymentTurn()
        {
            if (!CanEndDeploymentTurn) return false; // 시작 킹·전략 선택 등 필수 조건 미충족 종료 거부

            IsDeploymentChoicePending = false; // 정상 종료 시 외부 선택 대기 확실히 해제

            if (IsInitialDeployment)
            {
                IsInitialDeployment = false; // 시작 배치 상태 해제
                DeployedCardCount = 0; // 배치 수 초기화
                HasPlayerActed = false; // 첫 일반 턴 행동권 초기화
                IsPlayerActionTransitionPending = false; // 연출 대기 초기화
                CurrentState = TurnState.PlayerTurn; // 턴 번호 증가 없이 1턴 PlayerTurn 시작
                NotifyTurnChanged(); // 첫 일반 턴 시작 알림
                return true; // 성공 반환
            }

            StartNextPlayerTurn(); // 주기 배치 종료 후 다음 일반 턴 시작
            return true; // 성공 반환
        }

        public bool TryCompleteDeployment()
        {
            return TryEndDeploymentTurn(); // 이전 코드 호환 배치 종료 별칭
        }

        public bool SkipDeploymentTurn()
        {
            return TryEndDeploymentTurn(); // 이전 코드 호환 배치 스킵 별칭
        }

        public void EndBattle(BattleOutcome outcome = BattleOutcome.Defeat)
        {
            if (CurrentState == TurnState.BattleEnded) return; // 중복 종료 방지

            IsDeploymentChoicePending = false; // 전투 종료 시 전략형 선택 대기 해제
            IsPlayerActionTransitionPending = false; // 늦은 연출 완료 적 턴 진입 방지
            CurrentState = TurnState.BattleEnded; // 전투 종료 상태 변경
            Outcome = outcome; // 종료 결과 기록
            NotifyTurnChanged(); // UI 등에 종료 알림
        }

        private void StartNextPlayerTurn()
        {
            TurnNumber++; // 일반 턴 번호 증가
            IsInitialDeployment = false; // 이후 시작 배치 아님
            HasPlayerActed = false; // 새 일반 턴 행동권 초기화
            DeployedCardCount = 0; // 배치 수 초기화
            IsDeploymentChoicePending = false; // 배치 선택 대기 해제
            IsPlayerActionTransitionPending = false; // 연출 대기 초기화
            CurrentState = TurnState.PlayerTurn; // 플레이어 일반 턴 전환
            NotifyTurnChanged(); // 상태 변경 알림
        }

        private void NotifyTurnChanged()
        {
            TurnChanged?.Invoke(CurrentState, TurnNumber); // 현재 상태와 일반 턴 번호 전달
        }
    }
}
