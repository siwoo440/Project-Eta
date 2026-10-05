using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public sealed class AIActionCandidate
    {
        public PieceRuntimeState Actor { get; }
        public Vector2Int Origin { get; }
        public Vector2Int Target { get; }
        public AIActionType ActionType { get; }
        public PieceRuntimeState TargetPiece { get; }
        public PieceAbilityDefinition Ability { get; }
        public int Score { get; }

        // 기존 33~39일차 호출부 호환 생성자.
        public AIActionCandidate(
            PieceRuntimeState actor,
            Vector2Int origin,
            Vector2Int target,
            AIActionType actionType,
            PieceRuntimeState targetPiece,
            int score)
        {
            Actor = actor;
            Origin = origin;
            Target = target;
            ActionType = actionType;
            TargetPiece = targetPiece;
            Score = score;
            Ability = actionType == AIActionType.Ability
                ? AIAbilityCandidateRegistry.Resolve(actor, target, targetPiece)
                : null;
        }

        public AIActionCandidate(
            PieceRuntimeState actor,
            Vector2Int origin,
            Vector2Int target,
            PieceRuntimeState targetPiece,
            PieceAbilityDefinition ability,
            int score)
        {
            Actor = actor;
            Origin = origin;
            Target = target;
            ActionType = AIActionType.Ability;
            TargetPiece = targetPiece;
            Ability = ability;
            Score = score;

            AIAbilityCandidateRegistry.Register(actor, target, targetPiece, ability);
        }

        public override string ToString()
        {
            string actorName = Actor?.Definition != null ? Actor.Definition.DisplayName : "Unknown";
            string targetName = TargetPiece?.Definition != null ? TargetPiece.Definition.DisplayName : "-";
            string abilityName = Ability != null ? Ability.DisplayName : "-";
            return $"{ActionType} {actorName} {Origin}->{Target} Target={targetName} Ability={abilityName} Score={Score}";
        }
    }
}
