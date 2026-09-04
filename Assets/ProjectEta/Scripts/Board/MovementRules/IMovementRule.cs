using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스

namespace ProjectEta.Board // 보드 이동 규칙 타입을 모아두는 네임스페이스
{
    public interface IMovementRule // 하나의 이동 패턴이 공통으로 구현해야 하는 계약
    {
        MovementResult Resolve(Vector2Int origin, bool isPlayerPiece, BoardState board); // 기준 좌표와 진영·보드를 받아 이동/공격 후보를 계산
    }
}
