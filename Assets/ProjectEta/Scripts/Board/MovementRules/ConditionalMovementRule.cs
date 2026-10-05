using UnityEngine; // Vector2Int·Mathf 사용
using ProjectEta.Pieces; // 조건부 이동 타입 사용

namespace ProjectEta.Board
{
    public sealed class ConditionalMovementRule : IMovementRule
    {
        private static readonly Vector2Int[] OrthogonalDirections =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        }; // 상하좌우 방향

        private static readonly Vector2Int[] DiagonalDirections =
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        }; // 대각선 방향

        private static readonly Vector2Int[] AllDirections =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down,
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        }; // 8방향

        private readonly MovementConditionType _condition; // 실행할 조건부 이동 규칙

        public ConditionalMovementRule(MovementConditionType condition)
        {
            _condition = condition; // 조건 저장
        }

        public MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            switch (_condition)
            {
                case MovementConditionType.Pawn:
                    return ResolvePawn(origin, isPlayerPiece, board);
                case MovementConditionType.ChameleonCycle:
                    return ResolveChameleon(origin, isPlayerPiece, board);
                case MovementConditionType.Spearman:
                    return ResolveSpearman(origin, isPlayerPiece, board);
                case MovementConditionType.Shooter:
                    return ResolveShooter(origin, isPlayerPiece, board);
                case MovementConditionType.ShieldGuard:
                    return ResolveShieldGuard(origin, isPlayerPiece, board);
                case MovementConditionType.FlagBearer:
                    return ResolveFlagBearer(origin, isPlayerPiece, board);
                case MovementConditionType.Pursuer:
                    return ResolvePursuer(origin, isPlayerPiece, board);
                case MovementConditionType.Scout:
                    return ResolveScout(origin, isPlayerPiece, board);
                case MovementConditionType.Grenadier:
                    return ResolveGrenadier(origin, isPlayerPiece, board);
                case MovementConditionType.Pikeman:
                    return ResolvePikeman(origin, isPlayerPiece, board);
                case MovementConditionType.Crossbowman:
                    return ResolveCrossbowman(origin, isPlayerPiece, board);
                case MovementConditionType.Gryphon:
                    return ResolveGryphon(origin, isPlayerPiece, board);
                case MovementConditionType.Artillery:
                    return ResolveArtillery(origin, isPlayerPiece, board);
                case MovementConditionType.Vanguard:
                    return ResolveVanguard(origin, isPlayerPiece, board);
                case MovementConditionType.Sniper:
                    return ResolveSniper(origin, isPlayerPiece, board);
                case MovementConditionType.GrandGryphon:
                    return ResolveGrandGryphon(origin, isPlayerPiece, board);
                case MovementConditionType.IllusionistCycle:
                    return ResolveIllusionistCycle(origin, isPlayerPiece, board);
                case MovementConditionType.Deadeye:
                    return ResolveDeadeye(origin, isPlayerPiece, board);
                case MovementConditionType.PhantomGeneralCycle:
                    return ResolvePhantomGeneralCycle(origin, isPlayerPiece, board);
                default:
                    return new MovementResult();
            }
        }

        private static MovementResult ResolvePawn(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            Vector2Int forward = GetForward(isPlayerPiece);
            Vector2Int oneStep = origin + forward;
            bool oneStepClear = board.IsInsideBoard(oneStep) &&
                                !board.GetTile(oneStep).IsOccupied &&
                                !board.GetTile(oneStep).IsBlockedByObstacle;

            if (oneStepClear)
            {
                result.AddMove(oneStep);

                Vector2Int twoStep = origin + forward * 2;
                if (board.IsInsideBoard(twoStep) &&
                    !board.GetTile(twoStep).IsOccupied &&
                    !board.GetTile(twoStep).IsBlockedByObstacle)
                {
                    result.AddMove(twoStep);
                }
            }

            Vector2Int[] attackOffsets =
            {
                forward,
                forward + Vector2Int.left,
                forward + Vector2Int.right
            };

            foreach (Vector2Int offset in attackOffsets)
            {
                Vector2Int target = origin + offset;
                if (!board.IsInsideBoard(target)) continue;

                TileState tile = board.GetTile(target);
                if (tile.IsOccupied && tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece)
                {
                    result.AddAttack(target);
                }
            }

            return result;
        }

        private static MovementResult ResolveChameleon(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            if (board == null || !board.IsInsideBoard(origin)) return new MovementResult();

            TileState tile = board.GetTile(origin);
            PieceRuntimeState piece = tile != null ? tile.OccupyingPiece : null;
            if (piece == null) return new MovementResult();

            PieceMovementType movementType;
            switch (piece.MovementCycleIndex)
            {
                case 1:
                    movementType = PieceMovementType.Bishop;
                    break;
                case 2:
                    movementType = PieceMovementType.Rook;
                    break;
                case 3:
                    movementType = PieceMovementType.Queen;
                    break;
                default:
                    movementType = PieceMovementType.Knight;
                    break;
            }

            return MovementRuleFactory.CreateLegacy(movementType).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveSpearman(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddEmptyMove(result, origin + direction, board);

                for (int distance = 1; distance <= 2; distance++)
                {
                    Vector2Int target = origin + direction * distance;
                    if (!board.IsInsideBoard(target)) break;

                    TileState tile = board.GetTile(target);
                    if (tile == null || tile.IsBlockedByObstacle) break;
                    if (!tile.IsOccupied) continue;

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
                    break;
                }
            }

            return result;
        }

        private static MovementResult ResolveShooter(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int forward = GetForward(isPlayerPiece);
            Vector2Int[] directions =
            {
                forward + Vector2Int.left,
                forward + Vector2Int.right
            };

            return new SlideMovementRule(directions, BoardState.Width).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveShieldGuard(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int backward = -GetForward(isPlayerPiece);
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                backward
            };

            return new StepMovementRule(directions, 1).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveFlagBearer(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int forward = GetForward(isPlayerPiece);
            Vector2Int[] directions =
            {
                Vector2Int.left,
                Vector2Int.right,
                forward
            };

            return new StepMovementRule(directions, 1).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolvePursuer(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int forward = GetForward(isPlayerPiece);
            Vector2Int backward = -forward;
            Vector2Int[] directions =
            {
                forward,
                backward + Vector2Int.left,
                backward + Vector2Int.right
            };

            return new StepMovementRule(directions, 1).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveScout(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int forward = GetForward(isPlayerPiece);
            Vector2Int[] directions =
            {
                forward + Vector2Int.left,
                forward + Vector2Int.right,
                Vector2Int.left,
                Vector2Int.right
            };

            return new StepMovementRule(directions, 1).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveGrenadier(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddEmptyMove(result, origin + direction, board); // 직교 1칸 이동
            }

            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -3; dy <= 3; dy++)
                {
                    int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                    if (distance < 2 || distance > 3) continue;

                    Vector2Int target = origin + new Vector2Int(dx, dy);
                    AddEnemyAttack(result, target, isPlayerPiece, board); // 거리 2~3 지정 공격
                }
            }

            return result;
        }

        private static MovementResult ResolvePikeman(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddEmptyMove(result, origin + direction, board);

                for (int distance = 1; distance <= 3; distance++)
                {
                    Vector2Int target = origin + direction * distance;
                    if (!board.IsInsideBoard(target)) break;

                    TileState tile = board.GetTile(target);
                    if (tile == null || tile.IsBlockedByObstacle) break;
                    if (!tile.IsOccupied) continue;

                    if (distance >= 2 && tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece)
                    {
                        result.AddAttack(target);
                    }

                    break;
                }
            }

            return result;
        }

        private static MovementResult ResolveCrossbowman(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            Vector2Int backward = -GetForward(isPlayerPiece);
            AddEmptyMove(result, origin + Vector2Int.left, board);
            AddEmptyMove(result, origin + Vector2Int.right, board);
            AddEmptyMove(result, origin + backward, board);

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddFirstEnemyOnRay(result, origin, direction, 5, isPlayerPiece, board);
            }

            return result;
        }

        private static MovementResult ResolveGryphon(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int diagonal in DiagonalDirections)
            {
                Vector2Int entry = origin + diagonal;
                if (!board.IsInsideBoard(entry)) continue;

                TileState entryTile = board.GetTile(entry);
                if (entryTile == null || entryTile.IsBlockedByObstacle) continue;

                if (entryTile.IsOccupied)
                {
                    if (entryTile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(entry);
                    continue;
                }

                result.AddMove(entry);

                Vector2Int[] outward =
                {
                    new Vector2Int(diagonal.x, 0),
                    new Vector2Int(0, diagonal.y)
                };

                foreach (Vector2Int direction in outward)
                {
                    for (int step = 1; step <= BoardState.Width; step++)
                    {
                        Vector2Int target = entry + direction * step;
                        if (!board.IsInsideBoard(target)) break;

                        TileState tile = board.GetTile(target);
                        if (tile == null || tile.IsBlockedByObstacle) break;

                        if (!tile.IsOccupied)
                        {
                            result.AddMove(target);
                            continue;
                        }

                        if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
                        break;
                    }
                }
            }

            return result;
        }

        private static MovementResult ResolveArtillery(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                bool hasScreen = false;

                for (int step = 1; step <= BoardState.Width; step++)
                {
                    Vector2Int target = origin + direction * step;
                    if (!board.IsInsideBoard(target)) break;

                    TileState tile = board.GetTile(target);
                    if (tile == null || tile.IsBlockedByObstacle) break;

                    if (!hasScreen)
                    {
                        if (!tile.IsOccupied)
                        {
                            result.AddMove(target); // 스크린 전까지 성채형 이동
                            continue;
                        }

                        hasScreen = true; // 첫 기물을 포격 스크린으로 사용
                        continue;
                    }

                    if (!tile.IsOccupied) continue;

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
                    break; // 스크린 뒤 첫 기물에서 종료
                }
            }

            return result;
        }

        private static MovementResult ResolveVanguard(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            Vector2Int forward = GetForward(isPlayerPiece);
            result.MergeFrom(new StepMovementRule(new[] { forward }, 3).Resolve(origin, isPlayerPiece, board));
            result.MergeFrom(new StepMovementRule(new[] { Vector2Int.left, Vector2Int.right }, 1).Resolve(origin, isPlayerPiece, board));
            return result;
        }

        private static MovementResult ResolveSniper(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddEmptyMove(result, origin + direction, board); // 직교 1칸 이동
            }

            foreach (Vector2Int direction in AllDirections)
            {
                AddFirstEnemyOnRay(result, origin, direction, BoardState.Width, isPlayerPiece, board);
            }

            return result;
        }


        private static MovementResult ResolveGrandGryphon(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = ResolveGryphon(origin, isPlayerPiece, board); // 정그리폰 이동 먼저 계산
            if (board == null) return result;

            foreach (Vector2Int orthogonal in OrthogonalDirections)
            {
                Vector2Int entry = origin + orthogonal; // 직선 1칸 진입
                if (!board.IsInsideBoard(entry)) continue;

                TileState entryTile = board.GetTile(entry);
                if (entryTile == null || entryTile.IsBlockedByObstacle) continue;

                if (entryTile.IsOccupied)
                {
                    if (entryTile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(entry);
                    continue;
                }

                result.AddMove(entry);

                Vector2Int[] outward;
                if (orthogonal.x != 0)
                {
                    outward = new[]
                    {
                        new Vector2Int(orthogonal.x, 1),
                        new Vector2Int(orthogonal.x, -1)
                    };
                }
                else
                {
                    outward = new[]
                    {
                        new Vector2Int(1, orthogonal.y),
                        new Vector2Int(-1, orthogonal.y)
                    };
                }

                foreach (Vector2Int direction in outward)
                {
                    for (int step = 1; step <= BoardState.Width; step++)
                    {
                        Vector2Int target = entry + direction * step;
                        if (!board.IsInsideBoard(target)) break;

                        TileState tile = board.GetTile(target);
                        if (tile == null || tile.IsBlockedByObstacle) break;

                        if (!tile.IsOccupied)
                        {
                            result.AddMove(target);
                            continue;
                        }

                        if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
                        break;
                    }
                }
            }

            return result;
        }

        private static MovementResult ResolveIllusionistCycle(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            PieceRuntimeState piece = GetRuntimePiece(origin, board);
            int cycle = piece != null ? piece.MovementCycleIndex : 0;

            switch (cycle)
            {
                case 1:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Bishop).Resolve(origin, isPlayerPiece, board);
                case 2:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Rook).Resolve(origin, isPlayerPiece, board);
                default:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Knight).Resolve(origin, isPlayerPiece, board);
            }
        }

        private static MovementResult ResolveDeadeye(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult();
            if (board == null) return result;

            foreach (Vector2Int direction in OrthogonalDirections)
            {
                AddEmptyMove(result, origin + direction, board); // 직교 인접 1칸 이동
            }

            foreach (Vector2Int direction in AllDirections)
            {
                AddFirstEnemyOnRay(result, origin, direction, 8, isPlayerPiece, board); // 최대 8칸 사격
            }

            return result;
        }

        private static MovementResult ResolvePhantomGeneralCycle(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            PieceRuntimeState piece = GetRuntimePiece(origin, board);
            int cycle = piece != null ? piece.MovementCycleIndex : 0;

            switch (cycle)
            {
                case 1:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Bishop).Resolve(origin, isPlayerPiece, board);
                case 2:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Rook).Resolve(origin, isPlayerPiece, board);
                case 3:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Queen).Resolve(origin, isPlayerPiece, board);
                case 4:
                    return ResolveGrenadier(origin, isPlayerPiece, board);
                default:
                    return MovementRuleFactory.CreateLegacy(PieceMovementType.Knight).Resolve(origin, isPlayerPiece, board);
            }
        }

        private static PieceRuntimeState GetRuntimePiece(Vector2Int origin, BoardState board)
        {
            if (board == null || !board.IsInsideBoard(origin)) return null;
            TileState tile = board.GetTile(origin);
            return tile != null ? tile.OccupyingPiece : null;
        }

        private static void AddEmptyMove(MovementResult result, Vector2Int target, BoardState board)
        {
            if (result == null || board == null || !board.IsInsideBoard(target)) return;

            TileState tile = board.GetTile(target);
            if (tile != null && !tile.IsBlockedByObstacle && !tile.IsOccupied) result.AddMove(target);
        }

        private static void AddEnemyAttack(MovementResult result, Vector2Int target, bool isPlayerPiece, BoardState board)
        {
            if (result == null || board == null || !board.IsInsideBoard(target)) return;

            TileState tile = board.GetTile(target);
            if (tile == null || tile.IsBlockedByObstacle || !tile.IsOccupied) return;
            if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
        }

        private static void AddFirstEnemyOnRay(
            MovementResult result,
            Vector2Int origin,
            Vector2Int direction,
            int maxDistance,
            bool isPlayerPiece,
            BoardState board)
        {
            for (int distance = 1; distance <= maxDistance; distance++)
            {
                Vector2Int target = origin + direction * distance;
                if (!board.IsInsideBoard(target)) break;

                TileState tile = board.GetTile(target);
                if (tile == null || tile.IsBlockedByObstacle) break;
                if (!tile.IsOccupied) continue;

                if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) result.AddAttack(target);
                break;
            }
        }

        private static Vector2Int GetForward(bool isPlayerPiece)
        {
            return isPlayerPiece ? Vector2Int.up : Vector2Int.down;
        }
    }
}
