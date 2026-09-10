using ProjectEta.Run; // BoardMode·RunFlowPhase 사용

namespace ProjectEta.Board
{
    public static class Day66MapPresentationRules
    {
        public static bool ShouldShowRouteBuildings(BoardMode boardMode, RunFlowPhase flowPhase)
        {
            if (boardMode != BoardMode.Map) // 지도 보드 여부 확인
            {
                return false; // 전투판 지도 건물 숨김
            }

            if (flowPhase == RunFlowPhase.Shop || flowPhase == RunFlowPhase.Event) // 활동 오버레이 여부 확인
            {
                return false; // 상점·이벤트 지도 건물 숨김
            }

            return true; // 기존 Day66 테스트 호환 규칙 유지
        }

        public static bool ShouldShowBossHealth(BoardMode boardMode, RunFlowPhase flowPhase)
        {
            return boardMode == BoardMode.Battle && flowPhase == RunFlowPhase.Battle; // 실제 전투 상태만 허용
        }
    }
}
