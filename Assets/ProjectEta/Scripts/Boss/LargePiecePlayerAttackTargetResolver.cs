using System.Collections.Generic; // IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 대형 보스 관련 보조 로직을 모아두는 네임스페이스
{
    public static class LargePiecePlayerAttackTargetResolver // 2x2 보스 모델 클릭을 실제 공격 가능한 점유 칸으로 변환하는 순수 도우미
    {
        public static bool TryResolveAttackCell( // 같은 PieceRuntimeState가 차지한 여러 칸 중 현재 공격 가능한 칸을 하나 찾는 메서드
            BoardState board, // 현재 실제 전투 보드
            PieceRuntimeState targetPiece, // 플레이어가 클릭한 대형 적 런타임 기물
            IReadOnlyList<Vector2Int> attackTiles, // 현재 선택 아군의 실제 공격 가능 칸 목록
            out Vector2Int attackCell) // 기존 BoardInputController에 넘길 최종 공격 칸
        {
            attackCell = default; // 실패 시 기본 좌표 반환
            if (board == null || targetPiece == null || attackTiles == null) return false; // 필수 데이터가 없으면 해결 불가

            for (int i = 0; i < attackTiles.Count; i++) // 현재 기물이 실제로 공격 가능한 칸만 순회
            {
                Vector2Int candidate = attackTiles[i]; // 이번 공격 가능 후보 좌표
                TileState tile = board.GetTile(candidate); // 실제 보드 타일 조회
                if (tile == null) continue; // 보드 밖 또는 잘못된 좌표는 제외
                if (tile.OccupyingPiece != targetPiece) continue; // 클릭한 같은 대형 보스가 점유한 칸만 인정

                attackCell = candidate; // 실제 공격 가능한 동일 보스 점유 칸 저장
                return true; // 기존 공격 진입점에 넘길 수 있으므로 성공
            }

            return false; // 현재 선택 기물 기준으로 이 보스를 공격할 수 있는 점유 칸이 없음
        }
    }
}
