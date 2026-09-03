using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 관련 타입을 모아두는 네임스페이스
{
    public static class MovementResolver // 기물 종류별 이동/공격 가능 칸을 계산하는 정적 클래스
    {
        private static readonly Vector2Int[] OrthogonalDirections = // 상하좌우 4방향
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) // 우/좌/상/하
        };

        private static readonly Vector2Int[] DiagonalDirections = // 대각선 4방향
        {
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) // 우상/우하/좌상/좌하
        };

        private static readonly Vector2Int[] AllEightDirections = // 8방향(직선+대각선)
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1), // 직선 4방향
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1) // 대각선 4방향
        };

        private static readonly Vector2Int[] KnightOffsets = // 나이트형 도약 좌표 8개
        {
            new Vector2Int(1, 2), new Vector2Int(2, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1), // L자 이동 4가지
            new Vector2Int(1, -2), new Vector2Int(2, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1) // L자 이동 나머지 4가지
        };

        public static MovementResult GetReachableTiles(PieceMovementType movementType, Vector2Int origin, bool isPlayerPiece, BoardState board) // 기물 종류에 따라 도달 가능한 칸을 계산하는 메서드
        {
            switch (movementType) // 이동 타입별로 분기
            {
                case PieceMovementType.King: // 킹형이면
                    return SlideInDirections(origin, AllEightDirections, board, isPlayerPiece, maxSteps: 1); // 8방향으로 1칸만 이동

                case PieceMovementType.Pawn: // 폰형이면
                    return ResolvePawnMovement(origin, isPlayerPiece, board); // 폰 전용 규칙 적용

                case PieceMovementType.Knight: // 나이트형이면
                    return JumpToOffsets(origin, KnightOffsets, board, isPlayerPiece); // 도약 이동만 계산

                case PieceMovementType.Bishop: // 비숍형이면
                    return SlideInDirections(origin, DiagonalDirections, board, isPlayerPiece, maxSteps: BoardState.Width); // 대각선으로 막힐 때까지 이동

                case PieceMovementType.Rook: // 룩형이면
                    return SlideInDirections(origin, OrthogonalDirections, board, isPlayerPiece, maxSteps: BoardState.Width); // 직선으로 막힐 때까지 이동

                case PieceMovementType.Queen: // 퀸형이면
                    return SlideInDirections(origin, AllEightDirections, board, isPlayerPiece, maxSteps: BoardState.Width); // 직선+대각선으로 막힐 때까지 이동

                case PieceMovementType.Archbishop: // 아크비숍(비숍+나이트)이면
                    return CombineResults( // 두 이동 패턴 결과를 합침
                        SlideInDirections(origin, DiagonalDirections, board, isPlayerPiece, maxSteps: BoardState.Width), // 비숍 이동 부분
                        JumpToOffsets(origin, KnightOffsets, board, isPlayerPiece)); // 나이트 이동 부분

                case PieceMovementType.Chancellor: // 챈슬러(룩+나이트)이면
                    return CombineResults( // 두 이동 패턴 결과를 합침
                        SlideInDirections(origin, OrthogonalDirections, board, isPlayerPiece, maxSteps: BoardState.Width), // 룩 이동 부분
                        JumpToOffsets(origin, KnightOffsets, board, isPlayerPiece)); // 나이트 이동 부분

                case PieceMovementType.Amazon: // 아마존(퀸+나이트)이면
                    return CombineResults( // 두 이동 패턴 결과를 합침
                        SlideInDirections(origin, AllEightDirections, board, isPlayerPiece, maxSteps: BoardState.Width), // 퀸 이동 부분
                        JumpToOffsets(origin, KnightOffsets, board, isPlayerPiece)); // 나이트 이동 부분

                default: // 그 외(Custom 등 아직 규칙 없음)이면
                    return new MovementResult(); // 빈 결과 반환
            }
        }

        private static MovementResult SlideInDirections(Vector2Int origin, Vector2Int[] directions, BoardState board, bool isPlayerPiece, int maxSteps) // 주어진 방향들로 막힐 때까지(또는 maxSteps까지) 미끄러지는 이동을 계산하는 메서드
        {
            var result = new MovementResult(); // 결과 객체 생성

            foreach (var direction in directions) // 각 방향을 순회
            {
                for (int step = 1; step <= maxSteps; step++) // 1칸씩 늘려가며 검사
                {
                    var target = origin + direction * step; // 이번 칸 좌표 계산
                    if (!board.IsInsideBoard(target)) // 보드 밖이면
                    {
                        break; // 이 방향은 더 진행하지 않음
                    }

                    var tile = board.GetTile(target); // 이번 칸의 타일 조회
                    if (tile.IsBlockedByObstacle) // 장애물이 있으면
                    {
                        break; // 이 방향은 더 진행하지 않음
                    }

                    if (!tile.IsOccupied) // 비어있으면
                    {
                        result.AddMove(target); // 이동 가능 칸으로 추가
                        continue; // 같은 방향으로 계속 진행
                    }

                    if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) // 적 기물이 있으면
                    {
                        result.AddAttack(target); // 공격 가능 칸으로 추가
                    }

                    break; // 아군이든 적이든 기물을 만나면 이 방향은 더 진행하지 않음
                }
            }

            return result; // 완성된 결과 반환
        }

        private static MovementResult JumpToOffsets(Vector2Int origin, Vector2Int[] offsets, BoardState board, bool isPlayerPiece) // 중간 칸을 무시하고 정해진 상대 좌표로 도약하는 이동을 계산하는 메서드
        {
            var result = new MovementResult(); // 결과 객체 생성

            foreach (var offset in offsets) // 각 도약 좌표를 순회
            {
                var target = origin + offset; // 착지할 칸 좌표 계산
                if (!board.IsInsideBoard(target)) // 보드 밖이면
                {
                    continue; // 이 좌표는 건너뜀
                }

                var tile = board.GetTile(target); // 착지할 칸의 타일 조회
                if (tile.IsBlockedByObstacle) // 장애물이 있으면
                {
                    continue; // 이 좌표는 건너뜀
                }

                if (!tile.IsOccupied) // 비어있으면
                {
                    result.AddMove(target); // 이동 가능 칸으로 추가
                }
                else if (tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) // 적 기물이 있으면
                {
                    result.AddAttack(target); // 공격 가능 칸으로 추가
                }
                // 아군이 있으면 착지할 수 없으므로 아무것도 추가하지 않음
            }

            return result; // 완성된 결과 반환
        }

        private static MovementResult ResolvePawnMovement(Vector2Int origin, bool isPlayerPiece, BoardState board) // 폰 전용 이동/공격 규칙을 계산하는 메서드
        {
            var result = new MovementResult(); // 결과 객체 생성
            var forward = isPlayerPiece ? new Vector2Int(0, 1) : new Vector2Int(0, -1); // 아군은 +Y, 적군은 -Y 방향이 전진

            var oneStep = origin + forward; // 전방 1칸 좌표
            bool oneStepClear = board.IsInsideBoard(oneStep) && !board.GetTile(oneStep).IsOccupied && !board.GetTile(oneStep).IsBlockedByObstacle; // 1칸이 비어있는지 확인
            if (oneStepClear) // 1칸이 비어있으면
            {
                result.AddMove(oneStep); // 1칸 전진을 이동 가능으로 추가

                var twoStep = origin + forward * 2; // 전방 2칸 좌표
                if (board.IsInsideBoard(twoStep) && !board.GetTile(twoStep).IsOccupied && !board.GetTile(twoStep).IsBlockedByObstacle) // 2칸도 비어있으면
                {
                    result.AddMove(twoStep); // 2칸 전진도 이동 가능으로 추가
                }
            }

            var attackOffsets = new[] { forward, forward + new Vector2Int(-1, 0), forward + new Vector2Int(1, 0) }; // 공격 후보: 전방 1칸 + 대각선 좌우 1칸
            foreach (var offset in attackOffsets) // 각 공격 후보 좌표를 순회
            {
                var target = origin + offset; // 후보 칸 좌표 계산
                if (!board.IsInsideBoard(target)) // 보드 밖이면
                {
                    continue; // 이 좌표는 건너뜀
                }

                var tile = board.GetTile(target); // 후보 칸의 타일 조회
                if (tile.IsOccupied && tile.OccupyingPiece.IsPlayerPiece != isPlayerPiece) // 적 기물이 있으면
                {
                    result.AddAttack(target); // 공격 가능 칸으로 추가
                }
            }

            return result; // 완성된 결과 반환
        }

        private static MovementResult CombineResults(MovementResult first, MovementResult second) // 두 이동 결과를 하나로 합치는 메서드
        {
            first.MergeFrom(second); // 두 번째 결과를 첫 번째 결과에 병합
            return first; // 합쳐진 결과 반환
        }
    }
}
