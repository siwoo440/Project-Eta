namespace ProjectEta.Fusion // 합성 관련 타입을 모아두는 네임스페이스
{
    public enum FusionBlockReason // 22일차: 합성이 불가능한 구체적인 이유를 UI와 테스트에 그대로 전달하기 위한 열거형
    {
        None, // 합성 가능(차단 사유 없음)
        NotEnoughMaterials, // 재료가 아직 2장 모이지 않음
        NoRecipe, // 선택한 조합에 매칭되는 레시피가 없음
        MaterialNotFusable, // King처럼 합성 재료로 쓸 수 없는 분류의 기물이 포함됨
        GradeStepViolation, // 결과 등급이 재료 최고 등급보다 2단계 이상 높아 등급 상승 규칙을 위반함
        MaterialsMissingInHand, // 손패에 재료가 실제로 없음(동일 카드 합성 시 2장 미보유 포함)
        OwnedLimitReached, // 4·5성 결과 기물의 보유 수량 제한을 이미 채움
        NotDeploymentTurn // 배치 턴이 아니라 합성 자체가 불가능함
    }
}
