using System; // 저장 직렬화

namespace ProjectEta.Run // 이벤트 저장 데이터
{ // 영역 시작
    [Serializable] // 런 저장 포함
    public sealed class RunEventSession // 현재 노드의 고정 이벤트
    { // 타입 시작
        public string key; // 페이즈·깊이·노드 키
        public int seed; // 이벤트와 보상 Seed
        public RunEventDefinition definition; // 진입 시점 선택지 복사
        public bool resolved; // 적용 완료 여부
        public RunEventRecord result; // 다시 표시할 실제 결과
    } // 타입 종료
    [Serializable] // 완료 이력 저장
    public sealed class RunEventRecord // 재지급 방지와 실제 손익
    { // 타입 시작
        public string key; // 완료 노드 식별
        public string eventId; // 완료 이벤트 ID
        public string eventTitle; // 완료 이벤트 이름
        public string choiceId; // 확정 선택 ID
        public string choiceTitle; // 확정 선택 제목
        public int goldBefore; // 선택 전 Gold
        public int goldAfter; // 선택 후 Gold
        public int kingBefore; // 선택 전 왕 HP
        public int kingAfter; // 선택 후 왕 HP
        public string addedCardId; // 획득한 카드 ID
        public string addedCardName; // 획득한 카드 이름
        public string removedCardId; // 제거한 카드 ID
        public string removedCardName; // 제거한 카드 이름
        public string targetName; // 강화 대상 이름
        public int hpUpgrade; // 실제 카드 HP 증가
        public int atkUpgrade; // 실제 카드 ATK 증가
        public string informationId; // 선택 단서 후보 ID
        public bool newInformation; // 이번에 처음 열람 여부
    } // 타입 종료
} // 영역 종료
