using System; // 저장 키와 비교
using System.Collections.Generic; // 이벤트·카드 목록
using UnityEngine; // 독립 저장값 복사
using ProjectEta.Pieces; // 실제 카드 정의
using ProjectEta.Fusion; // 합성 연결 후보
using ProjectEta.Meta; // 영구 후보 열람

namespace ProjectEta.Run // 이벤트 실행
{ // 영역 시작
    public static class RunEventService // 선택·손익·재지급 방지
    { // 타입 시작
        public static string CurrentKey(RunState run) // 현재 이벤트 노드 키
        { // 메서드 시작
            return run == null ? string.Empty : RunPhaseProgressService.GetCurrentPhase(run) + ":" + run.CurrentRound + ":" + run.RouteMap.CurrentNodeId; // 페이즈별 노드 충돌 차단
        } // 메서드 종료
        public static RunEventSession GetOrCreate(RunState run) // 최초 진입 선택 고정
        { // 메서드 시작
            if (run == null || run.CurrentFlowPhase != RunFlowPhase.Event) return null; // 실제 이벤트 흐름만 허용
            string key = CurrentKey(run); // 현재 저장 키
            RunBalanceData data = run.BalanceData; // 해당 런 기록
            data.Normalize(); // 구버전 목록 복구
            if (data.activeEvent102 != null && data.activeEvent102.key == key && data.activeEvent102.definition != null && data.activeEvent102.definition.IsValid) return data.activeEvent102; // 이미 선택한 스냅샷 유지
            RunEventRecord previous = data.eventHistory102.Find(record => record.key == key); // 이미 처리한 노드 확인
            int phase = RunPhaseProgressService.GetCurrentPhase(run); // 현재 페이즈
            int seed = StageEventGenerator.CreateSeed(run.RouteMap.MapSeed, phase, run.CurrentRound, run.RouteMap.CurrentNodeId); // 기존 안정 Seed 규칙
            var eligible = new List<RunEventDefinition>(); // 아직 완료하지 않은 후보
            int total = 0; // 전체 가중치
            foreach (RunEventDefinition definition in RunEventCatalog.All) // 전체 35종
            { // 반복 시작
                if (definition.phase != 0 && definition.phase != phase) continue; // 전용 페이즈 제한
                if (data.eventHistory102.Exists(record => record.eventId == definition.id)) continue; // 완료 ID 재등장 차단
                eligible.Add(definition); // 후보 등록
                total += definition.weight; // 임시 가중치 합산
            } // 반복 종료
            RunEventDefinition selected = previous != null ? RunEventCatalog.Find(previous.eventId) : null; // 손상된 활성 기록의 완료 결과 복구
            if (selected == null && total > 0) // 신규 선택 필요
            { // 조건 시작
                int roll = (int)(unchecked((uint)seed) % (uint)total); // 음수 Seed도 안전한 선택
                foreach (RunEventDefinition definition in eligible) // 가중치 구간 순회
                { // 반복 시작
                    if (roll < definition.weight) // 해당 가중치 구간
                    { // 조건 시작
                        selected = definition; // 후보 확정
                        break; // 추가 추첨 중단
                    } // 조건 종료
                    roll -= definition.weight; // 다음 구간으로 이동
                } // 반복 종료
            } // 조건 종료
            selected = selected ?? Exhausted(); // 후보 소진 시 무보상 통과
            data.activeEvent102 = new RunEventSession // 현재 정의와 결과 고정
            { // 객체 시작
                key = key, // 현재 노드 키
                seed = seed, // 선택 Seed
                definition = selected.Copy(), // 선택 시점 데이터
                resolved = previous != null, // 기존 완료 여부
                result = previous // 기존 손익 결과
            }; // 객체 종료
            return data.activeEvent102; // 안전 체크포인트 저장 대상
        } // 메서드 종료
        public static string UnavailableReason(RunState run, RunEconomyState economy, RunEventSession session, RunEventChoice choice, int maxHp, IReadOnlyList<PieceDefinition> catalog, RunContentUnlockSnapshot unlocks, IReadOnlyList<FusionRecipe> recipes, PieceDefinition target = null) // 비활성 사유
        { // 메서드 시작
            if (run == null || economy == null || session == null || choice == null || !choice.IsValid) return "이벤트 정보 없음"; // 필수 상태 검사
            if (run.CurrentFlowPhase != RunFlowPhase.Event || session.key != CurrentKey(run) || !ReferenceEquals(session, run.BalanceData.activeEvent102)) return "현재 이벤트가 아님"; // 오래된 화면 콜백 차단
            if (session.resolved || run.BalanceData.eventHistory102.Exists(record => record.key == session.key)) return "이미 선택 완료"; // 중복 지급 차단
            if (session.definition?.choices == null || Array.IndexOf(session.definition.choices, choice) < 0) return "현재 선택지가 아님"; // 다른 이벤트 비용 주입 차단
            if (economy.Currency < choice.goldCost) return "Gold 부족 · 필요 " + choice.goldCost; // 재화 조건
            if (run.KingHp <= choice.kingCost) return "왕 HP 부족 · 생존 필요"; // 비용 사망 방지
            if (choice.kingHeal > 0 && (maxHp <= 0 || run.KingHp >= maxHp)) return "왕 HP 최대"; // 의미 없는 회복 지불 금지
            List<PieceDefinition> targets = RunEventCardRules.Targets(run, choice); // 유효 카드 대상
            if (choice.NeedsTarget && targets.Count == 0) return "선택 가능한 카드 없음"; // 대상 없는 지불 금지
            if (target != null && (!choice.NeedsTarget || !targets.Contains(target))) return "보유한 유효 카드가 아님"; // 오래된·외부 카드 거부
            bool gain = choice.cardOperation == RunEventCardOperation.Gain || choice.cardOperation == RunEventCardOperation.LinkedGain; // 직접 획득 여부
            if (gain && RunEventCardRules.Rewards(run, catalog, unlocks, recipes, choice.cardOperation == RunEventCardOperation.LinkedGain).Count == 0) return "획득 가능한 해금 1성 카드 없음"; // 후보 소진 지불 금지
            if (choice.cardOperation == RunEventCardOperation.Exchange) // 다른 1성 교환 후보
            { // 조건 시작
                if (target != null && RunEventCardRules.Rewards(run, catalog, unlocks, recipes, false, target.PieceId).Count == 0) return "교환 가능한 다른 1성 없음"; // 대상별 교환 불가
                if (target == null && !targets.Exists(card => RunEventCardRules.Rewards(run, catalog, unlocks, recipes, false, card.PieceId).Count > 0)) return "교환 가능한 다른 1성 없음"; // 전체 교환 불가
            } // 조건 종료
            return string.Empty; // 선택 가능
        } // 메서드 종료
        public static bool TryCommit(RunState run, RunEconomyState economy, RunEventSession session, RunEventChoice choice, PieceDefinition target, int maxHp, IReadOnlyList<PieceDefinition> catalog, RunContentUnlockSnapshot unlocks, IReadOnlyList<FusionRecipe> recipes, MetaProgressState meta, out RunEventRecord result) // 최종 대상 확정 후 한 번 적용
        { // 메서드 시작
            result = null; // 실패 결과 기본값
            if (!string.IsNullOrEmpty(UnavailableReason(run, economy, session, choice, maxHp, catalog, unlocks, recipes, target))) return false; // 모든 조건 재확인
            if (choice.NeedsTarget && target == null) return false; // 카드 선택 화면을 건너뛴 지불 금지
            PieceDefinition reward = null; // 지급 카드 준비
            if (choice.cardOperation == RunEventCardOperation.Gain || choice.cardOperation == RunEventCardOperation.LinkedGain || choice.cardOperation == RunEventCardOperation.Exchange) // 카드 보상이 있는 선택
            { // 조건 시작
                List<PieceDefinition> candidates = RunEventCardRules.Rewards(run, catalog, unlocks, recipes, choice.cardOperation == RunEventCardOperation.LinkedGain, choice.cardOperation == RunEventCardOperation.Exchange ? target.PieceId : null); // 최종 확정 후보
                int seed = StageEventGenerator.CreateFollowUpSeed(session.seed, 1, 1, session.key, session.definition.id, choice.id); // 선택별 독립 추첨
                reward = candidates[(int)(unchecked((uint)seed) % (uint)candidates.Count)]; // 조건 충족 무작위 1성 한 장
            } // 조건 종료
            PieceDefinition upgraded = null; // 강화 복제 준비
            if (choice.hpUpgrade > 0 || choice.atkUpgrade > 0) // 개별 능력치 강화
            { // 조건 시작
                upgraded = RuntimeCardUpgradeService.CreateRestoredCard(target, target.BaseHp + choice.hpUpgrade, target.BaseAtk + choice.atkUpgrade, target.DisplayName + " +1"); // 원본 보호 복제
                if (upgraded == null || ReferenceEquals(upgraded, target)) return false; // 실패 시 비용 보존
            } // 조건 종료
            int goldBefore = economy.Currency; // 실제 적용 전 Gold
            int kingBefore = run.KingHp; // 실제 적용 전 왕 HP
            if (choice.NeedsTarget && !run.Deck.RemoveFromOwnedPool(target)) // 확정 대상 제거·교체
            { // 조건 시작
                if (upgraded != null) DestroyClone(upgraded); // 실패 복제 정리
                return false; // 비용 보존
            } // 조건 종료
            if (upgraded != null) run.Deck.AddToOwnedPool(upgraded); // 변경 카드 교체
            session.resolved = true; // 획득 이벤트 콜백의 재진입 지급 차단
            if (choice.goldCost > 0) economy.TrySpend(choice.goldCost, "Event:" + session.definition.id); // 확정 시점 비용 지불
            if (choice.goldGain > 0) economy.Add(choice.goldGain, "Event:" + session.definition.id); // 정수 상한을 포함한 실제 지급
            run.KingHp -= choice.kingCost; // 검증한 생존 HP 비용
            if (choice.kingHeal > 0) run.KingHp = Math.Min(maxHp, run.KingHp + choice.kingHeal); // 회복 효과에만 최대 HP 적용
            if (reward != null) run.Deck.AddAcquiredCard(reward); // 검증한 지급 카드와 기존 획득 알림
            string informationId = choice.information ? session.definition.id : string.Empty; // 선택 단서 후보 열람
            bool fresh = choice.information && meta != null && meta.TryReadEventInformation(informationId); // 중복 알림 제외
            result = new RunEventRecord // 실제 손익 보관
            { // 객체 시작
                key = session.key, // 완료 노드
                eventId = session.definition.id, // 완료 이벤트
                eventTitle = session.definition.title, // 결과 제목
                choiceId = choice.id, // 확정 선택지
                choiceTitle = choice.title, // 확정 선택 제목
                goldBefore = goldBefore, // 선택 전 Gold
                goldAfter = economy.Currency, // 선택 후 실제 Gold
                kingBefore = kingBefore, // 선택 전 왕 HP
                kingAfter = run.KingHp, // 선택 후 실제 HP
                addedCardId = reward?.PieceId, // 지급 카드 ID
                addedCardName = reward?.DisplayName, // 지급 카드 이름
                removedCardId = upgraded == null && choice.NeedsTarget ? target?.PieceId : null, // 순수 제거·교환 대상
                removedCardName = upgraded == null && choice.NeedsTarget ? target?.DisplayName : null, // 결과 표시 대상 이름
                targetName = upgraded != null ? target.DisplayName : null, // 강화 대상 이름
                hpUpgrade = upgraded != null ? upgraded.BaseHp - target.BaseHp : 0, // 실제 HP 증가
                atkUpgrade = upgraded != null ? upgraded.BaseAtk - target.BaseAtk : 0, // 실제 ATK 증가
                informationId = informationId, // 후보 열람 이력
                newInformation = fresh // 첫 열람 알림 여부
            }; // 객체 종료
            session.result = result; // 이어하기 결과 표시
            session.resolved = true; // 같은 프레임 재지급 차단
            run.BalanceData.eventHistory102.Add(result); // 별도 상세 로그 상한과 독립된 완료 이력
            if (reward != null) RunBalanceTelemetry.RecordCard(run, reward, "Event:" + session.definition.id); // 기존 밸런스 기록 연결
            return true; // 선택 완료
        } // 메서드 종료
        private static RunEventDefinition Exhausted() // 전체 후보 소진 시 안전 통과
        { // 메서드 시작
            return new RunEventDefinition // 중복 보상 없는 대체 정의
            { // 객체 시작
                id = "EXHAUSTED", // 일반 이벤트와 분리
                title = "조용한 길", // 안전 통과 제목
                description = "주변을 살핀 뒤 다음 경로로 이동합니다.", // 무보상 진행 안내
                phase = 0, // 공통 대체
                weight = 1, // 유효 정의 기준
                choices = new[] // 변화 없는 세 선택
                { // 배열 시작
                    EmptyChoice("A", "지나간다"), // 첫 통과 선택
                    EmptyChoice("B", "길을 살핀다"), // 두 번째 통과 선택
                    EmptyChoice("C", "계속 이동한다") // 세 번째 통과 선택
                } // 배열 종료
            }; // 객체 종료
        } // 메서드 종료
        private static RunEventChoice EmptyChoice(string id, string title) // 자원 변화 없는 선택
        { // 메서드 시작
            return new RunEventChoice // 대체 선택지
            { // 객체 시작
                id = id, // 선택 ID
                title = title, // 선택 제목
                description = "변화 없음" // 비용·보상 없음
            }; // 객체 종료
        } // 메서드 종료
        private static void DestroyClone(PieceDefinition card) // 실패한 준비 자원 제거
        { // 메서드 시작
            if (Application.isPlaying) UnityEngine.Object.Destroy(card); // 플레이 중 지연 제거
            else UnityEngine.Object.DestroyImmediate(card); // 검사 중 즉시 제거
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
