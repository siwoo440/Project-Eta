using UnityEngine; // Vector2Int와 Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition과 PieceRuntimeState를 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 37일차 이후 대형 보스 기물 기반을 모아두는 네임스페이스
{
    public static class LargePieceBoardUtility // PieceDefinition.OccupancySize를 실제 보드 점유와 이동 복구에 연결하는 공통 유틸리티
    {
        public static Vector2Int GetFootprint(PieceDefinition definition) // 기물 정의에서 항상 1 이상인 점유 크기를 읽는 메서드
        {
            if (definition == null) return Vector2Int.one; // 정의가 없으면 안전한 1x1 사용

            var size = definition.OccupancySize; // 저장된 점유 크기 읽기
            return new Vector2Int(Mathf.Max(1, size.x), Mathf.Max(1, size.y)); // 0 이하 잘못된 값은 최소 1로 보정
        }

        public static bool IsLarge(PieceRuntimeState piece) // 런타임 기물이 2칸 이상을 차지하는 대형 기물인지 확인하는 메서드
        {
            if (piece?.Definition == null) return false; // 기물 정의가 없으면 일반 기물로 취급
            var size = GetFootprint(piece.Definition); // 정규화한 점유 크기 읽기
            return size.x > 1 || size.y > 1; // 가로나 세로 중 하나라도 2 이상이면 대형 기물
        }

        public static bool CanPlace(BoardState board, PieceDefinition definition, Vector2Int anchor, PieceRuntimeState ignorePiece = null) // 지정 기준점에 기물 점유 영역 전체를 놓을 수 있는지 검사
        {
            if (board == null || definition == null) return false; // 필수 데이터가 없으면 배치 불가
            return board.CanOccupyArea(anchor, GetFootprint(definition), ignorePiece); // BoardState 공통 영역 검사 재사용
        }

        public static bool TryPlace(BoardState board, PieceRuntimeState piece, Vector2Int anchor) // 기물 하나를 OccupancySize 전체에 같은 런타임 상태로 배치하는 메서드
        {
            if (board == null || piece?.Definition == null) return false; // 필수 데이터가 없으면 실패
            if (!board.CanOccupyArea(anchor, GetFootprint(piece.Definition), piece)) return false; // 부분 점유 전에 전체 영역 사전 검사

            if (piece.BoardPosition != anchor) piece.BoardPosition = anchor; // 기준 좌표가 다르면 런타임 위치를 먼저 동기화
            return board.TryOccupyArea(anchor, GetFootprint(piece.Definition), piece); // 모든 점유 칸에 같은 PieceRuntimeState 등록
        }

        public static bool IsFootprintComplete(BoardState board, PieceRuntimeState piece) // 현재 기물의 모든 점유 예정 칸이 실제로 같은 런타임 상태를 가리키는지 검사
        {
            if (board == null || piece?.Definition == null) return false; // 필수 데이터가 없으면 완료되지 않은 상태
            var size = GetFootprint(piece.Definition); // 점유 크기 읽기

            for (int x = 0; x < size.x; x++) // 점유 영역 가로 순회
            {
                for (int y = 0; y < size.y; y++) // 점유 영역 세로 순회
                {
                    var tile = board.GetTile(piece.BoardPosition + new Vector2Int(x, y)); // 현재 기물 기준점에서 실제 칸 조회
                    if (tile == null || tile.OccupyingPiece != piece) return false; // 한 칸이라도 같은 기물을 가리키지 않으면 불완전
                }
            }

            return true; // 모든 칸이 같은 기물 참조면 점유 완료
        }

        public static bool ExpandExistingAnchorOccupancy(BoardState board, PieceRuntimeState piece) // 기존 1x1 스폰이 만든 기준 칸을 OccupancySize 전체로 확장하는 메서드
        {
            if (board == null || piece?.Definition == null) return false; // 필수 데이터가 없으면 실패
            if (!IsLarge(piece)) return true; // 1x1 기물은 이미 완성된 것으로 취급
            if (IsFootprintComplete(board, piece)) return true; // 이미 전체 점유 상태면 중복 처리하지 않음

            var size = GetFootprint(piece.Definition); // 대형 기물 점유 크기
            if (!board.CanOccupyArea(piece.BoardPosition, size, piece)) return false; // 기존 기준 칸의 자기 자신은 허용하고 나머지 칸 충돌을 검사

            board.ClearPiece(piece); // 기존 기준 칸이나 잘못 남은 자기 점유를 모두 지움
            return board.TryOccupyArea(piece.BoardPosition, size, piece); // 같은 런타임 상태 하나로 전체 영역을 다시 점유
        }

        public static bool RepairAfterExistingMove(BoardState board, PieceRuntimeState piece, Vector2Int origin, Vector2Int destination) // 기존 1x1 이동 코드 실행 뒤 대형 점유를 전체 영역으로 복구하는 메서드
        {
            if (board == null || piece?.Definition == null) return false; // 필수 데이터가 없으면 실패
            if (!IsLarge(piece)) return true; // 일반 1x1 기물은 별도 복구가 필요 없음

            var size = GetFootprint(piece.Definition); // 대형 기물 점유 크기
            board.ClearPiece(piece); // 기존 이동 코드가 남긴 원점·목표점의 단일 점유와 나머지 잔여 점유를 모두 정리

            if (board.CanOccupyArea(destination, size, piece)) // 새 기준점의 전체 영역이 사용 가능하면
            {
                if (piece.BoardPosition != destination) piece.BoardPosition = destination; // 런타임 기준점 동기화
                return board.TryOccupyArea(destination, size, piece); // 새 2x2 영역 전체 점유
            }

            if (piece.BoardPosition != origin) piece.BoardPosition = origin; // 새 위치가 불가능하면 이전 기준점으로 롤백
            return board.TryOccupyArea(origin, size, piece); // 기존 점유 영역을 다시 복구
        }

        public static int ClearAllOccupiedCells(BoardState board, PieceRuntimeState piece) // 사망 시 대형 기물 전체 점유를 한 번에 해제하는 메서드
        {
            return board != null ? board.ClearPiece(piece) : 0; // BoardState의 동일 참조 전체 해제 기능 재사용
        }
    }
}
