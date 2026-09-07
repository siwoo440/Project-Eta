using ProjectEta.Battle; // TurnState를 사용하기 위한 네임스페이스

namespace ProjectEta.UI // 프로젝트 η 런타임 UI 타입을 모아두는 네임스페이스
{
    public static class BattleInteractionPresentation // 전투 조작 안내 문구를 Unity 오브젝트와 분리해 계산하는 클래스
    {
        public static string BuildInstruction(TurnState state, bool isInitialDeployment, bool isInitialKingPlaced, bool hasSelectedPiece, int moveCount, int attackCount) // 현재 전투 입력 상태를 한 줄 안내 문구로 만드는 메서드
        {
            if (state == TurnState.BattleEnded) // 전투가 끝난 상태면
            {
                return string.Empty; // 추가 조작 안내를 숨기기 위한 빈 문자열 반환
            }

            if (state == TurnState.DeploymentTurn && isInitialDeployment && !isInitialKingPlaced) // 최초 배치에서 킹이 아직 보드에 없으면
            {
                return "KING FIRST · 손패의 킹 카드를 아군 배치 영역으로 드래그"; // 킹 우선 배치 안내 반환
            }

            if (state == TurnState.DeploymentTurn) // 최초 또는 주기 배치 턴이면
            {
                return "DEPLOYMENT · 원하는 카드를 배치한 뒤 Space로 종료"; // 자유 배치와 명시적 종료 안내 반환
            }

            if (state == TurnState.EnemyTurn) // 적 턴이면
            {
                return "ENEMY TURN · 적 행동 처리 중"; // 플레이어 입력 대기 안내 반환
            }

            if (hasSelectedPiece) // 플레이어 턴에 기물이 선택되어 있으면
            {
                return $"선택 기물 · MOVE {moveCount} · ATTACK {attackCount}"; // 이동·공격 후보 수 안내 반환
            }

            return "PLAYER TURN · 기물 선택 또는 손패 카드를 보드로 드래그"; // 기본 플레이어 조작 안내 반환
        }

        public static string BuildLegend() // 보드 강조 색상 의미를 표시하는 문구를 만드는 메서드
        {
            return "<color=#66D966>■</color> MOVE   <color=#FF8A2A>■</color> ATTACK   <color=#8CBFFF>■</color> DEPLOY"; // 이동·공격·배치 색상 범례 반환
        }
    }
}
