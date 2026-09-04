using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResolver를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

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

        public static int EvaluateMoveBonus(AIActionCandidate candidate, BoardState board) // 이동 후보 하나의 역할별 추가 점수를 계산하는 메서드
        {
            if (candidate == null || candidate.Actor == null || board == null) return 0; // 필수 데이터가 없으면 보정하지 않음
            if (candidate.ActionType != AIActionType.Move) return 0; // 34일차 역할 보정은 이동 위치 선택에 집중하고 기존 공격 점수는 그대로 유지

            EnemyAIBasicRole role = EnemyAIRoleClassifier.GetBasicRole(candidate.Actor.Definition); // 행동 주체의 기본 AI 성격 분류
            if (role == EnemyAIBasicRole.None) return 0; // 특수형·보스·미분류 기물은 33일차 공통 점수만 사용

            bool hasKing = TryFindPlayerKing(board, out var playerKing); // 현재 플레이어 킹 탐색

            switch (role) // 기본 역할에 따라 서로 다른 이동 성격 적용
            {
                case EnemyAIBasicRole.Melee:
                    return EvaluateMelee(candidate, hasKing ? playerKing : null); // 근접형은 거리 접근 중심으로 평가
                case EnemyAIBasicRole.Slider:
                    return EvaluateSlider(candidate, board, hasKing ? playerKing : null); // 슬라이더는 공격선과 기동 공간 중심으로 평가
                case EnemyAIBasicRole.Jumper:
                    return EvaluateJumper(candidate, board, hasKing ? playerKing : null); // 도약형은 다음 공격 위치와 기동성 중심으로 평가
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

        private static int EvaluateSlider(AIActionCandidate candidate, BoardState board, PieceRuntimeState playerKing) // 슬라이더 이동 후보의 추가 점수를 계산하는 메서드
        {
            if (!TryResolveFromCandidatePosition(candidate, board, out var futureMovement)) return 0; // 후보 위치에서 실제 MovementResolver 계산에 실패하면 보정하지 않음

            int mobility = futureMovement.MoveTiles.Count + futureMovement.AttackTiles.Count; // 이동 후 열리는 전체 합법 선택지 수 계산
            int bonus = mobility * SliderMobilityScorePerTile; // 열린 라인이 길수록 작은 기동성 보너스 부여

            if (playerKing != null && futureMovement.AttackTiles.Contains(playerKing.BoardPosition)) // 후보 위치에서 실제 규칙상 킹을 공격할 수 있으면
            {
                bonus += SliderKingLineBonus; // 열린 공격선을 확보한 위치에 큰 추가 점수
            }

            return bonus; // 슬라이더 최종 보정 점수 반환
        }

        private static int EvaluateJumper(AIActionCandidate candidate, BoardState board, PieceRuntimeState playerKing) // 도약형 이동 후보의 추가 점수를 계산하는 메서드
        {
            if (!TryResolveFromCandidatePosition(candidate, board, out var futureMovement)) return 0; // 후보 위치에서 실제 이동 규칙 계산 실패 시 보정 없음

            int mobility = futureMovement.MoveTiles.Count + futureMovement.AttackTiles.Count; // 착지 후 가능한 다음 선택지 수 계산
            int bonus = mobility * JumperMobilityScorePerTile; // 장애물과 상관없이 많은 착지 선택지를 갖는 위치에 기동성 보너스

            if (playerKing != null && futureMovement.AttackTiles.Contains(playerKing.BoardPosition)) // 착지 후 다음 행동에 킹을 실제로 공격할 수 있으면
            {
                bonus += JumperKingThreatBonus; // 도약형의 공격 포지셔닝을 강하게 선호
            }

            return bonus; // 도약형 최종 보정 점수 반환
        }

        private static bool TryResolveFromCandidatePosition(AIActionCandidate candidate, BoardState board, out MovementResult futureMovement) // 실제 BoardPosition을 바꾸지 않고 후보 칸에서의 이동 규칙을 시뮬레이션하는 메서드
        {
            futureMovement = new MovementResult(); // 실패 시에도 빈 결과를 안전하게 반환
            var actor = candidate.Actor; // 행동 주체 참조
            var originTile = board.GetTile(candidate.Origin); // 현재 원점 타일 조회
            var targetTile = board.GetTile(candidate.Target); // 후보 목표 타일 조회

            if (originTile == null || targetTile == null) return false; // 보드 범위 밖이면 시뮬레이션 불가
            if (originTile.OccupyingPiece != actor) return false; // 현재 보드 상태와 후보의 행동 주체가 다르면 오래된 후보이므로 중단
            if (targetTile.IsOccupied || targetTile.IsBlockedByObstacle) return false; // 이동 후보는 반드시 빈 정상 칸이어야 함

            var originalOriginOccupant = originTile.OccupyingPiece; // 원상 복구를 위해 원점 점유 저장
            var originalTargetOccupant = targetTile.OccupyingPiece; // 원상 복구를 위해 목표 점유 저장

            try // 보드 점유만 잠시 바꿔 실제 이동 규칙에 후보 위치를 보여줌
            {
                originTile.OccupyingPiece = null; // 원점 점유를 잠시 비움
                targetTile.OccupyingPiece = actor; // 후보 목표 칸에 행동 주체가 있다고 가정

                futureMovement = MovementResolver.GetReachableTiles(actor.Definition, candidate.Target, actor.IsPlayerPiece, board); // 기존 MovementResolver로 후보 위치에서 다음 이동·공격 계산
                return true; // 정상 시뮬레이션 성공
            }
            finally // 예외 여부와 상관없이 실제 보드 상태를 반드시 원상 복구
            {
                originTile.OccupyingPiece = originalOriginOccupant; // 원점 점유 복구
                targetTile.OccupyingPiece = originalTargetOccupant; // 목표 점유 복구
            }
        }

        private static bool TryFindPlayerKing(BoardState board, out PieceRuntimeState king) // 플레이어 킹을 찾는 공통 메서드
        {
            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 점유 기물 조회
                    if (piece == null || !piece.IsPlayerPiece || piece.IsDead || piece.Definition == null) continue; // 살아 있는 플레이어 기물만 검사
                    if (piece.Definition.PieceId == "king" || piece.Definition.MovementType == PieceMovementType.King) // PieceId 또는 기존 이동 타입으로 킹 판별
                    {
                        king = piece; // 킹 반환
                        return true; // 탐색 성공
                    }
                }
            }

            king = null; // 찾지 못했음을 명시
            return false; // 탐색 실패
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b) // 근접형 거리 계산에 사용할 맨해튼 거리 함수
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // X 차이와 Y 차이의 합 반환
        }
    }
}
