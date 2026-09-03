using System; // [Serializable] 속성을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런(플레이 세션) 관련 타입을 모아두는 네임스페이스
{
    [Serializable] // JsonUtility로 직렬화 가능하게 하는 속성
    public class RunSaveData // 저장 파일에 기록될 런 전체 스냅샷 데이터
    {
        public int kingHp; // 저장 시점의 킹 체력
        public int currentRound; // 저장 시점의 현재 라운드
        public int metaCurrency; // 저장 시점의 메타 재화
        public List<string> handCardIds = new List<string>(); // 손패 카드들의 PieceId 목록
        public List<string> ownedCardPoolIds = new List<string>(); // 보유 카드 풀의 PieceId 목록
        public List<string> drawPileIds = new List<string>(); // 16일차: 저장 시점의 드로우 순서를 유지할 PieceId 목록
        public List<string> deadCardPileIds = new List<string>(); // 죽은 카드 더미의 PieceId 목록
        public List<PieceSaveData> boardPieces = new List<PieceSaveData>(); // 보드 위 기물 스냅샷 목록
    }

    [Serializable] // JsonUtility로 직렬화 가능하게 하는 속성
    public class PieceSaveData // 보드 위 기물 1개의 저장용 스냅샷 데이터
    {
        public int x; // 보드 가로 좌표
        public int y; // 보드 세로 좌표
        public string pieceId; // 기물 종류를 나타내는 PieceId
        public int currentHp; // 저장 시점의 현재 체력
        public bool isPlayerPiece; // 아군 기물 여부
    }
}
