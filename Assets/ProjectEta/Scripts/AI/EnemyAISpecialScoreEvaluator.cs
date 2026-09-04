using System; // StringComparison을 사용하기 위한 네임스페이스
using UnityEngine; // Mathf와 Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState와 MovementResult를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceCategory와 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public static class EnemyAISpecialScoreEvaluator // Cannon·Grasshopper·Nightrider·Chameleon의 특수 규칙 활용도를 평가하는 35일차 점수 계층
    {
        private const int CannonAttackBonus = 320; // Cannon이 실제 장거리 공격을 사용할 때의 보너스
        private const int CannonFutureAttackPerTarget = 70; // Cannon 이동 후 공격 가능한 플레이어 기물 1개당 보너스
        private const int CannonKingLineBonus = 650; // 이동 후 King 직선 공격선을 확보할 때의 추가 보너스

        private const int GrasshopperAttackBonus = 260; // Grasshopper가 허들을 이용한 실제 공격을 할 때의 보너스
        private const int GrasshopperFutureAttackPerTarget = 90; // 이동 후 특수 공격 가능 대상 1개당 보너스
        private const int GrasshopperKingThreatBonus = 600; // 이동 후 King을 위협할 수 있을 때의 보너스

        private const int NightriderAttackBonus = 220; // Nightrider의 장거리 Rider 공격 활용 보너스
        private const int NightriderFutureAttackPerTarget = 60; // 이동 후 공격 가능 대상 1개당 보너스
        private const int NightriderTravelBonusPerTile = 25; // 긴 Rider 이동 자체를 활용할 때 거리 1당 보너스
        private const int NightriderKingThreatBonus = 520; // 이동 후 King을 위협할 때의 보너스

        private const int ChameleonAttackBonus = 120; // Chameleon 현재 형태 공격 사용 보너스
        private const int ChameleonFutureMobilityPerTile = 20; // 다음 형태의 이동·공격 선택지 1개당 보너스
        private const int ChameleonKingThreatBonus = 480; // 다음 형태로 King을 위협할 수 있을 때의 보너스

        private const int GenericSpecialAttackBonus = 120; // 아직 전용 평가가 없는 Special 기물의 최소 공격 보너스
        private const int GenericFutureAttackPerTarget = 30; // 아직 전용 평가가 없는 Special 기물의 이동 후 공격 대상 보너스
        private const int GenericKingThreatBonus = 350; // 일반 Special 기물이 이동 후 King을 위협할 때의 보너스

        public static int Evaluate(AIActionCandidate candidate, BoardState board) // 행동 후보 하나의 특수 기물 보너스를 계산하는 메서드
        {
            if (candidate == null || candidate.Actor?.Definition == null || board == null) return 0; // 필수 데이터가 없으면 특수 점수 없음

            string pieceId = candidate.Actor.Definition.PieceId ?? string.Empty; // 기물 id를 안전하게 읽음

            if (string.Equals(pieceId, "cannon", StringComparison.OrdinalIgnoreCase)) return EvaluateCannon(candidate, board); // Cannon 전용 평가
            if (string.Equals(pieceId, "grasshopper", StringComparison.OrdinalIgnoreCase)) return EvaluateGrasshopper(candidate, board); // Grasshopper 전용 평가
            if (string.Equals(pieceId, "nightrider", StringComparison.OrdinalIgnoreCase)) return EvaluateNightrider(candidate, board); // Nightrider 전용 평가
            if (string.Equals(pieceId, "chameleon", StringComparison.OrdinalIgnoreCase)) return EvaluateChameleon(candidate, board); // Chameleon 전용 평가

            if (candidate.Actor.Definition.Category == PieceCategory.Special) // 아직 전용 분기를 만들지 않은 Special 기물이면
            {
                return EvaluateGenericSpecial(candidate, board); // 공통 Special 평가 적용
            }

            return 0; // 기본·합성 기물은 별도 특수 보너스 없음
        }

        private static int EvaluateCannon(AIActionCandidate candidate, BoardState board) // Cannon의 직선 사격 위치를 평가하는 메서드
        {
            if (candidate.ActionType == AIActionType.Attack) // 현재 즉시 공격이라면
            {
                int attackBonus = CannonAttackBonus; // Cannon 특수 공격 자체에 보너스
                if (IsKing(candidate.TargetPiece)) attackBonus += CannonKingLineBonus; // King 직접 포격은 추가 보너스
                return attackBonus; // 공격 보너스 반환
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, out var futureMovement)) return 0; // 이동 후 Cannon 규칙 계산 실패 시 보너스 없음

            int bonus = futureMovement.AttackTiles.Count * CannonFutureAttackPerTarget; // 이동 후 바로 공격 가능한 대상 수 반영
            if (ContainsPlayerKing(futureMovement, board)) bonus += CannonKingLineBonus; // King 직선 사격선 확보 시 큰 보너스
            return bonus; // Cannon 이동 특수 점수 반환
        }

        private static int EvaluateGrasshopper(AIActionCandidate candidate, BoardState board) // Grasshopper의 허들 활용 위치를 평가하는 메서드
        {
            if (candidate.ActionType == AIActionType.Attack) // 현재 허들을 이용한 공격이면
            {
                int attackBonus = GrasshopperAttackBonus; // 특수 공격 사용 보너스
                if (IsKing(candidate.TargetPiece)) attackBonus += GrasshopperKingThreatBonus; // King 공격이면 추가 보너스
                return attackBonus; // 공격 보너스 반환
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, out var futureMovement)) return 0; // 이동 후 Grasshopper 특수 규칙 계산
            int bonus = futureMovement.AttackTiles.Count * GrasshopperFutureAttackPerTarget; // 이동 후 허들 공격 가능 대상 수 반영
            if (ContainsPlayerKing(futureMovement, board)) bonus += GrasshopperKingThreatBonus; // 다음 행동에 King을 위협하면 큰 보너스
            return bonus; // Grasshopper 이동 특수 점수 반환
        }

        private static int EvaluateNightrider(AIActionCandidate candidate, BoardState board) // Nightrider의 반복 Knight 벡터 활용도를 평가하는 메서드
        {
            if (candidate.ActionType == AIActionType.Attack) // 현재 장거리 Rider 공격이라면
            {
                int attackBonus = NightriderAttackBonus; // 특수 공격 사용 보너스
                if (IsKing(candidate.TargetPiece)) attackBonus += NightriderKingThreatBonus; // King 공격 추가 보너스
                return attackBonus; // 공격 보너스 반환
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, out var futureMovement)) return 0; // 착지 후 Rider 이동·공격 후보 계산
            int travelDistance = ManhattanDistance(candidate.Origin, candidate.Target); // 이번 반복 벡터 이동의 실제 거리 계산
            int bonus = futureMovement.AttackTiles.Count * NightriderFutureAttackPerTarget; // 다음 공격 대상 수 보너스
            bonus += travelDistance * NightriderTravelBonusPerTile; // Nightrider의 장거리 기동을 실제로 활용하는 이동에 작은 보너스
            if (ContainsPlayerKing(futureMovement, board)) bonus += NightriderKingThreatBonus; // 다음 행동에 King 위협 시 큰 보너스
            return bonus; // Nightrider 최종 특수 점수 반환
        }

        private static int EvaluateChameleon(AIActionCandidate candidate, BoardState board) // Chameleon의 다음 이동 형태까지 미리 보는 평가 메서드
        {
            if (candidate.ActionType == AIActionType.Attack) // 현재 형태에서 즉시 공격이라면
            {
                int attackBonus = ChameleonAttackBonus; // 현재 형태 공격 사용 보너스
                if (IsKing(candidate.TargetPiece)) attackBonus += ChameleonKingThreatBonus; // King 공격이면 추가 보너스
                return attackBonus; // 공격 보너스 반환
            }

            PieceMovementType nextMovementType = GetNextChameleonMovementType(candidate.Actor.MovementCycleIndex); // 실제 이동 후 바뀔 다음 형태 계산
            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, nextMovementType, out var futureMovement)) return 0; // 다음 형태의 이동·공격 범위 계산

            int mobility = futureMovement.MoveTiles.Count + futureMovement.AttackTiles.Count; // 다음 형태의 전체 선택지 수
            int bonus = mobility * ChameleonFutureMobilityPerTile; // 다음 형태 활용도가 높은 위치에 보너스
            if (ContainsPlayerKing(futureMovement, board)) bonus += ChameleonKingThreatBonus; // 다음 형태가 바로 King을 공격할 수 있으면 큰 보너스
            return bonus; // Chameleon 최종 특수 점수 반환
        }

        private static int EvaluateGenericSpecial(AIActionCandidate candidate, BoardState board) // 아직 전용 AI가 없는 Special 기물의 공통 평가
        {
            if (candidate.ActionType == AIActionType.Attack) // 즉시 공격이면
            {
                int attackBonus = GenericSpecialAttackBonus; // 특수 공격 사용에 최소 보너스
                if (IsKing(candidate.TargetPiece)) attackBonus += GenericKingThreatBonus; // King 공격 추가 보너스
                return attackBonus; // 공격 점수 반환
            }

            if (!EnemyAICandidateSimulation.TryResolveFutureMovement(candidate, board, out var futureMovement)) return 0; // 이동 후 실제 특수 이동 규칙 계산
            int bonus = futureMovement.AttackTiles.Count * GenericFutureAttackPerTarget; // 이동 후 공격 가능 대상 수 반영
            if (ContainsPlayerKing(futureMovement, board)) bonus += GenericKingThreatBonus; // 다음 행동 King 위협 보너스
            return bonus; // 공통 Special 점수 반환
        }

        private static PieceMovementType GetNextChameleonMovementType(int currentCycleIndex) // 이동 후 Chameleon의 다음 형태를 계산하는 메서드
        {
            int nextIndex = (Mathf.Abs(currentCycleIndex) + 1) % 4; // 실제 BoardPosition 이동 시 AdvanceMovementCycle과 같은 방식으로 다음 인덱스 계산

            switch (nextIndex) // 다음 순환 단계에 대응하는 이동 타입 선택
            {
                case 1: return PieceMovementType.Bishop; // Knight 이동 후 Bishop
                case 2: return PieceMovementType.Rook; // Bishop 이동 후 Rook
                case 3: return PieceMovementType.Queen; // Rook 이동 후 Queen
                default: return PieceMovementType.Knight; // Queen 이동 후 다시 Knight
            }
        }

        private static bool ContainsPlayerKing(MovementResult movement, BoardState board) // 미래 공격 후보 안에 플레이어 King이 포함되는지 확인하는 메서드
        {
            if (movement == null || board == null) return false; // 필수 데이터가 없으면 false

            for (int i = 0; i < movement.AttackTiles.Count; i++) // 모든 미래 공격 좌표 순회
            {
                var target = board.GetTile(movement.AttackTiles[i])?.OccupyingPiece; // 실제 해당 칸 플레이어 기물 조회
                if (IsKing(target)) return true; // King이면 즉시 true
            }

            return false; // King 공격 후보가 없으면 false
        }

        private static bool IsKing(PieceRuntimeState piece) // PieceId와 Legacy 이동 타입으로 King 여부를 판별하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 King이 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId가 king이면 King
            return piece.Definition.MovementType == PieceMovementType.King; // 기존 데이터 호환을 위해 King 이동 타입도 허용
        }

        private static int ManhattanDistance(Vector2Int a, Vector2Int b) // Nightrider 이동 거리 보너스에 사용할 맨해튼 거리 계산
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // X·Y 차이 합 반환
        }
    }
}
