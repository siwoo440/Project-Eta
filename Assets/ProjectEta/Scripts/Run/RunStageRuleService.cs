using System; // ID 비교
using System.Collections.Generic; // 적 배치 복사
using ProjectEta.Round; // 라운드 규칙
using UnityEngine; // 페이즈 보정
using ProjectEta.Boss; // 페이즈 보스 선택

namespace ProjectEta.Run // 런 규칙 영역
{ // 영역 시작
    public static class RunStageRuleService // 안내·전투·보상 공통 규칙
    { // 타입 시작
        public static StageRuleSnapshot GetOrCreate(RunState run, StageDefinition definition, int phase, RunBalanceProfile balanceProfile = null) // 런에 고정된 규칙 조회
        { // 메서드 시작
            if (definition == null || !definition.RequiresBattle || definition.RoundDefinition == null) // 전투 규칙 누락 확인
            { // 조건 시작
                return null; // 비전투 규칙 제외
            } // 조건 종료
            int safePhase = Mathf.Clamp(phase, 1, 5); // 적용 페이즈 범위 보정
            var storedRules = run?.BalanceData?.stageRuleSnapshots; // 저장된 규칙 목록 조회
            if (storedRules != null) // 저장 목록 존재 확인
            { // 조건 시작
                foreach (StageRuleSnapshot stored in storedRules) // 기존 규칙 순회
                { // 반복 시작
                    if (stored != null && stored.IsValid && stored.phase == safePhase && stored.stageType == (int)definition.StageType && string.Equals(stored.stageDefinitionId, definition.StageId, StringComparison.Ordinal)) // 같은 페이즈·정의 확인
                    { // 조건 시작
                        return stored; // 현재 런의 기존 규칙 반환
                    } // 조건 종료
                } // 반복 종료
            } // 조건 종료
            RoundDefinition round = definition.RoundDefinition; // 현재 에셋 규칙 조회
            RunBalanceProfile balance = balanceProfile != null ? balanceProfile : RunBalanceProfile.Current; // 실제 보상 설정 조회
            var snapshot = new StageRuleSnapshot // 새 저장 규칙 생성
            { // 객체 시작
                phase = safePhase, // 페이즈 저장
                stageDefinitionId = definition.StageId, // 정의 ID 저장
                stageType = (int)definition.StageType, // 전투 종류 저장
                displayName = round.DisplayName, // 라운드 이름 저장
                turnLimit = round.TurnLimit, // 제한 턴 저장
                victoryGold = balance.GetBattleGold(definition.StageType, safePhase), // 승리 보상 저장
                isBossRound = round.IsBossRound, // 보스 여부 저장
                bossResourceName = round.BossResourceName, // 보스 이름 저장
                bossAnchor = round.BossAnchor, // 보스 좌표 저장
                initialEnemies = CopySpawns(round.InitialEnemies), // 시작 적 독립 복사
                reinforcements = CopySpawns(round.Reinforcements) // 증원 독립 복사
            }; // 객체 종료
            if (round.IsBossRound && (definition.StageType == StageType.MidBoss || definition.StageType == StageType.FinalBoss)) // 새 보스 규칙만 선택 연결
            { // 조건 시작
                string[] parts = definition.StageId.Split('_'); // 표준 스테이지 ID의 깊이 조회
                int depth = parts.Length > 1 && int.TryParse(parts[1], out int parsedDepth) ? parsedDepth : run?.CurrentRound ?? 0; // 사용자 정의 ID는 현재 깊이 사용
                snapshot.bossSelection = BossSelectionService.Select(run, safePhase, depth); // 선택 당시 데이터 저장
                if (snapshot.bossSelection != null) // 실제 보스 선택 존재 확인
                { // 조건 시작
                    snapshot.displayName = snapshot.bossSelection.displayName; // 지도와 전투 공통 이름
                    if (snapshot.bossSelection.variant == BossVariant.Enhanced) AddEnhancedReinforcement(snapshot); // 수치 외 강화형 증원 차이
                } // 조건 종료
            } // 조건 종료
            if (run?.BalanceData != null) // 저장 가능한 런 확인
            { // 조건 시작
                run.BalanceData.stageRuleSnapshots = storedRules ?? new List<StageRuleSnapshot>(); // 구버전 저장 목록 보정
                run.BalanceData.stageRuleSnapshots.RemoveAll(item => item == null || !item.IsValid); // 손상된 규칙 제외
                run.BalanceData.stageRuleSnapshots.Add(snapshot); // 새 규칙 보존
            } // 조건 종료
            return snapshot; // 적용 규칙 반환
        } // 메서드 종료
        private static List<EnemySpawnDefinition> CopySpawns(IReadOnlyList<EnemySpawnDefinition> source) // 배치 데이터 독립 복사
        { // 메서드 시작
            var result = new List<EnemySpawnDefinition>(); // 복사 목록 생성
            if (source == null) // 원본 누락 확인
            { // 조건 시작
                return result; // 빈 목록 반환
            } // 조건 종료
            foreach (EnemySpawnDefinition spawn in source) // 배치 목록 순회
            { // 반복 시작
                if (spawn != null) // 유효 배치 확인
                { // 조건 시작
                    result.Add(new EnemySpawnDefinition(spawn.PieceId, spawn.Position, spawn.SpawnTurn)); // 배치 값 복사
                } // 조건 종료
            } // 반복 종료
            return result; // 독립 목록 반환
        } // 메서드 종료
        private static void AddEnhancedReinforcement(StageRuleSnapshot rules) // 강화형 추가 병사 배치
        { // 메서드 시작
            var occupied = new HashSet<Vector2Int>(); // 초기·예약 증원 칸 보호
            foreach (EnemySpawnDefinition spawn in rules.initialEnemies) occupied.Add(spawn.Position); // 시작 병사 칸 예약
            foreach (EnemySpawnDefinition spawn in rules.reinforcements) occupied.Add(spawn.Position); // 기존 증원 칸 예약
            Vector2Int size = rules.bossSelection.size; // 실제 보스 점유 크기
            for (int x = 0; x < size.x; x++) // 보스 가로 점유 순회
            { // 반복 시작
                for (int y = 0; y < size.y; y++) occupied.Add(rules.bossAnchor + new Vector2Int(x, y)); // 보스 점유 칸 보호
            } // 반복 종료
            for (int y = 7; y < 10; y++) // 적 진영 빈 칸 순회
            { // 반복 시작
                for (int x = 9; x >= 0; x--) // 오른쪽부터 안정적인 위치 선택
                { // 반복 시작
                    var cell = new Vector2Int(x, y); // 증원 후보 칸
                    if (occupied.Contains(cell)) continue; // 충돌 예약 제외
                    rules.reinforcements.Add(new EnemySpawnDefinition("pawn", cell, 4)); // 강화형 4턴 추가 병사
                    return; // 한 번만 추가
                } // 반복 종료
            } // 반복 종료
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
