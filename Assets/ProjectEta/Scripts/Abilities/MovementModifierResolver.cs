using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class MovementModifierResolver
    {
        private static readonly Vector2Int[] EightDirections =
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        public static void Apply(PieceRuntimeState piece, BoardState board, MovementResult result)
        {
            if (piece == null || board == null || result == null) return;

            if (piece.CanMove || piece.CanAttack)
            {
                ApplyPassiveEffects(piece, board, result);
                ApplyRuntimeEffects(piece, board, result);
                FourStarMovementAbilityResolver.ApplyMovementCandidates(piece, board, result);
            }

            RemoveBlockedCandidates(board, result);
        }

        public static MovementResult PreviewEffect(
            PieceRuntimeState piece,
            BoardState board,
            AbilityEffectData effect)
        {
            var result = new MovementResult();

            if (piece == null || board == null || effect == null) return result;

            AddEffectCandidates(piece, board, result, effect);
            RemoveBlockedCandidates(board, result);
            return result;
        }

        private static void ApplyPassiveEffects(
            PieceRuntimeState piece,
            BoardState board,
            MovementResult result)
        {
            if (piece.Definition == null) return;

            PieceAbilityDefinition[] abilities = piece.Definition.Abilities;

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

                    if (effect != null && effect.EffectType == AbilityEffectType.ModifyMoveRange)
                    {
                        AddEffectCandidates(piece, board, result, effect);
                    }
                }
            }
        }

        private static void ApplyRuntimeEffects(
            PieceRuntimeState piece,
            BoardState board,
            MovementResult result)
        {
            IReadOnlyList<AbilityEffectData> effects =
                MovementRangeModifierService.GetRuntimeEffects(piece);

            for (int i = 0; i < effects.Count; i++)
            {
                AddEffectCandidates(piece, board, result, effects[i]);
            }
        }

        private static void AddEffectCandidates(
            PieceRuntimeState piece,
            BoardState board,
            MovementResult result,
            AbilityEffectData effect)
        {
            int maxSteps = Mathf.Max(0, effect.Amount);
            if (maxSteps <= 0) return;

            Vector2Int[] directions =
                effect.Vector != Vector2Int.zero
                    ? new[] { effect.Vector }
                    : EightDirections;

            for (int d = 0; d < directions.Length; d++)
            {
                Vector2Int direction = directions[d];
                if (direction == Vector2Int.zero) continue;

                for (int step = 1; step <= maxSteps; step++)
                {
                    Vector2Int target = piece.BoardPosition + direction * step;
                    if (!board.IsInsideBoard(target)) break;

                    TileState tile = board.GetTile(target);
                    if (tile == null || tile.IsBlocked) break;

                    if (!tile.IsOccupied)
                    {
                        if (piece.CanMove &&
                            board.CanOccupyArea(
                                target,
                                GetSafeFootprint(piece.Definition),
                                piece))
                        {
                            result.AddMove(target);
                        }

                        continue;
                    }

                    if (piece.CanAttack &&
                        tile.OccupyingPiece != null &&
                        tile.OccupyingPiece.IsPlayerPiece != piece.IsPlayerPiece)
                    {
                        result.AddAttack(target);
                    }

                    break;
                }
            }
        }

        private static void RemoveBlockedCandidates(BoardState board, MovementResult result)
        {
            result.MoveTiles.RemoveAll(position =>
            {
                TileState tile = board.GetTile(position);
                return tile == null || tile.IsBlocked;
            });

            result.AttackTiles.RemoveAll(position =>
            {
                TileState tile = board.GetTile(position);
                return tile == null || tile.IsBlocked;
            });
        }

        private static Vector2Int GetSafeFootprint(PieceDefinition definition)
        {
            if (definition == null) return Vector2Int.one;

            Vector2Int size = definition.OccupancySize;
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }
    }
}
