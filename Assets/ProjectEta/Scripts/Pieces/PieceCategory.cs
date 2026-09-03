namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public enum PieceCategory // 기물의 획득 경로/분류를 나타내는 열거형
    {
        Basic, // 기본 제공 기물
        Fusion, // 합성으로만 얻는 기물
        Special, // 특수 규칙을 가진 기물
        Monster, // 몬스터(적 전용) 기물
        Boss // 보스 전용 기물
    }
}
