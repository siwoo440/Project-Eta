using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;
using UnityEngine;

namespace ProjectEta.AI
{
    public static class EnemyAICandidatePruner
    {
        public static List<AIActionCandidate> Prune(
            BoardState board,
            IReadOnlyList<AIActionCandidate> candidates,
            int maxCandidates,
            out int invalidOrDuplicateCount,
            out bool budgetCapped)
        {
            invalidOrDuplicateCount = 0;
            budgetCapped = false;

            var validCandidates = new List<AIActionCandidate>();

            if (board == null || candidates == null)
            {
                return validCandidates;
            }

            var uniqueKeys = new HashSet<CandidateKey>();

            for (int i = 0; i < candidates.Count; i++)
            {
                AIActionCandidate candidate = candidates[i];

                if (!IsStillLegal(board, candidate))
                {
                    invalidOrDuplicateCount++;
                    continue;
                }

                var key = new CandidateKey(candidate);

                if (!uniqueKeys.Add(key))
                {
                    invalidOrDuplicateCount++;
                    continue;
                }

                validCandidates.Add(candidate);
            }

            int safeBudget = Math.Max(1, maxCandidates);

            if (validCandidates.Count <= safeBudget)
            {
                return validCandidates;
            }

            budgetCapped = true;

            var attacks = new List<AIActionCandidate>();
            var abilities = new List<AIActionCandidate>();
            var moves = new List<AIActionCandidate>();

            for (int i = 0; i < validCandidates.Count; i++)
            {
                AIActionCandidate candidate = validCandidates[i];

                if (candidate.ActionType == AIActionType.Attack)
                {
                    attacks.Add(candidate);
                }
                else if (candidate.ActionType == AIActionType.Ability)
                {
                    abilities.Add(candidate);
                }
                else
                {
                    moves.Add(candidate);
                }
            }

            attacks.Sort(CompareByBasePriority);
            abilities.Sort(CompareByBasePriority);
            moves.Sort(CompareByBasePriority);

            var pruned = new List<AIActionCandidate>(safeBudget);

            for (int i = 0; i < attacks.Count && pruned.Count < safeBudget; i++)
            {
                pruned.Add(attacks[i]);
            }

            for (int i = 0; i < abilities.Count && pruned.Count < safeBudget; i++)
            {
                pruned.Add(abilities[i]);
            }

            for (int i = 0; i < moves.Count && pruned.Count < safeBudget; i++)
            {
                pruned.Add(moves[i]);
            }

            return pruned;
        }

        private static bool IsStillLegal(
            BoardState board,
            AIActionCandidate candidate)
        {
            if (candidate == null || candidate.Actor == null) return false;
            if (candidate.Actor.IsPlayerPiece || candidate.Actor.IsDead) return false;

            TileState originTile = board.GetTile(candidate.Origin);
            TileState targetTile = board.GetTile(candidate.Target);

            if (originTile == null || targetTile == null) return false;
            if (originTile.OccupyingPiece != candidate.Actor) return false;

            if (candidate.ActionType == AIActionType.Move)
            {
                return !targetTile.IsOccupied &&
                       !targetTile.IsBlockedByObstacle;
            }

            if (candidate.ActionType == AIActionType.Attack)
            {
                PieceRuntimeState targetPiece =
                    targetTile.OccupyingPiece;

                if (targetPiece == null ||
                    targetPiece != candidate.TargetPiece)
                {
                    return false;
                }

                return targetPiece.IsPlayerPiece &&
                       !targetPiece.IsDead;
            }

            if (candidate.ActionType == AIActionType.Ability)
            {
                if (candidate.Ability == null) return false;

                var context = new AbilityExecutionContext(
                    candidate.Actor,
                    candidate.TargetPiece,
                    candidate.Target,
                    board: board);

                return PieceAbilityService
                    .PreviewAbility(candidate.Ability, context)
                    .Success;
            }

            return false;
        }

        private static int CompareByBasePriority(
            AIActionCandidate a,
            AIActionCandidate b)
        {
            if (object.ReferenceEquals(a, b)) return 0;
            if (a == null) return 1;
            if (b == null) return -1;

            if (a.Score != b.Score)
            {
                return b.Score.CompareTo(a.Score);
            }

            if (a.ActionType != b.ActionType)
            {
                int aRank = GetActionRank(a.ActionType);
                int bRank = GetActionRank(b.ActionType);
                return aRank.CompareTo(bRank);
            }

            string aId =
                a.Actor?.Definition?.PieceId ?? string.Empty;

            string bId =
                b.Actor?.Definition?.PieceId ?? string.Empty;

            int idComparison = string.Compare(
                aId,
                bId,
                StringComparison.Ordinal);

            if (idComparison != 0) return idComparison;

            if (a.Origin.y != b.Origin.y)
            {
                return a.Origin.y.CompareTo(b.Origin.y);
            }

            if (a.Origin.x != b.Origin.x)
            {
                return a.Origin.x.CompareTo(b.Origin.x);
            }

            if (a.Target.y != b.Target.y)
            {
                return a.Target.y.CompareTo(b.Target.y);
            }

            return a.Target.x.CompareTo(b.Target.x);
        }

        private static int GetActionRank(AIActionType type)
        {
            switch (type)
            {
                case AIActionType.Attack: return 0;
                case AIActionType.Ability: return 1;
                default: return 2;
            }
        }

        private readonly struct CandidateKey : IEquatable<CandidateKey>
        {
            private readonly PieceRuntimeState _actor;
            private readonly Vector2Int _origin;
            private readonly Vector2Int _target;
            private readonly AIActionType _actionType;
            private readonly string _abilityId;

            public CandidateKey(AIActionCandidate candidate)
            {
                _actor = candidate.Actor;
                _origin = candidate.Origin;
                _target = candidate.Target;
                _actionType = candidate.ActionType;
                _abilityId = candidate.Ability?.AbilityId ?? string.Empty;
            }

            public bool Equals(CandidateKey other)
            {
                return object.ReferenceEquals(_actor, other._actor) &&
                       _origin == other._origin &&
                       _target == other._target &&
                       _actionType == other._actionType &&
                       string.Equals(
                           _abilityId,
                           other._abilityId,
                           StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is CandidateKey other &&
                       Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash =
                        _actor != null
                            ? RuntimeHelpers.GetHashCode(_actor)
                            : 0;

                    hash = (hash * 397) ^
                           _origin.GetHashCode();

                    hash = (hash * 397) ^
                           _target.GetHashCode();

                    hash = (hash * 397) ^
                           (int)_actionType;

                    hash = (hash * 397) ^
                           (_abilityId != null
                               ? _abilityId.GetHashCode()
                               : 0);

                    return hash;
                }
            }
        }
    }
}
