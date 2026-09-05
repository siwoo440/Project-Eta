namespace ProjectEta.AI // 적 AI 관련 타입을 모아두는 네임스페이스
{
    public sealed class EnemyAIScoredCandidate // 디버그 재계산 없이 Base·Role·Threat·Special 분해 점수와 최종 후보를 함께 보관하는 39일차 데이터 객체
    {
        public AIActionCandidate BaseCandidate { get; } // 33일차 Base Planner가 만든 원본 합법 후보
        public int RoleBonus { get; } // 34일차 역할 보너스
        public int ThreatScore { get; } // 35일차 위협 점수
        public int SpecialBonus { get; } // 35일차 특수 기물 점수
        public AIActionCandidate FinalCandidate { get; } // 네 점수 계층을 합산한 실제 선택용 후보

        public EnemyAIScoredCandidate(AIActionCandidate baseCandidate, int roleBonus, int threatScore, int specialBonus, AIActionCandidate finalCandidate) // 분해 점수 생성자
        {
            BaseCandidate = baseCandidate; // 원본 후보 저장
            RoleBonus = roleBonus; // 역할 보너스 저장
            ThreatScore = threatScore; // 위협 점수 저장
            SpecialBonus = specialBonus; // 특수 점수 저장
            FinalCandidate = finalCandidate; // 최종 후보 저장
        }
    }
}
