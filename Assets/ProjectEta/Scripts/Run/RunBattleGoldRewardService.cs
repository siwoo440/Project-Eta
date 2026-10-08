using ProjectEta.Battle; // 승리 보상 경로
namespace ProjectEta.Run // 전투 재화 보상 영역
{ // 범위 시작
    public static class RunBattleGoldRewardService // 승리 후 재화 한 번 지급
    { // 범위 시작
        public static bool TryGrant(RunState run) // 실제 전투 완료 경로
        { // 범위 시작
            if (run == null || run.CurrentFlowPhase != RunFlowPhase.Battle || run.LastBattleOutcome != BattleOutcome.Victory) // 전투 진행 상태 확인
            { // 범위 시작
                return false; // 잘못된 지급 차단
            } // 범위 종료
            int phase = RunPhaseProgressService.GetCurrentPhase(run); // 완료 전 페이즈
            int stage = run.CurrentRound; // 완료 전 깊이
            string key = phase + ":" + stage; // 지도 정규화와 분기 재선택에도 동일한 전투 보상 키
            if (run.BalanceData.goldRewardClaims.Contains(key)) // 저장된 지급 이력 확인
            { // 범위 시작
                return false; // 동일 전투 재지급 차단
            } // 범위 종료
            StageType type = StageType.Battle; // 첫 전투 호환 기본값
            var current = run.RouteMap.CurrentNode; // 현재 노드 정의
            if (current != null) // 지도 노드 존재 확인
            { // 범위 시작
                if (!StageDefinitionCatalog.TryParseStageType(current.StageDefinitionId, out type)) // 잘못된 정의 확인
                { // 범위 시작
                    return false; // 손상된 노드 지급 차단
                } // 범위 종료
            } // 범위 종료
            int amount = RunBalanceProfile.Current.GetBattleGold(type, phase); // 설정된 임시 승리 보상
            if (amount <= 0) // 비전투 노드 확인
            { // 범위 시작
                return false; // 비전투 보상 제외
            } // 범위 종료
            var economy = RunEconomyService.GetOrCreate(run); // 실제 런 경제 연결
            run.BalanceData.goldRewardClaims.Add(key); // 지급 완료 이력 먼저 등록
            economy.Add(amount, "BattleReward:" + type); // Gold 지급과 기록
            return true; // 보상 지급 완료
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
