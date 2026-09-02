using ProjectEta.Board;
using ProjectEta.Cards;

namespace ProjectEta.Run
{
    public class RunState
    {
        private int _kingHp;

        public BoardState Board { get; }
        public DeckState Deck { get; }
        public HandState Hand { get; }
        public int CurrentRound { get; set; }
        public int MetaCurrency { get; set; }
        public bool IsDefeated => _kingHp <= 0;

        public int KingHp
        {
            get => _kingHp;
            set => _kingHp = value < 0 ? 0 : value;
        }

        public RunState(int startingKingHp)
        {
            _kingHp = startingKingHp;
            Board = new BoardState();
            Deck = new DeckState();
            Hand = new HandState();
            CurrentRound = 1;
        }
    }
}
