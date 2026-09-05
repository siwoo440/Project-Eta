namespace ProjectEta.Run // 런 흐름 상태 네임스페이스
{
    public enum RunFlowPhase // 로그라이트 런의 상위 진행 단계
    {
        Battle = 0, // 현재 스테이지 전투 진행
        Map = 1, // 전투 종료 후 경로 선택
        Reward = 2, // 카드 보상 스테이지 진행
        Shop = 3, // 상점 스테이지 진행
        Event = 4, // 이벤트 스테이지 진행
        Completed = 5, // 최종 스테이지 승리 완료
        Failed = 6 // 런 패배 종료
    }

    public sealed class RunFlowState // 전투·지도·비전투·런 종료 흐름 상태 객체
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

        public void EnterReward() // 카드 보상 단계 진입
        {
            Phase = RunFlowPhase.Reward; // 보상 흐름 지정
            BoardMode = BoardMode.Map; // 보상 중에도 경로 지도 배경 유지
        }

        public void EnterShop() // 상점 단계 진입
        {
            Phase = RunFlowPhase.Shop; // 상점 흐름 지정
            BoardMode = BoardMode.Map; // 상점 중에도 경로 지도 배경 유지
        }

        public void EnterEvent() // 이벤트 단계 진입
        {
            Phase = RunFlowPhase.Event; // 이벤트 흐름 지정
            BoardMode = BoardMode.Map; // 이벤트 중에도 경로 지도 배경 유지
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
