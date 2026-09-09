namespace ProjectEta.UI
{
    public static class Day66CardLiftState
    {
        public const float IdleOffset = -24f; // 대기 카드 하강 거리
        public const float RaisedOffset = 28f; // 선택 카드 상승 거리

        public static float ResolveTargetOffset(bool interactable, bool hovered, bool fusionSelected, bool pointerHeld)
        {
            if (fusionSelected) return RaisedOffset; // Fusion 선택 카드 상승 유지
            if (interactable && (hovered || pointerHeld)) return RaisedOffset; // 사용 예정 카드 상승
            return IdleOffset; // 나머지 카드 하강 유지
        }
    }
}
