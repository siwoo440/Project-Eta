using ProjectEta.Battle;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public sealed class MovementRangeModifierState
    {
        public PieceRuntimeState Piece { get; }
        public AbilityEffectData Effect { get; }
        public TurnManager TurnManager { get; }
        public int ExpiresOnPlayerTurn { get; }

        internal MovementRangeModifierState(
            PieceRuntimeState piece,
            AbilityEffectData effect,
            TurnManager turnManager,
            int expiresOnPlayerTurn)
        {
            Piece = piece;
            Effect = effect;
            TurnManager = turnManager;
            ExpiresOnPlayerTurn = expiresOnPlayerTurn;
        }
    }
}
