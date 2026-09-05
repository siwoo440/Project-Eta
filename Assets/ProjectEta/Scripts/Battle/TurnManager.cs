using System; // Action 이벤트를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 턴 관련 타입을 모아두는 네임스페이스
{
    public class TurnManager // 초기 배치·일반 턴·적 턴·주기 배치 턴을 관리하는 순수 상태 클래스
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
        public bool HasDeployedThisTurn => DeployedCardCount > 0; // 기존 호출부 호환용 배치 여부 프로퍼티
        public bool CanPlayerInput => CurrentState == TurnState.PlayerTurn || CurrentState == TurnState.DeploymentTurn; // 플레이어 입력 가능 여부
        public bool CanPlayerAct => CurrentState == TurnState.PlayerTurn && !HasPlayerActed; // 일반 기물 행동 가능 여부
        public bool CanDeploy => CurrentState == TurnState.DeploymentTurn; // 배치 턴 동안에는 장수 제한 없이 계속 카드 배치 가능
        public bool CanEndDeploymentTurn => CurrentState == TurnState.DeploymentTurn && (!IsInitialDeployment || IsInitialKingPlaced); // 배치 턴을 종료할 수 있는지 여부
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None; // 전투 종료 결과
        public bool IsPlayerActionTransitionPending { get; private set; } // 플레이어 행동 연출 완료를 기다리는 상태
        public bool IsPlayerActionTransitionDeferred { get; private set; } // 런타임에서 연출 완료 후 적 턴 진입을 사용할지 여부

        public TurnManager() // 새 전투용 턴 매니저 생성자
        {
            CurrentState = TurnState.DeploymentTurn; // 전투는 시작 배치 턴부터 시작
            TurnNumber = 1; // 시작 배치는 일반 턴을 소비하지 않으므로 1턴을 대기 상태로 유지
            IsInitialDeployment = true; // 현재 배치가 시작 배치임을 기록
            IsInitialKingPlaced = false; // 아직 필수 킹이 보드에 없음
            HasPlayerActed = false; // 일반 턴 미시작 상태
            DeployedCardCount = 0; // 시작 배치에서 아직 아무 카드도 놓지 않음
            IsPlayerActionTransitionPending = false; // 연출 대기 상태 초기화
            IsPlayerActionTransitionDeferred = false; // 순수 상태 테스트 호환을 위해 기본 즉시 전환 유지
        }

        public bool TryCompletePlayerAction() // 플레이어 일반 행동 1회를 완료하는 메서드
        {
            if (!CanPlayerAct)
            {
                return false; // 잘못된 행동 완료를 거부
            }

            HasPlayerActed = true; // 이번 턴 행동 완료 기록

            if (IsPlayerActionTransitionDeferred)
            {
                IsPlayerActionTransitionPending = true; // 적 턴 전환을 연출 완료까지 보류
                PlayerActionTransitionPending?.Invoke(); // 런타임 연출 대기 컨트롤러에 완료 예약 알림
                return true; // 행동 자체는 정상 완료로 처리
            }

            CurrentState = TurnState.EnemyTurn; // 기존 동작대로 즉시 적 턴 전환
            NotifyTurnChanged(); // 상태 변경 알림
            return true; // 성공 반환
        }

        public void SetPlayerActionTransitionDeferred(bool deferred) // 런타임 연출 대기 모드 설정
        {
            IsPlayerActionTransitionDeferred = deferred; // 지연 모드 상태 저장
        }

        public bool ReleaseDeferredPlayerActionTransition() // 플레이어 기물 연출 완료 후 적 턴으로 실제 전환
        {
            if (!IsPlayerActionTransitionPending || CurrentState != TurnState.PlayerTurn)
            {
                return false; // 대기 중이 아니거나 전투 상태가 이미 바뀌었으면 무시
            }

            IsPlayerActionTransitionPending = false; // 연출 대기 상태 해제
            CurrentState = TurnState.EnemyTurn; // 적 턴으로 실제 전환
            NotifyTurnChanged(); // AI와 턴 UI에 적 턴 시작 알림
            return true; // 전환 성공 반환
        }

        public bool CompleteEnemyTurn() // 적 행동 종료 후 주기 배치 또는 다음 플레이어 턴으로 전환하는 메서드
        {
            if (CurrentState != TurnState.EnemyTurn)
            {
                return false; // 잘못된 호출 거부
            }

            if (TurnNumber % DeploymentInterval == 0)
            {
                CurrentState = TurnState.DeploymentTurn; // 일반 턴 번호를 올리기 전에 배치 턴 진입
                IsInitialDeployment = false; // 시작 배치가 아닌 일반 주기 배치
                IsInitialKingPlaced = true; // 시작 킹 조건 유지
                DeployedCardCount = 0; // 새 배치 턴의 배치 수 초기화
                IsPlayerActionTransitionPending = false; // 이전 연출 대기 상태 제거
                NotifyTurnChanged(); // 배치 턴 진입 알림
                return true; // 성공 반환
            }

            StartNextPlayerTurn(); // 배치 시점이 아니면 다음 일반 턴 시작
            return true; // 성공 반환
        }

        public void MarkInitialKingPlaced() // 시작 배치에서 킹이 실제 보드에 놓였음을 등록하는 메서드
        {
            if (CurrentState != TurnState.DeploymentTurn || !IsInitialDeployment || IsInitialKingPlaced)
            {
                return; // 중복 상태 변경 없이 종료
            }

            IsInitialKingPlaced = true; // 필수 킹 조건 충족
            NotifyTurnChanged(); // UI 즉시 갱신
        }

        public void RegisterDeployment() // 배치 턴에서 카드 1장을 실제 보드에 놓았음을 누적하는 메서드
        {
            if (!CanDeploy)
            {
                return; // 일반 턴에서는 배치 수 변경 없음
            }

            DeployedCardCount++; // 카드 배치 수 증가
            NotifyTurnChanged(); // UI에 현재 배치 수 반영
        }

        public bool TryEndDeploymentTurn() // 사용자가 배치를 마치고 명시적으로 턴 종료할 때 호출하는 메서드
        {
            if (!CanEndDeploymentTurn)
            {
                return false; // 시작 킹 조건 미충족 등 잘못된 종료 거부
            }

            if (IsInitialDeployment)
            {
                IsInitialDeployment = false; // 시작 배치 상태 해제
                DeployedCardCount = 0; // 배치 수 초기화
                HasPlayerActed = false; // 첫 일반 턴 행동권 초기화
                IsPlayerActionTransitionPending = false; // 연출 대기 상태 초기화
                CurrentState = TurnState.PlayerTurn; // 턴 번호 증가 없이 1턴 PlayerTurn 시작
                NotifyTurnChanged(); // 첫 일반 턴 시작 알림
                return true; // 성공 반환
            }

            StartNextPlayerTurn(); // 주기 배치 종료 후 다음 번호의 일반 턴 시작
            return true; // 성공 반환
        }

        public bool TryCompleteDeployment() // 이전 코드와 테스트 호환을 위한 배치 턴 종료 별칭 메서드
        {
            return TryEndDeploymentTurn(); // 명시적 배치 종료 의미로 통일
        }

        public bool SkipDeploymentTurn() // 이전 코드 호환용 별칭
        {
            return TryEndDeploymentTurn(); // 주기 배치에서는 즉시 종료 가능
        }

        public void EndBattle(BattleOutcome outcome = BattleOutcome.Defeat) // 승리·패배 시 턴 진행을 멈추는 메서드
        {
            if (CurrentState == TurnState.BattleEnded)
            {
                return; // 중복 종료 방지
            }

            IsPlayerActionTransitionPending = false; // 늦은 연출 완료가 적 턴을 시작하지 않도록 대기 취소
            CurrentState = TurnState.BattleEnded; // 전투 종료 상태 변경
            Outcome = outcome; // 종료 결과 기록
            NotifyTurnChanged(); // UI 등에 종료 알림
        }

        private void StartNextPlayerTurn() // 다음 일반 PlayerTurn을 시작하는 공통 메서드
        {
            TurnNumber++; // 일반 턴 번호 증가
            IsInitialDeployment = false; // 이후 시작 배치 아님
            HasPlayerActed = false; // 새 일반 턴 행동권 초기화
            DeployedCardCount = 0; // 배치 수 초기화
            IsPlayerActionTransitionPending = false; // 연출 대기 상태 초기화
            CurrentState = TurnState.PlayerTurn; // 플레이어 일반 턴 전환
            NotifyTurnChanged(); // 상태 변경 알림
        }

        private void NotifyTurnChanged() // 현재 턴 상태를 구독자에게 전달하는 내부 메서드
        {
            TurnChanged?.Invoke(CurrentState, TurnNumber); // 현재 상태와 일반 턴 번호 전달
        }
    }
}
