using System.Collections.Generic; // Dictionary<TKey,TValue>, HashSet<T>, List<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIThreatMap // 전체 100칸을 선계산하지 않고 AI가 실제 평가하는 좌표만 계산·캐시하는 39일차 Lazy 위협 맵
    {
        private readonly BoardState _board; // 위협 판정에 사용할 현재 보드 참조
        private readonly List<PieceRuntimeState> _playerAttackers; // Build 시 한 번만 수집한 살아 있는 공격 가능 플레이어 기물 목록
        private readonly Dictionary<int, int> _threatCountCache = new Dictionary<int, int>(); // 좌표별 위협 기물 수 캐시

        public int ProbeCount { get; private set; } // 실제로 새 좌표의 위협을 계산한 횟수
        public int ResolverCallCount { get; private set; } // 위협 계산 때문에 MovementResolver를 실제 호출한 횟수

        private EnemyAIThreatMap(BoardState board, List<PieceRuntimeState> playerAttackers) // Build에서만 생성하는 내부 생성자
        {
            _board = board; // 현재 보드 저장
            _playerAttackers = playerAttackers ?? new List<PieceRuntimeState>(); // 공격자 목록을 null 없이 저장
        }

        public int GetThreatCount(Vector2Int position) // 특정 칸을 몇 개의 플레이어 기물이 위협하는지 Lazy 방식으로 반환하는 메서드
        {
            if (!IsInsideBoard(position)) return 0; // 보드 밖 좌표는 위협 없음

            int key = ToCacheKey(position); // 10x10 좌표를 작은 정수 키로 변환

            if (_threatCountCache.TryGetValue(key, out int cachedCount)) return cachedCount; // 이미 계산한 좌표는 MovementResolver 재호출 없이 즉시 반환

            int threatCount = CalculateThreatCount(position); // 처음 요청된 좌표만 실제 위협 계산
            _threatCountCache[key] = threatCount; // 같은 평가 사이클에서 재사용하도록 캐시
            ProbeCount++; // 실제 좌표 계산 횟수 증가
            return threatCount; // 계산된 위협 수 반환
        }

        public bool IsThreatened(Vector2Int position) // 특정 칸이 하나 이상의 플레이어 공격에 노출되는지 확인하는 메서드
        {
            return GetThreatCount(position) > 0; // 위협 수가 1 이상이면 위험 칸
        }

        public static EnemyAIThreatMap Build(BoardState board) // 현재 보드에서 공격 가능한 플레이어 기물만 한 번 수집하고 Lazy 위협 맵을 만드는 메서드
        {
            var attackers = new List<PieceRuntimeState>(); // 살아 있는 플레이어 공격자 목록
            if (board == null) return new EnemyAIThreatMap(null, attackers); // 보드가 없으면 빈 위협 맵 반환

            var visited = new HashSet<PieceRuntimeState>(); // 2x2 등 여러 칸이 같은 런타임 기물을 참조할 때 중복 수집 방지

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var attacker = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 기물 조회
                    if (attacker == null || !attacker.IsPlayerPiece || attacker.IsDead || attacker.Definition == null) continue; // 살아 있는 플레이어 기물만 사용
                    if (!attacker.CanAttack) continue; // 기절 등으로 공격권이 없으면 위협을 만들지 않음
                    if (!visited.Add(attacker)) continue; // 같은 런타임 기물은 한 번만 수집
                    attackers.Add(attacker); // 실제 위협 계산 때 사용할 공격자 목록에 추가
                }
            }

            return new EnemyAIThreatMap(board, attackers); // 아직 어떤 좌표도 계산하지 않은 Lazy 위협 맵 반환
        }

        private int CalculateThreatCount(Vector2Int targetPosition) // 요청된 좌표 하나를 현재 플레이어 공격자들이 몇 개 위협하는지 계산
        {
            if (_board == null) return 0; // 보드가 없으면 위협 없음

            var targetTile = _board.GetTile(targetPosition); // 위협을 검사할 실제 타일 조회
            if (targetTile == null || targetTile.IsBlockedByObstacle) return 0; // 존재하지 않거나 장애물 칸은 위협 대상에서 제외

            int count = 0; // 이 좌표를 위협하는 플레이어 기물 수

            for (int i = 0; i < _playerAttackers.Count; i++) // Build에서 수집한 플레이어 공격자만 순회
            {
                var attacker = _playerAttackers[i]; // 현재 공격자 참조
                if (attacker == null || attacker.IsDead || !attacker.CanAttack || attacker.Definition == null) continue; // 평가 중 상태가 달라졌다면 안전하게 제외
                if (targetTile.OccupyingPiece == attacker) continue; // 공격자 자신의 현재 칸은 위협 대상에서 제외
                if (CanThreatenPosition(attacker, targetPosition, targetTile)) count++; // 실제 공격 가능하면 위협 기물 수 증가
            }

            return count; // 최종 위협 기물 수 반환
        }

        private bool CanThreatenPosition(PieceRuntimeState attacker, Vector2Int targetPosition, TileState targetTile) // 기존 35일차 방식과 동일한 가상 적 배치로 좌표 하나만 판정
        {
            var originalOccupant = targetTile.OccupyingPiece; // 검사 후 원상복구할 기존 점유 기물
            bool replacedOccupant = false; // 빈 칸·아군 칸을 가상 적으로 교체했는지 기록

            try // 가상 적 배치 후 기존 MovementResolver를 그대로 호출
            {
                if (originalOccupant == null || originalOccupant.IsPlayerPiece) // 빈 칸이거나 플레이어 기물이 있는 칸이면
                {
                    targetTile.OccupyingPiece = new PieceRuntimeState(attacker.Definition, targetPosition, false); // Pawn 대각 공격 등 실제 공격 규칙을 확인하기 위한 가상 적 배치
                    replacedOccupant = true; // finally에서 반드시 원래 점유로 복구하도록 표시
                }

                var movement = MovementResolver.GetReachableTiles(attacker, _board); // 플레이어 실제 런타임 이동·공격 규칙으로 후보 계산
                ResolverCallCount++; // 성능 측정을 위해 실제 MovementResolver 호출 수 증가
                return movement.AttackTiles.Contains(targetPosition); // 요청 좌표가 실제 공격 후보면 위협 true
            }
            finally // 어떤 상황에서도 보드 점유 상태를 원래대로 돌림
            {
                if (replacedOccupant) targetTile.OccupyingPiece = originalOccupant; // 가상 적을 제거하고 기존 점유 복원
            }
        }

        private static bool IsInsideBoard(Vector2Int position) // 좌표가 10x10 보드 안인지 확인하는 도우미
        {
            return position.x >= 0 && position.x < BoardState.Width && position.y >= 0 && position.y < BoardState.Height; // X·Y 범위를 모두 검사
        }

        private static int ToCacheKey(Vector2Int position) // Vector2Int Dictionary 키 대신 작은 정수 키를 사용해 반복 비교 비용을 줄이는 도우미
        {
            return position.y * BoardState.Width + position.x; // 10x10 보드에서 0~99의 고유 키 생성
        }
    }
}
