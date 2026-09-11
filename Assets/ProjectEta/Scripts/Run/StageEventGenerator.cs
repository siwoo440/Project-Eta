using System; // System.Random 사용
using System.Collections.Generic; // IReadOnlyList<T>·List<T> 사용

namespace ProjectEta.Run
{
    public static class StageEventGenerator
    {
        private static readonly StageEventDefinition[] Definitions = BuildDefinitions(); // 전체 이벤트 콘텐츠 정의

        public static IReadOnlyList<StageEventDefinition> AllDefinitions => Definitions; // 테스트·검증용 전체 이벤트 목록

        public static StageEventScenario Create(int mapSeed, int phase, int stage, string nodeId)
        {
            int safePhase = NormalizePhase(phase); // Phase 범위 보정
            int safeStage = NormalizeStage(stage); // Stage 범위 보정
            int seed = CreateSeed(mapSeed, safePhase, safeStage, nodeId); // 이벤트 고유 Seed 생성
            IReadOnlyList<StageEventDefinition> eligible = GetEligibleDefinitions(safePhase); // 현재 Phase 이벤트 후보 조회
            StageEventDefinition selected = SelectWeighted(eligible, safePhase, seed); // 가중치 기반 이벤트 선택
            return new StageEventScenario(selected, seed); // 재현 가능한 이벤트 시나리오 반환
        }

        public static StageEventScenario Create(int depth)
        {
            int safeDepth = depth <= 0 ? 1 : depth; // 기존 호출 깊이 보정
            int cycle = (safeDepth - 1) % 3; // 기존 3종 순환 호환
            StageEventDefinition definition = Definitions[cycle]; // 기존 CardFind·Rest·RiskReward 순서 유지
            return new StageEventScenario(definition, CreateSeed(0, 1, safeDepth, $"legacy_{safeDepth}")); // 기존 호출 호환 시나리오 반환
        }

        public static int CreateSeed(int mapSeed, int phase, int stage, string nodeId)
        {
            unchecked
            {
                int seed = 23; // 안정 해시 시작값
                seed = seed * 31 + mapSeed; // RouteMap Seed 반영
                seed = seed * 31 + phase; // Phase 반영
                seed = seed * 31 + stage; // Stage 반영

                if (!string.IsNullOrEmpty(nodeId))
                {
                    for (int i = 0; i < nodeId.Length; i++)
                    {
                        seed = seed * 31 + nodeId[i]; // NodeId 안정 해시 반영
                    }
                }

                return seed; // 최종 이벤트 Seed 반환
            }
        }

        public static int CreateFollowUpSeed(int mapSeed, int phase, int stage, string nodeId, string eventId, string choiceId)
        {
            unchecked
            {
                int seed = CreateSeed(mapSeed, phase, stage, nodeId); // 이벤트 기본 Seed 생성
                seed = AppendStableText(seed, eventId); // 이벤트 ID 반영
                seed = AppendStableText(seed, choiceId); // 선택지 ID 반영
                return seed; // 후속 카드 후보 Seed 반환
            }
        }

        public static IReadOnlyList<StageEventDefinition> GetEligibleDefinitions(int phase)
        {
            int safePhase = NormalizePhase(phase); // Phase 범위 보정
            var result = new List<StageEventDefinition>(); // 등장 가능 이벤트 목록 생성

            for (int i = 0; i < Definitions.Length; i++)
            {
                StageEventDefinition definition = Definitions[i]; // 현재 이벤트 정의 조회
                if (definition != null && definition.IsAvailableInPhase(safePhase)) result.Add(definition); // Phase 조건 충족 이벤트 등록
            }

            return result; // 최종 이벤트 후보 반환
        }

        private static StageEventDefinition SelectWeighted(IReadOnlyList<StageEventDefinition> definitions, int phase, int seed)
        {
            if (definitions == null || definitions.Count == 0) return Definitions[0]; // 후보 누락 기본 이벤트 반환
            int totalWeight = 0; // 전체 가중치 초기화

            for (int i = 0; i < definitions.Count; i++)
            {
                totalWeight += StageEventRules.GetEventWeight(definitions[i], phase); // 후보 가중치 합산
            }

            if (totalWeight <= 0) return definitions[0]; // 잘못된 가중치 기본 후보 반환
            var random = new Random(seed); // 재현 가능한 난수 생성
            int roll = random.Next(totalWeight); // 가중치 범위 난수 추첨

            for (int i = 0; i < definitions.Count; i++)
            {
                StageEventDefinition definition = definitions[i]; // 현재 후보 조회
                int weight = StageEventRules.GetEventWeight(definition, phase); // 현재 후보 가중치 조회
                if (roll < weight) return definition; // 현재 구간 이벤트 선택
                roll -= weight; // 다음 가중치 구간 이동
            }

            return definitions[definitions.Count - 1]; // 부동 오차 없는 최종 안전 반환
        }

        private static int AppendStableText(int seed, string value)
        {
            if (string.IsNullOrEmpty(value)) return seed; // 빈 문자열 해시 생략

            unchecked
            {
                for (int i = 0; i < value.Length; i++)
                {
                    seed = seed * 31 + value[i]; // 문자열 문자별 안정 해시 반영
                }
            }

            return seed; // 문자열 반영 Seed 반환
        }

