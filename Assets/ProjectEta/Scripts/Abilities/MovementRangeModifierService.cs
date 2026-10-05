using System;
using System.Collections.Generic;
using ProjectEta.Battle;
using ProjectEta.Pieces;

namespace ProjectEta.Abilities
{
    public static class MovementRangeModifierService
    {
        private static readonly List<MovementRangeModifierState> States =
            new List<MovementRangeModifierState>();

        private static readonly Dictionary<TurnManager, Action<TurnState, int>> TurnHandlers =
            new Dictionary<TurnManager, Action<TurnState, int>>();

        public static int ActiveCount => States.Count;

        public static void Register(
            PieceRuntimeState piece,
            AbilityEffectData effect,
            TurnManager turnManager,
            int durationPlayerTurns = 1)
        {
            if (piece == null || effect == null) return;

            int currentTurn = turnManager != null ? turnManager.TurnNumber : 0;
            int expires = currentTurn + Math.Max(1, durationPlayerTurns);

            States.Add(new MovementRangeModifierState(piece, effect, turnManager, expires));
            EnsureTurnSubscription(turnManager);
        }

        public static IReadOnlyList<AbilityEffectData> GetRuntimeEffects(PieceRuntimeState piece)
        {
            var result = new List<AbilityEffectData>();

            for (int i = 0; i < States.Count; i++)
            {
                MovementRangeModifierState state = States[i];

                if (state != null &&
                    object.ReferenceEquals(state.Piece, piece) &&
                    state.Effect != null)
                {
                    result.Add(state.Effect);
                }
            }

            return result;
        }

        public static void ClearAll()
        {
            foreach (KeyValuePair<TurnManager, Action<TurnState, int>> pair in TurnHandlers)
            {
                if (pair.Key != null) pair.Key.TurnChanged -= pair.Value;
            }

            TurnHandlers.Clear();
            States.Clear();
        }

        private static void EnsureTurnSubscription(TurnManager turnManager)
        {
            if (turnManager == null || TurnHandlers.ContainsKey(turnManager)) return;

            Action<TurnState, int> handler = null;
            handler = (state, turnNumber) =>
            {
                if (state == TurnState.BattleEnded)
                {
                    ClearForTurn(turnManager, true, turnNumber);
                    turnManager.TurnChanged -= handler;
                    TurnHandlers.Remove(turnManager);
                    return;
                }

                if (state == TurnState.PlayerTurn)
                {
                    ClearForTurn(turnManager, false, turnNumber);
                }
            };

            TurnHandlers.Add(turnManager, handler);
            turnManager.TurnChanged += handler;
        }

        private static void ClearForTurn(TurnManager turnManager, bool clearAll, int turnNumber)
        {
            for (int i = States.Count - 1; i >= 0; i--)
            {
                MovementRangeModifierState state = States[i];

                if (state == null || !object.ReferenceEquals(state.TurnManager, turnManager)) continue;
                if (!clearAll && turnNumber < state.ExpiresOnPlayerTurn) continue;

                States.RemoveAt(i);
            }
        }
    }
}
