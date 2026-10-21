using System; // ID 비교와 오류 보고
using System.Collections.Generic; // 보스 목록
using UnityEngine; // Resources 조회

namespace ProjectEta.Boss // 보스 콘텐츠 영역
{ // 영역 시작
    public static class BossCatalog // 15종 등록과 페이즈 후보 조회
    { // 타입 시작
        private static BossDefinition[] _all; // 등록된 보스 목록
        public static IReadOnlyList<BossDefinition> All => _all ?? (_all = Load()); // 등록 목록 재사용
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 새 플레이 초기화
        private static void ResetCache() // 이전 에셋 참조 해제
        { // 메서드 시작
            _all = null; // 다음 조회 재로드
        } // 메서드 종료
        private static BossDefinition[] Load() // 실제 보스 에셋 로드
        { // 메서드 시작
            BossDefinition[] definitions = Resources.LoadAll<BossDefinition>("Bosses101"); // 등록 폴더 조회
            Array.Sort(definitions, (a, b) => string.CompareOrdinal(a.bossId, b.bossId)); // 리소스 로드 순서 독립
            var ids = new HashSet<string>(StringComparer.Ordinal); // 중복 ID 검사
            foreach (BossDefinition definition in definitions) // 등록 에셋 순회
            { // 반복 시작
                if (!definition.IsValid || !ids.Add(definition.bossId)) throw new InvalidOperationException("보스 데이터 오류: " + definition.name); // 잘못된 등록 차단
            } // 반복 종료
            return definitions; // 정렬된 유효 목록
        } // 메서드 종료
        public static BossDefinition Find(string id) // ID 기반 기획 데이터 조회
        { // 메서드 시작
            foreach (BossDefinition definition in All) // 전체 목록 순회
            { // 반복 시작
                if (string.Equals(definition.bossId, id, StringComparison.Ordinal)) return definition; // 일치 보스 반환
            } // 반복 종료
            return null; // 미등록 ID 제외
        } // 메서드 종료
        public static List<BossDefinition> GetPool(int phase) // 페이즈 후보 목록 생성
        { // 메서드 시작
            var result = new List<BossDefinition>(); // 독립 후보 목록
            foreach (BossDefinition definition in All) // 등록 보스 순회
            { // 반복 시작
                if (definition.phase == phase) result.Add(definition); // 같은 페이즈 후보 추가
            } // 반복 종료
            return result; // 수정 가능한 독립 목록
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
