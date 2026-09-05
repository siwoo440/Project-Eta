using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // AI 점수 디버그 관련 타입을 기존 AI 네임스페이스에 함께 배치
{
    public sealed class AIDebugScoreSnapshotBuilder // 실제 AI와 동일한 1회 평가 결과를 재사용해 F1 창의 중복 계산을 없애는 39일차 디버그 빌더
    {
        private readonly EnemyAIAdvancedPlanner _advancedPlanner; // 실제 AI와 같은 캐시·후보 예산을 사용하는 최종 행동 평가기

        public AIDebugScoreSnapshotBuilder() : this(new EnemyAIAdvancedPlanner()) // 기존 자동 생성 경로와 호환되는 기본 생성자
        {
        }

        public AIDebugScoreSnapshotBuilder(EnemyAIAdvancedPlanner advancedPlanner) // 테스트나 다른 디버그 도구가 동일 플래너를 주입할 수 있는 생성자
        {
            _advancedPlanner = advancedPlanner ?? new EnemyAIAdvancedPlanner(); // null이면 기본 최종 플래너를 생성
        }

        public AIDebugScoreSnapshot Build(BoardState board) // 현재 보드 상태를 읽어 디버그 창에 표시할 전체 점수 로그를 만드는 메서드
        {
            if (board == null) return AIDebugScoreSnapshot.Empty(); // 보드가 없으면 빈 로그 반환

            var scoredCandidates = _advancedPlanner.BuildScoredCandidates(board); // Base부터 Special까지 실제 최종 평가를 정확히 한 번만 실행
            _advancedPlanner.TryChooseBestAction(scoredCandidates, out var selectedAction); // 이미 계산한 최종 후보 안에서 재계산 없이 실제 선택 행동 결정
            var performance = _advancedPlanner.LastPerformanceStats; // 방금 수행한 후보 절감·캐시·시간 통계 읽기
            var entries = new List<AIDebugScoreEntry>(scoredCandidates.Count); // 실제 정밀 평가 후보 수만큼 로그 항목 목록 준비

            for (int i = 0; i < scoredCandidates.Count; i++) // 정밀 평가가 끝난 후보만 순회
            {
                var scored = scoredCandidates[i]; // 점수 분해 객체 참조
                var baseCandidate = scored?.BaseCandidate; // 33일차 Base 후보 참조
                var finalCandidate = scored?.FinalCandidate; // 실제 선택용 최종 후보 참조
                if (baseCandidate == null || finalCandidate == null) continue; // 잘못된 항목은 디버그 화면에서 제외

                var role = EnemyAIRoleClassifier.GetBasicRole(baseCandidate.Actor?.Definition); // 행동 주체의 기본 AI 역할 분류
                bool isSelected = IsSameAction(finalCandidate, selectedAction); // 실제 선택 행동과 같은 후보인지 확인

                entries.Add(new AIDebugScoreEntry( // 한 줄의 점수 로그 생성
                    baseCandidate.Actor, // 행동 주체
                    baseCandidate.Origin, // 시작 좌표
                    baseCandidate.Target, // 목표 좌표
                    baseCandidate.ActionType, // 이동 또는 공격
                    role, // 기본 AI 역할
                    baseCandidate.Score, // Base Score
                    scored.RoleBonus, // Role Bonus
                    scored.ThreatScore, // Threat Score
                    scored.SpecialBonus, // Special Bonus
                    finalCandidate.Score, // Final Score
                    isSelected)); // 실제 선택 여부
            }

            entries.Sort(CompareEntries); // 실제 선택 행동과 높은 Final Score가 위로 오도록 정렬

            AIDebugScoreEntry selectedEntry = null; // 실제 선택 로그를 찾기 위한 변수
            for (int i = 0; i < entries.Count; i++) // 정렬된 로그 목록 순회
            {
                if (!entries[i].IsSelected) continue; // 선택된 행동이 아니면 건너뜀
                selectedEntry = entries[i]; // 실제 선택 행동 로그 저장
                break; // 하나만 존재하므로 탐색 종료
            }

            return new AIDebugScoreSnapshot(entries, selectedEntry, performance.TotalCandidateCount, performance); // 한 번의 실제 계산 결과와 성능 값을 함께 반환
        }

        private static bool IsSameAction(AIActionCandidate a, AIActionCandidate b) // 두 행동 후보가 동일한 실제 행동인지 비교하는 메서드
        {
            if (a == null || b == null) return false; // 둘 중 하나라도 없으면 동일하지 않음
            if (a.Actor != b.Actor) return false; // 행동 주체가 다르면 다른 행동
            if (a.ActionType != b.ActionType) return false; // 이동/공격 종류가 다르면 다른 행동
            if (a.Origin != b.Origin) return false; // 시작 좌표가 다르면 다른 행동
            return a.Target == b.Target; // 목표 좌표까지 같으면 동일 행동
        }

        private static int CompareEntries(AIDebugScoreEntry a, AIDebugScoreEntry b) // 디버그 로그 표시 순서를 결정하는 비교 함수
        {
            if (a.IsSelected != b.IsSelected) return a.IsSelected ? -1 : 1; // 실제 선택 행동을 항상 첫 줄에 배치
            if (a.FinalScore != b.FinalScore) return b.FinalScore.CompareTo(a.FinalScore); // 다음으로 Final Score 높은 순

            string aId = a.Actor?.Definition?.PieceId ?? string.Empty; // 첫 후보 PieceId 읽기
            string bId = b.Actor?.Definition?.PieceId ?? string.Empty; // 둘째 후보 PieceId 읽기
            int idComparison = string.Compare(aId, bId, StringComparison.Ordinal); // 문화권 영향 없는 문자열 비교
            if (idComparison != 0) return idComparison; // PieceId가 다르면 사전순

            if (a.Origin.y != b.Origin.y) return a.Origin.y.CompareTo(b.Origin.y); // 같은 기물이면 원점 Y 순
            if (a.Origin.x != b.Origin.x) return a.Origin.x.CompareTo(b.Origin.x); // 다음으로 원점 X 순
            if (a.Target.y != b.Target.y) return a.Target.y.CompareTo(b.Target.y); // 다음으로 목표 Y 순
            return a.Target.x.CompareTo(b.Target.x); // 마지막으로 목표 X 순
        }
    }
}
