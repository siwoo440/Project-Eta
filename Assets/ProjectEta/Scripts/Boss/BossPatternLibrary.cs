using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>와 HashSet<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int, Vector2, Mathf를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceRuntimeState와 PieceMovementType을 사용하기 위한 네임스페이스

namespace ProjectEta.Boss // 2x2 이상 보스 전투 관련 타입을 모아두는 네임스페이스
{
    public static class BossPatternLibrary // 텔레그래프 표시와 실제 공격이 반드시 같은 TargetCells를 공유하도록 위험 칸을 계산하는 순수 패턴 모음
    {
        public static List<Vector2Int> BuildSlamRing(BoardState board, PieceRuntimeState boss) // 2x2 보스 몸체를 둘러싼 한 칸 외곽 링을 만드는 메서드
        {
            var result = new List<Vector2Int>(); // 최종 위험 칸 목록
            if (board == null || boss?.Definition == null) return result; // 필수 데이터가 없으면 빈 목록 반환

            Vector2Int size = LargePieceBoardUtility.GetFootprint(boss.Definition); // 실제 보스 점유 크기 읽기
            int minX = boss.BoardPosition.x - 1; // 점유 영역보다 왼쪽 한 칸부터 검사
            int maxX = boss.BoardPosition.x + size.x; // 점유 영역보다 오른쪽 한 칸까지 검사
            int minY = boss.BoardPosition.y - 1; // 점유 영역보다 아래 한 칸부터 검사
            int maxY = boss.BoardPosition.y + size.y; // 점유 영역보다 위 한 칸까지 검사

            for (int x = minX; x <= maxX; x++) // 4x4 외곽 사각형 가로 순회
            {
                for (int y = minY; y <= maxY; y++) // 4x4 외곽 사각형 세로 순회
                {
                    var cell = new Vector2Int(x, y); // 현재 후보 칸 구성
                    if (!board.IsInsideBoard(cell)) continue; // 보드 경계 밖은 위험 칸에서 제외
                    if (IsInsideFootprint(boss.BoardPosition, size, cell)) continue; // 보스 자신의 2x2 몸체는 공격 범위에서 제외
                    result.Add(cell); // 나머지 외곽 링 칸을 실제 위험 칸으로 등록
                }
            }

            return result; // UI와 실제 공격이 그대로 사용할 동일 목록 반환
        }

        public static List<Vector2Int> BuildKingLane(BoardState board, PieceRuntimeState boss, PieceRuntimeState king, int length) // King 방향으로 보스 몸체 폭만큼 넓은 직선 공격 범위를 만드는 메서드
        {
            var result = new List<Vector2Int>(); // 최종 위험 칸 목록
            if (board == null || boss?.Definition == null || king == null) return result; // 필수 데이터가 없으면 빈 목록 반환

            Vector2Int size = LargePieceBoardUtility.GetFootprint(boss.Definition); // 보스 점유 크기 읽기
            int safeLength = Mathf.Max(1, length); // 최소 한 칸 길이를 보장
            Vector2 bossCenter = new Vector2( // 2x2 전체 중앙을 기준으로 King 방향을 판정
                boss.BoardPosition.x + (size.x - 1) * 0.5f, // 보스 중심 X
                boss.BoardPosition.y + (size.y - 1) * 0.5f); // 보스 중심 Y
            Vector2 delta = new Vector2(king.BoardPosition.x - bossCenter.x, king.BoardPosition.y - bossCenter.y); // King까지 방향 벡터
            bool horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y); // X 차이가 크거나 같으면 수평 공격으로 선택

