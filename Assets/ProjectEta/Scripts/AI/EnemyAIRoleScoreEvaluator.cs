using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResult를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAIRoleScoreEvaluator // 33일차 공통 점수에 역할별 이동 성격 점수를 더하는 34일차 평가기
    {
        private const int MeleeApproachScorePerTile = 60; // 근접형이 킹과의 거리를 1칸 줄일 때 얻는 추가 점수
        private const int MeleeAdjacentBonus = 180; // 이동 후 킹 바로 옆까지 접근했을 때의 추가 보너스
        private const int SliderMobilityScorePerTile = 4; // 슬라이더의 이동·공격 가능한 공간 1칸당 추가 점수
        private const int SliderKingLineBonus = 350; // 이동 후 킹을 직접 겨눌 수 있는 라인을 확보했을 때의 추가 점수
        private const int JumperMobilityScorePerTile = 6; // 도약형의 다음 이동·공격 선택지 1칸당 추가 점수
        private const int JumperKingThreatBonus = 420; // 이동 후 다음 행동에 킹을 바로 공격할 수 있을 때의 추가 점수

        public static int EvaluateMoveBonus(AIActionCandidate candidate, BoardState board) // 기존 호출부와 테스트 호환을 유지하는 API
        {
            return EvaluateMoveBonus(candidate, board, null); // 공유 컨텍스트가 없는 기존 평가 경로 사용
        }

        public static int EvaluateMoveBonus(AIActionCandidate candidate, BoardState board, EnemyAIEvaluationContext context) // 39일차 공유 캐시를 사용할 수 있는 최적화 오버로드
        {
            if (candidate == null || candidate.Actor == null || board == null) return 0; // 필수 데이터가 없으면 보정하지 않음
            if (candidate.ActionType != AIActionType.Move) return 0; // 역할 보정은 이동 위치 선택에 집중

            EnemyAIBasicRole role = EnemyAIRoleClassifier.GetBasicRole(candidate.Actor.Definition); // 행동 주체의 기본 AI 성격 분류
            if (role == EnemyAIBasicRole.None) return 0; // 특수형·보스·미분류 기물은 기본 점수만 사용

            PieceRuntimeState playerKing = context != null ? context.PlayerKing : FindPlayerKing(board); // 최적화 경로에서는 후보마다 King을 다시 찾지 않음

            switch (role) // 기본 역할에 따라 서로 다른 이동 성격 적용
            {
                case EnemyAIBasicRole.Melee:
                    return EvaluateMelee(candidate, playerKing); // 근접형은 거리 접근 중심으로 평가
                case EnemyAIBasicRole.Slider:
                    return EvaluateSlider(candidate, board, playerKing, context); // 슬라이더는 공격선과 기동 공간 중심으로 평가
                case EnemyAIBasicRole.Jumper:
                    return EvaluateJumper(candidate, board, playerKing, context); // 도약형은 다음 공격 위치와 기동성 중심으로 평가
                default:
                    return 0; // 예상하지 못한 역할은 보정 없음
            }
        }

        private static int EvaluateMelee(AIActionCandidate candidate, PieceRuntimeState playerKing) // 근접형 이동 후보의 추가 점수를 계산하는 메서드
        {
            if (playerKing == null) return 0; // 킹이 없으면 역할별 접근 목표가 없으므로 보정하지 않음

            int beforeDistance = ManhattanDistance(candidate.Origin, playerKing.BoardPosition); // 이동 전 킹과의 거리 계산
            int afterDistance = ManhattanDistance(candidate.Target, playerKing.BoardPosition); // 이동 후 킹과의 거리 계산
            int bonus = (beforeDistance - afterDistance) * MeleeApproachScorePerTile; // 가까워진 칸 수만큼 가점, 멀어진 만큼 감점

            if (afterDistance <= 1) bonus += MeleeAdjacentBonus; // 다음 행동에 바로 근접 압박이 가능한 위치면 추가 보너스

            return bonus; // 근접형 최종 보정 점수 반환
        }

        private static int EvaluateSlider(AIActionCandidate candidate, BoardState board, PieceRuntimeState playerKing, EnemyAIEvaluationContext context) // 슬라이더 이동 후보의 추가 점수를 계산하는 메서드
        {
            if (!TryResolveFromCandidatePosition(candidate, board, context, out var futureMovement)) return 0; // 후보 위치의 다음 이동 결과를 캐시 우선으로 계산

            int mobility = futureMovement.MoveTiles.Count + futureMovement.AttackTiles.Count; // 이동 후 열리는 전체 합법 선택지 수 계산
            int bonus = mobility * SliderMobilityScorePerTile; // 열린 라인이 길수록 작은 기동성 보너스 부여

            if (playerKing != null && futureMovement.AttackTiles.Contains(playerKing.BoardPosition)) bonus += SliderKingLineBonus; // 후보 위치에서 킹 공격선을 확보하면 큰 보너스

            return bonus; // 슬라이더 최종 보정 점수 반환
        }

        private static int EvaluateJumper(AIActionCandidate candidate, BoardState board, PieceRuntimeState playerKing, EnemyAIEvaluationContext context) // 도약형 이동 후보의 추가 점수를 계산하는 메서드
        {
            if (!TryResolveFromCandidatePosition(candidate, board, context, out var futureMovement)) return 0; // 후보 위치의 다음 이동 결과를 캐시 우선으로 계산

            int mobility = futureMovement.MoveTiles.Count + futureMovement.AttackTiles.Count; // 착지 후 가능한 다음 선택지 수 계산
            int bonus = mobility * JumperMobilityScorePerTile; // 많은 착지 선택지를 갖는 위치에 기동성 보너스

            if (playerKing != null && futureMovement.AttackTiles.Contains(playerKing.BoardPosition)) bonus += JumperKingThreatBonus; // 다음 행동에 킹을 위협하면 큰 보너스

            return bonus; // 도약형 최종 보정 점수 반환
        }

        private static bool TryResolveFromCandidatePosition(AIActionCandidate candidate, BoardState board, EnemyAIEvaluationContext context, out MovementResult futureMovement) // 후보 위치 미래 이동을 공유 캐시 또는 기존 시뮬레이터로 계산
        {
            if (context != null) return context.TryResolveFutureMovement(candidate, out futureMovement); // 39일차 최적화 경로에서는 같은 후보 결과를 재사용
            return EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, out futureMovement); // 기존 단독 호출은 이전 동작과 동일하게 직접 계산
        }

        private static PieceRuntimeState FindPlayerKing(BoardState board) // 공유 컨텍스트가 없는 기존 API에서 사용할 King 탐색 도우미
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.IsDead || piece.Definition == null) continue; // 살아 있는 플레이어 기물만 검사
                    if (piece.Definition.PieceId == "king" || piece.Definition.MovementType == PieceMovementType.King) return piece; // PieceId 또는 Legacy 이동 타입으로 King 판별
                }
            }

            return null; // 찾지 못하면 null 반환
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b) // 근접형 거리 계산에 사용할 맨해튼 거리 함수
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // X 차이와 Y 차이의 합 반환
        }
    }
}
