using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public static class AIAbilityCandidateRegistry
    {
        private static readonly Dictionary<Key, PieceAbilityDefinition> Abilities =
            new Dictionary<Key, PieceAbilityDefinition>();

        public static void Register(
            PieceRuntimeState actor,
            Vector2Int target,
            PieceRuntimeState targetPiece,
            PieceAbilityDefinition ability)
        {
            if (actor == null || ability == null) return;
            Abilities[new Key(actor, target, targetPiece)] = ability;
        }

        public static PieceAbilityDefinition Resolve(
            PieceRuntimeState actor,
            Vector2Int target,
            PieceRuntimeState targetPiece)
        {
            if (actor == null) return null;
            Abilities.TryGetValue(new Key(actor, target, targetPiece), out PieceAbilityDefinition ability);
            return ability;
        }

        public static void Clear()
        {
            Abilities.Clear();
        }

        private readonly struct Key : IEquatable<Key>
        {
            private readonly PieceRuntimeState _actor;
            private readonly Vector2Int _target;
            private readonly PieceRuntimeState _targetPiece;

            public Key(PieceRuntimeState actor, Vector2Int target, PieceRuntimeState targetPiece)
            {
                _actor = actor;
                _target = target;
                _targetPiece = targetPiece;
            }

            public bool Equals(Key other)
            {
                return object.ReferenceEquals(_actor, other._actor) &&
                       _target == other._target &&
                       object.ReferenceEquals(_targetPiece, other._targetPiece);
            }

            public override bool Equals(object obj)
            {
                return obj is Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _actor != null ? RuntimeHelpers.GetHashCode(_actor) : 0;
                    hash = (hash * 397) ^ _target.GetHashCode();
                    hash = (hash * 397) ^ (_targetPiece != null ? RuntimeHelpers.GetHashCode(_targetPiece) : 0);
                    return hash;
                }
            }
        }
    }
}
