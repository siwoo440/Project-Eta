using System.Collections.Generic; // 해금 원형 목록
using UnityEngine; // 플레이 세션 초기화

namespace ProjectEta.Run // 적 편성 개발 설정 영역
{ // 네임스페이스 시작
    public static class EnemyEncounterDebugOverrides // 다음 전투 편성 강제 선택 상태
    { // 클래스 시작
        public static string ForcedProfileId // 강제 적용할 원형 ID
        { // 속성 시작
            get; // 현재 강제 원형 조회
            private set; // 개발 도구 내부 변경
        } = string.Empty; // 자동 선택 기본값

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 플레이 세션 정적 상태 초기화
        private static void ResetRuntimeState() // 도메인 재사용 상태 정리
        { // 메서드 시작
            Clear(); // 자동 선택 복귀
        } // 메서드 종료

        public static string Cycle(StageType stageType, int phase, int direction) // 해금 원형 앞뒤 순환
        { // 메서드 시작
            IReadOnlyList<EnemyEncounterProfile> profiles = EnemyEncounterProfileCatalog.GetAvailableProfiles(stageType, phase); // 현재 해금 원형 조회
            if (profiles.Count == 0) // 지원하지 않는 스테이지 확인
            { // 조건 시작
                Clear(); // 잘못된 강제값 제거
                return ForcedProfileId; // 빈 선택 반환
            } // 조건 종료
            int currentIndex = -1; // 자동 선택 위치
            for (int index = 0; index < profiles.Count; index++) // 해금 원형 순회
            { // 반복 시작
                if (profiles[index].ProfileId == ForcedProfileId) // 현재 강제 원형 확인
                { // 조건 시작
                    currentIndex = index; // 현재 위치 저장
                    break; // 검색 종료
                } // 조건 종료
            } // 반복 종료
            int step = direction < 0 ? -1 : 1; // 순환 방향 보정
            int nextIndex = (currentIndex + step + profiles.Count) % profiles.Count; // 다음 안전 위치 계산
            ForcedProfileId = profiles[nextIndex].ProfileId; // 강제 원형 적용
            return ForcedProfileId; // 선택 결과 반환
        } // 메서드 종료

        public static void Set(string profileId) // 외부 개발 도구 원형 지정
        { // 메서드 시작
            ForcedProfileId = profileId ?? string.Empty; // null 없는 강제값 저장
        } // 메서드 종료

        public static bool Consume(string appliedProfileId) // 성공 적용된 다음 편성 강제값 소비
        { // 메서드 시작
            if (string.IsNullOrWhiteSpace(ForcedProfileId) || ForcedProfileId != appliedProfileId) return false; // 미적용 강제값 유지
            Clear(); // 한 번 적용한 강제값 제거
            return true; // 소비 성공 반환
        } // 메서드 종료

        public static void Clear() // 자동 원형 선택 복귀
        { // 메서드 시작
            ForcedProfileId = string.Empty; // 강제 원형 제거
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
