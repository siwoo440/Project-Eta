using System; // JSON 저장 지정
using UnityEngine; // 점유 크기

namespace ProjectEta.Boss // 보스 저장 영역
{ // 영역 시작
    public enum BossVariant // 같은 정체성의 전투 형태
    { // 열거 시작
        Base = 0, // 깊이 5 기본형
        Enhanced = 1 // 깊이 10 강화형
    } // 열거 종료
    [Serializable] // 런 저장 포함
    public sealed class BossSelectionSnapshot // 선택 당시의 실제 보스 기준
    { // 타입 시작
        public string bossId; // 기획상 고유 ID
        public string pieceId; // 전투·저장 기물 ID
        public string displayName; // 선택 당시 표시 이름
        public int phase; // 선택한 페이즈
        public int depth; // 선택한 보스 깊이
        public BossVariant variant; // 기본·강화 형태
        public int maxHp; // 선택 당시 최대 체력
        public int attack; // 선택 당시 공격력
        public Vector2Int size; // 선택 당시 점유 크기
        public string templateResource; // 공통 행동 기물 원형
        public int immuneStatusTags; // 선택 당시 상태 면역
        public bool provisional; // 임시 수치 여부
        public bool bespokePatternsImplemented; // 전용 패턴 연결 여부
        public string plannedMovement; // 후속 이동 사양
        public string plannedSpecial; // 후속 특수 행동 사양
        public string plannedEnhanced; // 후속 강화형 사양
        public bool IsValid => !string.IsNullOrWhiteSpace(bossId) && pieceId == "boss101_" + bossId + "_" + (int)variant && !string.IsNullOrWhiteSpace(displayName) && phase >= 0 && phase <= 5 && depth >= 1 && depth <= 10 && (variant == BossVariant.Base || variant == BossVariant.Enhanced) && maxHp > 0 && maxHp <= 9999 && attack >= 0 && attack <= 999 && size.x >= 1 && size.x <= 2 && size.y >= 1 && size.y <= 2 && !string.IsNullOrWhiteSpace(templateResource); // 저장 값 유효성
        public string VariantLabel => variant == BossVariant.Enhanced ? "강화형" : "기본형"; // 화면 형태 안내
    } // 타입 종료
} // 영역 종료
