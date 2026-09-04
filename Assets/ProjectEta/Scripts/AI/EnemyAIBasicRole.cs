namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public enum EnemyAIBasicRole // 34일차에서 사용하는 세 가지 기본 AI 성격 분류
    {
        None = 0, // 특수 기물·보스·역할 미지정 기물처럼 34일차 보정을 사용하지 않는 상태
        Melee = 1, // 킹에게 적극적으로 접근하는 근접형
        Slider = 2, // 열린 직선·대각 공격선과 넓은 기동 공간을 선호하는 슬라이더형
        Jumper = 3 // 장애물을 넘어 다음 공격 위치를 잡는 도약형
    }
}
