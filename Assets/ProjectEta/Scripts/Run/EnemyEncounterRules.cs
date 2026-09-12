using UnityEngine; // Mathf 사용
using ProjectEta.Board; // BoardState 크기 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class EnemyEncounterRules
    {
        public static int EnemyFallbackStartRow => BoardState.Height / 2; // 적 진영 대체 배치 시작 행
        public static int EliteFallbackStartRow => Mathf.Max(EnemyFallbackStartRow, BoardState.Height - 2); // Elite 추가 적 후방 배치 시작 행

        public static bool CanUsePiece(PieceDefinition piece)
        {
            if (piece == null) return false; // 빈 기물 제외
            if (piece.MovementType == PieceMovementType.King) return false; // 플레이어 King 제외
            if (piece.Category == PieceCategory.Fusion) return false; // Fusion 기물 제외
            if (piece.Category == PieceCategory.Boss) return false; // Boss 전용 기물 제외
            return true; // 일반 적 후보 허용
        }

        public static int GetPieceThreatScore(PieceDefinition piece)
        {
            if (!CanUsePiece(piece)) return 0; // 사용할 수 없는 기물 위협도 제거

            int hpScore = Mathf.Max(1, piece.BaseHp) * 2; // HP 위협도 계산
            int atkScore = Mathf.Max(0, piece.BaseAtk) * 3; // 공격력 위협도 계산
            int monsterBonus = piece.Category == PieceCategory.Monster ? 3 : 0; // Monster 추가 위협도 계산
            int specialBonus = piece.Category == PieceCategory.Special ? 1 : 0; // Special 추가 위협도 계산
            return hpScore + atkScore + monsterBonus + specialBonus; // 총 기물 위협도 반환
        }

        public static int GetEliteExtraEnemyCount(int phase, int stage)
        {
            int safePhase = Mathf.Clamp(phase, RunPhaseProgressService.FirstPhase, RunPhaseProgressService.TotalPhases); // Phase 범위 보정
            int safeStage = Mathf.Clamp(stage, RoundState.FirstRound, RoundState.FinalRound); // Stage 범위 보정
            return safePhase >= 3 || safeStage >= 6 ? 2 : 1; // 중후반 Elite 추가 적 증가
        }

        public static int GetMinimumCandidateIndex(int candidateCount, int phase, bool elite)
        {
            if (candidateCount <= 1) return 0; // 단일 후보 인덱스 반환

            int safePhase = Mathf.Clamp(phase, RunPhaseProgressService.FirstPhase, RunPhaseProgressService.TotalPhases); // Phase 범위 보정
            float ratio = safePhase <= 2 ? 0f : safePhase == 3 ? 0.10f : safePhase == 4 ? 0.20f : 0.30f; // Phase별 최소 품질 비율 계산
            int minimum = Mathf.FloorToInt((candidateCount - 1) * ratio); // 최소 후보 인덱스 계산

            if (elite)
            {
                int maximum = GetMaximumCandidateIndex(candidateCount, safePhase); // 현재 Phase 최대 후보 인덱스 조회
                minimum += (maximum - minimum) / 2; // Elite 후보를 상위 절반으로 제한
            }

            return Mathf.Clamp(minimum, 0, candidateCount - 1); // 최소 후보 인덱스 반환
        }

        public static int GetMaximumCandidateIndex(int candidateCount, int phase)
        {
            if (candidateCount <= 1) return 0; // 단일 후보 인덱스 반환

            int safePhase = Mathf.Clamp(phase, RunPhaseProgressService.FirstPhase, RunPhaseProgressService.TotalPhases); // Phase 범위 보정
            float ratio = safePhase == 1 ? 0.55f : safePhase == 2 ? 0.70f : safePhase == 3 ? 0.85f : safePhase == 4 ? 0.95f : 1f; // Phase별 최대 품질 비율 계산
            int maximum = Mathf.CeilToInt((candidateCount - 1) * ratio); // 최대 후보 인덱스 계산
            return Mathf.Clamp(maximum, 0, candidateCount - 1); // 최대 후보 인덱스 반환
        }
    }
}
