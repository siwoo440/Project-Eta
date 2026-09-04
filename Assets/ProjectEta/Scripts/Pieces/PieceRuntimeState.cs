using System.Collections.Generic; // List<T>와 IReadOnlyList<T>를 사용하기 위한 네임스페이스
using UnityEngine; // Vector2Int, Mathf 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class PieceRuntimeState // 보드 위 기물 1개의 가변 상태를 담는 클래스
    {
        private int _currentHp; // 현재 체력을 저장하는 내부 필드
        private Vector2Int _boardPosition; // 현재 보드 위치를 저장하는 내부 필드
        private int _movementCycleIndex; // Chameleon 같은 순환 이동 기물의 현재 단계
        private readonly List<RuntimeStatusEffect> _statusEffects = new List<RuntimeStatusEffect>(); // 27일차: 현재 걸려 있는 상태 이상 목록

        public PieceDefinition Definition { get; } // 이 기물이 참조하는 고정 데이터

        public Vector2Int BoardPosition // 현재 보드 좌표 프로퍼티
        {
            get => _boardPosition; // 현재 좌표 반환
            set // 실제 이동으로 좌표가 바뀔 때 호출
            {
                if (_boardPosition == value) return; // 같은 좌표 재대입은 상태를 변경하지 않음
                _boardPosition = value; // 새 위치 저장
                AdvanceMovementCycle(); // 실제 위치 이동이 발생하면 Chameleon 순환 단계를 한 단계 진행
            }
        }

        public bool IsPlayerPiece { get; set; } // 아군 기물 여부
        public bool IsSelected { get; set; } // 현재 선택된 상태인지 여부
        public bool CanMove { get; set; } = true; // 이동 가능 여부
        public bool CanAttack { get; set; } = true; // 공격 가능 여부
        public bool IsDead => _currentHp <= 0; // 체력이 0 이하이면 사망 상태
        public int MovementCycleIndex => _movementCycleIndex; // 외부 이동 규칙이 읽는 현재 순환 단계
        public IReadOnlyList<RuntimeStatusEffect> StatusEffects => _statusEffects; // 27일차: 외부에서 읽는 현재 상태 이상 목록

        public int CurrentHp // 현재 체력 프로퍼티
        {
            get => _currentHp; // 현재 체력 반환
            set => _currentHp = Mathf.Max(0, value); // 음수로 내려가지 않도록 제한
        }

        public PieceRuntimeState(PieceDefinition definition, Vector2Int boardPosition, bool isPlayerPiece) // 런타임 상태 생성자
        {
            Definition = definition; // 고정 데이터 참조 저장
            _boardPosition = boardPosition; // 생성 시 시작 위치는 이동으로 취급하지 않고 직접 저장
            IsPlayerPiece = isPlayerPiece; // 아군 여부 저장
            _currentHp = definition != null ? definition.BaseHp : 0; // 기물 데이터가 있으면 기본 체력으로 초기화
            _movementCycleIndex = 0; // Chameleon은 Knight 단계부터 시작
        }

        public void AdvanceMovementCycle() // Chameleon 이동 후 다음 이동 능력으로 진행하는 메서드
        {
            if (Definition == null || Definition.PieceId != "chameleon") return; // 카멜레온이 아니면 순환 상태를 사용하지 않음
            _movementCycleIndex = (_movementCycleIndex + 1) % 4; // Knight→Bishop→Rook→Queen→Knight 순환
        }

        public void RestoreMovementCycleIndex(int index) // 저장 데이터에서 순환 단계를 복원하는 메서드
        {
            _movementCycleIndex = Mathf.Abs(index) % 4; // 잘못된 값도 0~3 범위로 보정
        }

        public bool ApplyStatus(StatusEffectDefinition statusDefinition) // 27일차: 상태 이상을 새로 걸거나 재적용하는 메서드
        {
            if (statusDefinition == null) return false; // 정의가 없으면 아무 것도 하지 않음

            if (Definition != null && (Definition.ImmuneStatusTags & statusDefinition.StatusType) != 0) // 이 기물이 해당 상태에 면역이면
            {
                return false; // 부여되지 않음
            }

            var existing = FindStatus(statusDefinition.StatusType); // 이미 걸려 있는 같은 상태가 있는지 조회
            if (existing != null) // 이미 걸려 있으면
            {
                existing.Reapply(); // 지속 턴 갱신(및 중첩형이면 중첩 증가)
            }
            else // 처음 걸리는 상태라면
            {
                _statusEffects.Add(new RuntimeStatusEffect(statusDefinition)); // 새 상태 이상 추가
            }

            return true; // 부여 성공
        }

        public bool HasStatus(StatusEffectType statusType) // 특정 상태 이상 보유 여부를 확인하는 메서드
        {
            return FindStatus(statusType) != null; // 목록에서 찾아지면 보유 중
        }

        public RuntimeStatusEffect FindStatus(StatusEffectType statusType) // 특정 상태 이상의 현재 상태를 조회하는 메서드
        {
            for (int i = 0; i < _statusEffects.Count; i++) // 걸려 있는 상태를 순회
            {
                if (_statusEffects[i].Definition.StatusType == statusType) // 종류가 일치하면
                {
                    return _statusEffects[i]; // 해당 상태 반환
                }
            }

            return null; // 없으면 null 반환
        }

        public void RemoveStatus(StatusEffectType statusType) // 특정 상태 이상을 강제로 제거하는 메서드
        {
            _statusEffects.RemoveAll(effect => effect.Definition.StatusType == statusType); // 종류가 일치하는 상태를 모두 제거
        }

        public void TickStatusEffects() // 턴 종료 시 모든 상태 이상의 지속 턴을 감소시키고 만료된 항목을 제거하는 메서드
        {
            for (int i = _statusEffects.Count - 1; i >= 0; i--) // 제거 중 인덱스가 어긋나지 않도록 뒤에서부터 순회
            {
                if (_statusEffects[i].Tick()) // 지속 턴을 감소시키고 만료됐으면
                {
                    _statusEffects.RemoveAt(i); // 목록에서 제거
                }
            }
        }

        public void RestoreStatusEffect(StatusEffectDefinition statusDefinition, int remainingTurns, int stackCount) // 저장 데이터로부터 상태 이상 1건을 그대로 복원하는 메서드
        {
            if (statusDefinition == null) return; // 정의를 찾지 못했으면 복원하지 않음

            var effect = new RuntimeStatusEffect(statusDefinition); // 기본값으로 상태 생성
            effect.RestoreState(remainingTurns, stackCount); // 저장된 지속 턴·중첩 수로 덮어쓰기
            _statusEffects.Add(effect); // 목록에 추가
        }
    }
}
