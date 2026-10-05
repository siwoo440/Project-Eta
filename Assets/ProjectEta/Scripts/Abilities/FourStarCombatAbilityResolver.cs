using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectEta.Battle;
using ProjectEta.Board;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class FourStarCombatAbilityResolver
    {
        private static readonly Vector2Int[] Cross4 =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private static readonly Vector2Int[] Around8 =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        public static void ProcessBeforeDamage(DamageContext context)
        {
            if (context == null || context.Target == null) return;

            ApplySourceBonuses(context);
            TemporaryDamageModifierService.ApplyAndConsume(context);
            ApplyGrandGuardianShare(context);
            ApplyBastionFortify(context);
        }

        public static void ProcessAfterAttack(CombatResult result)
        {
            if (result?.Attacker == null || result.Defender == null) return;
            if (result.DamageDealt <= 0) return;

            ApplyGrandCannonBarrage(result);
            ApplyArchmageSpell(result);
            ApplyWarClericHeal(result);
            ApplyStormKnightReposition(result);
        }

        public static int PreviewDamageBonus(
            PieceRuntimeState attacker,
            PieceRuntimeState target)
        {
            if (attacker == null || target == null || target.Definition == null) return 0;

            int bonus = TemporaryDamageModifierService.Peek(attacker);

            if (HasAbility(attacker, FourStarAbilityIds.ExecutionerExecute))
            {
                int maxHp = Mathf.Max(1, target.Definition.BaseHp);
                if (target.CurrentHp * 3 <= maxHp) bonus += 2;
            }

            if (HasAbility(attacker, FourStarAbilityIds.DeadeyeLongshot))
            {
                int distance = Mathf.Max(
                    Mathf.Abs(attacker.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(attacker.BoardPosition.y - target.BoardPosition.y));

                if (distance >= 5) bonus += 1;
            }

            return bonus;
        }

        public static bool HasAbility(PieceRuntimeState piece, string abilityId)
        {
            return FindAbility(piece, abilityId) != null;
        }

        public static PieceAbilityDefinition FindAbility(
            PieceRuntimeState piece,
            string abilityId)
        {
            if (piece?.Definition == null || string.IsNullOrWhiteSpace(abilityId)) return null;

            PieceAbilityDefinition[] abilities = piece.Definition.Abilities;

            for (int i = 0; i < abilities.Length; i++)
            {
                PieceAbilityDefinition ability = abilities[i];

                if (ability != null &&
                    string.Equals(
                        ability.AbilityId,
                        abilityId,
                        StringComparison.Ordinal))
                {
                    return ability;
                }
            }

            return null;
        }

        private static void ApplySourceBonuses(DamageContext context)
        {
            PieceRuntimeState source = context.Source;
            PieceRuntimeState target = context.Target;

            if (source == null || target == null || target.Definition == null) return;

            if (HasAbility(source, FourStarAbilityIds.SiegeChariotSiege))
            {
                FourStarMovementAbilityResolver.ProcessBeforeAttack(source, target);
            }

            if (HasAbility(source, FourStarAbilityIds.ExecutionerExecute))
            {
                int maxHp = Mathf.Max(1, target.Definition.BaseHp);

                if (target.CurrentHp * 3 <= maxHp)
                {
                    context.Amount += 2;
                }
            }

            if (HasAbility(source, FourStarAbilityIds.DeadeyeLongshot))
            {
                int distance = Mathf.Max(
                    Mathf.Abs(source.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(source.BoardPosition.y - target.BoardPosition.y));

                if (distance >= 5)
                {
                    context.Amount += 1;
                }
            }
        }

        private static void ApplyGrandGuardianShare(DamageContext context)
        {
            PieceRuntimeState target = context.Target;
            BoardState board = AbilityBoardRegistry.FindBoardContaining(target);
            if (board == null) return;

            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);
            PieceRuntimeState protector = null;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState candidate = pieces[i];

                if (candidate == null ||
                    candidate.IsDead ||
                    candidate.CurrentHp <= 1 ||
                    object.ReferenceEquals(candidate, target) ||
                    candidate.IsPlayerPiece != target.IsPlayerPiece ||
                    !HasAbility(candidate, FourStarAbilityIds.GrandGuardianShare))
                {
                    continue;
                }

                int distance = Mathf.Max(
                    Mathf.Abs(candidate.BoardPosition.x - target.BoardPosition.x),
                    Mathf.Abs(candidate.BoardPosition.y - target.BoardPosition.y));

                if (distance > 1) continue;

                if (protector == null ||
                    candidate.BoardPosition.y < protector.BoardPosition.y ||
                    (candidate.BoardPosition.y == protector.BoardPosition.y &&
                     candidate.BoardPosition.x < protector.BoardPosition.x))
                {
                    protector = candidate;
                }
            }

            if (protector == null || context.Amount <= 1) return;

            context.Amount = Mathf.Max(1, context.Amount - 1);
            protector.CurrentHp -= 1;
        }

        private static void ApplyBastionFortify(DamageContext context)
        {
            PieceRuntimeState target = context.Target;

            if (!HasAbility(target, FourStarAbilityIds.BastionFortify)) return;
            if (context.Amount <= 1) return;

            bool activated = target.TryConsumeBastionFortify();

            // Player Bastion은 EnemyTurn 시작 훅이 없으므로 그 턴 첫 피격에서 현재 이동 여부를 기준으로 지연 판정한다.
            if (!activated && target.IsPlayerPiece)
            {
                activated = target.TryUseLazyBastionFortify();
            }

            if (!activated) return;

            context.Amount = Mathf.Max(1, context.Amount - 2);
        }

        private static void ApplyGrandCannonBarrage(CombatResult result)
        {
            PieceRuntimeState attacker = result.Attacker;

            if (!HasAbility(attacker, FourStarAbilityIds.GrandCannonBarrage)) return;

            BoardState board = AbilityBoardRegistry.FindBoardContaining(attacker);
            if (board == null) return;

            Vector2Int center = result.Defender.BoardPosition;
            int hits = 0;

            for (int i = 0; i < Cross4.Length && hits < 2; i++)
            {
                TileState tile = board.GetTile(center + Cross4[i]);
                PieceRuntimeState splashTarget = tile?.OccupyingPiece;

                if (splashTarget == null ||
                    splashTarget.IsDead ||
                    splashTarget.IsPlayerPiece == attacker.IsPlayerPiece ||
                    object.ReferenceEquals(splashTarget, result.Defender))
                {
                    continue;
                }

                DamageResolver.ApplyDamage(
                    splashTarget,
                    1,
                    attacker,
                    null);

                hits++;

                if (splashTarget.IsDead)
                {
                    board.ClearPiece(splashTarget);
                    DestroyPieceView(splashTarget);
                }
            }
        }

        private static void ApplyArchmageSpell(CombatResult result)
        {
            if (result.Defender.IsDead) return;

            PieceAbilityDefinition ability = FindAbility(
                result.Attacker,
                FourStarAbilityIds.ArchmageSpellChoice);

            if (ability == null) return;

            AbilityEffectData[] effects = ability.Effects;

            // 현재 입력 UI가 선택형 상태를 직접 넘기지 않으므로
            // 속박 → 화상 → 독 순으로 실행 가능한 첫 효과를 기본 선택한다.
            AbilityEffectData selected =
                FindStatusEffect(effects, StatusEffectType.Root) ??
                FindStatusEffect(effects, StatusEffectType.Burn) ??
                FindStatusEffect(effects, StatusEffectType.Poison);

            if (selected == null) return;

            var executor = new ApplyStatusAbilityExecutor();
            var context = new AbilityExecutionContext(
                result.Attacker,
                result.Defender,
                result.Defender.BoardPosition);

            if (!executor.CanExecute(selected, context, out _))
            {
                for (int i = 0; i < effects.Length; i++)
                {
                    AbilityEffectData candidate = effects[i];
                    if (candidate == null ||
                        candidate.EffectType != AbilityEffectType.ApplyStatus)
                    {
                        continue;
                    }

                    if (executor.CanExecute(candidate, context, out _))
                    {
                        selected = candidate;
                        break;
                    }
                }
            }

            if (executor.CanExecute(selected, context, out _))
            {
                executor.Execute(selected, context);
            }
        }

        private static AbilityEffectData FindStatusEffect(
            AbilityEffectData[] effects,
            StatusEffectType type)
        {
            if (effects == null) return null;

            for (int i = 0; i < effects.Length; i++)
            {
                AbilityEffectData effect = effects[i];

                if (effect != null &&
                    effect.EffectType == AbilityEffectType.ApplyStatus &&
                    effect.StatusEffect != null &&
                    effect.StatusEffect.StatusType == type)
                {
                    return effect;
                }
            }

            return null;
        }

        private static void ApplyWarClericHeal(CombatResult result)
        {
            PieceRuntimeState attacker = result.Attacker;

            PieceAbilityDefinition ability = FindAbility(
                attacker,
                FourStarAbilityIds.WarClericBattleHeal);

            if (ability == null) return;

            BoardState board = AbilityBoardRegistry.FindBoardContaining(attacker);
            if (board == null) return;

            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);
            PieceRuntimeState healTarget = null;

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState candidate = pieces[i];

                if (candidate == null ||
                    candidate.IsDead ||
                    object.ReferenceEquals(candidate, attacker) ||
                    candidate.IsPlayerPiece != attacker.IsPlayerPiece ||
                    candidate.Definition == null ||
                    candidate.CurrentHp >= candidate.Definition.BaseHp)
                {
                    continue;
                }

                int distance = Mathf.Max(
                    Mathf.Abs(candidate.BoardPosition.x - attacker.BoardPosition.x),
                    Mathf.Abs(candidate.BoardPosition.y - attacker.BoardPosition.y));

                if (distance > 1) continue;

                if (healTarget == null ||
                    candidate.CurrentHp < healTarget.CurrentHp ||
                    (candidate.CurrentHp == healTarget.CurrentHp &&
                     string.Compare(
                         candidate.Definition.PieceId,
                         healTarget.Definition.PieceId,
                         StringComparison.Ordinal) < 0))
                {
                    healTarget = candidate;
                }
            }

            if (healTarget == null) return;

            AbilityEffectData healEffect = null;

            for (int i = 0; i < ability.Effects.Length; i++)
            {
                if (ability.Effects[i] != null &&
                    ability.Effects[i].EffectType == AbilityEffectType.Heal)
                {
                    healEffect = ability.Effects[i];
                    break;
                }
            }

            if (healEffect == null) return;

            new HealAbilityExecutor().Execute(
                healEffect,
                new AbilityExecutionContext(
                    attacker,
                    healTarget,
                    healTarget.BoardPosition,
                    board: board));
        }

        private static void ApplyStormKnightReposition(CombatResult result)
        {
            PieceRuntimeState attacker = result.Attacker;

            if (result.Defender.IsDead ||
                !HasAbility(attacker, FourStarAbilityIds.StormKnightReposition))
            {
                return;
            }

            BoardState board = AbilityBoardRegistry.FindBoardContaining(attacker);
            if (board == null) return;

            Vector2Int origin = attacker.BoardPosition;
            Vector2Int best = default;
            bool found = false;
            int bestSafety = int.MinValue;

            for (int i = 0; i < Around8.Length; i++)
            {
                Vector2Int candidate = origin + Around8[i];

                if (!board.IsInsideBoard(candidate) ||
                    !board.CanOccupyArea(
                        candidate,
                        attacker.Definition != null
                            ? attacker.Definition.OccupancySize
                            : Vector2Int.one,
                        attacker))
                {
                    continue;
                }

                int safety = SumEnemyDistance(
                    attacker,
                    candidate,
                    board);

                if (!found ||
                    safety > bestSafety ||
                    (safety == bestSafety &&
                     (candidate.y < best.y ||
                      (candidate.y == best.y && candidate.x < best.x))))
                {
                    found = true;
                    best = candidate;
                    bestSafety = safety;
                }
            }

            if (!found) return;

            board.ClearPiece(attacker);
            attacker.BoardPosition = best;
            board.TryOccupyArea(
                best,
                attacker.Definition != null
                    ? attacker.Definition.OccupancySize
                    : Vector2Int.one,
                attacker);

            MovePieceView(attacker, best);
        }

        private static int SumEnemyDistance(
            PieceRuntimeState actor,
            Vector2Int position,
            BoardState board)
        {
            int total = 0;
            IReadOnlyList<PieceRuntimeState> pieces = AbilityBoardRegistry.GetUniquePieces(board);

            for (int i = 0; i < pieces.Count; i++)
            {
                PieceRuntimeState piece = pieces[i];

                if (piece == null ||
                    piece.IsDead ||
                    piece.IsPlayerPiece == actor.IsPlayerPiece)
                {
                    continue;
                }

                total +=
                    Mathf.Abs(position.x - piece.BoardPosition.x) +
                    Mathf.Abs(position.y - piece.BoardPosition.y);
            }

            return total;
        }

        private static void MovePieceView(
            PieceRuntimeState state,
            Vector2Int destination)
        {
            BoardView boardView = UnityEngine.Object.FindFirstObjectByType<BoardView>();
            if (boardView == null) return;

            PieceView[] views =
                UnityEngine.Object.FindObjectsByType<PieceView>(
                    FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] != null &&
                    object.ReferenceEquals(views[i].RuntimeState, state))
                {
                    views[i].MoveTo(
                        destination,
                        boardView.TileSize);
                    return;
                }
            }
        }

        private static void DestroyPieceView(PieceRuntimeState state)
        {
            PieceView[] views =
                UnityEngine.Object.FindObjectsByType<PieceView>(
                    FindObjectsSortMode.None);

            for (int i = 0; i < views.Length; i++)
            {
                if (views[i] == null ||
                    !object.ReferenceEquals(views[i].RuntimeState, state))
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(views[i].gameObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(views[i].gameObject);
                }

                return;
            }
        }
    }
}
