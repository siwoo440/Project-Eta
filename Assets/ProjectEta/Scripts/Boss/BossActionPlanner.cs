using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState, PieceCategory, PieceMovementType를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public sealed class BossActionPlanner // 2x2 보스의 기본 근접 공격과 상하좌우 이동 후보를 생성하고 점수로 선택하는 38일차 플래너
    {
        private const int AttackBaseScore = 4000; // 보스가 공격 가능한 상황에서는 이동보다 공격을 강하게 우선하기 위한 기본 점수
        private const int KingAttackBonus = 7000; // 플레이어 King 직접 공격을 최우선으로 만드는 추가 점수
        private const int LethalAttackBonus = 1000; // 현재 공격으로 처치 가능한 대상에게 주는 추가 점수
        private const int DamageScorePerPoint = 50; // 보스 고정 ATK 1당 공격 가치 점수
        private const int MoveBaseScore = 800; // 공격 대상이 없을 때 유효한 2x2 이동 후보 기본 점수
        private const int ApproachScorePerTile = 250; // 목표 플레이어와 거리를 1칸 줄일 때 받는 접근 보너스
        private const int AdjacentPreparationBonus = 900; // 이동 후 다음 턴에 목표를 바로 공격할 수 있는 위치의 준비 보너스

        private static readonly Vector2Int[] CardinalDirections = // 38일차 기본형에서 허용할 보스 1칸 이동 방향
        {
            Vector2Int.up, // 보드 위쪽으로 Anchor 1칸 이동
            Vector2Int.down, // 보드 아래쪽으로 Anchor 1칸 이동
            Vector2Int.left, // 보드 왼쪽으로 Anchor 1칸 이동
            Vector2Int.right // 보드 오른쪽으로 Anchor 1칸 이동
        };

        public List<BossActionCandidate> BuildCandidates(BoardState board) // 현재 보드의 모든 살아 있는 적 Boss에서 기본 행동 후보를 생성하는 메서드
        {
            var candidates = new List<BossActionCandidate>(); // 최종 후보 목록 생성
            if (board == null) return candidates; // 보드가 없으면 빈 목록 반환

            var visitedBosses = new HashSet<PieceRuntimeState>(); // 2x2 네 칸이 같은 보스를 가리켜도 한 번만 평가하기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var boss = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (!IsActiveEnemyBoss(boss)) continue; // 살아 있는 적 Boss만 사용
                    if (!visitedBosses.Add(boss)) continue; // 같은 2x2 보스의 두 번째 이후 칸은 건너뜀

                    AddAttackCandidates(boss, board, candidates); // 현재 점유 영역 외곽 공격 후보 추가
                    AddMoveCandidates(boss, board, candidates); // 상하좌우 2x2 이동 후보 추가
                }
            }

            return candidates; // 생성된 전체 보스 후보 반환
        }

        public bool TryChooseAction(BoardState board, out BossActionCandidate selectedAction) // 모든 보스 후보 중 결정론적으로 최고 행동 하나를 선택하는 메서드
        {
            var candidates = BuildCandidates(board); // 현재 보드 후보 생성

            if (candidates.Count == 0) // 보스가 없거나 아무 행동도 할 수 없으면
            {
                selectedAction = null; // 선택 행동 없음
                return false; // 실패 반환
            }

            selectedAction = candidates[0]; // 첫 후보를 임시 최고 행동으로 설정

            for (int i = 1; i < candidates.Count; i++) // 나머지 후보를 순회
            {
                if (IsBetterCandidate(candidates[i], selectedAction)) selectedAction = candidates[i]; // 더 높은 우선순위 후보로 교체
            }

            return true; // 최종 행동 선택 성공
        }

        private static void AddAttackCandidates(PieceRuntimeState boss, BoardState board, List<BossActionCandidate> candidates) // 2x2 전체 외곽에 인접한 플레이어를 공격 후보로 추가하는 메서드
        {
            if (boss == null || !boss.CanAttack) return; // 공격권이 없으면 공격 후보를 만들지 않음

            var seenTargets = new HashSet<PieceRuntimeState>(); // 같은 대상이 둘 이상의 외곽 칸과 맞닿아도 한 번만 후보화하기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 플레이어 기물 탐색을 위해 보드 전체 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 세로 순회
                {
                    var target = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 타일 점유 기물 조회
                    if (target == null || !target.IsPlayerPiece || target.IsDead) continue; // 살아 있는 플레이어 기물만 후보 대상
                    if (!seenTargets.Add(target)) continue; // 동일 런타임 기물 중복 방지
                    if (!IsCellAdjacentToFootprint(boss.BoardPosition, LargePieceBoardUtility.GetFootprint(boss.Definition), target.BoardPosition)) continue; // 보스 외곽 한 칸 범위가 아니면 공격 불가

                    int score = AttackBaseScore; // 공격 기본 점수에서 시작
                    score += boss.Definition != null ? boss.Definition.BaseAtk * DamageScorePerPoint : 0; // 고정 ATK 가치 반영

                    if (IsKing(target)) score += KingAttackBonus; // King 직접 공격은 최우선
                    if (boss.Definition != null && boss.Definition.BaseAtk >= target.CurrentHp) score += LethalAttackBonus; // 처치 가능 대상 추가 보너스

                    candidates.Add(new BossActionCandidate( // 완성된 공격 후보 추가
                        boss, // 행동 주체 보스
                        BossActionType.Attack, // 공격 종류
                        boss.BoardPosition, // 현재 Anchor
                        target.BoardPosition, // 공격 대상 좌표
                        target, // 실제 플레이어 대상
                        score)); // 최종 공격 점수
                }
            }
        }

        private static void AddMoveCandidates(PieceRuntimeState boss, BoardState board, List<BossActionCandidate> candidates) // 공격할 대상이 없을 때 사용할 상하좌우 이동 후보를 추가하는 메서드
        {
            if (boss == null || !boss.CanMove || boss.Definition == null) return; // 이동권 또는 정의가 없으면 이동 후보 없음

            Vector2Int footprint = LargePieceBoardUtility.GetFootprint(boss.Definition); // 현재 2x2 점유 크기
            bool hasPriorityTarget = TryFindPriorityPlayerTarget(board, out var priorityTarget); // King 우선, 없으면 첫 플레이어 기물을 접근 목표로 탐색
            int beforeDistance = hasPriorityTarget // 목표가 있으면
                ? DistanceFromFootprintToCell(boss.BoardPosition, footprint, priorityTarget.BoardPosition) // 현재 보스 영역과 목표 사이 거리 계산
                : 0; // 플레이어가 없으면 거리 보너스 없음

            for (int i = 0; i < CardinalDirections.Length; i++) // 상하좌우 네 방향 순회
            {
                Vector2Int destination = boss.BoardPosition + CardinalDirections[i]; // 새 Anchor 후보 계산
                if (!board.CanOccupyArea(destination, footprint, boss)) continue; // 자기 현재 점유는 허용하고 새 2x2 영역에 다른 기물·장애물·보드 밖이 있으면 제외

                int score = MoveBaseScore; // 이동 기본 점수

                if (hasPriorityTarget) // 접근할 플레이어가 있으면
                {
                    int afterDistance = DistanceFromFootprintToCell(destination, footprint, priorityTarget.BoardPosition); // 이동 후 목표와 거리 계산
                    score += (beforeDistance - afterDistance) * ApproachScorePerTile; // 가까워진 칸 수만큼 가점, 멀어지면 감점

                    if (IsCellAdjacentToFootprint(destination, footprint, priorityTarget.BoardPosition)) // 이동 후 목표가 바로 외곽 공격 범위에 들어오면
                    {
                        score += AdjacentPreparationBonus; // 다음 턴 공격 준비 위치 추가 보너스
                    }
                }

                candidates.Add(new BossActionCandidate( // 이동 후보 추가
                    boss, // 행동 주체
                    BossActionType.Move, // 이동 종류
                    boss.BoardPosition, // 현재 Anchor
                    destination, // 새 Anchor
                    null, // 이동에는 공격 대상 없음
                    score)); // 최종 이동 점수
            }
        }

        public static bool IsCellAdjacentToFootprint(Vector2Int anchor, Vector2Int footprint, Vector2Int cell) // 셀이 사각 보스 영역 바로 바깥 1칸에 인접하는지 확인하는 메서드
        {
            int minX = anchor.x; // 보스 영역 최소 X
            int maxX = anchor.x + Mathf.Max(1, footprint.x) - 1; // 보스 영역 최대 X
            int minY = anchor.y; // 보스 영역 최소 Y
            int maxY = anchor.y + Mathf.Max(1, footprint.y) - 1; // 보스 영역 최대 Y

            bool inside = cell.x >= minX && cell.x <= maxX && cell.y >= minY && cell.y <= maxY; // 셀이 보스 내부 점유 칸인지 확인
            if (inside) return false; // 보스 자신의 점유 영역은 공격 외곽이 아님

            bool withinExpandedX = cell.x >= minX - 1 && cell.x <= maxX + 1; // 가로로 보스 영역에서 최대 한 칸 떨어져 있는지 확인
            bool withinExpandedY = cell.y >= minY - 1 && cell.y <= maxY + 1; // 세로로 보스 영역에서 최대 한 칸 떨어져 있는지 확인
            return withinExpandedX && withinExpandedY; // 대각선을 포함한 전체 외곽 링이면 공격 가능
        }

        public static int DistanceFromFootprintToCell(Vector2Int anchor, Vector2Int footprint, Vector2Int cell) // 사각 점유 영역에서 특정 셀까지의 맨해튼 최소 거리를 계산하는 메서드
        {
            int minX = anchor.x; // 영역 최소 X
            int maxX = anchor.x + Mathf.Max(1, footprint.x) - 1; // 영역 최대 X
            int minY = anchor.y; // 영역 최소 Y
            int maxY = anchor.y + Mathf.Max(1, footprint.y) - 1; // 영역 최대 Y

            int dx = cell.x < minX // 대상이 영역 왼쪽이면
                ? minX - cell.x // 왼쪽 간격 반환
                : cell.x > maxX // 대상이 영역 오른쪽이면
                    ? cell.x - maxX // 오른쪽 간격 반환
                    : 0; // X축 범위 안이면 가로 거리 0

            int dy = cell.y < minY // 대상이 영역 아래쪽이면
                ? minY - cell.y // 아래 간격 반환
                : cell.y > maxY // 대상이 영역 위쪽이면
                    ? cell.y - maxY // 위 간격 반환
                    : 0; // Y축 범위 안이면 세로 거리 0

            return dx + dy; // 사각 영역과 셀 사이 최소 맨해튼 거리 반환
        }

        private static bool TryFindPriorityPlayerTarget(BoardState board, out PieceRuntimeState target) // 이동 접근 목표를 King 우선으로 찾는 메서드
        {
            PieceRuntimeState fallback = null; // King이 없을 때 사용할 첫 플레이어 기물

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.IsDead) continue; // 살아 있는 플레이어만 확인
                    if (IsKing(piece)) // King이면
                    {
                        target = piece; // 즉시 최우선 목표로 선택
                        return true; // 탐색 성공
                    }

                    if (fallback == null) fallback = piece; // 첫 일반 플레이어를 fallback으로 저장
                }
            }

            target = fallback; // King이 없으면 일반 플레이어 반환
            return target != null; // 실제 목표 존재 여부 반환
        }

        private static bool IsActiveEnemyBoss(PieceRuntimeState piece) // 현재 보드 기물이 38일차 행동 대상 Boss인지 검사하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의 없는 기물 제외
            if (piece.IsPlayerPiece || piece.IsDead) return false; // 플레이어·사망 기물 제외
            return piece.Definition.Category == PieceCategory.Boss; // Boss 카테고리만 사용
        }

        private static bool IsBetterCandidate(BossActionCandidate challenger, BossActionCandidate currentBest) // 동점에서도 실행 결과가 항상 같도록 하는 결정론적 비교 메서드
        {
            if (challenger.Score != currentBest.Score) return challenger.Score > currentBest.Score; // 1순위: 높은 점수
            if (challenger.ActionType != currentBest.ActionType) return challenger.ActionType == BossActionType.Attack; // 2순위: 공격 우선

            string challengerId = challenger.Actor?.Definition?.PieceId ?? string.Empty; // 도전자 PieceId
            string currentId = currentBest.Actor?.Definition?.PieceId ?? string.Empty; // 현재 최고 후보 PieceId
            int idCompare = string.Compare(challengerId, currentId, StringComparison.Ordinal); // 문화권 영향 없는 문자열 비교
            if (idCompare != 0) return idCompare < 0; // 3순위: PieceId 사전순

            if (challenger.Origin.y != currentBest.Origin.y) return challenger.Origin.y < currentBest.Origin.y; // 4순위: 보스 Anchor Y
            if (challenger.Origin.x != currentBest.Origin.x) return challenger.Origin.x < currentBest.Origin.x; // 5순위: 보스 Anchor X
            if (challenger.Target.y != currentBest.Target.y) return challenger.Target.y < currentBest.Target.y; // 6순위: 목표 Y
            return challenger.Target.x < currentBest.Target.x; // 7순위: 목표 X
        }

        private static bool IsKing(PieceRuntimeState piece) // 플레이어 King 여부를 현재 데이터와 기존 Legacy 이동 타입 양쪽으로 판별하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 King이 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId 우선 판별
            return piece.Definition.MovementType == PieceMovementType.King; // 구형 데이터 호환 판별
        }
    }
}
