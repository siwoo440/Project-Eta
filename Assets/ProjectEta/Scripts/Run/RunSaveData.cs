using System; // [Serializable] 사용
using System.Collections.Generic; // List<T> 사용

namespace ProjectEta.Run
{
    [Serializable]
    public class RunSaveData
    {
        public const int CurrentVersion = 2; // 51일차 런 세이브 포맷 버전

        public int saveVersion; // 저장 포맷 버전
        public string runId; // 51일차 런 고유 ID
        public int kingHp; // 저장 시점 킹 체력
        public int currentRound; // 저장 시점 현재 라운드
        public int metaCurrency; // 기존 런 세이브 호환 재화
        public int roundStatus; // 현재 라운드 진행 상태
        public int battleOutcome; // 현재 라운드 전투 결과
        public bool isBossRound; // 저장 시점 보스 라운드 여부
        public int flowPhase; // 51일차 RunFlowPhase 저장
        public int runCurrency; // 51일차 런 전용 Gold 저장
        public int selectedKingArchetype; // 51일차 선택 킹 타입 저장
        public string currentStageDefinitionId; // 51일차 현재 활성 스테이지 정의 ID
        public RouteMapSaveData routeMap = new RouteMapSaveData(); // 51일차 경로 지도 저장
        public List<CardSaveData> ownedCards = new List<CardSaveData>(); // 51일차 강화 스탯 포함 보유 카드 저장
        public List<string> handCardIds = new List<string>(); // 손패 카드 PieceId 목록
        public List<string> ownedCardPoolIds = new List<string>(); // 구버전 보유 카드 풀 PieceId 목록
        public List<string> drawPileIds = new List<string>(); // 드로우 순서 PieceId 목록
        public List<string> deadCardPileIds = new List<string>(); // 죽은 카드 더미 PieceId 목록
        public List<PieceSaveData> boardPieces = new List<PieceSaveData>(); // 보드 위 기물 스냅샷
        public List<string> discoveredRecipeIds = new List<string>(); // 발견 합성식 RecipeId 목록
    }

    [Serializable]
    public class RouteMapSaveData
    {
        public int mapSeed; // 현재 런 경로 지도 시드
        public int currentDepth; // 현재 지도 깊이
        public string currentNodeId; // 현재 킹 노드 ID
        public string selectedNodeId; // 현재 선택 스테이지 노드 ID
        public int kingX; // 지도 킹 X 좌표
        public int kingY; // 지도 킹 Y 좌표
        public List<string> selectedPathNodeIds = new List<string>(); // 런 동안 선택한 실제 경로 노드 ID
        public List<string> visitedNodeIds = new List<string>(); // 런 동안 방문한 노드 ID 이력
        public List<RouteNodeSaveData> nodes = new List<RouteNodeSaveData>(); // 현재 지도 노드 스냅샷
    }

    [Serializable]
    public class RouteNodeSaveData
    {
        public string nodeId; // 경로 노드 ID
        public int x; // 경로 노드 X 좌표
        public int y; // 경로 노드 Y 좌표
        public int depth; // 경로 노드 깊이
        public string stageDefinitionId; // 연결 StageDefinition ID
        public bool visited; // 방문 여부
        public List<string> nextNodeIds = new List<string>(); // 다음 연결 노드 ID 목록
    }

    [Serializable]
    public class CardSaveData
    {
        public string pieceId; // 원본 PieceId
        public string displayName; // 런타임 강화 표시 이름
        public int baseHp; // 저장 시점 카드 HP
        public int baseAtk; // 저장 시점 카드 ATK
    }

    [Serializable]
    public class PieceSaveData
    {
        public int x; // 보드 가로 좌표
        public int y; // 보드 세로 좌표
        public string pieceId; // 기물 PieceId
        public int currentHp; // 저장 시점 현재 체력
        public bool isPlayerPiece; // 아군 기물 여부
        public int movementCycleIndex; // Chameleon 이동 순환 단계
        public List<StatusEffectSaveData> statusEffects = new List<StatusEffectSaveData>(); // 저장 상태 이상 목록
    }

    [Serializable]
    public class StatusEffectSaveData
    {
        public int statusType; // 상태 종류 비트값
        public int remainingTurns; // 남은 지속 턴
        public int stackCount; // 현재 중첩 수
    }
}
