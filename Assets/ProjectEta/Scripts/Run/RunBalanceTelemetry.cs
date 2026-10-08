using System; // 문자열과 안전 보정
using System.Collections.Generic; // 후보 목록
using ProjectEta.Fusion; // 실제 합성식
using ProjectEta.Pieces; // 획득 기물
using ProjectEta.Battle; // 전투 결과

namespace ProjectEta.Run // 경제 측정 연결 영역
{ // 범위 시작
    public static class RunBalanceTelemetry // 실제 확정 지점에서만 기록
    { // 범위 시작
        public const int MaximumEntries = 2048; // 런당 상세 기록 상한
        private static RunBalanceEntry Create(RunState run, string kind, string source, int phase = 0, int stage = 0) // 발생 시점 기록 생성
        { // 범위 시작
            return new RunBalanceEntry // 공통 측정 항목
            { // 범위 시작
                kind = kind, // 기록 종류
                source = source ?? string.Empty, // 발생 경로
                phase = phase > 0 ? phase : RunPhaseProgressService.GetCurrentPhase(run), // 완료 전 페이즈 우선
                stage = stage > 0 ? stage : run.CurrentRound, // 완료 전 깊이 우선
                nodeId = run.RouteMap.CurrentNodeId ?? string.Empty, // 노드 기준 위치
                successful = true // 확정 기록 기본 상태
            }; // 범위 종료
        } // 범위 종료
        private static void Append(RunState run, RunBalanceEntry entry) // 상세 기록 저장
        { // 범위 시작
            var data = run.BalanceData; // 런에 소속된 기록
            data.Normalize(); // 누락 목록 보정
            data.profileId = data.profileId ?? RunBalanceProfile.Current.ProfileId; // 최초 사용 설정 기록
            data.totals = RunBalanceReport.GetTotals(data); // 이전 측정 저장과 실제 누적 총계 연결
            data.totalsInitialized = true; // 빈 직렬화 객체와 실제 총계 구분
            data.totals.Accumulate(entry); // 상세 상한 이전에 런 전체 총계 갱신
            entry.sequence = data.nextSequence++; // 단조 증가 기록 순서
            if (data.entries.Count >= MaximumEntries) // 상세 기록 상한 확인
            { // 범위 시작
                data.droppedEntries++; // 기록 생략 수 증가
                return; // 게임 진행 유지
            } // 범위 종료
            data.entries.Add(entry); // 실제 기록 추가
        } // 범위 종료
        public static void InitializeGold(RunState run, int gold) // 시작 또는 복원 시 기준 재화
        { // 범위 시작
            if (run != null && run.BalanceData.initialGold < 0) // 최초 기준만 등록
            { // 범위 시작
                run.BalanceData.initialGold = Math.Max(0, gold); // 실제 측정 시작 Gold
                run.BalanceData.profileId = RunBalanceProfile.Current.ProfileId; // 사용 설정 기록
            } // 범위 종료
        } // 범위 종료
        public static void RecordGold(RunState run, int before, int after, int amount, bool success, string reason) // 실제 재화 변경과 실패 시도 기록
        { // 범위 시작
            if (run == null) // 독립 경제 테스트 호환
            { // 범위 시작
                return; // 런 없는 상태 기록 제외
            } // 범위 종료
            var entry = Create(run, "Gold", reason); // 경제 기록 생성
            entry.beforeGold = before; // 변경 전 재화
            entry.afterGold = after; // 실제 변경 후 재화
            entry.amount = amount; // 시도한 변동량
            entry.successful = success; // 실제 적용 결과
            Append(run, entry); // 상세 기록 추가
        } // 범위 종료
        public static void RecordOffers(RunState run, string source, int seed, IReadOnlyList<PieceDefinition> cards, int phase = 0, int stage = 0) // 후보 제시 기록
        { // 범위 시작
            RecordOffersAt(run, source, seed, cards, phase, stage, run?.RouteMap.CurrentNodeId); // 현재 노드 후보 기록
        } // 범위 종료
        public static void RecordOffersAt(RunState run, string source, int seed, IReadOnlyList<PieceDefinition> cards, int phase, int stage, string nodeId) // 완료 노드 후보 기록
        { // 범위 시작
            if (run == null || cards == null) // 필수 후보 상태 확인
            { // 범위 시작
                return; // 준비 전 기록 제외
            } // 범위 종료
            var entry = Create(run, "Offer", source, phase, stage); // 후보 발생 시점 기록
            entry.nodeId = nodeId ?? string.Empty; // 전환 전 노드 보존
            entry.seed = seed; // 재현 Seed 기록
            foreach (var previous in run.BalanceData.entries) // 이어하기와 재표시 이력 검사
            { // 범위 시작
                if (previous != null && previous.kind == "Offer" && previous.source == entry.source && previous.phase == entry.phase && previous.stage == entry.stage && previous.nodeId == entry.nodeId && previous.seed == seed) // 같은 후보 화면 확인
                { // 범위 시작
                    return; // 중복 제시 기록 제외
                } // 범위 종료
            } // 범위 종료
            foreach (var card in cards) // 제시된 후보 순회
            { // 범위 시작
                if (card != null) // 유효 후보 확인
                { // 범위 시작
                    entry.candidateIds.Add(card.PieceId); // 후보 ID 기록
                } // 범위 종료
            } // 범위 종료
            Append(run, entry); // 후보 묶음 기록
        } // 범위 종료
        public static void RecordCard(RunState run, PieceDefinition card, string source, int phase = 0, int stage = 0) // 실제 카드 획득 기록
        { // 범위 시작
            RecordCardAt(run, card, source, phase, stage, run?.RouteMap.CurrentNodeId); // 현재 노드 획득 기록
        } // 범위 종료
        public static void RecordCardAt(RunState run, PieceDefinition card, string source, int phase, int stage, string nodeId) // 완료 노드 획득 기록
        { // 범위 시작
            if (run == null || card == null) // 필수 획득 상태 확인
            { // 범위 시작
                return; // 빈 획득 제외
            } // 범위 종료
            var entry = Create(run, "Acquired", source, phase, stage); // 획득 시점 기록
            entry.nodeId = nodeId ?? string.Empty; // 전환 전 노드 보존
            entry.pieceId = card.PieceId; // 실제 획득 재료
            entry.grade = (int)card.Grade; // 실제 획득 등급
            Append(run, entry); // 카드 획득 기록
        } // 범위 종료
        public static void RecordFusion(RunState run, FusionRecipe recipe, int turn) // 실제 합성 완료 기록
        { // 범위 시작
            if (run == null || recipe?.Result == null) // 유효 합성 결과 확인
            { // 범위 시작
                return; // 미완료 합성 제외
            } // 범위 종료
            var entry = Create(run, "Fusion", "Fusion"); // 합성 시점 기록
            entry.turn = turn; // 실제 배치 턴
            entry.recipeId = recipe.RecipeId; // 실행한 합성식
            entry.pieceId = recipe.Result.PieceId; // 결과 기물
            entry.grade = (int)recipe.Result.Grade; // 도달 등급
            entry.materialIds.Add(recipe.MaterialA.PieceId); // 소모한 첫 재료
            entry.materialIds.Add(recipe.MaterialB.PieceId); // 소모한 둘째 재료
            if (entry.grade >= 2 && entry.grade <= 5 && !run.BalanceData.firstFusions.Exists(x => x != null && x.grade == entry.grade)) // 최초 상위 등급 도달 확인
            { // 범위 시작
                run.BalanceData.firstFusions.Add(entry); // 상세 기록 상한과 별도로 최초 도달 보존
            } // 범위 종료
            Append(run, entry); // 합성 완료 기록
        } // 범위 종료
        public static void RecordEncounter(RunState run, EnemyEncounterResult encounter) // 실제 배치 완료 편성 기록
        { // 범위 시작
            if (run == null || encounter == null || string.IsNullOrWhiteSpace(encounter.EncounterId)) // 필수 편성 상태 확인
            { // 범위 시작
                return; // 빈 편성 기록 제외
            } // 범위 종료
            run.BalanceData.Normalize(); // 구버전 목록 보정
            if (run.BalanceData.encounterClaims.Contains(encounter.EncounterId)) // 같은 편성 기록 여부 확인
            { // 범위 시작
                return; // 저장 복원과 재구성 중복 제외
            } // 범위 종료
            var entry = Create(run, "Encounter", encounter.ProfileId, encounter.Phase, encounter.Stage); // 전투 시작 기록 생성
            entry.nodeId = encounter.NodeId; // 편성 노드 보존
            entry.seed = encounter.Seed; // 재현 Seed 저장
            entry.encounterId = encounter.EncounterId; // 편성 고유 ID 저장
            entry.enemyCount = encounter.Spawns.Count; // 실제 생성 예정 적 수 저장
            entry.threatScore = encounter.ThreatScore; // 시작 위협도 저장
            entry.kingHp = run.KingHp; // 전투 시작 왕 체력 저장
            entry.startingKingHp = run.KingHp; // 연결 결과용 시작 왕 체력 저장
            entry.profileId = encounter.ProfileId; // 편성 원형 전용 열 저장
            entry.stageType = (int)encounter.StageType; // 전투 종류 전용 열 저장
            foreach (EnemyEncounterSpawn spawn in encounter.Spawns) // 실제 편성 순서 순회
            { // 범위 시작
                if (spawn?.Piece != null) // 유효 기물 확인
                { // 범위 시작
                    entry.candidateIds.Add(spawn.Piece.PieceId); // 적 기물 ID 순서 저장
                } // 범위 종료
            } // 범위 종료
            run.BalanceData.activeEncounter = new RunBattleEncounterSnapshot // 상세 상한과 독립된 시작 편성 저장
            { // 범위 시작
                encounterId = encounter.EncounterId, // 편성 고유 ID 저장
                profileId = encounter.ProfileId, // 편성 원형 ID 저장
                stageType = (int)encounter.StageType, // 전투 종류 저장
                phase = encounter.Phase, // 시작 페이즈 저장
                stage = encounter.Stage, // 시작 스테이지 저장
                nodeId = encounter.NodeId, // 시작 노드 저장
                enemyCount = encounter.Spawns.Count, // 시작 적 수 저장
                threatScore = encounter.ThreatScore, // 시작 위협도 저장
                startingKingHp = run.KingHp // 시작 왕 체력 저장
            }; // 범위 종료
            run.BalanceData.SetLastEncounterProfile(encounter.StageType, encounter.ProfileId); // 종류별 최근 원형을 상세 상한과 별도 저장
            run.BalanceData.encounterClaims.Add(encounter.EncounterId); // 상세 상한과 별도 중복 방지
            Append(run, entry); // 편성 상세 기록 추가
        } // 범위 종료
        public static void RecordBattleResult(RunState run, BattleOutcome outcome, int turn) // 실제 전투 종료 난이도 기록
        { // 범위 시작
            if (run == null || outcome == BattleOutcome.None) // 유효 전투 결과 확인
            { // 범위 시작
                return; // 미결정 결과 제외
            } // 범위 종료
            int phase = RunPhaseProgressService.GetCurrentPhase(run); // 완료 전 페이즈 조회
            int stage = run.CurrentRound; // 완료 전 깊이 조회
            string nodeId = run.RouteMap.CurrentNodeId ?? string.Empty; // 완료 전 노드 조회
            string claim = phase + ":" + stage + ":" + nodeId; // 전투 결과 중복 키
            run.BalanceData.Normalize(); // 구버전 목록 보정
            if (run.BalanceData.battleResultClaims.Contains(claim)) // 기존 결과 기록 확인
            { // 범위 시작
                return; // 같은 전투 결과 재기록 차단
            } // 범위 종료
            var entry = Create(run, "BattleResult", outcome.ToString(), phase, stage); // 전투 결과 기록 생성
            entry.nodeId = nodeId; // 완료 노드 보존
            entry.turn = Math.Max(0, turn); // 종료 턴 음수 보정
            entry.kingHp = Math.Max(0, run.KingHp); // 종료 왕 체력 보정
            entry.outcome = (int)outcome; // 승패 열거값 저장
            RunBattleEncounterSnapshot snapshot = run.BalanceData.activeEncounter; // 상세 상한과 독립된 시작 편성 조회
            if (snapshot != null && snapshot.Matches(phase, stage, nodeId)) // 현재 전투 시작 편성 확인
            { // 범위 시작
                entry.encounterId = snapshot.encounterId; // 시작 편성 ID 연결
                entry.profileId = snapshot.profileId; // 시작 원형 ID 연결
                entry.stageType = snapshot.stageType; // 시작 전투 종류 연결
                entry.enemyCount = snapshot.enemyCount; // 시작 적 수 연결
                entry.threatScore = snapshot.threatScore; // 시작 위협도 연결
                entry.startingKingHp = snapshot.startingKingHp; // 시작 왕 체력 연결
                snapshot.isCompleted = true; // 최근 편성 완료 상태 저장
                snapshot.outcome = entry.outcome; // 최근 승패 저장
                snapshot.turn = entry.turn; // 최근 종료 턴 저장
                snapshot.endingKingHp = entry.kingHp; // 최근 종료 왕 체력 저장
            } // 범위 종료
            else // 구버전 상세 기록 대체 조회
            { // 범위 시작
                for (int index = run.BalanceData.entries.Count - 1; index >= 0; index--) // 최근 편성 기록 역순 검색
                { // 범위 시작
                    RunBalanceEntry previous = run.BalanceData.entries[index]; // 현재 이전 기록 조회
                    if (previous == null || previous.kind != "Encounter") continue; // 편성 이외 기록 제외
                    if (previous.phase != phase || previous.stage != stage || previous.nodeId != nodeId) continue; // 다른 전투 위치 제외
                    entry.encounterId = previous.encounterId; // 시작 편성 ID 연결
                    entry.profileId = string.IsNullOrWhiteSpace(previous.profileId) ? previous.source : previous.profileId; // 구버전 원형 ID 보정
                    entry.stageType = previous.stageType; // 시작 전투 종류 연결
                    entry.enemyCount = previous.enemyCount; // 시작 적 수 연결
                    entry.threatScore = previous.threatScore; // 시작 위협도 연결
                    entry.startingKingHp = previous.startingKingHp > 0 ? previous.startingKingHp : previous.kingHp; // 구버전 시작 왕 체력 보정
                    break; // 최근 일치 편성 사용
                } // 범위 종료
            } // 범위 종료
            run.BalanceData.latestBattleResult = new RunBattleResultSnapshot // 상세 상한과 독립된 최신 전투 결과 저장
            { // 범위 시작
                phase = phase, // 완료 페이즈 저장
                stage = stage, // 완료 스테이지 저장
                nodeId = nodeId, // 완료 노드 저장
                encounterId = entry.encounterId, // 연결 편성 ID 저장
                profileId = entry.profileId, // 연결 원형 ID 저장
                stageType = entry.stageType, // 전투 종류 저장
                enemyCount = entry.enemyCount, // 시작 적 수 저장
                threatScore = entry.threatScore, // 시작 위협도 저장
                startingKingHp = entry.startingKingHp, // 시작 왕 체력 저장
                endingKingHp = entry.kingHp, // 종료 왕 체력 저장
                turn = entry.turn, // 종료 턴 저장
                outcome = entry.outcome // 승패 저장
            }; // 범위 종료
            run.BalanceData.battleResultClaims.Add(claim); // 상세 상한과 별도 결과 완료 보존
            AccumulateDifficulty(run.BalanceData, entry); // 원형별 실제 난이도 지표 누적
            Append(run, entry); // 전투 결과 상세 기록 추가
        } // 범위 종료
        private static void AccumulateDifficulty(RunBalanceData data, RunBalanceEntry entry) // 완료 전투 원형별 집계
        { // 범위 시작
            if (data == null || entry == null || string.IsNullOrWhiteSpace(entry.profileId)) // 연결 편성 누락 확인
            { // 범위 시작
                return; // 원형 없는 구버전 결과 집계 제외
            } // 범위 종료
            data.Normalize(); // 집계 목록 보정
            RunBattleDifficultySummary summary = data.difficultySummaries.Find(item => item != null && item.profileId == entry.profileId && item.stageType == entry.stageType); // 같은 원형 집계 조회
            if (summary == null) // 첫 완료 전투 확인
            { // 범위 시작
                summary = new RunBattleDifficultySummary // 새 원형 집계 생성
                { // 범위 시작
                    profileId = entry.profileId, // 편성 원형 ID 저장
                    stageType = entry.stageType // 전투 종류 저장
                }; // 범위 종료
                data.difficultySummaries.Add(summary); // 런 집계 목록 추가
            } // 범위 종료
            summary.Accumulate(entry.outcome, entry.turn, entry.startingKingHp, entry.kingHp, entry.threatScore); // 완료 결과 누적
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
