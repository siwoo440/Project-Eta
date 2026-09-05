using UnityEngine; // Mathf를 사용해 잘못된 예산 값을 안전한 범위로 보정하기 위한 네임스페이스

namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIEvaluationBudget // 한 EnemyTurn에서 비싼 AI 평가를 수행할 최대 후보 수를 정의하는 39일차 예산 객체
    {
        public const int DefaultMaxHeavyEvaluationCandidates = 128; // 일반적인 10x10 전투는 거의 건드리지 않으면서 극단적 후보 폭증을 막는 기본 상한

        public int MaxHeavyEvaluationCandidates { get; } // Role·Threat·Special까지 정밀 평가할 최대 후보 수

        public EnemyAIEvaluationBudget(int maxHeavyEvaluationCandidates = DefaultMaxHeavyEvaluationCandidates) // 테스트와 런타임에서 같은 예산 구조를 사용하기 위한 생성자
        {
            MaxHeavyEvaluationCandidates = Mathf.Max(1, maxHeavyEvaluationCandidates); // 0 이하가 들어와도 최소 한 후보는 평가하도록 보정
        }
    }
}
