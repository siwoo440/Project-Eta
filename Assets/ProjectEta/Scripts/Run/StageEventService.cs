using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class StageEventService
    {
        public static bool CanChoose(RunState runState, RunEconomyState economy, StageEventChoice choice, int maxKingHp)
        {
            if (runState == null || economy == null || choice == null) return false; // 필수 이벤트 상태 누락 차단
            bool hasManageableCard = ShopService.GetManageableCards(runState).Count > 0; // 강화 가능한 카드 존재 여부 조회
            return CanChoose(runState.KingHp, economy.Currency, hasManageableCard, choice, maxKingHp); // 순수 조건 판정 위임
        }

        public static bool CanChoose(int kingHp, int currency, bool hasManageableCard, StageEventChoice choice, int maxKingHp)
        {
            if (choice == null) return false; // 빈 선택지 차단

            switch (choice.EffectType)
            {
                case StageEventChoiceEffectType.HealKing:
                    return maxKingHp > 0 && kingHp < maxKingHp; // 무료 회복 가능 여부 반환
                case StageEventChoiceEffectType.RiskRewardCard:
                    return kingHp > 1; // 즉사하지 않는 경우만 위험 계약 허용
                case StageEventChoiceEffectType.PurchaseCard:
                    return currency >= StageEventRules.TravelingMerchantCardPrice; // 이벤트 카드 구매 비용 검사
                case StageEventChoiceEffectType.PaidHeal:
                    return maxKingHp > 0 && kingHp < maxKingHp && currency >= StageEventRules.ShrineHealPrice; // 유료 회복 조건 검사
                case StageEventChoiceEffectType.UpgradeCard:
                    return hasManageableCard; // 강화 가능한 카드 존재 여부 반환
                default:
                    return true; // 나머지 선택지 기본 허용
            }
        }

        public static bool TryResolveChoice(RunState runState, RunEconomyState economy, StageEventChoice choice, int maxKingHp, out StageEventResolution resolution)
        {
            resolution = null; // 기본 실패 결과 초기화
            if (!CanChoose(runState, economy, choice, maxKingHp)) return false; // 선택 조건 미충족 차단

            switch (choice.EffectType)
            {
                case StageEventChoiceEffectType.CardReward:
                    resolution = new StageEventResolution(StageEventFollowUp.CardChoice, null); // 무료 카드 선택 화면 연결
                    return true; // 선택 처리 성공 반환
                case StageEventChoiceEffectType.RiskRewardCard:
                    resolution = new StageEventResolution(StageEventFollowUp.CardChoice, null); // 위험 카드 선택 화면 연결
                    return true; // 선택 처리 성공 반환
                case StageEventChoiceEffectType.PurchaseCard:
                    resolution = new StageEventResolution(StageEventFollowUp.CardChoice, null); // 유료 카드 선택 화면 연결
                    return true; // 선택 처리 성공 반환
                case StageEventChoiceEffectType.UpgradeCard:
                    resolution = new StageEventResolution(StageEventFollowUp.UpgradeChoice, null); // 강화 카드 선택 화면 연결
                    return true; // 선택 처리 성공 반환
                case StageEventChoiceEffectType.HealKing:
                    return TryResolveFreeHeal(runState, choice, maxKingHp, out resolution); // 무료 회복 처리
                case StageEventChoiceEffectType.CurrencyGain:
                    economy.Add(StageEventRules.GoldCacheCurrency); // 이벤트 Gold 보상 지급
                    resolution = new StageEventResolution(
                        StageEventFollowUp.Complete,
                        new StageChoiceResult(StageChoiceEffectType.CurrencyChanged, choice.Title, StageEventRules.GoldCacheCurrency, 0, null)); // Gold 획득 결과 생성
                    return true; // Gold 이벤트 성공 반환
                case StageEventChoiceEffectType.PaidHeal:
                    return TryResolvePaidHeal(runState, economy, choice, maxKingHp, out resolution); // 유료 회복 처리
                default:
                    resolution = new StageEventResolution(
                        StageEventFollowUp.Complete,
                        new StageChoiceResult(StageChoiceEffectType.None, choice.Title, 0, 0, null)); // 무변화 결과 생성
                    return true; // 무변화 선택 성공 반환
            }
        }

        public static bool TryTakeEventCard(RunState runState, RunEconomyState economy, PieceDefinition card, StageEventChoice choice, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || economy == null || card == null || choice == null) return false; // 필수 카드 이벤트 상태 누락 차단

            if (choice.EffectType == StageEventChoiceEffectType.CardReward)
            {
                if (!CardRewardRules.TryAddOwnedCard(runState.Deck, card)) return false; // 무료 카드 획득 실패 차단
                result = new StageChoiceResult(StageChoiceEffectType.CardAdded, $"{card.DisplayName} 획득", 0, 0, card); // 무료 카드 결과 생성
                return true; // 무료 카드 획득 성공 반환
            }

            if (choice.EffectType == StageEventChoiceEffectType.RiskRewardCard)
            {
                if (runState.KingHp <= 1) return false; // 위험 이벤트 즉사 차단
                if (!CardRewardRules.TryAddOwnedCard(runState.Deck, card)) return false; // 카드 추가 실패 시 비용 미적용
                runState.KingHp -= 1; // 위험 계약 HP 비용 적용
                economy.Add(StageEventRules.RiskRewardCurrency); // 위험 계약 Gold 보상 지급
                result = new StageChoiceResult(
                    StageChoiceEffectType.Mixed,
                    $"위험한 계약: {card.DisplayName} 획득",
                    StageEventRules.RiskRewardCurrency,
                    -1,
                    card); // 위험 계약 최종 결과 생성
                return true; // 위험 계약 성공 반환
            }

            if (choice.EffectType == StageEventChoiceEffectType.PurchaseCard)
            {
                int price = StageEventRules.TravelingMerchantCardPrice; // 이벤트 카드 가격 조회
                if (!economy.TrySpend(price)) return false; // Gold 부족 구매 차단

                if (!CardRewardRules.TryAddOwnedCard(runState.Deck, card))
                {
                    economy.Add(price); // 카드 추가 실패 Gold 환불
                    return false; // 유료 카드 획득 실패 반환
                }

                result = new StageChoiceResult(StageChoiceEffectType.CardAdded, $"{card.DisplayName} 구매", -price, 0, card); // 유료 카드 결과 생성
                return true; // 이벤트 카드 구매 성공 반환
            }

            return false; // 카드 선택과 무관한 효과 차단
        }

        public static bool TryUpgradeEventCard(RunState runState, PieceDefinition card, StageEventChoice choice, out StageChoiceResult result)
        {
            result = null; // 기본 실패 결과 초기화
            if (runState == null || card == null || choice == null) return false; // 필수 강화 상태 누락 차단
            if (choice.EffectType != StageEventChoiceEffectType.UpgradeCard) return false; // 강화 이벤트 외 호출 차단

            if (!RuntimeCardUpgradeService.TryUpgradeOwnedCard(runState.Deck, card, out PieceDefinition upgraded)) return false; // 런타임 카드 강화 실패 차단
            result = new StageChoiceResult(StageChoiceEffectType.CardUpgraded, $"{card.DisplayName} → {upgraded.DisplayName}", 0, 0, upgraded); // 무료 강화 결과 생성
            return true; // 이벤트 강화 성공 반환
        }

        private static bool TryResolveFreeHeal(RunState runState, StageEventChoice choice, int maxKingHp, out StageEventResolution resolution)
        {
            resolution = null; // 기본 실패 결과 초기화
            if (runState.KingHp >= maxKingHp) return false; // 최대 HP 무료 회복 차단
            int before = runState.KingHp; // 회복 전 HP 저장
            runState.KingHp = System.Math.Min(maxKingHp, runState.KingHp + StageEventRules.HealAmount); // 무료 HP 회복 적용
            resolution = new StageEventResolution(
                StageEventFollowUp.Complete,
                new StageChoiceResult(StageChoiceEffectType.KingHpChanged, choice.Title, 0, runState.KingHp - before, null)); // 무료 회복 결과 생성
            return true; // 무료 회복 성공 반환
        }

        private static bool TryResolvePaidHeal(RunState runState, RunEconomyState economy, StageEventChoice choice, int maxKingHp, out StageEventResolution resolution)
        {
            resolution = null; // 기본 실패 결과 초기화
            if (runState.KingHp >= maxKingHp) return false; // 최대 HP 유료 회복 차단
            int price = StageEventRules.ShrineHealPrice; // 제단 회복 가격 조회
            if (!economy.TrySpend(price)) return false; // Gold 부족 회복 차단
            int before = runState.KingHp; // 회복 전 HP 저장
            runState.KingHp = System.Math.Min(maxKingHp, runState.KingHp + StageEventRules.HealAmount); // 유료 HP 회복 적용
            resolution = new StageEventResolution(
                StageEventFollowUp.Complete,
                new StageChoiceResult(StageChoiceEffectType.KingHpChanged, choice.Title, -price, runState.KingHp - before, null)); // 유료 회복 결과 생성
            return true; // 유료 회복 성공 반환
        }
    }
}
