using System; // StringComparison을 사용하기 위한 네임스페이스
using System.Collections.Generic; // List<T>를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardState를 사용하기 위한 네임스페이스

namespace ProjectEta.AI // AI 점수 디버그 관련 타입을 기존 AI 네임스페이스에 함께 배치
{
    public sealed class AIDebugScoreSnapshotBuilder // 33일차 공통 점수와 34일차 역할 보너스를 디버그용으로 분해해 수집하는 클래스
    {
        private readonly EnemyAIPlanner _basePlanner = new EnemyAIPlanner(); // 33일차 공통 점수 후보를 생성하는 플래너
        private readonly EnemyAIRolePlanner _rolePlanner = new EnemyAIRolePlanner(); // 34일차 역할 점수를 반영해 실제 선택 행동을 결정하는 플래너

        public AIDebugScoreSnapshot Build(BoardState board) // 현재 보드 상태를 읽어 디버그 창에 표시할 전체 점수 로그를 만드는 메서드
        {
            if (board == null) return AIDebugScoreSnapshot.Empty(); // 보드가 없으면 빈 로그 반환

            var baseCandidates = _basePlanner.BuildCandidates(board); // 33일차 공통 점수까지 계산된 합법 행동 후보 생성
            _rolePlanner.TryChooseAction(board, out var selectedAction); // 34일차 역할 점수까지 반영한 실제 선택 행동 계산

            var entries = new List<AIDebugScoreEntry>(baseCandidates.Count); // 후보 수만큼 로그 항목 목록 준비

            for (int i = 0; i < baseCandidates.Count; i++) // 모든 공통 후보를 순회
            {
                var candidate = baseCandidates[i]; // 현재 공통 후보 참조
                int roleBonus = EnemyAIRoleScoreEvaluator.EvaluateMoveBonus(candidate, board); // 동일 후보에 34일차 역할 보너스 계산
                int finalScore = candidate.Score + roleBonus; // 실제 역할 플래너와 같은 방식으로 최종 점수 계산
                var role = EnemyAIRoleClassifier.GetBasicRole(candidate.Actor?.Definition); // 행동 주체의 기본 AI 역할 분류
                bool isSelected = IsSameAction(candidate, selectedAction); // 실제 선택 행동과 같은 후보인지 확인

                entries.Add(new AIDebugScoreEntry( // 한 줄의 점수 로그 생성
                    candidate.Actor, // 행동 주체
                    candidate.Origin, // 시작 좌표
                    candidate.Target, // 목표 좌표
                    candidate.ActionType, // 이동 또는 공격
                    role, // 기본 AI 역할
                    candidate.Score, // 33일차 공통 점수
                    roleBonus, // 34일차 역할 추가 점수
                    finalScore, // 최종 점수
                    isSelected)); // 실제 선택 여부
            }

            entries.Sort(CompareEntries); // 높은 점수와 실제 선택 행동을 위쪽에 보여주도록 정렬

            AIDebugScoreEntry selectedEntry = null; // 실제 선택 로그를 찾기 위한 변수
            for (int i = 0; i < entries.Count; i++) // 정렬된 로그 목록 순회
            {
                if (!entries[i].IsSelected) continue; // 선택된 행동이 아니면 건너뜀
                selectedEntry = entries[i]; // 실제 선택 행동 로그 저장
                break; // 하나만 존재하므로 탐색 종료
            }

            return new AIDebugScoreSnapshot(entries, selectedEntry); // 완성된 디버그 스냅샷 반환
        }

        private static bool IsSameAction(AIActionCandidate a, AIActionCandidate b) // 두 행동 후보가 동일한 실제 행동인지 비교하는 메서드
        {
            if (a == null || b == null) return false; // 둘 중 하나라도 없으면 동일하지 않음
            if (a.Actor != b.Actor) return false; // 행동 주체가 다르면 다른 행동
            if (a.ActionType != b.ActionType) return false; // 이동/공격 종류가 다르면 다른 행동
            if (a.Origin != b.Origin) return false; // 시작 좌표가 다르면 다른 행동
            return a.Target == b.Target; // 최종적으로 목표 좌표가 같으면 같은 행동으로 판단
        }

        private static int CompareEntries(AIDebugScoreEntry a, AIDebugScoreEntry b) // 디버그 로그 표시 순서를 결정하는 비교 함수
        {
            if (a.IsSelected != b.IsSelected) return a.IsSelected ? -1 : 1; // 실제 선택 행동을 항상 가장 위에 배치
            if (a.FinalScore != b.FinalScore) return b.FinalScore.CompareTo(a.FinalScore); // 그 다음 최종 점수가 높은 순서

            string aId = a.Actor?.Definition?.PieceId ?? string.Empty; // 첫 후보 PieceId 읽기
            string bId = b.Actor?.Definition?.PieceId ?? string.Empty; // 둘째 후보 PieceId 읽기
            int idComparison = string.Compare(aId, bId, StringComparison.Ordinal); // 문화권 영향을 받지 않는 문자열 비교
            if (idComparison != 0) return idComparison; // PieceId가 다르면 사전순으로 정렬

            if (a.Origin.y != b.Origin.y) return a.Origin.y.CompareTo(b.Origin.y); // 같은 기물이면 원점 Y 순서
            if (a.Origin.x != b.Origin.x) return a.Origin.x.CompareTo(b.Origin.x); // 다음으로 원점 X 순서
            if (a.Target.y != b.Target.y) return a.Target.y.CompareTo(b.Target.y); // 다음으로 목표 Y 순서
            return a.Target.x.CompareTo(b.Target.x); // 마지막으로 목표 X 순서
        }
    }
}
