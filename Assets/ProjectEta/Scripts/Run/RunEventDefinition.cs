using System; // 저장 직렬화
using System.Collections.Generic; // ID 중복 검사
using UnityEngine; // 독립 정의 복사

namespace ProjectEta.Run // 런 이벤트 데이터
{ // 영역 시작
    public enum RunEventCardOperation // 카드 처리 종류
    { // 열거 시작
        None = 0, // 카드 변화 없음
        Gain = 1, // 해금 1성 획득
        LinkedGain = 2, // 발견 합성 재료 획득
        Remove = 3, // 선택 1성 제거
        Exchange = 4 // 다른 해금 1성 교환
    } // 열거 종료
    [Serializable] // 선택 결과 저장
    public sealed class RunEventChoice // 기획 선택지의 수치
    { // 타입 시작
        public string id; // 선택지 ID
        public string title; // 선택 제목
        public string description; // 비용과 효과 안내
        public int goldCost; // 지불 Gold
        public int goldGain; // 획득 Gold
        public int kingCost; // 왕 HP 비용
        public int kingHeal; // 왕 HP 회복
        public int hpUpgrade; // 대상 카드 HP 증가
        public int atkUpgrade; // 대상 카드 ATK 증가
        public RunEventCardOperation cardOperation; // 카드 효과
        public bool information; // 선택 단서 후보 열람
        public bool NeedsTarget => hpUpgrade > 0 || atkUpgrade > 0 || cardOperation == RunEventCardOperation.Remove || cardOperation == RunEventCardOperation.Exchange; // 직접 고를 카드 필요 여부
        public bool IsValid => !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(title) && goldCost >= 0 && goldGain >= 0 && kingCost >= 0 && kingCost <= 1 && kingHeal >= 0 && kingHeal <= 1 && hpUpgrade >= 0 && hpUpgrade <= 1 && atkUpgrade >= 0 && atkUpgrade <= 1 && (int)cardOperation >= 0 && (int)cardOperation <= 4; // 잘못된 수치 차단
    } // 타입 종료
    [Serializable] // 카탈로그와 선택 스냅샷
    public sealed class RunEventDefinition // 기획 이벤트 정의
    { // 타입 시작
        public string id; // EV01부터 EV35
        public string title; // 기획 이벤트 이름
        public string description; // 이벤트 유형 안내
        public int phase; // 공통 0 또는 전용 페이즈
        public int weight; // 임시 등장 가중치
        public RunEventChoice[] choices; // 기획 선택지
        public bool IsValid // 실행 가능한 정의 검사
        { // 속성 시작
            get // 검사값 조회
            { // 조회 시작
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title) || phase < 0 || phase > 5 || weight <= 0 || choices == null || choices.Length != 3) return false; // 기본 필수값 검사
                var ids = new HashSet<string>(StringComparer.Ordinal); // 선택지 ID 중복 차단
                foreach (RunEventChoice choice in choices) // 모든 선택지 검사
                { // 반복 시작
                    if (choice == null || !choice.IsValid || !ids.Add(choice.id)) return false; // 손상 선택지 차단
                } // 반복 종료
                return true; // 유효한 정의
            } // 조회 종료
        } // 속성 종료
        public RunEventDefinition Copy() // 선택 시점 정의 독립 복사
        { // 메서드 시작
            return JsonUtility.FromJson<RunEventDefinition>(JsonUtility.ToJson(this)); // 이후 카탈로그 변경 분리
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
