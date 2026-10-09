using System.Collections.Generic; // 증원 턴 목록
using ProjectEta.Round; // 증원 배치

namespace ProjectEta.Run // 지도 안내 영역
{ // 영역 시작
    public static class StagePreviewFormatter // 실제 규칙 기반 지도 안내
    { // 타입 시작
        public static string Build(RunState run, StageNode node) // 노드 규칙·보상 안내 생성
        { // 메서드 시작
            if (node == null) // 빈 노드 확인
            { // 조건 시작
                return string.Empty; // 빈 안내 반환
            } // 조건 종료
            StageDefinition definition = StageDefinitionCatalog.Resolve(node.StageDefinitionId, node.Depth); // 스테이지 정의 조회
            string status = GetStatus(run, node); // 현재 경로 상태 조회
            int phase = RunPhaseProgressService.GetCurrentPhase(run); // 현재 페이즈 조회
            StageRuleSnapshot rules = RunStageRuleService.GetOrCreate(run, definition, phase); // 안내와 전투의 공통 규칙 조회
            if (rules != null) // 전투 규칙 존재 확인
            { // 조건 시작
                string reward = definition.StageType == StageType.FinalBoss ? "최종 승리 · 런 종료" : "1성 기물 · 카드 1장 선택"; // 실제 완료 보상 안내
                return $"제한 {rules.turnLimit}턴\n증원 {GetReinforcementTurns(rules.reinforcements)}\n승리 보상 {rules.victoryGold} Gold\n{reward}\n상태: {status}"; // 전투 안내 반환
            } // 조건 종료
            string detail = GetActivityDescription(definition != null ? definition.StageType : StageType.Battle); // 비전투 활동 안내
            return $"{detail}\n상태: {status}"; // 비전투 안내 반환
        } // 메서드 종료
        public static string GetStatus(RunState run, StageNode node) // 지도 접근 상태 판정
        { // 메서드 시작
            if (node != null && node.Visited) // 방문 완료 확인
            { // 조건 시작
                return "방문 완료"; // 완료 상태 반환
            } // 조건 종료
            if (run?.RouteMap != null && node != null) // 현재 지도 존재 확인
            { // 조건 시작
                foreach (StageNode candidate in run.RouteMap.GetSelectableNodes()) // 실제 선택 후보 순회
                { // 반복 시작
                    if (candidate != null && candidate.NodeId == node.NodeId) // 선택 가능 노드 확인
                    { // 조건 시작
                        return "선택 가능"; // 접근 가능 반환
                    } // 조건 종료
                } // 반복 종료
            } // 조건 종료
            return "현재 경로에서 접근 불가"; // 경로 외 상태 반환
        } // 메서드 종료
        public static string GetReinforcementTurns(IReadOnlyList<EnemySpawnDefinition> spawns) // 실제 증원 턴 요약
        { // 메서드 시작
            var turns = new SortedSet<int>(); // 중복 없는 턴 집합
            if (spawns != null) // 증원 목록 존재 확인
            { // 조건 시작
                foreach (EnemySpawnDefinition spawn in spawns) // 증원 배치 순회
                { // 반복 시작
                    if (spawn != null) // 유효 배치 확인
                    { // 조건 시작
                        turns.Add(spawn.SpawnTurn); // 증원 턴 추가
                    } // 조건 종료
                } // 반복 종료
            } // 조건 종료
            return turns.Count == 0 ? "없음" : string.Join("·", turns) + "턴"; // 정렬된 증원 안내 반환
        } // 메서드 종료
        private static string GetActivityDescription(StageType type) // 비전투 노드 활동 안내
        { // 메서드 시작
            switch (type) // 활동 종류 분기
            { // 분기 시작
                case StageType.Reward: return "1성 기물 · 카드 1장 선택"; // 카드 보상 안내
                case StageType.Shop: return "1성 기물 구매 · 강화 · 카드 복구\n미해금 기물은 후보에서 제외"; // 상점 활동 안내
                case StageType.Event: return "선택에 따라 재화·덱 변경"; // 이벤트 활동 안내
                default: return "스테이지 규칙 확인 필요"; // 누락 데이터 안내
            } // 분기 종료
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
