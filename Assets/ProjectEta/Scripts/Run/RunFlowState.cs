namespace ProjectEta.Run
{
    public enum RunFlowPhase
    {
        Battle = 0, // 현재 스테이지 전투 진행
        Map = 1, // 전투 종료 후 경로 선택
        Reward = 2, // 카드 보상 스테이지 진행
        Shop = 3, // 상점 스테이지 진행
        Event = 4, // 이벤트 스테이지 진행
        Completed = 5, // 최종 스테이지 승리 완료
        Failed = 6 // 런 패배 종료
    }

    public sealed class RunFlowState
    {
        public RunFlowPhase Phase { get; private set; } // 현재 런 진행 단계
        public BoardMode BoardMode { get; private set; } // 현재 체스판 역할
        public bool IsRunFinished => Phase == RunFlowPhase.Completed || Phase == RunFlowPhase.Failed; // 런 종료 여부

        public RunFlowState()
        {
            EnterBattle(); // 기본 전투 상태로 시작
        }

        public void EnterBattle()
        {
            Phase = RunFlowPhase.Battle; // 전투 흐름 지정
            BoardMode = BoardMode.Battle; // 체스판 전투판 지정
        }

        public void EnterMap()
        {
            Phase = RunFlowPhase.Map; // 지도 흐름 지정
            BoardMode = BoardMode.Map; // 체스판 경로 지도 지정
        }

        public void EnterReward()
        {
            Phase = RunFlowPhase.Reward; // 보상 흐름 지정
            BoardMode = BoardMode.Map; // 보상 중 지도 배경 유지
        }

        public void EnterShop()
        {
            Phase = RunFlowPhase.Shop; // 상점 흐름 지정
            BoardMode = BoardMode.Map; // 상점 중 지도 배경 유지
        }

        public void EnterEvent()
        {
            Phase = RunFlowPhase.Event; // 이벤트 흐름 지정
            BoardMode = BoardMode.Map; // 이벤트 중 지도 배경 유지
        }

        public void CompleteRun()
        {
            Phase = RunFlowPhase.Completed; // 런 완료 상태 지정
            BoardMode = BoardMode.Battle; // 지도 입력 차단
        }

        public void FailRun()
        {
            Phase = RunFlowPhase.Failed; // 런 실패 상태 지정
            BoardMode = BoardMode.Battle; // 지도 입력 차단
        }

        public void Restore(int rawPhase)
        {
            if (rawPhase < (int)RunFlowPhase.Battle || rawPhase > (int)RunFlowPhase.Failed)
            {
                EnterBattle(); // 잘못된 저장 값은 전투 상태 fallback
                return;
            }

            RunFlowPhase phase = (RunFlowPhase)rawPhase; // 검증된 저장 흐름 변환

            switch (phase)
            {
                case RunFlowPhase.Map:
                    EnterMap(); // 지도 상태 복원
                    break;
                case RunFlowPhase.Reward:
                    EnterReward(); // 보상 상태 복원
                    break;
                case RunFlowPhase.Shop:
                    EnterShop(); // 상점 상태 복원
                    break;
                case RunFlowPhase.Event:
                    EnterEvent(); // 이벤트 상태 복원
                    break;
                case RunFlowPhase.Completed:
                    CompleteRun(); // 완료 상태 복원
                    break;
                case RunFlowPhase.Failed:
                    FailRun(); // 실패 상태 복원
                    break;
                default:
                    EnterBattle(); // 전투 상태 복원
                    break;
            }
        }
    }
}
