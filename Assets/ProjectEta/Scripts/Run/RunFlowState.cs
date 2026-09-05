namespace ProjectEta.Run // 런 흐름 상태 네임스페이스
{
    public enum RunFlowPhase // 로그라이트 런의 상위 진행 단계
    {
        Battle = 0, // 현재 스테이지 전투 진행
        Map = 1, // 전투 종료 후 경로 선택
        Completed = 2, // 최종 스테이지 승리 완료
        Failed = 3 // 런 패배 종료
    }

    public sealed class RunFlowState // 전투·지도·런 종료 흐름 상태 객체
    {
        public RunFlowPhase Phase { get; private set; } // 현재 런 진행 단계
        public BoardMode BoardMode { get; private set; } // 현재 체스판 역할
        public bool IsRunFinished => Phase == RunFlowPhase.Completed || Phase == RunFlowPhase.Failed; // 런 종료 여부

        public RunFlowState() // 새 런 흐름 생성
        {
            EnterBattle(); // 기본 전투 상태로 시작
        }

        public void EnterBattle() // 전투 단계 진입
        {
            Phase = RunFlowPhase.Battle; // 전투 흐름 지정
            BoardMode = BoardMode.Battle; // 체스판을 전투판으로 지정
        }

        public void EnterMap() // 경로 지도 단계 진입
        {
            Phase = RunFlowPhase.Map; // 지도 흐름 지정
            BoardMode = BoardMode.Map; // 체스판을 경로 지도로 지정
        }

        public void CompleteRun() // 최종 승리 처리
        {
            Phase = RunFlowPhase.Completed; // 런 완료 상태 지정
            BoardMode = BoardMode.Battle; // 지도 입력이 열리지 않도록 전투판 역할 유지
        }

        public void FailRun() // 런 패배 처리
        {
            Phase = RunFlowPhase.Failed; // 런 실패 상태 지정
            BoardMode = BoardMode.Battle; // 지도 입력이 열리지 않도록 전투판 역할 유지
        }
    }
}
