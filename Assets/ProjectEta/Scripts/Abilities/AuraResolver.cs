using System; // StringComparer 사용
using System.Collections.Generic; // Dictionary·IReadOnlyList 사용
using UnityEngine; // Mathf 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public static class AuraResolver
    {
        public static int GetAttack(PieceRuntimeState target, BoardState board = null)
        {
            if (target == null || target.Definition == null) return 0;

            int modifier = GetAttackModifier(target, board);
            return Mathf.Max(0, target.Definition.BaseAtk + modifier);
        }

        public static int GetAttackModifier(PieceRuntimeState target, BoardState board = null)
        {
            if (target == null || target.Definition == null || target.IsDead) return 0;

            board = board ?? AbilityBoardRegistry.FindBoardContaining(target);
            if (board == null) return 0;

            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);
            var strongestByGroup = new Dictionary<string, int>(StringComparer.Ordinal);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState source = pieces[i];

                if (source == null ||
                    source.IsDead ||
                    source.Definition == null ||
                    source.IsPlayerPiece != target.IsPlayerPiece)
                {
                    continue;
                }

                PieceAbilityDefinition[] abilities = source.Definition.Abilities;

                for (int abilityIndex = 0; abilityIndex < abilities.Length; abilityIndex++)
                {
                    PieceAbilityDefinition ability = abilities[abilityIndex];
                    if (ability == null ||
                        ability.Trigger != AbilityTrigger.Passive ||
                        ability.ActionCost != AbilityActionCost.None)
                    {
                        continue;
                    }

                    AbilityEffectData[] effects = ability.Effects;

                    for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
                    {
                        AbilityEffectData effect = effects[effectIndex];
                        if (effect == null || effect.EffectType != AbilityEffectType.Aura) continue;
                        if (object.ReferenceEquals(source, target) && !effect.IncludeSelf) continue;

                        int distance = Mathf.Max(
                            Mathf.Abs(source.BoardPosition.x - target.BoardPosition.x),
                            Mathf.Abs(source.BoardPosition.y - target.BoardPosition.y));

                        int radius = effect.Radius > 0 ? effect.Radius : 1;
                        if (distance > radius) continue;

                        string groupId = effect.AuraGroupId;

                        if (!strongestByGroup.TryGetValue(groupId, out int current) ||
                            effect.Amount > current)
                        {
                            strongestByGroup[groupId] = effect.Amount; // 동일 계열은 가장 높은 값 하나만 사용
                        }
                    }
                }
            }

            int total = 0;
            foreach (KeyValuePair<string, int> entry in strongestByGroup)
            {
                total += entry.Value; // 서로 다른 Aura 계열은 합산
            }

            return total;
        }
    }
}
