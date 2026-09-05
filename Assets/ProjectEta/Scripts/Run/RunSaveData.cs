using System; // [Serializable] 속성 사용
using System.Collections.Generic; // List<T> 사용

namespace ProjectEta.Run // 런 관련 타입 네임스페이스
{
    [Serializable] // JsonUtility 직렬화 지정
    public class RunSaveData // 런 전체 저장 스냅샷
    {
        public int kingHp; // 저장 시점 킹 체력
        public int currentRound; // 저장 시점 현재 라운드
        public int metaCurrency; // 저장 시점 메타 재화
        public int roundStatus; // 42일차 현재 라운드 진행 상태
        public int battleOutcome; // 42일차 현재 라운드 전투 결과
        public bool isBossRound; // 42일차 저장 시점 보스 라운드 여부
        public List<string> handCardIds = new List<string>(); // 손패 카드 PieceId 목록
        public List<string> ownedCardPoolIds = new List<string>(); // 보유 카드 풀 PieceId 목록
        public List<string> drawPileIds = new List<string>(); // 드로우 순서 PieceId 목록
        public List<string> deadCardPileIds = new List<string>(); // 죽은 카드 더미 PieceId 목록
        public List<PieceSaveData> boardPieces = new List<PieceSaveData>(); // 보드 위 기물 스냅샷
        public List<string> discoveredRecipeIds = new List<string>(); // 발견 합성식 RecipeId 목록
    }

    [Serializable] // JsonUtility 직렬화 지정
    public class PieceSaveData // 보드 기물 저장 스냅샷
    {
        public int x; // 보드 가로 좌표
        public int y; // 보드 세로 좌표
        public string pieceId; // 기물 PieceId
        public int currentHp; // 저장 시점 현재 체력
        public bool isPlayerPiece; // 아군 기물 여부
        public int movementCycleIndex; // Chameleon 이동 순환 단계
        public List<StatusEffectSaveData> statusEffects = new List<StatusEffectSaveData>(); // 저장 상태 이상 목록
    }

    [Serializable] // JsonUtility 직렬화 지정
    public class StatusEffectSaveData // 상태 이상 저장 스냅샷
    {
        public int statusType; // 상태 종류 비트값
        public int remainingTurns; // 남은 지속 턴
        public int stackCount; // 현재 중첩 수
    }
}
