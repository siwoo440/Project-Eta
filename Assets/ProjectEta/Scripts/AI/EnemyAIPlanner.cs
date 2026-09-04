using System; // StringComparison과 Math를 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIPlanner // 보드 상태를 읽고 합법 행동 후보를 생성·평가·선택하는 33일차 공통 AI 두뇌
    {
        private const int MoveBaseScore = 10; // 단순 이동 행동이 갖는 최소 점수
        private const int DistanceApproachScore = 25; // 킹과의 맨해튼 거리를 1칸 줄일 때 얻는 점수
        private const int ImmediateAttackScore = 1000; // 즉시 공격 가능한 행동에 주는 큰 기본 보너스
        private const int DirectKingAttackScore = 5000; // 플레이어 킹을 직접 공격할 수 있을 때 추가하는 최우선 보너스
        private const int LethalAttackScore = 400; // 이번 공격으로 대상을 처치할 수 있을 때 추가하는 점수
        private const int DamageScorePerPoint = 40; // 실제 예상 피해 1당 추가하는 점수
        private const int CurrentHpScorePerPoint = 2; // 생존력이 높은 기물이 동일 상황에서 조금 더 안정적으로 행동하도록 주는 점수

        public List<AIActionCandidate> BuildCandidates(BoardState board) // 현재 보드의 모든 적 기물에서 합법 행동 후보를 수집하는 메서드
        {
            var candidates = new List<AIActionCandidate>(); // 이번 평가에서 사용할 후보 목록 생성
            if (board == null) return candidates; // 보드가 없으면 빈 후보 목록 반환

            var visitedPieces = new HashSet<PieceRuntimeState>(); // 향후 2x2 점유처럼 여러 칸이 같은 기물을 참조해도 중복 평가하지 않기 위한 집합
            bool hasPriorityTarget = TryFindPlayerKing(board, out var playerKing); // 플레이어 킹 위치를 공통 목표로 우선 탐색
            Vector2Int priorityTargetPosition = hasPriorityTarget ? playerKing.BoardPosition : FindFirstPlayerPiecePosition(board); // 킹이 없으면 첫 아군 기물을 임시 목표로 사용
            bool hasAnyPlayerPiece = hasPriorityTarget || HasAnyPlayerPiece(board); // 이동 거리 평가에 사용할 아군 존재 여부 계산

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 방향 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 방향 순회
                {
                    var tile = board.GetTile(new Vector2Int(x, y)); // 현재 타일 조회
                    var actor = tile?.OccupyingPiece; // 현재 타일의 점유 기물 조회

                    if (actor == null || actor.IsPlayerPiece || actor.IsDead) continue; // 빈 칸·플레이어 기물·사망 기물은 AI 행동 주체에서 제외
                    if (!visitedPieces.Add(actor)) continue; // 이미 같은 런타임 기물을 평가했다면 중복 후보 생성을 건너뜀

                    var movement = MovementResolver.GetReachableTiles(actor, board); // 플레이어와 완전히 같은 MovementResolver로 합법 이동·공격 후보 계산

                    for (int i = 0; i < movement.MoveTiles.Count; i++) // 모든 합법 이동 후보 순회
                    {
                        var target = movement.MoveTiles[i]; // 이번 이동 목표 좌표
                        var targetTile = board.GetTile(target); // 실제 보드 타일 조회
                        if (targetTile == null || targetTile.IsOccupied || targetTile.IsBlockedByObstacle) continue; // 방어적으로 다시 확인해 불법 후보를 제거

                        int score = EvaluateMove(actor, target, hasAnyPlayerPiece, priorityTargetPosition); // 공통 이동 점수 계산
                        candidates.Add(new AIActionCandidate(actor, actor.BoardPosition, target, AIActionType.Move, null, score)); // 이동 후보 추가
                    }

                    for (int i = 0; i < movement.AttackTiles.Count; i++) // 모든 합법 공격 후보 순회
                    {
                        var target = movement.AttackTiles[i]; // 이번 공격 목표 좌표
                        var targetTile = board.GetTile(target); // 실제 공격 대상 타일 조회
                        var targetPiece = targetTile?.OccupyingPiece; // 실제 공격 대상 기물 조회
                        if (targetPiece == null || !targetPiece.IsPlayerPiece || targetPiece.IsDead) continue; // 플레이어의 살아 있는 기물이 아니면 공격 후보에서 제외

                        int score = EvaluateAttack(actor, targetPiece); // 공통 공격 점수 계산
                        candidates.Add(new AIActionCandidate(actor, actor.BoardPosition, target, AIActionType.Attack, targetPiece, score)); // 공격 후보 추가
                    }
                }
            }

            return candidates; // 생성한 모든 합법 후보 반환
        }

        public bool TryChooseAction(BoardState board, out AIActionCandidate selectedAction) // 현재 보드에서 가장 높은 점수의 행동 하나를 결정론적으로 고르는 메서드
        {
            var candidates = BuildCandidates(board); // 모든 적 기물의 합법 후보 생성
            if (candidates.Count == 0) // 행동 후보가 하나도 없으면
            {
                selectedAction = null; // 선택 결과 없음
                return false; // AI가 행동할 수 없음을 반환
            }

            selectedAction = candidates[0]; // 첫 후보를 임시 최고 후보로 설정

            for (int i = 1; i < candidates.Count; i++) // 나머지 후보를 순회
            {
                if (IsBetterCandidate(candidates[i], selectedAction)) // 현재 후보가 기존 최고 후보보다 우선하면
                {
                    selectedAction = candidates[i]; // 최고 후보 교체
                }
            }

            return true; // 최종 행동 선택 성공
        }

        private static int EvaluateMove(PieceRuntimeState actor, Vector2Int target, bool hasPriorityTarget, Vector2Int priorityTargetPosition) // 이동 행동의 기본 점수를 계산하는 메서드
        {
            int score = MoveBaseScore; // 모든 합법 이동은 최소 기본 점수를 가짐
            score += Mathf.Max(0, actor.CurrentHp) * CurrentHpScorePerPoint; // 현재 HP가 높은 기물에 아주 작은 안정성 보너스 부여

            if (hasPriorityTarget) // 추적할 플레이어 목표가 존재하면
            {
                int beforeDistance = ManhattanDistance(actor.BoardPosition, priorityTargetPosition); // 이동 전 킹과의 거리
                int afterDistance = ManhattanDistance(target, priorityTargetPosition); // 이동 후 킹과의 거리
                score += (beforeDistance - afterDistance) * DistanceApproachScore; // 킹과 가까워지면 가점, 멀어지면 감점
            }

            return score; // 최종 이동 점수 반환
        }

        private static int EvaluateAttack(PieceRuntimeState actor, PieceRuntimeState targetPiece) // 공격 행동의 공통 점수를 계산하는 메서드
        {
            int score = ImmediateAttackScore; // 즉시 공격은 일반 이동보다 크게 우선
            score += Mathf.Max(0, actor.CurrentHp) * CurrentHpScorePerPoint; // 동일 공격 상황에서는 생존력이 높은 행동 주체를 약간 우선

            int attackPower = actor.Definition != null ? Mathf.Max(0, actor.Definition.BaseAtk) : 0; // 공격자의 고정 ATK 읽기
            int expectedDamage = Mathf.Min(attackPower, Mathf.Max(0, targetPiece.CurrentHp)); // 현재 규칙에서 실제 감소할 예상 피해 계산
            score += expectedDamage * DamageScorePerPoint; // 더 큰 피해를 주는 공격에 추가 점수

            if (attackPower >= targetPiece.CurrentHp) // 이번 고정 피해로 대상을 처치할 수 있으면
            {
                score += LethalAttackScore; // 처치 보너스 추가
            }

            if (IsKing(targetPiece)) // 공격 대상이 플레이어 킹이면
            {
                score += DirectKingAttackScore; // 다른 모든 일반 공격보다 강한 킹 직접 공격 보너스 추가
            }

            return score; // 최종 공격 점수 반환
        }

        private static bool IsBetterCandidate(AIActionCandidate challenger, AIActionCandidate currentBest) // 점수 동점까지 포함한 완전 결정론적 우선순위 비교
        {
            if (challenger.Score != currentBest.Score) return challenger.Score > currentBest.Score; // 1순위: 점수가 높은 행동
            if (challenger.ActionType != currentBest.ActionType) return challenger.ActionType == AIActionType.Attack; // 2순위: 동점이면 공격 우선

            string challengerId = challenger.Actor?.Definition?.PieceId ?? string.Empty; // 도전자 PieceId 읽기
            string currentId = currentBest.Actor?.Definition?.PieceId ?? string.Empty; // 현재 최고 후보 PieceId 읽기
            int idComparison = string.Compare(challengerId, currentId, StringComparison.Ordinal); // 문자열을 문화권 영향 없이 비교
            if (idComparison != 0) return idComparison < 0; // 3순위: PieceId 사전순

            if (challenger.Origin.y != currentBest.Origin.y) return challenger.Origin.y < currentBest.Origin.y; // 4순위: 행동 주체 Y가 작은 쪽
            if (challenger.Origin.x != currentBest.Origin.x) return challenger.Origin.x < currentBest.Origin.x; // 5순위: 행동 주체 X가 작은 쪽
            if (challenger.Target.y != currentBest.Target.y) return challenger.Target.y < currentBest.Target.y; // 6순위: 목표 Y가 작은 쪽
            return challenger.Target.x < currentBest.Target.x; // 7순위: 목표 X가 작은 쪽
        }

        private static bool TryFindPlayerKing(BoardState board, out PieceRuntimeState king) // 플레이어 킹 런타임 기물을 찾는 메서드
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.IsDead) continue; // 살아 있는 플레이어 기물만 검사

                    if (IsKing(piece)) // 킹 정의인지 확인
                    {
                        king = piece; // 실제 킹 반환
                        return true; // 탐색 성공
                    }
                }
            }

            king = null; // 킹을 찾지 못했음을 명시
            return false; // 탐색 실패
        }

        private static Vector2Int FindFirstPlayerPiecePosition(BoardState board) // 킹이 없을 때 사용할 첫 플레이어 기물 위치를 찾는 메서드
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece != null && piece.IsPlayerPiece && !piece.IsDead) return piece.BoardPosition; // 첫 살아 있는 플레이어 기물 위치 반환
                }
            }

            return Vector2Int.zero; // 플레이어 기물이 하나도 없으면 기본 좌표 반환
        }

        private static bool HasAnyPlayerPiece(BoardState board) // 살아 있는 플레이어 기물이 하나라도 있는지 확인하는 메서드
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 점유 기물 조회
                    if (piece != null && piece.IsPlayerPiece && !piece.IsDead) return true; // 살아 있는 플레이어 기물을 찾으면 즉시 true
                }
            }

            return false; // 끝까지 찾지 못하면 플레이어 기물 없음
        }

        private static bool IsKing(PieceRuntimeState piece) // PieceId와 구형 이동 타입을 함께 사용해 킹 여부를 안전하게 판별
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 킹이 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId가 king이면 킹
            return piece.Definition.MovementType == PieceMovementType.King; // 기존 데이터 호환을 위해 MovementType King도 허용
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b) // 체스판 좌표의 단순 맨해튼 거리를 계산하는 메서드
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // 가로 차이와 세로 차이의 합 반환
        }
    }
}
