using ProjectEta.Battle; // 전투 결과 열거형 사용

namespace ProjectEta.Run // 런 상태 타입 네임스페이스
{
    public enum RoundProgressStatus // 현재 라운드 진행 단계
    {
        NotStarted = 0, // 라운드 시작 전
        InProgress = 1, // 라운드 진행 중
        Cleared = 2, // 라운드 승리 완료
        Failed = 3 // 라운드 실패 완료
    }

    public sealed class RoundState // 1~10라운드 진행 상태 객체
    {
        public const int FirstRound = 1; // 첫 라운드 번호
        public const int FinalRound = 10; // 마지막 라운드 번호

        private int _roundNumber; // 현재 라운드 번호 내부 값

        public int RoundNumber => _roundNumber; // 현재 라운드 번호
        public bool IsBossRound => IsBossRoundNumber(_roundNumber); // 5·10라운드 보스 여부
        public RoundProgressStatus Status { get; private set; } // 현재 라운드 진행 상태
        public BattleOutcome BattleOutcome { get; private set; } // 현재 라운드 전투 결과

        public RoundState(int roundNumber = FirstRound) // 새 라운드 상태 생성
        {
            _roundNumber = ClampRoundNumber(roundNumber); // 1~10 범위 보정
            Status = RoundProgressStatus.NotStarted; // 시작 전 상태 지정
            BattleOutcome = BattleOutcome.None; // 전투 결과 초기화
        }

        public void SetRoundNumber(int roundNumber) // 현재 라운드 번호 변경
        {
            int clampedRound = ClampRoundNumber(roundNumber); // 입력 라운드 범위 보정
            if (_roundNumber == clampedRound) return; // 동일 라운드 재설정 방지

            _roundNumber = clampedRound; // 새 라운드 번호 반영
            ResetProgress(); // 새 라운드 진행 상태 초기화
        }

        public void Begin() // 현재 라운드 시작 처리
        {
            Status = RoundProgressStatus.InProgress; // 진행 중 상태 지정
            BattleOutcome = BattleOutcome.None; // 이전 결과 제거
        }

        public void Complete(BattleOutcome outcome) // 전투 결과 기반 라운드 종료 처리
        {
            BattleOutcome = outcome; // 전투 결과 기록

            if (outcome == BattleOutcome.Victory) // 승리 결과 처리
            {
                Status = RoundProgressStatus.Cleared; // 클리어 상태 지정
                return; // 승리 처리 종료
            }

            if (outcome == BattleOutcome.Defeat) // 패배 결과 처리
            {
                Status = RoundProgressStatus.Failed; // 실패 상태 지정
                return; // 패배 처리 종료
            }

            Status = RoundProgressStatus.InProgress; // 결과 없음은 진행 중으로 유지
        }

        public void Restore(int roundNumber, RoundProgressStatus status, BattleOutcome outcome) // 저장 데이터 기반 상태 복원
        {
            _roundNumber = ClampRoundNumber(roundNumber); // 저장 라운드 범위 보정
            Status = IsValidStatus(status) ? status : RoundProgressStatus.NotStarted; // 잘못된 상태 기본값 보정
            BattleOutcome = IsValidOutcome(outcome) ? outcome : BattleOutcome.None; // 잘못된 결과 기본값 보정

            if (Status == RoundProgressStatus.Cleared) // 클리어 상태 정합성 보정
            {
                BattleOutcome = BattleOutcome.Victory; // 클리어 결과를 승리로 통일
                return; // 클리어 보정 종료
            }

            if (Status == RoundProgressStatus.Failed) // 실패 상태 정합성 보정
            {
                BattleOutcome = BattleOutcome.Defeat; // 실패 결과를 패배로 통일
                return; // 실패 보정 종료
            }

            BattleOutcome = BattleOutcome.None; // 시작 전·진행 중 결과 제거
        }

        public void ResetProgress() // 현재 라운드 진행 상태 초기화
        {
            Status = RoundProgressStatus.NotStarted; // 시작 전 상태 복구
            BattleOutcome = BattleOutcome.None; // 전투 결과 제거
        }

        public static bool IsBossRoundNumber(int roundNumber) // 라운드 번호 기반 보스 여부 판정
        {
            return roundNumber == 5 || roundNumber == 10; // 5·10라운드만 보스 라운드
        }

        private static int ClampRoundNumber(int roundNumber) // 라운드 번호 범위 제한
        {
            if (roundNumber < FirstRound) return FirstRound; // 최소 라운드 보정
            if (roundNumber > FinalRound) return FinalRound; // 최대 라운드 보정
            return roundNumber; // 정상 범위 반환
        }

        private static bool IsValidStatus(RoundProgressStatus status) // 라운드 상태 값 검증
        {
            return (int)status >= (int)RoundProgressStatus.NotStarted && (int)status <= (int)RoundProgressStatus.Failed; // 정의 범위 확인
        }

        private static bool IsValidOutcome(BattleOutcome outcome) // 전투 결과 값 검증
        {
            return (int)outcome >= (int)BattleOutcome.None && (int)outcome <= (int)BattleOutcome.Defeat; // 정의 범위 확인
        }
    }
}
