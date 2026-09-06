using System.Collections.Generic; // RunState별 경제 상태 저장
using UnityEngine; // Mathf·런타임 초기화 사용

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
        public const int PrototypeKingMaxHp = 3; // 임시 킹 최대 HP
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
            if (amount < 0 || Currency < amount) return false; // 잘못된 비용·잔액 부족 차단
            Currency -= amount; // 재화 지불
            return true; // 지불 성공 반환
        }

        public void Add(int amount)
        {
            Currency = Mathf.Max(0, Currency + amount); // 재화 증감 적용
        }

        public void RestoreCurrency(int amount)
        {
            Currency = Mathf.Max(0, amount); // 저장 Gold를 음수 없이 직접 복원
        }
    }

    public static class RunEconomyService
    {
        private static readonly Dictionary<RunState, RunEconomyState> States = new Dictionary<RunState, RunEconomyState>(); // 런별 경제 상태

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            States.Clear(); // 플레이 세션 경제 상태 초기화
        }

        public static RunEconomyState GetOrCreate(RunState runState)
        {
            if (runState == null) return null; // 런 상태 누락 방어
            if (States.TryGetValue(runState, out RunEconomyState existing)) return existing; // 기존 경제 상태 재사용

            var state = new RunEconomyState(RunEconomyRules.StartingCurrency); // 새 런 경제 상태 생성
            States.Add(runState, state); // 런 참조에 경제 상태 연결
            return state; // 신규 경제 상태 반환
        }

        public static RunEconomyState Restore(RunState runState, int currency)
        {
            if (runState == null) return null; // 런 상태 누락 방어

            if (!States.TryGetValue(runState, out RunEconomyState state))
            {
                state = new RunEconomyState(currency); // 저장 Gold로 경제 상태 생성
                States.Add(runState, state); // 복원 런에 경제 상태 연결
            }
            else
            {
                state.RestoreCurrency(currency); // 기존 경제 상태 저장 Gold로 교체
            }

            return state; // 복원 경제 상태 반환
        }

        public static bool TryGet(RunState runState, out RunEconomyState state)
        {
            if (runState == null)
            {
                state = null; // 잘못된 런 상태 결과 초기화
                return false; // 조회 실패 반환
            }

            return States.TryGetValue(runState, out state); // 등록된 경제 상태 조회
        }

        public static void Remove(RunState runState)
        {
            if (runState == null) return; // 잘못된 런 상태 차단
            States.Remove(runState); // 종료 런 경제 상태 제거
        }

        public static void ResetForTests()
        {
            States.Clear(); // EditMode 테스트 상태 초기화
        }
    }
}
