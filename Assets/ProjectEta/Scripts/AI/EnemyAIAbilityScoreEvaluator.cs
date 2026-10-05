using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.AI
{
    public static class EnemyAIAbilityScoreEvaluator
    {
        public static int Evaluate(
            PieceRuntimeState actor,
            PieceRuntimeState targetPiece,
            Vector2Int targetPosition,
            PieceAbilityDefinition ability,
            BoardState board)
        {
            if (actor == null || ability == null || board == null) return int.MinValue / 4;

            var context = new AbilityExecutionContext(
                actor,
                targetPiece,
                targetPosition,
                board: board);

            AbilityExecutionResult preview = PieceAbilityService.PreviewAbility(ability, context);
            if (!preview.Success) return int.MinValue / 4;

            AbilityEffectData[] effects = ability.Effects;
            if (effects.Length == 0) return int.MinValue / 4;

            int score = 0;

            for (int i = 0; i < effects.Length; i++)
            {
                AbilityEffectData effect = effects[i];
                if (effect == null) continue;

                switch (effect.EffectType)
                {
                    case AbilityEffectType.Heal:
                        score += preview.Amount * 140;
                        if (targetPiece != null && targetPiece.Definition != null)
                        {
                            int missingHp = Mathf.Max(0, targetPiece.Definition.BaseHp - targetPiece.CurrentHp);
                            score += missingHp * 25;
                            if (targetPiece.CurrentHp * 2 <= Mathf.Max(1, targetPiece.Definition.BaseHp))
                            {
                                score += 220;
                            }
                        }
                        break;

                    case AbilityEffectType.ModifyMoveRange:
                        score += 180 + preview.Amount * 25;
                        if (targetPiece != null &&
                            (targetPiece.Definition.RoleTags & PieceRoleTag.Attacker) != 0)
                        {
                            score += 90;
                        }
                        break;

                    case AbilityEffectType.Summon:
                        score += 260;
                        if (effect.SummonPiece != null)
                        {
                            score += Mathf.Max(0, effect.SummonPiece.BaseHp) * 12;
                            score += Mathf.Max(0, effect.SummonPiece.BaseAtk) * 24;
                        }

                        score += EvaluateSummonPosition(board, actor, targetPosition);
                        break;
                }
            }

            return score;
        }

        private static int EvaluateSummonPosition(
            BoardState board,
            PieceRuntimeState actor,
            Vector2Int position)
        {
            int score = 0;

            for (int x = 0; x < BoardState.Width; x++)
            {
                for (int y = 0; y < BoardState.Height; y++)
                {
                    PieceRuntimeState piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece;
                    if (piece == null || piece.IsDead || piece.IsPlayerPiece == actor.IsPlayerPiece) continue;

                    int distance = Mathf.Abs(position.x - piece.BoardPosition.x) +
                                   Mathf.Abs(position.y - piece.BoardPosition.y);

                    if (distance == 1) score += 80;
                    else if (distance == 2) score += 40;
                }
            }

            return score;
        }
    }
}
