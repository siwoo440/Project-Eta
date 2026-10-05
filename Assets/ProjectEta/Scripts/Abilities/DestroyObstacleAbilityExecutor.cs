using ProjectEta.Board;

namespace ProjectEta.Abilities
{
    public sealed class DestroyObstacleAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.DestroyObstacle;

        public bool CanExecute(
            AbilityEffectData effect,
            AbilityExecutionContext context,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.Owner == null)
            {
                failureReason = "DestroyObstacle 소유자 또는 실행 데이터가 없습니다.";
                return false;
            }

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            if (board == null || !board.IsInsideBoard(context.TargetPosition))
            {
                failureReason = "유효한 장애물 대상 칸이 없습니다.";
                return false;
            }

            TileState tile = board.GetTile(context.TargetPosition);

            if (tile == null || !tile.HasObstacle)
            {
                failureReason = "대상 칸에 장애물이 없습니다.";
                return false;
            }

            if (!tile.IsObstacleDestructible)
            {
                failureReason = "파괴할 수 없는 장애물입니다.";
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
                1,
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
            TileState tile = board.GetTile(context.TargetPosition);

            return tile != null && tile.TryDestroyObstacle()
                ? preview
                : AbilityExecutionResult.Failed("장애물 파괴 실행에 실패했습니다.");
        }
    }
}
