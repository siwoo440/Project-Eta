using UnityEngine; // Mathf 사용

namespace ProjectEta.UI
{
    public static class Day66TransitionMotion
    {
        public const float StaggerInterval = 0.065f; // 오브젝트별 순차 시작 간격
        public const float WhooshDuration = 0.28f; // 위로 날아가는 개별 연출 시간
        public const float DropDuration = 0.24f; // 위에서 내려오는 개별 연출 시간
        public const float TransitionGap = 0.08f; // 나감과 등장 사이 짧은 공백

        public static float GetStaggerDelay(int index)
        {
            return Mathf.Max(0, index) * StaggerInterval; // 순서에 따른 시작 지연 계산
        }

        public static float GetSequenceDuration(int objectCount)
        {
            int safeCount = Mathf.Max(0, objectCount); // 음수 개수 방어
            if (safeCount == 0) return 0f; // 대상 없음 처리
            return WhooshDuration + GetStaggerDelay(safeCount - 1); // 마지막 오브젝트 종료 시간 계산
        }

        public static float EvaluateWhooshY(float startY, float height, float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime); // 진행률 범위 보정
            float eased = t * t * t; // 점점 빨라지는 위쪽 발사 곡선 적용
            return startY + Mathf.Max(0f, height) * eased; // 상승 위치 반환
        }

        public static float EvaluateDropY(float landingY, float height, float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime); // 진행률 범위 보정
            float startY = landingY + Mathf.Max(0f, height); // 공중 시작 높이 계산

            if (t < 0.82f)
            {
                float fallT = t / 0.82f; // 주 낙하 구간 진행률 계산
                float eased = 1f - Mathf.Pow(1f - fallT, 3f); // 빠르게 내려와 감속하는 낙하 곡선 적용
                float impactY = landingY - Mathf.Max(0f, height) * 0.045f; // 착지 순간 살짝 눌리는 높이 계산
                return Mathf.Lerp(startY, impactY, eased); // 충돌 직전 위치 반환
            }

            float settleT = (t - 0.82f) / 0.18f; // 착지 후 복원 구간 진행률 계산
            float settled = 1f - Mathf.Pow(1f - settleT, 2f); // 짧은 복원 감속 적용
            float pressedY = landingY - Mathf.Max(0f, height) * 0.045f; // 눌린 착지 높이 계산
            return Mathf.Lerp(pressedY, landingY, settled); // 최종 착지 위치 반환
        }
    }
}
