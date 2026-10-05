using ProjectEta.Board;

namespace ProjectEta.Abilities
{
    public sealed class ModifyMoveRangeAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.ModifyMoveRange;

        public bool CanExecute(
            AbilityEffectData effect,
            AbilityExecutionContext context,
            out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.Owner == null)
            {
                failureReason = "ModifyMoveRange 소유자 또는 실행 데이터가 없습니다.";
                return false;
            }

            if (context.Owner.IsDead)
            {
                failureReason = "사망한 기물의 이동 범위를 변경할 수 없습니다.";
                return false;
            }

            if (effect.Amount <= 0)
            {
                failureReason = "추가 이동 범위는 1 이상이어야 합니다.";
                return false;
            }

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            if (board == null)
            {
                failureReason = "이동 범위를 계산할 BoardState가 없습니다.";
                return false;
            }

            if (!context.Owner.CanMove && !context.Owner.CanAttack)
            {
                failureReason = "현재 기물은 이동과 공격이 모두 제한되어 있습니다.";
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

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            MovementResult candidates = MovementModifierResolver.PreviewEffect(context.Owner, board, effect);

            return AbilityExecutionResult.Succeeded(
                candidates.MoveTiles.Count + candidates.AttackTiles.Count,
                context.Owner,
                false,
                new[] { context.Owner },
                candidates.MoveTiles);
        }

        public AbilityExecutionResult Execute(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            MovementRangeModifierService.Register(
                context.Owner,
                effect,
                context.TurnManager,
                effect.DurationTurns > 0 ? effect.DurationTurns : 1);

            return preview;
        }
    }
}
