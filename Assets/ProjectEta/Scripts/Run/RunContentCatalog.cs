using UnityEngine; // 리소스 데이터 참조
using ProjectEta.Pieces; // 기물과 상태 데이터

namespace ProjectEta.Run // 자동 불러오기 콘텐츠 영역
{ // 범위 시작
    [CreateAssetMenu(fileName = "RunContent", menuName = "ProjectEta/Run Content Catalog")] // 리소스 연결 에셋 메뉴
    public sealed class RunContentCatalog : ScriptableObject // 런 복원용 원본 데이터 참조
    { // 범위 시작
        [SerializeField] private PieceDatabase _pieceDatabase; // 기존 기물 데이터베이스 참조
        [SerializeField] private StatusEffectDatabase _statusEffectDatabase; // 공통 상태 데이터베이스 참조
        public PieceDatabase PieceDatabase => _pieceDatabase; // 기물 정의 조회 데이터
        public StatusEffectDatabase StatusEffectDatabase => _statusEffectDatabase; // 상태 이상 복원 데이터
    } // 범위 종료
} // 범위 종료
