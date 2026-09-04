using UnityEngine; // Mathf를 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class RuntimeStatusEffect // 27일차: 기물 1개에 실제로 걸린 상태 이상 1건의 가변 상태
    {
        public StatusEffectDefinition Definition { get; } // 이 상태가 참조하는 고정 규칙
        public int RemainingTurns { get; private set; } // 남은 지속 턴 수
        public int StackCount { get; private set; } // 현재 중첩 수

        public RuntimeStatusEffect(StatusEffectDefinition definition) // 최초 적용 시 생성자
        {
            Definition = definition; // 고정 규칙 저장
            RemainingTurns = definition.DefaultDurationTurns; // 기본 지속 턴으로 초기화
            StackCount = 1; // 최초 적용은 1중첩부터 시작
        }

        public void Reapply() // 같은 상태가 다시 걸렸을 때 처리
        {
            RemainingTurns = Definition.DefaultDurationTurns; // 두 방식 모두 지속 턴은 갱신
            if (Definition.StackMode == StatusStackMode.StacksAdd) // 중첩형이면
            {
                StackCount = Mathf.Min(Definition.MaxStacks, StackCount + 1); // 최대치까지 중첩 수 증가
            }
        }

        public void RestoreState(int remainingTurns, int stackCount) // 저장 데이터로부터 상태를 그대로 복원
        {
            RemainingTurns = Mathf.Max(0, remainingTurns); // 음수 방지
            StackCount = Mathf.Clamp(stackCount, 1, Definition.MaxStacks); // 1~최대 중첩 범위로 보정
        }

        public bool Tick() // 턴 종료 시 지속 턴을 1 감소시키고 만료 여부를 반환
        {
            RemainingTurns--; // 지속 턴 감소
            return RemainingTurns <= 0; // 0 이하가 되면 제거 대상
        }
    }
}
