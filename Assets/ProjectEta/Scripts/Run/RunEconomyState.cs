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
        private readonly RunState _owner; // 경제 기록 소유 런
        public int Currency { get; private set; } // 현재 런 전용 재화

        public RunEconomyState(int startingCurrency) : this(startingCurrency, null) // 기존 독립 생성 호환
        { // 생성 범위
        } // 생성 종료
        public RunEconomyState(int startingCurrency, RunState owner) // 기록 가능한 런 경제
        { // 초기화 범위
            _owner = owner; // 기록할 실제 런
            Currency = Mathf.Max(0, startingCurrency); // 음수 시작 Gold 차단
            RunBalanceTelemetry.InitializeGold(_owner, Currency); // 시작 또는 복원 기준 기록
        } // 초기화 종료

        public bool TrySpend(int amount) // 기존 지출 호출 호환
        { // 지출 범위
            return TrySpend(amount, "OtherSpend"); // 기본 지출 사유
        } // 지출 종료
        public bool TrySpend(int amount, string reason) // 실제 지출과 부족 시도 기록
        { // 기록 지출 범위
            if (amount < 0) // 음수 비용 확인
            { // 잘못된 비용 분기
                return false; // 음수 비용 거부
            } // 비용 검사 종료
            int before = Currency; // 지출 전 재화
            bool success = Currency >= amount; // 실제 잔액 확인
            if (success) // 비용 적용 가능 확인
            { // 비용 적용 범위
                Currency -= amount; // 실제 지출 적용
            } // 비용 적용 종료
            if (amount > 0) // 유의미한 지출 시도
            { // 측정 기록 범위
                RunBalanceTelemetry.RecordGold(_owner, before, Currency, -amount, success, reason); // 성공과 부족 구분 기록
            } // 측정 기록 종료
            return success; // 실제 지출 결과
        } // 기록 지출 종료
        public void Add(int amount) // 기존 재화 변경 호환
        { // 변경 범위
            Add(amount, "OtherGold"); // 기본 변경 사유
        } // 변경 종료
        public void Add(int amount, string reason) // 수입과 환불 구분 기록
        { // 재화 기록 범위
            int before = Currency; // 변경 전 재화
            Currency = (int)System.Math.Max(0L, System.Math.Min(int.MaxValue, (long)Currency + amount)); // 합계 오버플로 방지
            if (Currency != before) // 실제 변동 확인
            { // 변동 기록 범위
                RunBalanceTelemetry.RecordGold(_owner, before, Currency, amount, true, reason); // 실제 변경량 기록
            } // 변동 기록 종료
        } // 재화 기록 종료

        public void RestoreCurrency(int amount)
        {
            Currency = Mathf.Max(0, amount); // 저장 Gold를 음수 없이 직접 복원
            if (_owner != null && _owner.BalanceData.entries.Count == 0) // 측정 시작 전 복원 확인
            { // 복원 기준 범위
                _owner.BalanceData.initialGold = Currency; // 저장 Gold를 측정 시작 값으로 보정
            } // 복원 기준 종료
            RunBalanceTelemetry.InitializeGold(_owner, Currency); // 복원을 새 수입에서 제외
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

            var state = new RunEconomyState(Mathf.Max(0, RunBalanceProfile.Current.startingGold), runState); // 새 런 경제 상태 생성
            States.Add(runState, state); // 런 참조에 경제 상태 연결
            return state; // 신규 경제 상태 반환
        }

        public static RunEconomyState Restore(RunState runState, int currency)
        {
            if (runState == null) return null; // 런 상태 누락 방어

            if (!States.TryGetValue(runState, out RunEconomyState state))
            {
                state = new RunEconomyState(currency, runState); // 저장 Gold로 경제 상태 생성
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
