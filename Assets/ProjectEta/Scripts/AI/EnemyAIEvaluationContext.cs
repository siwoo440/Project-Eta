using System; // StringComparison과 IEquatable<T>를 사용하기 위한 네임스페이스
using System.Collections.Generic; // Dictionary<TKey,TValue>를 사용하기 위한 네임스페이스
using System.Runtime.CompilerServices; // 참조 동일성 기반 해시 코드를 만들기 위한 RuntimeHelpers 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResult를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIEvaluationContext // 한 AI 평가 동안 반복 사용하는 King·Threat·미래 이동 결과를 공유하는 39일차 캐시 컨텍스트
    {
        private readonly Dictionary<AIActionCandidate, MovementResult> _defaultFutureMovement = new Dictionary<AIActionCandidate, MovementResult>(); // 일반 후보 미래 이동 결과 캐시
        private readonly Dictionary<OverrideMovementKey, MovementResult> _overrideFutureMovement = new Dictionary<OverrideMovementKey, MovementResult>(); // Chameleon처럼 강제 이동 타입이 있는 미래 이동 결과 캐시

        public BoardState Board { get; } // 이번 평가가 읽는 실제 보드 상태
        public PieceRuntimeState PlayerKing { get; } // 보드 전체를 한 번만 순회해 찾은 플레이어 King
        public EnemyAIThreatMap ThreatMap { get; } // 요청된 좌표만 실제 계산하는 Lazy Threat Map
        public int FutureMovementResolveCount { get; private set; } // 캐시 적중을 제외한 실제 미래 MovementResolver 호출 횟수

        public EnemyAIEvaluationContext(BoardState board) // 한 번의 후보 평가용 공유 컨텍스트 생성자
        {
            Board = board; // 현재 보드 참조 저장
            PlayerKing = FindPlayerKing(board); // King을 후보마다 찾지 않고 최초 한 번만 탐색
            ThreatMap = EnemyAIThreatMap.Build(board); // 위협 맵은 좌표 요청 전에는 실제 위협 계산을 하지 않는 Lazy 구조로 준비
        }

        public bool TryResolveFutureMovement(AIActionCandidate candidate, out MovementResult futureMovement) // 기본 PieceDefinition 이동 규칙으로 후보 위치의 다음 행동을 캐시해서 계산
        {
            futureMovement = new MovementResult(); // 실패 시에도 빈 결과를 반환
            if (candidate == null) return false; // 후보가 없으면 계산 불가

            if (_defaultFutureMovement.TryGetValue(candidate, out var cachedMovement)) // 동일 후보를 이미 계산했다면
            {
                futureMovement = cachedMovement; // 같은 MovementResult를 즉시 재사용
                return true; // 캐시 적중 성공
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, Board, out var resolvedMovement)) return false; // 실제 보드 시뮬레이션이 실패하면 캐시하지 않음

            _defaultFutureMovement[candidate] = resolvedMovement; // 같은 후보의 다음 평가 계층이 재사용하도록 저장
            FutureMovementResolveCount++; // 실제 MovementResolver 계산 횟수만 증가
            futureMovement = resolvedMovement; // 계산 결과 반환
            return true; // 미래 이동 계산 성공
        }

        public bool TryResolveFutureMovement(AIActionCandidate candidate, PieceMovementType overrideMovementType, out MovementResult futureMovement) // Chameleon 다음 형태처럼 이동 타입을 강제로 지정해 캐시하는 오버로드
        {
            futureMovement = new MovementResult(); // 실패 시 빈 결과 준비
            if (candidate == null) return false; // 후보가 없으면 계산 불가

            var key = new OverrideMovementKey(candidate, overrideMovementType); // 후보 참조와 강제 이동 타입을 함께 캐시 키로 구성

            if (_overrideFutureMovement.TryGetValue(key, out var cachedMovement)) // 같은 후보·같은 형태를 이미 계산했다면
            {
                futureMovement = cachedMovement; // 기존 결과 재사용
                return true; // 캐시 적중 성공
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, Board, overrideMovementType, out var resolvedMovement)) return false; // 강제 이동 타입으로 실제 계산

            _overrideFutureMovement[key] = resolvedMovement; // 같은 다음 형태 평가에서 재사용하도록 저장
            FutureMovementResolveCount++; // 실제 계산 횟수만 증가
            futureMovement = resolvedMovement; // 결과 반환
            return true; // 미래 이동 계산 성공
        }

        private static PieceRuntimeState FindPlayerKing(BoardState board) // 후보마다 반복되던 King 탐색을 한 번으로 줄이는 도우미
        {
            if (board == null) return null; // 보드가 없으면 King 없음

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.IsDead || piece.Definition == null) continue; // 살아 있는 플레이어 기물만 검사
                    if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return piece; // PieceId가 King이면 즉시 반환
                    if (piece.Definition.MovementType == PieceMovementType.King) return piece; // 기존 데이터 호환을 위해 Legacy King 이동 타입도 허용
                }
            }

            return null; // 끝까지 찾지 못하면 King 없음
        }

        private readonly struct OverrideMovementKey : IEquatable<OverrideMovementKey> // 후보 참조와 강제 이동 타입을 묶는 작은 캐시 키
        {
            private readonly AIActionCandidate _candidate; // 같은 후보 객체인지 비교할 참조
            private readonly PieceMovementType _movementType; // 강제로 사용할 이동 타입

            public OverrideMovementKey(AIActionCandidate candidate, PieceMovementType movementType) // 캐시 키 생성자
            {
                _candidate = candidate; // 후보 참조 저장
                _movementType = movementType; // 이동 타입 저장
            }

            public bool Equals(OverrideMovementKey other) // Dictionary가 같은 키인지 확인하는 비교
            {
                return ReferenceEquals(_candidate, other._candidate) && _movementType == other._movementType; // 후보는 참조 동일성, 이동 타입은 값 동일성으로 비교
            }

            public override bool Equals(object obj) // object 기반 비교 오버라이드
            {
                return obj is OverrideMovementKey other && Equals(other); // 같은 구조체 타입일 때만 실제 비교
            }

            public override int GetHashCode() // Dictionary용 결정론적 해시 생성
            {
                unchecked // 정수 오버플로를 해시 계산에 허용
                {
                    int candidateHash = _candidate != null ? RuntimeHelpers.GetHashCode(_candidate) : 0; // 후보 객체의 참조 해시 사용
                    return (candidateHash * 397) ^ (int)_movementType; // 이동 타입과 결합해 캐시 키 완성
                }
            }
        }
    }
}
