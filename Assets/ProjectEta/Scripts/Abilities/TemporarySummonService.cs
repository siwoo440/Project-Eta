using System; // Action 사용
using System.Collections.Generic; // List·Dictionary 사용
using UnityEngine; // Application·Object 사용
using ProjectEta.Battle; // TurnManager·TurnState 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public static class TemporarySummonService
    {
        private sealed class Entry
        {
            public PieceRuntimeState Piece;
            public BoardState Board;
            public TurnManager TurnManager;
        }

        private static readonly List<Entry> Entries = new List<Entry>();
        private static readonly Dictionary<TurnManager, Action<TurnState, int>> TurnHandlers =
            new Dictionary<TurnManager, Action<TurnState, int>>();

        public static int ActiveCount => Entries.Count;

        public static void Register(PieceRuntimeState piece, BoardState board, TurnManager turnManager)
        {
            if (piece == null || board == null) return;

            Entries.Add(new Entry
            {
                Piece = piece,
                Board = board,
                TurnManager = turnManager
            });

            if (turnManager == null || TurnHandlers.ContainsKey(turnManager)) return;

            Action<TurnState, int> handler = null;
            handler = (state, _) =>
            {
                if (state != TurnState.BattleEnded) return;

                ClearForTurn(turnManager);
                turnManager.TurnChanged -= handler;
                TurnHandlers.Remove(turnManager);
            };

            TurnHandlers.Add(turnManager, handler);
            turnManager.TurnChanged += handler;
        }

        public static int ClearForTurn(TurnManager turnManager)
        {
            int cleared = 0;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                Entry entry = Entries[i];
                if (turnManager != null && !object.ReferenceEquals(entry.TurnManager, turnManager)) continue;

                ClearEntry(entry);
                Entries.RemoveAt(i);
                cleared++;
            }

            return cleared;
        }

        public static void ClearAll()
        {
            foreach (KeyValuePair<TurnManager, Action<TurnState, int>> pair in TurnHandlers)
            {
                if (pair.Key != null) pair.Key.TurnChanged -= pair.Value;
            }

            TurnHandlers.Clear();

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                ClearEntry(Entries[i]);
            }

            Entries.Clear();
        }

        private static void ClearEntry(Entry entry)
        {
            if (entry == null || entry.Piece == null) return;

            entry.Board?.ClearPiece(entry.Piece);

            PieceDefinition definition = entry.Piece.Definition;
            if (definition == null || !definition.IsRuntimeTemporarySummonDefinition) return;

            if (Application.isPlaying) UnityEngine.Object.Destroy(definition);
            else UnityEngine.Object.DestroyImmediate(definition);
        }
    }
}
