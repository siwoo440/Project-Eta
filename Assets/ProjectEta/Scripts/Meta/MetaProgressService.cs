using UnityEngine; // RuntimeInitializeOnLoadMethod 사용

namespace ProjectEta.Meta
{
    public static class MetaProgressService
    {
        private static MetaProgressState _current; // 현재 세션 영구 진행 상태 캐시

        public static MetaProgressState Current
        {
            get
            {
                if (_current == null) _current = MetaProgressSaveService.LoadFromDisk(); // 최초 접근 시 디스크 로드
                return _current; // 현재 영구 진행 상태 반환
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            _current = null; // 플레이 세션 재시작 시 디스크 재로드 준비
        }

        public static bool Save()
        {
            return MetaProgressSaveService.SaveToDisk(Current); // 현재 영구 진행 상태 저장
        }

        public static void Reload()
        {
            _current = MetaProgressSaveService.LoadFromDisk(); // 현재 저장 파일 다시 읽기
        }

        public static void SetCurrentForTests(MetaProgressState state)
        {
            _current = state; // EditMode 테스트용 상태 교체
        }
    }
}