            if (horizontal) // 좌우 방향 직선 공격을 만들 경우
            {
                int sign = delta.x >= 0f ? 1 : -1; // King이 오른쪽이면 +1, 왼쪽이면 -1
                int startX = sign > 0 ? boss.BoardPosition.x + size.x : boss.BoardPosition.x - 1; // 보스 몸체 바로 바깥 X에서 시작

                for (int step = 0; step < safeLength; step++) // 직선 길이만큼 전진
                {
                    int x = startX + sign * step; // 현재 거리의 X 좌표
                    for (int row = 0; row < size.y; row++) // 보스 세로 점유 폭만큼 두꺼운 공격선 생성
                    {
                        var cell = new Vector2Int(x, boss.BoardPosition.y + row); // 실제 위험 칸 구성
                        AddIfValid(board, boss.BoardPosition, size, cell, result); // 보드 안이며 보스 몸체가 아니면 추가
                    }
                }
            }
            else // 상하 방향 직선 공격을 만들 경우
            {
                int sign = delta.y >= 0f ? 1 : -1; // King이 위면 +1, 아래면 -1
                int startY = sign > 0 ? boss.BoardPosition.y + size.y : boss.BoardPosition.y - 1; // 보스 몸체 바로 바깥 Y에서 시작

                for (int step = 0; step < safeLength; step++) // 직선 길이만큼 전진
                {
                    int y = startY + sign * step; // 현재 거리의 Y 좌표
                    for (int column = 0; column < size.x; column++) // 보스 가로 점유 폭만큼 두꺼운 공격선 생성
                    {
                        var cell = new Vector2Int(boss.BoardPosition.x + column, y); // 실제 위험 칸 구성
                        AddIfValid(board, boss.BoardPosition, size, cell, result); // 보드 안이며 보스 몸체가 아니면 추가
                    }
                }
            }

            return result; // 예고와 실행이 함께 사용할 직선 위험 칸 반환
        }

        public static PieceRuntimeState FindPlayerKing(BoardState board) // 현재 보드에서 살아 있는 플레이어 King을 찾는 메서드
        {
            if (board == null) return null; // 보드가 없으면 찾을 수 없음
            var visited = new HashSet<PieceRuntimeState>(); // 향후 대형 플레이어 기물까지 중복 검사하지 않기 위한 집합

            for (int x = 0; x < BoardState.Width; x++) // 보드 가로 순회
            {
                for (int y = 0; y < BoardState.Height; y++) // 보드 세로 순회
                {
                    var piece = board.GetTile(new Vector2Int(x, y))?.OccupyingPiece; // 현재 칸 기물 조회
                    if (piece == null || piece.IsDead || !piece.IsPlayerPiece || !visited.Add(piece)) continue; // 살아 있는 플레이어 기물만 한 번 검사
                    if (IsKing(piece)) return piece; // King이면 즉시 반환
                }
            }

            return null; // 살아 있는 플레이어 King이 없으면 null
        }

        public static string GetDisplayName(BossPatternType patternType) // 패턴 종류를 플레이어에게 보여 줄 한글 이름으로 바꾸는 메서드
        {
            return patternType == BossPatternType.KingLane ? "왕을 겨누는 직선" : "주변 강타"; // 현재 두 패턴 이름 반환
        }

        private static void AddIfValid(BoardState board, Vector2Int anchor, Vector2Int size, Vector2Int cell, List<Vector2Int> result) // 중복 없는 유효 위험 칸만 추가하는 공통 도우미
        {
            if (!board.IsInsideBoard(cell)) return; // 보드 밖 제외
            if (IsInsideFootprint(anchor, size, cell)) return; // 보스 몸체 제외
            if (!result.Contains(cell)) result.Add(cell); // 같은 칸 중복 등록 방지
        }

        private static bool IsInsideFootprint(Vector2Int anchor, Vector2Int size, Vector2Int cell) // 셀이 보스 사각 점유 영역 내부인지 확인하는 메서드
        {
            return cell.x >= anchor.x && cell.x < anchor.x + Mathf.Max(1, size.x) // X 범위 안이고
                && cell.y >= anchor.y && cell.y < anchor.y + Mathf.Max(1, size.y); // Y 범위 안이면 몸체 내부
        }

        private static bool IsKing(PieceRuntimeState piece) // King 여부를 PieceId와 기존 이동 타입 양쪽으로 판별하는 메서드
        {
            if (piece?.Definition == null) return false; // 정의가 없으면 King 아님
            if (string.Equals(piece.Definition.PieceId, "king", StringComparison.OrdinalIgnoreCase)) return true; // PieceId 우선 판별
            return piece.Definition.MovementType == PieceMovementType.King; // Legacy 데이터 호환
        }
    }
}
