using System;
using UnityEngine;
using ProjectEta.Abilities;
using ProjectEta.Pieces;

namespace ProjectEta.Battle
{
    public class BattleHooks
    {
        public event Action<PieceRuntimeState, Vector2Int, Vector2Int> BeforeMove;
        public event Action<PieceRuntimeState, Vector2Int, Vector2Int> AfterMove;
        public event Action<PieceRuntimeState, PieceRuntimeState> BeforeAttack;
        public event Action<CombatResult> AfterAttack;
        public event Action<DamageContext> BeforeDamage;
        public event Action<PieceRuntimeState, PieceRuntimeState, int> AfterDamage;
        public event Action<TurnState, int> TurnStart;
        public event Action<TurnState, int> TurnEnd;

        public void RaiseBeforeMove(
            PieceRuntimeState piece,
            Vector2Int origin,
            Vector2Int destination)
        {
            FourStarMovementAbilityResolver.ProcessBeforeAction(piece);
            BeforeMove?.Invoke(piece, origin, destination);
        }

        public void RaiseAfterMove(
            PieceRuntimeState piece,
            Vector2Int origin,
            Vector2Int destination)
        {
            FourStarMovementAbilityResolver.ProcessAfterMove(
                piece,
                origin,
                destination);

            AfterMove?.Invoke(piece, origin, destination);
        }

        public void RaiseBeforeAttack(
            PieceRuntimeState attacker,
            PieceRuntimeState defender)
        {
            FourStarMovementAbilityResolver.ProcessBeforeAttack(
                attacker,
                defender);

            BeforeAttack?.Invoke(attacker, defender);
        }

        public void RaiseAfterAttack(CombatResult result)
        {
            FourStarCombatAbilityResolver.ProcessAfterAttack(result);
            AfterAttack?.Invoke(result);
        }

        public void RaiseBeforeDamage(DamageContext context)
        {
            BeforeDamage?.Invoke(context);
        }

        public void RaiseAfterDamage(
            PieceRuntimeState target,
            PieceRuntimeState source,
            int appliedAmount)
        {
            AfterDamage?.Invoke(
                target,
                source,
                appliedAmount);
        }

        public void RaiseTurnStart(
            TurnState state,
            int turnNumber)
        {
            FourStarTurnStateService.ProcessTurnStart(
                state,
                turnNumber);

            TurnStart?.Invoke(
                state,
                turnNumber);
        }

        public void RaiseTurnEnd(
            TurnState state,
            int turnNumber)
        {
            TurnEnd?.Invoke(
                state,
                turnNumber);
        }
    }
}
