using System.Collections.Generic; // IReadOnlyList·List 사용
using UnityEngine; // Vector2Int 사용
using ProjectEta.Pieces; // PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public sealed class AbilityExecutionResult
    {
        private readonly List<PieceRuntimeState> _affectedPieces;
        private readonly List<Vector2Int> _affectedTiles;

        public bool Success { get; }
        public string FailureReason { get; }
        public int Amount { get; }
        public bool ConsumedAction { get; }
        public PieceRuntimeState ActualTarget { get; }
        public IReadOnlyList<PieceRuntimeState> AffectedPieces => _affectedPieces;
        public IReadOnlyList<Vector2Int> AffectedTiles => _affectedTiles;

        private AbilityExecutionResult(
            bool success,
            string failureReason,
            int amount,
            bool consumedAction,
            PieceRuntimeState actualTarget,
            IEnumerable<PieceRuntimeState> affectedPieces,
            IEnumerable<Vector2Int> affectedTiles)
        {
            Success = success;
            FailureReason = failureReason ?? string.Empty;
            Amount = amount;
            ConsumedAction = consumedAction;
            ActualTarget = actualTarget;
            _affectedPieces = affectedPieces != null
                ? new List<PieceRuntimeState>(affectedPieces)
                : new List<PieceRuntimeState>();
            _affectedTiles = affectedTiles != null
                ? new List<Vector2Int>(affectedTiles)
                : new List<Vector2Int>();
        }

        public static AbilityExecutionResult Succeeded(
            int amount = 0,
            PieceRuntimeState actualTarget = null,
            bool consumedAction = false,
            IEnumerable<PieceRuntimeState> affectedPieces = null,
            IEnumerable<Vector2Int> affectedTiles = null)
        {
            return new AbilityExecutionResult(
                true,
                string.Empty,
                amount,
                consumedAction,
                actualTarget,
                affectedPieces,
                affectedTiles);
        }

        public static AbilityExecutionResult Failed(string reason)
        {
            return new AbilityExecutionResult(
                false,
                reason,
                0,
                false,
                null,
                null,
                null);
        }

        public AbilityExecutionResult WithConsumedAction(bool consumed)
        {
            return new AbilityExecutionResult(
                Success,
                FailureReason,
                Amount,
                consumed,
                ActualTarget,
                _affectedPieces,
                _affectedTiles);
        }
    }
}
