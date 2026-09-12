using System.Collections.Generic; // IReadOnlyList<T>·List<T>·HashSet<T> 사용
using ProjectEta.Board; // BoardState 크기 사용

namespace ProjectEta.Run
{
    public enum EnemyEncounterContentIssueType
    {
        EmptyEncounter = 0, // 적 배치 없음
        NullSpawn = 1, // 빈 배치 항목
        NullPiece = 2, // 적 기물 누락
        ForbiddenPiece = 3, // 허용되지 않는 기물
        InvalidCell = 4, // 보드 밖·아군 진영 배치
        DuplicateCell = 5 // 같은 Cell 중복 배치
    }

    public sealed class EnemyEncounterContentIssue
    {
        public EnemyEncounterContentIssueType IssueType { get; } // 문제 종류
        public int SpawnIndex { get; } // 관련 배치 순번

        public EnemyEncounterContentIssue(EnemyEncounterContentIssueType issueType, int spawnIndex)
        {
            IssueType = issueType; // 문제 종류 저장
            SpawnIndex = spawnIndex; // 배치 순번 저장
        }
    }

    public static class EnemyEncounterContentValidator
    {
        public static IReadOnlyList<EnemyEncounterContentIssue> Validate(EnemyEncounterResult encounter)
        {
            var issues = new List<EnemyEncounterContentIssue>(); // 검증 문제 목록 생성

            if (encounter == null || encounter.Spawns == null || encounter.Spawns.Count <= 0)
            {
                issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.EmptyEncounter, -1)); // 빈 Encounter 문제 등록
                return issues; // 추가 검사 없이 반환
            }

            var cells = new HashSet<int>(); // 점유 Cell 중복 검사 집합

            for (int i = 0; i < encounter.Spawns.Count; i++)
            {
                EnemyEncounterSpawn spawn = encounter.Spawns[i]; // 현재 배치 조회
                if (spawn == null)
                {
                    issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.NullSpawn, i)); // 빈 배치 문제 등록
                    continue; // 다음 배치 검사
                }

                if (spawn.Piece == null)
                {
                    issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.NullPiece, i)); // 기물 누락 문제 등록
                }
                else if (!EnemyEncounterRules.CanUsePiece(spawn.Piece))
                {
                    issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.ForbiddenPiece, i)); // 금지 기물 문제 등록
                }

                if (!IsEnemyBoardCell(spawn.Position.x, spawn.Position.y))
                {
                    issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.InvalidCell, i)); // 잘못된 Cell 문제 등록
                    continue; // 중복 Cell 검사 생략
                }

                int cellKey = spawn.Position.y * BoardState.Width + spawn.Position.x; // Cell 키 계산
                if (!cells.Add(cellKey))
                {
                    issues.Add(new EnemyEncounterContentIssue(EnemyEncounterContentIssueType.DuplicateCell, i)); // 중복 Cell 문제 등록
                }
            }

            return issues; // 전체 Encounter 검증 결과 반환
        }

        private static bool IsEnemyBoardCell(int col, int row)
        {
            return col >= 0
                && col < BoardState.Width
                && row >= EnemyEncounterRules.EnemyFallbackStartRow
                && row < BoardState.Height; // 적 진영 내부 좌표 여부 반환
        }
    }
}
