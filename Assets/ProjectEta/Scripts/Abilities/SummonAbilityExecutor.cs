using System.Collections.Generic; // List 사용
using UnityEngine; // Vector2Int·Object 사용
using ProjectEta.Board; // BoardState 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용

namespace ProjectEta.Abilities
{
    public sealed class SummonAbilityExecutor : IAbilityEffectExecutor
    {
        public AbilityEffectType EffectType => AbilityEffectType.Summon;

        public bool CanExecute(AbilityEffectData effect, AbilityExecutionContext context, out string failureReason)
        {
            failureReason = string.Empty;

            if (effect == null || context == null || context.Owner == null)
            {
                failureReason = "Summon 소유자 또는 실행 데이터가 없습니다.";
                return false;
            }

            if (effect.SummonPiece == null)
            {
                failureReason = "소환할 PieceDefinition이 없습니다.";
                return false;
            }

            if (effect.MaxActiveSummons > 0 &&
                TemporarySummonService.CountForSource(context.Owner) >= effect.MaxActiveSummons)
            {
                failureReason = "현재 소환자가 유지할 수 있는 임시 소환물 상한에 도달했습니다.";
                return false;
            }

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            if (board == null)
            {
                failureReason = "소환에 사용할 BoardState가 없습니다.";
                return false;
            }

            Vector2Int footprint = GetSafeFootprint(effect.SummonPiece);
            if (!board.CanOccupyArea(context.TargetPosition, footprint))
            {
                failureReason = "대상 위치에 소환물을 배치할 수 없습니다.";
                return false;
            }

            return true;
        }

        public AbilityExecutionResult Preview(AbilityEffectData effect, AbilityExecutionContext context)
        {
            if (!CanExecute(effect, context, out string reason))
            {
                return AbilityExecutionResult.Failed(reason);
            }

            return AbilityExecutionResult.Succeeded(
                1,
                null,
                false,
                null,
                GetFootprintTiles(context.TargetPosition, GetSafeFootprint(effect.SummonPiece)));
        }

        public AbilityExecutionResult Execute(AbilityEffectData effect, AbilityExecutionContext context)
        {
            AbilityExecutionResult preview = Preview(effect, context);
            if (!preview.Success) return preview;

            BoardState board = context.Board ?? AbilityBoardRegistry.FindBoardContaining(context.Owner);
            PieceDefinition runtimeDefinition = effect.SummonPiece.CreateTemporarySummonRuntimeDefinition();
            var summoned = new PieceRuntimeState(runtimeDefinition, context.TargetPosition, context.Owner.IsPlayerPiece);
            summoned.MarkAsTemporarySummon();

            Vector2Int footprint = GetSafeFootprint(runtimeDefinition);

            if (!board.TryOccupyArea(context.TargetPosition, footprint, summoned))
            {
                if (Application.isPlaying) UnityEngine.Object.Destroy(runtimeDefinition);
                else UnityEngine.Object.DestroyImmediate(runtimeDefinition);

                return AbilityExecutionResult.Failed("소환 직전 Board 상태가 변경되어 배치에 실패했습니다.");
            }

            TemporarySummonService.Register(summoned, context.Owner, board, context.TurnManager);

            return AbilityExecutionResult.Succeeded(
                1,
                summoned,
                false,
                new[] { summoned },
                GetFootprintTiles(context.TargetPosition, footprint));
        }

        private static Vector2Int GetSafeFootprint(PieceDefinition definition)
        {
            if (definition == null) return Vector2Int.one;
            Vector2Int size = definition.OccupancySize;
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y));
        }

        private static IReadOnlyList<Vector2Int> GetFootprintTiles(Vector2Int anchor, Vector2Int size)
        {
            var tiles = new List<Vector2Int>();

            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    tiles.Add(anchor + new Vector2Int(x, y));
                }
            }

            return tiles;
        }
    }
}
