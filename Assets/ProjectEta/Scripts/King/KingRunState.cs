namespace ProjectEta.King
{
    public sealed class KingRunState
    {
        public const int AttackRageMaxStacks = 2; // 공격형 킹 격노 최대 스택

        public KingArchetype Archetype { get; private set; } = KingArchetype.Default; // 현재 런 선택 킹
        public int RageStacks { get; private set; } // 공격형 킹 전투 격노 스택

        public bool Select(KingArchetype archetype)
        {
            if (Archetype == archetype) return false; // 동일 킹 재선택 차단
            Archetype = archetype; // 새 킹 타입 저장
            ResetBattleScopedState(); // 킹 변경 시 전투 임시 상태 초기화
            return true; // 킹 선택 변경 성공 반환
        }

        public bool TryAddRage()
        {
            if (Archetype != KingArchetype.Attack) return false; // 공격형 외 격노 획득 차단
            if (RageStacks >= AttackRageMaxStacks) return false; // 최대 격노 초과 차단
            RageStacks++; // 격노 1스택 증가
            return true; // 격노 획득 성공 반환
        }

        public int ConsumeRage()
        {
            int consumed = RageStacks; // 현재 격노 스택 저장
            RageStacks = 0; // 공격 후 격노 전부 소비
            return consumed; // 소비한 스택 수 반환
        }

        public void ResetBattleScopedState()
        {
            RageStacks = 0; // 전투 종료 시 격노 초기화
        }
    }
}
