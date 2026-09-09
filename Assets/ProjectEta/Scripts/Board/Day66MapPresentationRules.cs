using ProjectEta.Run; // BoardMode·RunFlowPhase 사용

namespace ProjectEta.Board
{
    public static class Day66MapPresentationRules
    {
        public static bool ShouldShowRouteBuildings(BoardMode boardMode, RunFlowPhase flowPhase)
        {
            if (boardMode != BoardMode.Map) // 지도 보드 여부 확인
            {
                return false; // 전투판에서는 지도 건물 숨김
            }

            if (flowPhase == RunFlowPhase.Shop || flowPhase == RunFlowPhase.Event) // 판 위 활동 오버레이 여부 확인
            {
                return false; // 상점·이벤트 UI 아래 건물 숨김
            }

            return true; // Map·Reward 지도 배경에서는 건물 유지
        }

        public static bool ShouldShowBossHealth(BoardMode boardMode, RunFlowPhase flowPhase)
        {
            return boardMode == BoardMode.Battle && flowPhase == RunFlowPhase.Battle; // 실제 전투 상태에서만 보스바 표시
        }
    }
}
