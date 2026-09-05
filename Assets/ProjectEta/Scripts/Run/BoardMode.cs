namespace ProjectEta.Run // 런 보드 모드 네임스페이스
{
    public enum BoardMode // 동일 10×10 체스판의 현재 역할
    {
        Battle = 0, // 기물 전투를 수행하는 전투판 모드
        Map = 1 // 다음 스테이지를 고르는 경로 지도 모드
    }
}
