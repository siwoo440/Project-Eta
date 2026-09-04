using UnityEngine; // Vector2Int, Mathf 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class PieceRuntimeState // 보드 위 기물 1개의 가변 상태를 담는 클래스
    {
        private int _currentHp; // 현재 체력을 저장하는 내부 필드
        private Vector2Int _boardPosition; // 현재 보드 위치를 저장하는 내부 필드
        private int _movementCycleIndex; // Chameleon 같은 순환 이동 기물의 현재 단계

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
    }
}
