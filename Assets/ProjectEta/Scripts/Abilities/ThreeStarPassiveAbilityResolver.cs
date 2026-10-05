using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class ThreeStarPassiveAbilityResolver
    {
        public const string PaladinGuardId = "three_paladin_guard";
        public const string WarChariotChargeId = "three_war_chariot_charge";
        public const string GuardianEscortId = "three_guardian_escort";
        public const string HunterTrackingId = "three_hunter_tracking";
        public const string VanguardFrontlineId = "three_vanguard_frontline";

        public static void ProcessBeforeDamage(DamageContext context)
        {
            if (context == null || context.Target == null) return;

            ApplySourceDamageCondition(context);
            ApplyAdjacentProtection(context);
        }

        private static void ApplySourceDamageCondition(DamageContext context)
        {
            PieceRuntimeState source = context.Source;
            PieceRuntimeState target = context.Target;

            if (source == null || target == null || source.Definition == null || target.Definition == null) return;

            if (HasAbility(source, WarChariotChargeId) && source.LastMoveDistance >= 3)
            {
                context.Amount += 1;
                source.ClearLastMoveDistance();
            }

            if (HasAbility(source, HunterTrackingId))
            {
                int maxHp = Mathf.Max(1, target.Definition.BaseHp);
                if (target.CurrentHp * 2 <= maxHp)
                {
                    context.Amount += 1;
                }
            }

            if (HasAbility(source, VanguardFrontlineId))
            {
                int deltaY = target.BoardPosition.y - source.BoardPosition.y;
                bool isForward = source.IsPlayerPiece ? deltaY > 0 : deltaY < 0;

                if (isForward)
                {
                    context.Amount += 1;
                }
            }
        }

        private static void ApplyAdjacentProtection(DamageContext context)
        {
            PieceRuntimeState target = context.Target;
            BoardState board = AbilityBoardRegistry.FindBoardContaining(target);
            if (board == null) return;

            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);
            PieceRuntimeState chosenProtector = null;
            string chosenAbilityId = null;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState candidate = pieces[i];

                if (candidate == null ||
                    candidate.IsDead ||
                    candidate.Definition == null ||
                    object.ReferenceEquals(candidate, target) ||
                    candidate.IsPlayerPiece != target.IsPlayerPiece)
                {
                    continue;
                }

                int distance = Mathf.Max(
                    Mathf.Abs(candidate.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(candidate.BoardPosition.y - target.BoardPosition.y));

                if (distance > 1) continue;

                string abilityId = null;

                if (HasAbility(candidate, GuardianEscortId) && IsKing(target))
                {
                    abilityId = GuardianEscortId;
                }
                else if (HasAbility(candidate, PaladinGuardId))
                {
                    abilityId = PaladinGuardId;
                }

                if (string.IsNullOrWhiteSpace(abilityId) || candidate.HasUsedBattleAbility(abilityId)) continue;

                if (chosenProtector == null ||
                    CompareProtectionPriority(candidate, abilityId, chosenProtector, chosenAbilityId) < 0)
                {
                    chosenProtector = candidate;
                    chosenAbilityId = abilityId;
                }
            }

            if (chosenProtector == null || string.IsNullOrWhiteSpace(chosenAbilityId)) return;
            if (!chosenProtector.TryMarkBattleAbilityUsed(chosenAbilityId)) return;

            context.Amount = Mathf.Max(1, context.Amount - 1);
        }

        private static int CompareProtectionPriority(
            PieceRuntimeState left,
            string leftAbilityId,
            PieceRuntimeState right,
            string rightAbilityId)
        {
            bool leftGuardian = string.Equals(leftAbilityId, GuardianEscortId, StringComparison.Ordinal);
            bool rightGuardian = string.Equals(rightAbilityId, GuardianEscortId, StringComparison.Ordinal);

            if (leftGuardian != rightGuardian) return leftGuardian ? -1 : 1;

            int y = left.BoardPosition.y.CompareTo(right.BoardPosition.y);
            if (y != 0) return y;

            int x = left.BoardPosition.x.CompareTo(right.BoardPosition.x);
            if (x != 0) return x;

            return string.Compare(
                left.Definition?.PieceId ?? string.Empty,
                right.Definition?.PieceId ?? string.Empty,
                StringComparison.Ordinal);
        }

        public static bool HasAbility(PieceRuntimeState piece, string abilityId)
        {
            if (piece?.Definition == null || string.IsNullOrWhiteSpace(abilityId)) return false;

            PieceAbilityDefinition[] abilities = piece.Definition.Abilities;

            for (int i = 0; i < abilities.Length; i++)
            {
                PieceAbilityDefinition ability = abilities[i];
                if (ability != null &&
                    string.Equals(ability.AbilityId, abilityId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsKing(PieceRuntimeState piece)
        {
            if (piece?.Definition == null) return false;
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true;
            return piece.Definition.MovementType == PieceMovementType.King;
        }
    }
}
