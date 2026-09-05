using System.Collections.Generic; // RunState별 경제 상태 저장
using UnityEngine; // 런타임 초기화 사용

namespace ProjectEta.Run
{
    public static class RunEconomyRules
    {
        public const int StartingCurrency = 100; // 새 런 시작 재화
        public const int CardPurchasePrice = 30; // 카드 구매 가격
        public const int CardRemovePrice = 40; // 카드 제거 가격
        public const int HealPrice = 20; // 킹 회복 가격
        public const int CardUpgradePrice = 50; // 카드 강화 가격
        public const int RiskRewardCurrency = 60; // 위험 이벤트 재화 보상
        public const int PrototypeKingMaxHp = 3; // 47일차 임시 킹 최대 HP
    }

    public sealed class RunEconomyState
    {
        public int Currency { get; private set; } // 현재 런 전용 재화

        public RunEconomyState(int startingCurrency)
        {
            Currency = Mathf.Max(0, startingCurrency); // 시작 재화 보정
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || Currency < amount) return false;
            Currency -= amount; // 재화 지불
            return true;
        }

        public void Add(int amount)
        {
            Currency = Mathf.Max(0, Currency + amount); // 재화 증감 적용
        }
    }

    public static class RunEconomyService
    {
        private static readonly Dictionary<RunState, RunEconomyState> States = new Dictionary<RunState, RunEconomyState>(); // 런별 임시 경제 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            States.Clear(); // 플레이 세션 경제 상태 초기화
        }

        public static RunEconomyState GetOrCreate(RunState runState)
        {
            if (runState == null) return null;
            if (States.TryGetValue(runState, out RunEconomyState existing)) return existing;

            var state = new RunEconomyState(RunEconomyRules.StartingCurrency); // 새 런 경제 상태 생성
            States.Add(runState, state); // 런 참조에 경제 상태 연결
            return state;
        }

        public static void ResetForTests()
        {
            States.Clear(); // EditMode 테스트 상태 초기화
        }
    }
}
