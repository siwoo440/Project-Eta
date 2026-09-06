namespace ProjectEta.SceneFlow
{
    public sealed class SceneTransitionGate
    {
        public bool IsTransitioning { get; private set; } // 현재 씬 전환 진행 여부

        public bool TryBegin()
        {
            if (IsTransitioning) return false; // 버튼 연타·중복 LoadScene 차단

            IsTransitioning = true; // 씬 전환 시작 기록
            return true; // 첫 전환 요청 허용
        }

        public void Complete()
        {
            IsTransitioning = false; // 새 씬 로드 완료 후 전환 잠금 해제
        }
    }
}
