namespace ProjectEta.Run
{
    public sealed class RunContinueInfo
    {
        public int Stage { get; } // 현재 Stage
        public RunFlowPhase FlowPhase { get; } // 현재 상위 진행 흐름
        public int Gold { get; } // 현재 런 Gold
        public int KingHp { get; } // 현재 King HP
        public int VisitedNodeCount { get; } // 방문 경로 노드 수

        public RunContinueInfo(int stage, RunFlowPhase flowPhase, int gold, int kingHp, int visitedNodeCount)
        {
            Stage = stage; // 현재 Stage 저장
            FlowPhase = flowPhase; // 현재 진행 흐름 저장
            Gold = gold; // 현재 Gold 저장
            KingHp = kingHp; // 현재 King HP 저장
            VisitedNodeCount = visitedNodeCount; // 방문 경로 수 저장
        }
    }
}
