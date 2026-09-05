using ProjectEta.Board; // 보드 상태 사용
using ProjectEta.Cards; // 손패 상태 사용

namespace ProjectEta.Run // 런 상태 타입 네임스페이스
{
    public sealed class BattleState // 현재 전투 한 판의 임시 상태 객체
    {
        public BoardState Board { get; } // 현재 전투 보드 상태
        public HandState Hand { get; } // 현재 전투 손패 상태

        public BattleState() // 새 전투 상태 생성
        {
            Board = new BoardState(); // 새 보드 상태 생성
            Hand = new HandState(); // 새 손패 상태 생성
        }
    }
}
