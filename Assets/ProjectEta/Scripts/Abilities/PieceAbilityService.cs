using System; // StringComparison 사용
using System.Collections.Generic; // IReadOnlyList 사용
using UnityEngine; // Mathf 사용
using ProjectEta.Battle; // DamageContext 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public static class PieceAbilityService
    {
        public static AbilityExecutionResult PreviewAbility(
            PieceAbilityDefinition ability,
            AbilityExecutionContext context)
        {
            return EvaluateAbilityInternal(ability, context, execute: false);
        }

        public static AbilityExecutionResult ExecuteAbility(
            PieceAbilityDefinition ability,
            AbilityExecutionContext context)
        {
            return EvaluateAbilityInternal(ability, context, execute: true);
        }

        public static void ProcessBeforeDamage(DamageContext damageContext)
        {
            if (damageContext == null || damageContext.Target == null) return;

            ApplyRedirect(damageContext); // 1. 실제 피해 대상 먼저 확정

            PieceRuntimeState actualTarget = damageContext.Target;
            PieceRuntimeState source = damageContext.Source;

            if (source != null)
            {
                ApplyTriggeredEffects(
                    source,
                    AbilityTrigger.BeforeDamage,
                    AbilityEffectType.ModifyDamage,
                    damageContext);
            }

            if (actualTarget != null && !object.ReferenceEquals(actualTarget, source))
            {
                ApplyTriggeredEffects(
                    actualTarget,
                    AbilityTrigger.BeforeDamage,
                    AbilityEffectType.ModifyDamage,
                    damageContext);
            }
        }

        public static bool HasEffect(
            PieceRuntimeState piece,
            AbilityTrigger trigger,
            AbilityEffectType effectType)
        {
            return TryGetFirstEffect(piece, trigger, effectType, out _, out _);
        }

        private static AbilityExecutionResult EvaluateAbilityInternal(
            PieceAbilityDefinition ability,
            AbilityExecutionContext context,
            bool execute)
        {
            if (ability == null) return AbilityExecutionResult.Failed("Ability 정의가 없습니다.");
            if (context == null) return AbilityExecutionResult.Failed("Ability 실행 Context가 없습니다.");

            if (ability.ActionCost == AbilityActionCost.PlayerAction &&
                context.TurnManager != null)
            {
                bool allowed = context.Owner != null && context.Owner.IsPlayerPiece
                    ? context.TurnManager.CanPlayerAct
                    : context.TurnManager.CurrentState == TurnState.EnemyTurn;

                if (!allowed)
                {
                    return AbilityExecutionResult.Failed("현재 턴에는 일반 행동을 사용할 수 없습니다.");
                }
            }

            AbilityEffectData[] effects = ability.Effects;
            if (effects.Length == 0) return AbilityExecutionResult.Failed("Ability Effect가 없습니다.");

            int totalAmount = 0;
            PieceRuntimeState actualTarget = context.TargetPiece;

            // 실제 실행 전에 모든 Effect Preview가 성공하는지 검사해 부분 적용을 방지한다.
            for (int i = 0; i < effects.Length; i++)
            {
                AbilityEffectData effect = effects[i];
                if (effect == null) return AbilityExecutionResult.Failed("비어 있는 Ability Effect가 있습니다.");

                if (!AbilityEffectRegistry.TryGet(effect.EffectType, out IAbilityEffectExecutor executor))
                {
                    return AbilityExecutionResult.Failed($"{effect.EffectType} Effect는 아직 구현되지 않았습니다.");
                }

                AbilityExecutionResult preview = executor.Preview(
                    effect,
                    new AbilityExecutionContext(
                        context.Owner,
                        context.TargetPiece,
                        context.TargetPosition,
                        context.Board,
                        context.RunState,
                        context.BattleHooks,
                        context.TurnManager,
                        context.DamageContext,
                        isPreview: true));

                if (!preview.Success) return preview;
            }

            if (!execute)
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    AbilityEffectRegistry.TryGet(effects[i].EffectType, out IAbilityEffectExecutor executor);
                    AbilityExecutionResult preview = executor.Preview(effects[i], context);
                    totalAmount += preview.Amount;
                    if (preview.ActualTarget != null) actualTarget = preview.ActualTarget;
                }

                return AbilityExecutionResult.Succeeded(totalAmount, actualTarget);
            }

            for (int i = 0; i < effects.Length; i++)
            {
                AbilityEffectData effect = effects[i];
                AbilityEffectRegistry.TryGet(effect.EffectType, out IAbilityEffectExecutor executor);
                AbilityExecutionResult result = executor.Execute(effect, context);

                if (!result.Success) return result; // Preview 통과 후라면 정상적으로는 발생하지 않아야 함
                totalAmount += result.Amount;
                if (result.ActualTarget != null) actualTarget = result.ActualTarget;
            }

            bool consumedAction = false;

            if (ability.ActionCost == AbilityActionCost.PlayerAction &&
                context.TurnManager != null &&
                context.Owner != null &&
                context.Owner.IsPlayerPiece)
            {
                consumedAction = context.TurnManager.TryCompletePlayerAction();
            }

            return AbilityExecutionResult.Succeeded(
                totalAmount,
                actualTarget,
                consumedAction,
                actualTarget != null ? new[] { actualTarget } : null);
        }

        private static void ApplyRedirect(DamageContext damageContext)
        {
            PieceRuntimeState protectedTarget = damageContext.Target;
            if (protectedTarget == null || damageContext.RedirectCount > 0) return;

            ProjectEta.Board.BoardState board = AbilityBoardRegistry.FindBoardContaining(protectedTarget);
            if (board == null) return;

            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);

            PieceRuntimeState bestProtector = null;
            AbilityEffectData bestEffect = null;
            int bestDistance = int.MaxValue;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState candidate = pieces[i];
                if (candidate == null ||
                    candidate.IsDead ||
                    object.ReferenceEquals(candidate, protectedTarget) ||
                    object.ReferenceEquals(candidate, damageContext.Source) ||
                    candidate.IsPlayerPiece != protectedTarget.IsPlayerPiece)
                {
                    continue;
                }

                if (!TryGetFirstEffect(
                        candidate,
                        AbilityTrigger.BeforeDamage,
                        AbilityEffectType.RedirectDamage,
                        out _,
                        out AbilityEffectData effect) &&
                    !TryGetFirstEffect(
                        candidate,
                        AbilityTrigger.Passive,
                        AbilityEffectType.RedirectDamage,
                        out _,
                        out effect))
                {
                    continue;
                }

                int radius = effect.Radius > 0 ? effect.Radius : 1;
                int distance =
                    Mathf.Abs(candidate.BoardPosition.x - protectedTarget.BoardPosition.x) +
                    Mathf.Abs(candidate.BoardPosition.y - protectedTarget.BoardPosition.y);

                if (distance > radius) continue;

                if (bestProtector == null ||
                    distance < bestDistance ||
                    (distance == bestDistance && CompareRedirectPriority(candidate, bestProtector) < 0))
                {
                    bestProtector = candidate;
                    bestEffect = effect;
                    bestDistance = distance;
                }
            }

            if (bestProtector == null || bestEffect == null) return;
            if (!AbilityEffectRegistry.TryGet(AbilityEffectType.RedirectDamage, out IAbilityEffectExecutor executor)) return;

            var context = new AbilityExecutionContext(
                bestProtector,
                protectedTarget,
                protectedTarget.BoardPosition,
                damageContext: damageContext);

            executor.Execute(bestEffect, context);
        }

        private static void ApplyTriggeredEffects(
            PieceRuntimeState owner,
            AbilityTrigger trigger,
            AbilityEffectType effectType,
            DamageContext damageContext)
        {
            if (owner == null || owner.Definition == null) return;

            PieceAbilityDefinition[] abilities = owner.Definition.Abilities;

            for (int abilityIndex = 0; abilityIndex < abilities.Length; abilityIndex++)
            {
                PieceAbilityDefinition ability = abilities[abilityIndex];
                if (ability == null || ability.Trigger != trigger || ability.ActionCost != AbilityActionCost.None) continue;

                AbilityEffectData[] effects = ability.Effects;

                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                {
                    AbilityEffectData effect = effects[effectIndex];
                    if (effect == null || effect.EffectType != effectType) continue;
                    if (!AbilityEffectRegistry.TryGet(effectType, out IAbilityEffectExecutor executor)) continue;

                    var context = new AbilityExecutionContext(
                        owner,
                        damageContext.Target,
                        damageContext.Target != null ? damageContext.Target.BoardPosition : Vector2Int.zero,
                        damageContext: damageContext);

                    executor.Execute(effect, context);
                }
            }
        }

        private static bool TryGetFirstEffect(
            PieceRuntimeState piece,
            AbilityTrigger trigger,
            AbilityEffectType effectType,
            out PieceAbilityDefinition ability,
            out AbilityEffectData effect)
        {
            ability = null;
            effect = null;

            if (piece == null || piece.Definition == null) return false;

            PieceAbilityDefinition[] abilities = piece.Definition.Abilities;

            for (int abilityIndex = 0; abilityIndex < abilities.Length; abilityIndex++)
            {
                PieceAbilityDefinition candidateAbility = abilities[abilityIndex];
                if (candidateAbility == null ||
                    candidateAbility.Trigger != trigger ||
                    candidateAbility.ActionCost != AbilityActionCost.None)
                {
                    continue;
                }

                AbilityEffectData[] effects = candidateAbility.Effects;

                for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                {
                    AbilityEffectData candidateEffect = effects[effectIndex];
                    if (candidateEffect == null || candidateEffect.EffectType != effectType) continue;

                    ability = candidateAbility;
                    effect = candidateEffect;
                    return true;
                }
            }

            return false;
        }

        private static int CompareRedirectPriority(PieceRuntimeState left, PieceRuntimeState right)
        {
            int y = left.BoardPosition.y.CompareTo(right.BoardPosition.y);
            if (y != 0) return y;

            int x = left.BoardPosition.x.CompareTo(right.BoardPosition.x);
            if (x != 0) return x;

            string leftId = left.Definition != null ? left.Definition.PieceId : string.Empty;
            string rightId = right.Definition != null ? right.Definition.PieceId : string.Empty;
            return string.Compare(leftId, rightId, StringComparison.Ordinal);
        }
    }
}
