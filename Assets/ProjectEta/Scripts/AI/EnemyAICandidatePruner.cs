using System; // IEquatable<T>와 StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 HashSet<T>를 사용하기 위한 네임스페이스
using System.Runtime.CompilerServices; // 참조 동일성 해시에 사용할 RuntimeHelpers 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAICandidatePruner // 정밀 평가 전에 무효·중복 후보를 제거하고 극단적 후보 폭증을 예산 안으로 제한하는 39일차 필터
    {
        public static List<AIActionCandidate> Prune(BoardState board, IReadOnlyList<AIActionCandidate> candidates, int maxCandidates, out int invalidOrDuplicateCount, out bool budgetCapped) // 값싼 필터와 예산 상한을 적용하는 메서드
        {
            invalidOrDuplicateCount = 0; // 시작 시 무효·중복 제거 수 초기화
            budgetCapped = false; // 시작 시 예산 상한 미사용
            var validCandidates = new List<AIActionCandidate>(); // 정상 후보를 모을 목록

            if (board == null || candidates == null) return validCandidates; // 필수 데이터가 없으면 빈 목록 반환

            var uniqueKeys = new HashSet<CandidateKey>(); // 동일 Actor·Origin·Target·Action 후보 중복을 제거할 집합

            for (int i = 0; i < candidates.Count; i++) // Base 후보 전체 순회
            {
                var candidate = candidates[i]; // 현재 후보 참조

                if (!IsStillLegal(board, candidate)) // 생성 직후라도 현재 보드 기준으로 무효라면
                {
                    invalidOrDuplicateCount++; // 무효 제거 수 증가
                    continue; // 정밀 평가하지 않음
                }

                var key = new CandidateKey(candidate); // 후보의 행동 정체성을 캐시 키로 변환

                if (!uniqueKeys.Add(key)) // 이미 같은 행동 후보가 존재하면
                {
                    invalidOrDuplicateCount++; // 중복 제거 수 증가
                    continue; // 중복 후보 제외
                }

                validCandidates.Add(candidate); // 정상·고유 후보를 정밀 평가 후보에 추가
            }

            int safeBudget = Math.Max(1, maxCandidates); // 0 이하 예산이 들어와도 최소 한 후보는 남김

            if (validCandidates.Count <= safeBudget) return validCandidates; // 정상 후보가 예산 이하면 원래 순서 그대로 반환해 기존 행동을 최대한 유지

            budgetCapped = true; // 여기부터는 극단적 후보 폭증에 대한 상한이 실제 적용됨
            var attacks = new List<AIActionCandidate>(); // 즉시 공격 후보를 별도로 보존할 목록
            var moves = new List<AIActionCandidate>(); // 일반 이동 후보 목록

            for (int i = 0; i < validCandidates.Count; i++) // 정상 후보를 공격과 이동으로 분리
            {
                if (validCandidates[i].ActionType == AIActionType.Attack) attacks.Add(validCandidates[i]); // 즉시 공격 후보는 공격 목록으로
                else moves.Add(validCandidates[i]); // 나머지는 이동 목록으로
            }

            attacks.Sort(CompareByBasePriority); // King 공격·치명 공격 등이 이미 Base Score에 반영되므로 높은 Base 공격부터 보존
            moves.Sort(CompareByBasePriority); // 남은 예산에는 높은 Base 이동부터 채움

            var pruned = new List<AIActionCandidate>(safeBudget); // 최종 예산 크기만큼 결과 목록 준비

            for (int i = 0; i < attacks.Count && pruned.Count < safeBudget; i++) pruned.Add(attacks[i]); // 즉시 공격을 가장 먼저 보존
            for (int i = 0; i < moves.Count && pruned.Count < safeBudget; i++) pruned.Add(moves[i]); // 남은 자리를 이동 후보로 채움

            return pruned; // 예산 안으로 제한된 정밀 평가 후보 반환
        }

        private static bool IsStillLegal(BoardState board, AIActionCandidate candidate) // 현재 보드 기준으로 후보가 여전히 실행 가능한지 값싼 검사를 수행
        {
            if (candidate == null || candidate.Actor == null) return false; // 행동 주체가 없으면 무효
            if (candidate.Actor.IsPlayerPiece || candidate.Actor.IsDead) return false; // 플레이어 기물·사망 기물은 적 AI 행동 주체가 아님

            var originTile = board.GetTile(candidate.Origin); // 후보 원점 타일 조회
            var targetTile = board.GetTile(candidate.Target); // 후보 목표 타일 조회
            if (originTile == null || targetTile == null) return false; // 보드 밖 좌표는 무효
            if (originTile.OccupyingPiece != candidate.Actor) return false; // 원점 점유가 바뀌었다면 오래된 후보이므로 무효

            if (candidate.ActionType == AIActionType.Move) return !targetTile.IsOccupied && !targetTile.IsBlockedByObstacle; // 이동은 빈 정상 칸일 때만 유효

            if (candidate.ActionType == AIActionType.Attack) // 공격 후보라면
            {
                var targetPiece = targetTile.OccupyingPiece; // 현재 실제 공격 대상 조회
                if (targetPiece == null || targetPiece != candidate.TargetPiece) return false; // 후보가 가리키던 대상과 실제 점유가 다르면 무효
                return targetPiece.IsPlayerPiece && !targetPiece.IsDead; // 살아 있는 플레이어 기물만 공격 가능
            }

            return false; // 알 수 없는 행동 종류는 안전하게 제외
        }

        private static int CompareByBasePriority(AIActionCandidate a, AIActionCandidate b) // 예산 초과 시 높은 Base 우선순위를 안정적으로 유지하는 정렬 비교
        {
            if (ReferenceEquals(a, b)) return 0; // 같은 객체면 동일 순위
            if (a == null) return 1; // null은 뒤로
            if (b == null) return -1; // 정상 후보는 null보다 앞
            if (a.Score != b.Score) return b.Score.CompareTo(a.Score); // 1순위: Base Score 높은 후보
            if (a.ActionType != b.ActionType) return a.ActionType == AIActionType.Attack ? -1 : 1; // 2순위: 동점이면 공격 우선

            string aId = a.Actor?.Definition?.PieceId ?? string.Empty; // 첫 후보 PieceId
            string bId = b.Actor?.Definition?.PieceId ?? string.Empty; // 둘째 후보 PieceId
            int idComparison = string.Compare(aId, bId, StringComparison.Ordinal); // 문화권 영향 없는 문자열 비교
            if (idComparison != 0) return idComparison; // 3순위: PieceId 사전순

            if (a.Origin.y != b.Origin.y) return a.Origin.y.CompareTo(b.Origin.y); // 4순위: 원점 Y
            if (a.Origin.x != b.Origin.x) return a.Origin.x.CompareTo(b.Origin.x); // 5순위: 원점 X
            if (a.Target.y != b.Target.y) return a.Target.y.CompareTo(b.Target.y); // 6순위: 목표 Y
            return a.Target.x.CompareTo(b.Target.x); // 7순위: 목표 X
        }

        private readonly struct CandidateKey : IEquatable<CandidateKey> // 동일 행동 중복 제거에 사용할 작은 값 객체
        {
            private readonly PieceRuntimeState _actor; // 행동 주체 참조
            private readonly Vector2Int _origin; // 시작 좌표
            private readonly Vector2Int _target; // 목표 좌표
            private readonly AIActionType _actionType; // 행동 종류

            public CandidateKey(AIActionCandidate candidate) // 후보에서 중복 검사 키를 만드는 생성자
            {
                _actor = candidate.Actor; // 행동 주체 저장
                _origin = candidate.Origin; // 시작 좌표 저장
                _target = candidate.Target; // 목표 좌표 저장
                _actionType = candidate.ActionType; // 행동 종류 저장
            }

            public bool Equals(CandidateKey other) // 후보 행동 정체성 비교
            {
                return ReferenceEquals(_actor, other._actor) && _origin == other._origin && _target == other._target && _actionType == other._actionType; // 같은 기물·좌표·행동이면 중복
            }

            public override bool Equals(object obj) // object 비교 오버라이드
            {
                return obj is CandidateKey other && Equals(other); // 같은 키 타입만 비교
            }

            public override int GetHashCode() // HashSet용 참조 기반 해시 생성
            {
                unchecked // 정수 오버플로를 해시 계산에 허용
                {
                    int hash = _actor != null ? RuntimeHelpers.GetHashCode(_actor) : 0; // 행동 주체 참조 해시
                    hash = (hash * 397) ^ _origin.GetHashCode(); // 원점 좌표 결합
                    hash = (hash * 397) ^ _target.GetHashCode(); // 목표 좌표 결합
                    hash = (hash * 397) ^ (int)_actionType; // 행동 종류 결합
                    return hash; // 최종 해시 반환
                }
            }
        }
    }
}
