using System; // [Serializable] 속성을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런 관련 타입을 모아두는 네임스페이스
{
    [Serializable] // JsonUtility로 직렬화 가능하게 지정
    public class RunSaveData // 저장 파일에 기록될 런 전체 스냅샷
    {
        public int kingHp; // 저장 시점의 킹 체력
        public int currentRound; // 저장 시점의 현재 라운드
        public int metaCurrency; // 저장 시점의 메타 재화
        public List<string> handCardIds = new List<string>(); // 손패 카드 PieceId 목록
        public List<string> ownedCardPoolIds = new List<string>(); // 보유 카드 풀 PieceId 목록
        public List<string> drawPileIds = new List<string>(); // 드로우 순서를 유지할 PieceId 목록
        public List<string> deadCardPileIds = new List<string>(); // 죽은 카드 더미 PieceId 목록
        public List<PieceSaveData> boardPieces = new List<PieceSaveData>(); // 보드 위 기물 스냅샷
        public List<string> discoveredRecipeIds = new List<string>(); // 발견한 숨김 합성식 RecipeId 목록
    }

    [Serializable] // JsonUtility로 직렬화 가능하게 지정
    public class PieceSaveData // 보드 위 기물 1개의 저장용 스냅샷
    {
        public int x; // 보드 가로 좌표
        public int y; // 보드 세로 좌표
        public string pieceId; // 기물 종류 PieceId
        public int currentHp; // 저장 시점 현재 체력
        public bool isPlayerPiece; // 아군 기물 여부
        public int movementCycleIndex; // 25일차: Chameleon의 Knight/Bishop/Rook/Queen 현재 순환 단계
    }
}
