using System; // 데이터 오류와 ID 비교
using System.Collections.Generic; // 이벤트 목록
using UnityEngine; // Resources JSON

namespace ProjectEta.Run // 기획 이벤트 풀
{ // 영역 시작
    public static class RunEventCatalog // 실제 EV01부터 EV35 데이터
    { // 타입 시작
        [Serializable] // JSON 최상위 구조
        private sealed class CatalogData // 이벤트 배열 묶음
        { // 타입 시작
            public RunEventDefinition[] events; // 파일의 전체 정의
        } // 타입 종료
        private static RunEventDefinition[] _events; // 검증 완료 카탈로그
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] // 도메인 재시작 없는 플레이 대응
        private static void Reset() // 이전 카탈로그 캐시 정리
        { // 메서드 시작
            _events = null; // 현재 Resources 다시 조회
        } // 메서드 종료
        public static IReadOnlyList<RunEventDefinition> All // 전체 정의 조회
        { // 속성 시작
            get // 카탈로그 조회
            { // 조회 시작
                if (_events != null) return _events; // 검증된 정의 재사용
                TextAsset asset = Resources.Load<TextAsset>("Events102"); // 실제 기획 데이터
                CatalogData data = asset != null ? JsonUtility.FromJson<CatalogData>(asset.text) : null; // JSON 구조 복원
                if (data?.events == null || data.events.Length != 35) throw new InvalidOperationException("EV01~EV35 데이터 누락"); // 불완전 데이터 실행 차단
                var ids = new HashSet<string>(StringComparer.Ordinal); // 이벤트 ID 검사
                foreach (RunEventDefinition definition in data.events) // 정의 순회
                { // 반복 시작
                    if (definition == null || !definition.IsValid || !ids.Add(definition.id)) throw new InvalidOperationException("이벤트 데이터 손상·중복"); // 잘못된 선택지 차단
                } // 반복 종료
                _events = data.events; // 검증 결과 보관
                return _events; // 실제 정의 반환
            } // 조회 종료
        } // 속성 종료
        public static RunEventDefinition Find(string id) // 저장 이벤트 조회
        { // 메서드 시작
            foreach (RunEventDefinition definition in All) // 실제 정의 순회
            { // 반복 시작
                if (string.Equals(definition.id, id, StringComparison.Ordinal)) return definition; // 같은 ID 정의
            } // 반복 종료
            return null; // 정의 없음
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
