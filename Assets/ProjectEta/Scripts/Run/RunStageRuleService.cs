using System; // ID 비교
using System.Collections.Generic; // 적 배치 복사
using ProjectEta.Round; // 라운드 규칙
using UnityEngine; // 페이즈 보정

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
    } // 타입 종료
} // 영역 종료
