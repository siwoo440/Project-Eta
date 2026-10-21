using UnityEngine; // 보스 데이터 에셋

namespace ProjectEta.Boss // 보스 콘텐츠 영역
{ // 영역 시작
    [CreateAssetMenu(menuName = "ProjectEta/Boss Definition")] // 보스 데이터 생성 메뉴
    public sealed class BossDefinition : ScriptableObject // 기획상 보스 정체성과 임시 전투 기준
    { // 타입 시작
        public string bossId; // 고유 보스 ID
        public string displayName; // 기획서 표시 이름
        public int phase; // 등장 페이즈·마스터 킹은 0
        public Vector2Int size = Vector2Int.one; // 실제 점유 크기
        public int baseHp = 16; // 임시 기본 체력
        public int baseAtk = 2; // 임시 기본 공격력
        public string templateResource = "MidBoss74"; // 기존 공통 행동 기물 원형
        public string plannedMovement; // 후속 전용 이동 사양
        public string plannedSpecial; // 후속 예고형 특수 행동 사양
        public string plannedEnhanced; // 후속 강화형 특수 규칙 사양
        public bool provisional = true; // 실측 전 임시 수치 표시
        public bool bespokePatternsImplemented; // 보스별 전용 패턴 연결 여부
        public bool IsValid => !string.IsNullOrWhiteSpace(bossId) && !string.IsNullOrWhiteSpace(displayName) && phase >= 0 && phase <= 5 && baseHp > 0 && baseAtk >= 0 && size.x >= 1 && size.x <= 2 && size.y >= 1 && size.y <= 2 && !string.IsNullOrWhiteSpace(templateResource); // 등록 데이터 유효성
    } // 타입 종료
} // 영역 종료
