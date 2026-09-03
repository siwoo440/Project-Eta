using UnityEngine; // Vector2Int, Mathf 등을 사용하기 위한 네임스페이스

namespace ProjectEta.Pieces // 기물 관련 타입을 모아두는 네임스페이스
{
    public class PieceRuntimeState // 보드 위 기물 1개의 가변(런타임) 상태를 담는 클래스
    {
        private int _currentHp; // 현재 체력을 저장하는 내부 필드

        public PieceDefinition Definition { get; } // 이 기물이 참조하는 고정 데이터
        public Vector2Int BoardPosition { get; set; } // 현재 보드 좌표
        public bool IsPlayerPiece { get; set; } // 아군 기물 여부
        public bool IsSelected { get; set; } // 현재 선택된 상태인지 여부
        public bool CanMove { get; set; } = true; // 이동 가능 여부
        public bool CanAttack { get; set; } = true; // 공격 가능 여부
        public bool IsDead => _currentHp <= 0; // 체력이 0 이하이면 사망 상태로 판정

        public int CurrentHp // 현재 체력 프로퍼티
        {
            get => _currentHp; // 현재 체력 값을 반환
            set => _currentHp = Mathf.Max(0, value); // 음수로 내려가지 않도록 0 이상으로 제한해 저장
        }

        public PieceRuntimeState(PieceDefinition definition, Vector2Int boardPosition, bool isPlayerPiece) // 기물 런타임 상태 생성자
        {
            Definition = definition; // 고정 데이터 참조 저장
            BoardPosition = boardPosition; // 시작 좌표 저장
            IsPlayerPiece = isPlayerPiece; // 아군 여부 저장
            _currentHp = definition.BaseHp; // 고정 데이터의 기본 체력으로 현재 체력 초기화
        }
    }
}
