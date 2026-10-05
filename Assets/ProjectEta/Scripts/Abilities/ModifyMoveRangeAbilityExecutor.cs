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

            var target = context.TargetPiece ?? context.Owner;

            if (target == null || target.IsDead)
            {
                failureReason = "사망한 기물의 이동 범위를 변경할 수 없습니다.";
                return false;
            }

            if (effect.Amount <= 0)
            {
                failureReason = "추가 이동 범위는 1 이상이어야 합니다.";
                return false;
            }

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(target);
            if (board == null)
            {
                failureReason = "이동 범위를 계산할 BoardState가 없습니다.";
                return false;
            }

            if (!target.CanMove && !target.CanAttack)
            {
                failureReason = "현재 기물은 이동과 공격이 모두 제한되어 있습니다.";
                return false;
            }

            if (MovementRangeModifierService.HasEquivalentEffect(target, effect))
            {
                failureReason = "같은 이동 보정이 이미 적용되어 있습니다.";
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

            var target = context.TargetPiece ?? context.Owner;
            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(target);
            MovementResult candidates = MovementModifierResolver.PreviewEffect(target, board, effect);

            return AbilityExecutionResult.Succeeded(
                candidates.MoveTiles.Count + candidates.AttackTiles.Count,
                target,
                false,
                new[] { target },
                candidates.MoveTiles);
        }

        public AbilityExecutionResult Execute(
            AbilityEffectData effect,
            AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            var target = context.TargetPiece ?? context.Owner;
            MovementRangeModifierService.Register(
                target,
                effect,
                context.TurnManager,
                effect.DurationTurns > 0 ? effect.DurationTurns : 1);

            return preview;
        }
    }
}
