using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIThreatMap // 플레이어가 현재 보드에서 공격할 수 있는 칸의 위험도를 저장하는 10x10 위협 맵
    {
        private readonly int[,] _threatCounts = new int[BoardState.Width, BoardState.Height]; // 각 칸을 위협하는 플레이어 기물 수

        public int GetThreatCount(Vector2Int position) // 특정 칸을 몇 개의 플레이어 기물이 위협하는지 반환하는 메서드
        {
            if (position.x < 0 || position.x >= BoardState.Width) return 0; // X가 보드 밖이면 위협 없음
            if (position.y < 0 || position.y >= BoardState.Height) return 0; // Y가 보드 밖이면 위협 없음
            return _threatCounts[position.x, position.y]; // 저장된 위협 기물 수 반환
        }

        public bool IsThreatened(Vector2Int position) // 특정 칸이 하나 이상의 플레이어 공격에 노출되는지 확인하는 메서드
        {
            return GetThreatCount(position) > 0; // 위협 수가 1 이상이면 위험 칸
        }

        public static EnemyAIThreatMap Build(BoardState board) // 현재 플레이어 기물의 실제 공격 규칙을 이용해 전체 위협 맵을 만드는 메서드
        {
            var map = new EnemyAIThreatMap(); // 비어 있는 새 위협 맵 생성
            if (board == null) return map; // 보드가 없으면 빈 맵 반환

            var visited = new HashSet<PieceRuntimeState>(); // 향후 2x2 점유 기물이 여러 칸에서 중복 계산되는 것을 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var attacker = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 기물 조회
                    if (attacker == null || !attacker.IsPlayerPiece || attacker.IsDead || attacker.Definition == null) continue; // 살아 있는 플레이어 기물만 공격자로 사용
                    if (!attacker.CanAttack) continue; // 기절 등으로 공격권이 없으면 위협을 만들지 않음
                    if (!visited.Add(attacker)) continue; // 같은 런타임 기물은 한 번만 계산

                    AddThreatsForAttacker(attacker, board, map); // 이 플레이어 기물이 공격할 수 있는 모든 칸을 위협 맵에 누적
                }
            }

            return map; // 완성된 위협 맵 반환
        }

        private static void AddThreatsForAttacker(PieceRuntimeState attacker, BoardState board, EnemyAIThreatMap map) // 플레이어 기물 하나가 공격 가능한 칸을 계산하는 메서드
        {
            for (int tx = 0; tx < BoardState.Width; tx++) // 공격 가능성을 검사할 X 좌표 순회
            {
                for (int ty = 0; ty < BoardState.Height; ty++) // 공격 가능성을 검사할 Y 좌표 순회
                {
                    var targetPosition = new Vector2Int(tx, ty); // 이번에 검사할 가상 적 위치
                    var targetTile = board.GetTile(targetPosition); // 대상 타일 조회
                    if (targetTile == null || targetTile.IsBlockedByObstacle) continue; // 존재하지 않거나 장애물 칸이면 이동 대상 위협 평가에서 제외

                    var originalOccupant = targetTile.OccupyingPiece; // 검사 후 원상복구할 기존 점유 기물
                    if (originalOccupant == attacker) continue; // 공격자 자신의 현재 칸은 위협 대상에서 제외

                    PieceRuntimeState dummyEnemy = null; // 빈 칸·아군 점유 칸을 실제 공격 대상으로 인식시키기 위한 임시 적 기물
                    bool replacedOccupant = false; // 검사 과정에서 타일 점유를 임시 교체했는지 기록

                    try // 가상 적 배치 후 기존 MovementResolver를 그대로 호출
                    {
                        if (originalOccupant == null || originalOccupant.IsPlayerPiece) // 빈 칸이거나 다른 플레이어 기물이 있는 칸이면
                        {
                            dummyEnemy = new PieceRuntimeState(attacker.Definition, targetPosition, false); // 공격 판정용 임시 적 기물 생성
                            targetTile.OccupyingPiece = dummyEnemy; // 해당 칸에 적이 있다고 가정해 Pawn의 대각 공격 등 실제 공격 규칙을 정확히 검사
                            replacedOccupant = true; // 이후 반드시 원상복구하도록 기록
                        }

                        var movement = MovementResolver.GetReachableTiles(attacker, board); // 플레이어 실제 런타임 규칙으로 이동·공격 후보 계산
                        if (movement.AttackTiles.Contains(targetPosition)) // 임시 적 또는 기존 적을 실제로 공격할 수 있는 칸이면
                        {
                            map._threatCounts[tx, ty]++; // 이 칸을 위협하는 플레이어 기물 수를 1 증가
                        }
                    }
                    finally // 어떤 상황에서도 보드 점유 상태를 원래대로 돌림
                    {
                        if (replacedOccupant) targetTile.OccupyingPiece = originalOccupant; // 빈 칸·아군 칸을 원래 점유 상태로 복구
                    }
                }
            }
        }
    }
}
