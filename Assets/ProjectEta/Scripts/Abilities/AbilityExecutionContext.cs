using UnityEngine; // Vector2Int 사용
using ProjectEta.Battle; // DamageContext·BattleHooks·TurnManager 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용
using ProjectEta.Run; // RunState 사용

namespace ProjectEta.Abilities
{
    public sealed class AbilityExecutionContext
    {
        public PieceRuntimeState Owner { get; }
        public PieceRuntimeState TargetPiece { get; }
        public Vector2Int TargetPosition { get; }
        public BoardState Board { get; }
        public RunState RunState { get; }
        public BattleHooks BattleHooks { get; }
        public TurnManager TurnManager { get; }
        public DamageContext DamageContext { get; }
        public bool IsPreview { get; }

        public AbilityExecutionContext(
            PieceRuntimeState owner,
            PieceRuntimeState targetPiece = null,
            Vector2Int? targetPosition = null,
            BoardState board = null,
            RunState runState = null,
            BattleHooks battleHooks = null,
            TurnManager turnManager = null,
            DamageContext damageContext = null,
            bool isPreview = false)
        {
            Owner = owner;
            TargetPiece = targetPiece;
            TargetPosition = targetPosition ?? (targetPiece != null ? targetPiece.BoardPosition : Vector2Int.zero);
            Board = board;
            RunState = runState;
            BattleHooks = battleHooks;
            TurnManager = turnManager;
            DamageContext = damageContext;
            IsPreview = isPreview;
        }

        public AbilityExecutionContext ForOwner(PieceRuntimeState owner, PieceRuntimeState targetPiece = null, bool? preview = null)
        {
            return new AbilityExecutionContext(
                owner,
                targetPiece ?? TargetPiece,
                targetPiece != null ? targetPiece.BoardPosition : TargetPosition,
                Board,
                RunState,
                BattleHooks,
                TurnManager,
                DamageContext,
                preview ?? IsPreview);
        }
    }
}
