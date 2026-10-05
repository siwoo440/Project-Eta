using ProjectEta.Board;

namespace ProjectEta.Abilities
{
    public sealed class BlockTileAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.BlockTile;

        public bool CanExecute(
            AbilityEffectData effect,
            AbilityExecutionContext context,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.Owner == null)
            {
                failureReason = "BlockTile 소유자 또는 실행 데이터가 없습니다.";
                return false;
            }

            if (context.Owner.IsDead)
            {
                failureReason = "사망한 기물은 칸을 봉쇄할 수 없습니다.";
                return false;
            }

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            if (board == null)
            {
                failureReason = "봉쇄에 사용할 BoardState가 없습니다.";
                return false;
            }

            if (!TileBlockService.CanBlock(board, context.TargetPosition))
            {
                failureReason = "대상 칸을 봉쇄할 수 없습니다.";
                return false;
            }

            return true;
        }

        public AbilityExecutionResult Preview(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            if (!CanExecute(effect, context, out string reason))
            {
                return AbilityExecutionResult.Failed(reason);
            }

            return AbilityExecutionResult.Succeeded(
                effect.DurationTurns > 0 ? effect.DurationTurns : 1,
                null,
                false,
                null,
                new[] { context.TargetPosition });
        }

        public AbilityExecutionResult Execute(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);

            bool blocked = TileBlockService.TryBlock(
                board,
                context.TargetPosition,
                context.Owner,
                context.TurnManager,
                effect.DurationTurns > 0 ? effect.DurationTurns : 1);

            return blocked
                ? preview
                : AbilityExecutionResult.Failed("칸 봉쇄 실행에 실패했습니다.");
        }
    }
}
