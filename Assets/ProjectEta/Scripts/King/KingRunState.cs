namespace ProjectEta.King
{
    public sealed class KingRunState
    {
        public const int AttackRageMaxStacks = 2; // 공격형 킹 격노 최대 스택

        public KingArchetype Archetype { get; private set; } = KingArchetype.Default; // 현재 런 선택 킹
        public int RageStacks { get; private set; } // 공격형 킹 전투 격노 스택
        public bool BarrierActive { get; private set; } // 방어형 킹 왕의 요새 방벽 활성 여부
        public bool KingMovedThisTurn { get; private set; } // 방어형 킹 현재 플레이어 턴 이동 여부
        public bool StrategyPreparationPending { get; private set; } // 전략형 킹 카드 선택 진행 여부
        public int StrategyPreparationTurnNumber { get; private set; } = -1; // 현재 전투에서 전술적 준비를 처리한 배치 턴 번호

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

        public void BeginPlayerTurn()
        {
            KingMovedThisTurn = false; // 새 플레이어 턴 시작 시 킹 이동 기록 초기화
        }

        public bool MarkKingMoved()
        {
            if (Archetype != KingArchetype.Defense) return false; // 방어형 킹 외 이동 추적 불필요
            if (KingMovedThisTurn) return false; // 같은 턴 중복 이동 기록 차단
            KingMovedThisTurn = true; // 현재 플레이어 턴 킹 이동 기록
            return true; // 이동 기록 변경 반환
        }

        public bool TryGainBarrier()
        {
            if (Archetype != KingArchetype.Defense) return false; // 방어형 킹 외 방벽 획득 차단
            if (KingMovedThisTurn) return false; // 현재 턴 킹 이동 시 방벽 획득 차단
            if (BarrierActive) return false; // 최대 1개 방벽 중복 획득 차단
            BarrierActive = true; // 왕의 요새 방벽 활성화
            return true; // 신규 방벽 획득 반환
        }

        public bool ConsumeBarrier()
        {
            if (!BarrierActive) return false; // 방벽이 없으면 소비 실패
            BarrierActive = false; // 다음 피해에서 방벽 제거
            return true; // 방벽 소비 성공 반환
        }

        public bool TryBeginStrategyPreparation(int turnNumber)
        {
            if (Archetype != KingArchetype.Strategy) return false; // 전략형 킹 외 전술적 준비 차단
            if (StrategyPreparationPending) return false; // 이미 선택 중이면 중복 시작 차단
            if (StrategyPreparationTurnNumber == turnNumber) return false; // 같은 배치 턴 중복 발동 차단
            StrategyPreparationTurnNumber = turnNumber; // 현재 배치 턴 처리 번호 기록
            StrategyPreparationPending = true; // 카드 선택 진행 상태 활성화
            return true; // 전술적 준비 시작 성공 반환
        }

        public void MarkStrategyPreparationSkipped(int turnNumber)
        {
            StrategyPreparationTurnNumber = turnNumber; // 후보 없음·손패 가득 참 상태도 해당 배치 턴 처리 완료로 기록
            StrategyPreparationPending = false; // 카드 선택 대기 해제
        }

        public void CompleteStrategyPreparation()
        {
            StrategyPreparationPending = false; // 카드 선택 완료 상태 반영
        }

        public void ResetBattleScopedState()
        {
            RageStacks = 0; // 전투 종료 시 공격형 격노 초기화
            BarrierActive = false; // 전투 종료 시 방어형 방벽 초기화
            KingMovedThisTurn = false; // 전투 종료 시 이동 추적 초기화
            StrategyPreparationPending = false; // 전투 종료 시 전략형 선택 대기 초기화
            StrategyPreparationTurnNumber = -1; // 다음 전투에서 같은 턴 번호 재발동 허용
        }
    }
}
