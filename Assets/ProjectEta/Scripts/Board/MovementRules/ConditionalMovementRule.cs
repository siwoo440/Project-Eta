using UnityEngine; // Vector2Int 사용
using ProjectEta.Pieces; // 조건부 이동 타입 사용

namespace ProjectEta.Board
{
    public sealed class ConditionalMovementRule : IMovementRule
    {
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
                default:
                    return new MovementResult();
            }
        }

        private static MovementResult ResolvePawn(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            var result = new MovementResult(); // 폰 결과 생성
            if (board == null) return result;

            Vector2Int forward = GetForward(isPlayerPiece); // 진영별 전진 방향
            Vector2Int oneStep = origin + forward; // 전방 1칸
            bool oneStepClear = board.IsInsideBoard(oneStep) &&
                                !board.GetTile(oneStep).IsOccupied &&
                                !board.GetTile(oneStep).IsBlockedByObstacle;

            if (oneStepClear)
            {
                result.AddMove(oneStep); // 1칸 전진

                Vector2Int twoStep = origin + forward * 2; // 전방 2칸
                if (board.IsInsideBoard(twoStep) &&
                    !board.GetTile(twoStep).IsOccupied &&
                    !board.GetTile(twoStep).IsBlockedByObstacle)
                {
                    result.AddMove(twoStep); // 2칸 전진
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
                Vector2Int target = origin + offset; // 공격 후보 계산
                if (!board.IsInsideBoard(target)) continue;

                TileState tile = board.GetTile(target);
                if (tile.IsOccupied && tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece)
                {
                    result.AddAttack(target); // 적이 있을 때만 공격
                }
            }

            return result;
        }

        private static MovementResult ResolveChameleon(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            if (board == null || !board.IsInsideBoard(origin)) return new MovementResult();

            TileState tile = board.GetTile(origin); // 현재 칸 조회
            PieceRuntimeState piece = tile != null ? tile.OccupyingPiece : null; // 런타임 기물 조회
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
            var result = new MovementResult(); // 창병 결과 생성
            if (board == null) return result;

            Vector2Int[] directions =
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int moveTarget = origin + direction; // 이동은 직교 1칸
                if (board.IsInsideBoard(moveTarget))
                {
                    TileState moveTile = board.GetTile(moveTarget);
                    if (moveTile != null && !moveTile.IsBlockedByObstacle && !moveTile.IsOccupied)
                    {
                        result.AddMove(moveTarget);
                    }
                }

                for (int distance = 1; distance <= 2; distance++)
                {
                    Vector2Int attackTarget = origin + direction * distance; // 같은 방향 최대 2칸 공격
                    if (!board.IsInsideBoard(attackTarget)) break;

                    TileState attackTile = board.GetTile(attackTarget);
                    if (attackTile == null || attackTile.IsBlockedByObstacle) break;
                    if (!attackTile.IsOccupied) continue;

                    if (attackTile.OccupyingPiece.IsPlayerPiece != isPlayerPiece)
                    {
                        result.AddAttack(attackTarget);
                    }

                    break;
                }
            }

            return result;
        }

        private static MovementResult ResolveShooter(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int forward = GetForward(isPlayerPiece); // 진영별 전진 방향
            Vector2Int[] directions =
            {
                forward + Vector2Int.left,
                forward + Vector2Int.right
            };

            return new SlideMovementRule(directions, BoardState.Width).Resolve(origin, isPlayerPiece, board);
        }

        private static MovementResult ResolveShieldGuard(Vector2Int origin, bool isPlayerPiece, BoardState board)
        {
            Vector2Int backward = -GetForward(isPlayerPiece); // 진영별 후방 방향
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
            Vector2Int forward = GetForward(isPlayerPiece); // 진영별 전진 방향
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
            Vector2Int forward = GetForward(isPlayerPiece); // 진영별 전진 방향
            Vector2Int backward = -forward; // 진영별 후방 방향
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
            Vector2Int forward = GetForward(isPlayerPiece); // 진영별 전진 방향
            Vector2Int[] directions =
            {
                forward + Vector2Int.left,
                forward + Vector2Int.right,
                Vector2Int.left,
                Vector2Int.right
            };

            return new StepMovementRule(directions, 1).Resolve(origin, isPlayerPiece, board);
        }

        private static Vector2Int GetForward(bool isPlayerPiece)
        {
            return isPlayerPiece ? Vector2Int.up : Vector2Int.down; // 아군 +Y, 적군 -Y
        }
    }
}
