using System; // Action 이벤트를 사용하기 위한 네임스페이스

namespace ProjectEta.Battle // 전투 턴 관련 타입을 모아두는 네임스페이스
{
    public class TurnManager // 플레이어 턴·적 턴·턴 번호·행동 권한을 관리하는 순수 상태 클래스
    {
        public event Action<TurnState, int> TurnChanged; // 턴 상태나 턴 번호가 바뀔 때 UI 등에 알리는 이벤트

        public TurnState CurrentState { get; private set; } // 현재 턴 상태
        public int TurnNumber { get; private set; } // 현재 전체 턴 번호
        public bool HasPlayerActed { get; private set; } // 이번 플레이어 턴에 행동을 이미 완료했는지 여부
        public bool CanPlayerInput => CurrentState == TurnState.PlayerTurn; // 현재 플레이어 입력 자체가 허용되는지 여부
        public bool CanPlayerAct => CurrentState == TurnState.PlayerTurn && !HasPlayerActed; // 현재 플레이어가 일반 행동을 1회 수행할 수 있는지 여부
        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None; // 13일차: 전투가 어떻게 끝났는지(승리/패배) 기록

        public TurnManager() // 새 전투용 턴 매니저 생성자
        {
            CurrentState = TurnState.PlayerTurn; // 전투는 플레이어 턴부터 시작
            TurnNumber = 1; // 첫 전체 턴 번호는 1
            HasPlayerActed = false; // 첫 턴에는 아직 행동하지 않은 상태로 시작
        }

        public bool TryCompletePlayerAction() // 플레이어의 이번 턴 일반 행동을 완료 처리하는 메서드
        {
            if (!CanPlayerAct) // 플레이어 턴이 아니거나 이미 행동했다면
            {
                return false; // 중복 행동을 허용하지 않고 실패 반환
            }

            HasPlayerActed = true; // 이번 턴에 플레이어가 행동했음을 기록
            CurrentState = TurnState.EnemyTurn; // 플레이어 행동 직후 적 턴으로 전환
            NotifyTurnChanged(); // 턴 변경을 UI와 외부 시스템에 알림
            return true; // 정상적으로 행동 완료 처리됐음을 반환
        }

        public bool CompleteEnemyTurn() // 적 행동이 끝났을 때 다음 플레이어 턴으로 전환하는 메서드
        {
            if (CurrentState != TurnState.EnemyTurn) // 현재 적 턴이 아니라면
            {
                return false; // 잘못된 순서의 턴 종료를 거부
            }

            TurnNumber++; // 플레이어+적 행동 한 묶음이 끝났으므로 전체 턴 번호 증가
            HasPlayerActed = false; // 새 플레이어 턴에서 다시 한 번 행동할 수 있도록 초기화
            CurrentState = TurnState.PlayerTurn; // 다음 플레이어 턴으로 전환
            NotifyTurnChanged(); // 턴 변경을 UI와 외부 시스템에 알림
            return true; // 정상적으로 다음 턴으로 넘어갔음을 반환
        }

        public void EndBattle(BattleOutcome outcome = BattleOutcome.Defeat) // 승리·패배 시 턴 진행을 완전히 멈추는 메서드(13일차: 결과 구분 추가, 기본값은 기존 호출부와의 호환을 위한 패배)
        {
            if (CurrentState == TurnState.BattleEnded) // 이미 전투가 종료된 상태라면
            {
                return; // 중복 종료 처리를 하지 않음
            }

            CurrentState = TurnState.BattleEnded; // 현재 상태를 전투 종료로 변경
            Outcome = outcome; // 전투 종료 사유(승리/패배)를 기록
            NotifyTurnChanged(); // UI가 종료 상태를 표시하도록 알림
        }

        private void NotifyTurnChanged() // 현재 턴 상태를 구독자에게 전달하는 내부 메서드
        {
            TurnChanged?.Invoke(CurrentState, TurnNumber); // 현재 상태와 턴 번호를 이벤트로 전달
        }
    }
}