        private static int NormalizePhase(int phase)
        {
            if (phase < RunPhaseProgressService.FirstPhase) return RunPhaseProgressService.FirstPhase; // 최소 Phase 보정
            if (phase > RunPhaseProgressService.TotalPhases) return RunPhaseProgressService.TotalPhases; // 최대 Phase 보정
            return phase; // 정상 Phase 반환
        }

        private static int NormalizeStage(int stage)
        {
            if (stage < RoundState.FirstRound) return RoundState.FirstRound; // 최소 Stage 보정
            if (stage > RoundState.FinalRound) return RoundState.FinalRound; // 최대 Stage 보정
            return stage; // 정상 Stage 반환
        }

        private static StageEventDefinition[] BuildDefinitions()
        {
            return new[]
            {
                new StageEventDefinition(
                    "card_find",
                    StageEventType.CardFind,
                    "버려진 카드 꾸러미",
                    "판 위에 오래된 카드 꾸러미가 놓여 있습니다.\n쓸 만한 카드 한 장을 챙길 수 있습니다.",
                    1,
                    5,
                    22,
                    new StageEventChoice("take_card", "카드 꾸러미를 연다", "카드 후보 중 한 장을 무료로 획득합니다.", StageEventChoiceEffectType.CardReward),
                    new StageEventChoice("leave", "그냥 지나간다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 기본 카드 발견 이벤트
                new StageEventDefinition(
                    "rest",
                    StageEventType.Rest,
                    "조용한 휴식처",
                    "잠시 숨을 고를 수 있는 안전한 장소입니다.\n킹의 HP를 회복할 수 있습니다.",
                    1,
                    5,
                    18,
                    new StageEventChoice("rest", "잠시 휴식한다", $"King HP +{StageEventRules.HealAmount}", StageEventChoiceEffectType.HealKing),
                    new StageEventChoice("leave", "바로 떠난다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 무료 회복 이벤트
                new StageEventDefinition(
                    "risk_contract",
                    StageEventType.RiskReward,
                    "위험한 계약",
                    "체력을 대가로 Gold와 카드 보상을 받을 수 있습니다.\nHP가 1이면 계약할 수 없습니다.",
                    1,
                    5,
                    14,
                    new StageEventChoice("accept", "위험한 계약을 맺는다", $"King HP -1 / Gold +{StageEventRules.RiskRewardCurrency} / 카드 1장", StageEventChoiceEffectType.RiskRewardCard),
                    new StageEventChoice("leave", "계약을 거절한다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 위험 보상 이벤트
                new StageEventDefinition(
                    "traveling_merchant",
                    StageEventType.TravelingMerchant,
                    "떠돌이 상인",
                    "경로 밖을 떠도는 상인이 카드 한 장을 제안합니다.\n정식 Shop보다 선택지는 적지만 즉시 거래할 수 있습니다.",
                    2,
                    5,
                    12,
                    new StageEventChoice("buy_card", "카드를 살펴본다", $"카드 1장 · {StageEventRules.TravelingMerchantCardPrice} Gold", StageEventChoiceEffectType.PurchaseCard),
                    new StageEventChoice("leave", "거래하지 않는다", "Gold를 아끼고 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 유료 카드 이벤트
                new StageEventDefinition(
                    "gold_cache",
                    StageEventType.GoldCache,
                    "버려진 금고",
                    "누군가 급히 떠나며 남긴 작은 금고입니다.\n안에는 아직 사용할 수 있는 Gold가 남아 있습니다.",
                    1,
                    5,
                    12,
                    new StageEventChoice("take_gold", "Gold를 챙긴다", $"Gold +{StageEventRules.GoldCacheCurrency}", StageEventChoiceEffectType.CurrencyGain),
                    new StageEventChoice("leave", "건드리지 않는다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 재화 획득 이벤트
                new StageEventDefinition(
                    "shrine",
                    StageEventType.Shrine,
                    "낡은 회복 제단",
                    "Gold를 바치면 킹의 상처를 회복시키는 오래된 제단입니다.",
                    2,
                    5,
                    10,
                    new StageEventChoice("heal", "Gold를 바친다", $"{StageEventRules.ShrineHealPrice} Gold / King HP +{StageEventRules.HealAmount}", StageEventChoiceEffectType.PaidHeal),
                    new StageEventChoice("leave", "제단을 떠난다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)), // 유료 회복 이벤트
                new StageEventDefinition(
                    "training",
                    StageEventType.Training,
                    "버려진 훈련장",
                    "아직 사용할 수 있는 훈련 장비가 남아 있습니다.\n보유 카드 한 장을 즉시 강화할 수 있습니다.",
                    3,
                    5,
                    12,
                    new StageEventChoice("train", "카드 한 장을 훈련한다", "선택 카드 HP/ATK +1", StageEventChoiceEffectType.UpgradeCard),
                    new StageEventChoice("leave", "훈련장을 떠난다", "아무 변화 없이 다음 경로로 이동합니다.", StageEventChoiceEffectType.None)) // 무료 강화 이벤트
            }; // 전체 이벤트 콘텐츠 반환
        }
    }
}
