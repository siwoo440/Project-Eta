using System.Collections.Generic; // 결과 문구 조합 목록 사용
using ProjectEta.Run; // RunFlowPhase·RunEconomyRules 사용

namespace ProjectEta.UI // 65일차 UI 상태 네임스페이스
{
    public sealed class Day65ActivityDelta // Stage Activity 한 번의 상태 변화 값
    {
        public static readonly Day65ActivityDelta None = new Day65ActivityDelta(0, 0, 0); // 변화 없는 공용 결과

        public int CurrencyDelta { get; } // Gold 변화량
        public int KingHpDelta { get; } // King HP 변화량
        public int OwnedCardDelta { get; } // 보유 카드 수 변화량
        public bool HasChange => CurrencyDelta != 0 || KingHpDelta != 0 || OwnedCardDelta != 0; // 실제 변화 존재 여부

        public Day65ActivityDelta(int currencyDelta, int kingHpDelta, int ownedCardDelta) // 변화량 생성
        {
            CurrencyDelta = currencyDelta; // Gold 변화량 저장
            KingHpDelta = kingHpDelta; // King HP 변화량 저장
            OwnedCardDelta = ownedCardDelta; // 보유 카드 변화량 저장
        }

        public string BuildSummary(RunFlowPhase phase) // 현재 활동 기준 결과 알림 문구 생성
        {
            if (!HasChange) // 변화 없음 확인
            {
                return string.Empty; // 알림 문구 생략
            }

            if (phase == RunFlowPhase.Shop) // 상점 결과 특수 판정
            {
                if (CurrencyDelta == -RunEconomyRules.CardPurchasePrice && OwnedCardDelta > 0) // 카드 구매 패턴 확인
                {
                    return $"카드 구매 완료 · Gold {FormatSigned(CurrencyDelta)}"; // 구매 결과 문구 반환
                }

                if (CurrencyDelta == -RunEconomyRules.CardRemovePrice && OwnedCardDelta < 0) // 카드 제거 패턴 확인
                {
                    return $"카드 제거 완료 · Gold {FormatSigned(CurrencyDelta)}"; // 제거 결과 문구 반환
                }

                if (CurrencyDelta == -RunEconomyRules.HealPrice && KingHpDelta > 0) // King 회복 패턴 확인
                {
                    return $"King HP 회복 · HP {FormatSigned(KingHpDelta)} · Gold {FormatSigned(CurrencyDelta)}"; // 회복 결과 문구 반환
                }

                if (CurrencyDelta == -RunEconomyRules.CardUpgradePrice && OwnedCardDelta == 0 && KingHpDelta == 0) // 카드 강화 패턴 확인
                {
                    return $"카드 강화 완료 · Gold {FormatSigned(CurrencyDelta)}"; // 강화 결과 문구 반환
                }
            }

            if (phase == RunFlowPhase.Event) // 이벤트 결과 특수 판정
            {
                if (CurrencyDelta == RunEconomyRules.RiskRewardCurrency && KingHpDelta < 0) // 위험 계약 패턴 확인
                {
                    return $"위험한 계약 · HP {FormatSigned(KingHpDelta)} · Gold {FormatSigned(CurrencyDelta)}"; // 위험 계약 결과 문구 반환
                }

                if (OwnedCardDelta > 0 && CurrencyDelta == 0) // 무료 카드 획득 패턴 확인
                {
                    return $"카드 획득 · 보유 카드 {FormatSigned(OwnedCardDelta)}"; // 이벤트 카드 획득 문구 반환
                }

                if (KingHpDelta > 0 && CurrencyDelta == 0) // 무료 휴식 패턴 확인
                {
                    return $"휴식 완료 · HP {FormatSigned(KingHpDelta)}"; // 이벤트 회복 문구 반환
                }
            }

            if (phase == RunFlowPhase.Reward && OwnedCardDelta > 0) // 일반 카드 보상 패턴 확인
            {
                return $"카드 보상 획득 · 보유 카드 {FormatSigned(OwnedCardDelta)}"; // 카드 보상 획득 문구 반환
            }

            var parts = new List<string>(); // 일반 결과 문구 조각 생성

            if (CurrencyDelta != 0) // Gold 변화 확인
            {
                parts.Add($"Gold {FormatSigned(CurrencyDelta)}"); // Gold 변화 문구 추가
            }

            if (KingHpDelta != 0) // HP 변화 확인
            {
                parts.Add($"HP {FormatSigned(KingHpDelta)}"); // HP 변화 문구 추가
            }

            if (OwnedCardDelta != 0) // 보유 카드 변화 확인
            {
                parts.Add($"보유 카드 {FormatSigned(OwnedCardDelta)}"); // 카드 변화 문구 추가
            }

            return string.Join(" · ", parts); // 일반 결과 문구 결합
        }

        private static string FormatSigned(int value) // 양수·음수 부호 포함 숫자 변환
        {
            return value > 0 ? $"+{value}" : value.ToString(); // 양수에만 + 부호 추가
        }
    }

    public sealed class Day65ActivityDeltaState // Stage Activity 값 변화 추적 상태
    {
        private bool _hasSnapshot; // 기준 스냅샷 존재 여부
        private int _currency; // 이전 Gold
        private int _kingHp; // 이전 King HP
        private int _ownedCardCount; // 이전 보유 카드 수

        public bool HasSnapshot => _hasSnapshot; // 기준 스냅샷 존재 여부 외부 조회

        public Day65ActivityDelta Capture(int currency, int kingHp, int ownedCardCount) // 현재 값을 비교하고 새 기준으로 저장
        {
            if (!_hasSnapshot) // 최초 값 입력 확인
            {
                Reset(currency, kingHp, ownedCardCount); // 최초 기준점 저장
                return Day65ActivityDelta.None; // 최초 입력은 변화 없음 반환
            }

            int currencyDelta = currency - _currency; // Gold 변화량 계산
            int kingHpDelta = kingHp - _kingHp; // King HP 변화량 계산
            int ownedCardDelta = ownedCardCount - _ownedCardCount; // 보유 카드 변화량 계산

            _currency = currency; // 현재 Gold를 다음 기준으로 저장
            _kingHp = kingHp; // 현재 HP를 다음 기준으로 저장
            _ownedCardCount = ownedCardCount; // 현재 카드 수를 다음 기준으로 저장

            return new Day65ActivityDelta(currencyDelta, kingHpDelta, ownedCardDelta); // 계산된 변화 반환
        }

        public void Reset(int currency, int kingHp, int ownedCardCount) // 기준 스냅샷 강제 초기화
        {
            _currency = currency; // 기준 Gold 저장
            _kingHp = kingHp; // 기준 King HP 저장
            _ownedCardCount = ownedCardCount; // 기준 카드 수 저장
            _hasSnapshot = true; // 기준점 생성 완료 표시
        }

        public void Clear() // 런 변경 시 추적 상태 초기화
        {
            _currency = 0; // 이전 Gold 초기화
            _kingHp = 0; // 이전 HP 초기화
            _ownedCardCount = 0; // 이전 카드 수 초기화
            _hasSnapshot = false; // 기준점 없음 상태로 전환
        }
    }
}
