using System.Collections.Generic; // 진단 목록 사용

namespace ProjectEta.SceneFlow // 씬 초기화 네임스페이스
{ // 네임스페이스 범위
    public readonly struct RuntimeComponentStatus // 단일 관리자 개수 정보
    { // 구조체 범위
        public RuntimeComponentStatus(string name, int count) // 관리자 상태 생성자
        { // 생성자 범위
            Name = name ?? string.Empty; // 안전한 관리자 이름 저장
            Count = count < 0 ? 0 : count; // 음수 개수 방지
        } // 생성자 종료

        public string Name { get; } // 관리자 이름
        public int Count { get; } // 현재 객체 개수
    } // 구조체 종료

    public sealed class BattleRuntimeDiagnostics // Battle 초기화 진단 결과
    { // 클래스 범위
        private static readonly BattleRuntimeDiagnostics EmptyValue = Create(string.Empty, 0f, new RuntimeComponentStatus[0]); // 빈 진단 결과

        private BattleRuntimeDiagnostics(string sceneName, float elapsedMilliseconds, int requiredCount, int readyCount, string[] missingNames, string[] duplicateNames) // 진단 결과 생성자
        { // 생성자 범위
            SceneName = sceneName ?? string.Empty; // 진단 씬 이름 저장
            ElapsedMilliseconds = elapsedMilliseconds < 0f ? 0f : elapsedMilliseconds; // 음수 시간 방지
            RequiredCount = requiredCount; // 필수 관리자 수 저장
            ReadyCount = readyCount; // 준비 관리자 수 저장
            MissingNames = missingNames; // 누락 관리자 목록 저장
            DuplicateNames = duplicateNames; // 중복 관리자 목록 저장
        } // 생성자 종료

        public static BattleRuntimeDiagnostics Empty => EmptyValue; // 빈 진단 결과 제공
        public string SceneName { get; } // 진단 씬 이름
        public float ElapsedMilliseconds { get; } // 초기화 소요 시간
        public int RequiredCount { get; } // 필수 관리자 수
        public int ReadyCount { get; } // 준비 관리자 수
        public IReadOnlyList<string> MissingNames { get; } // 누락 관리자 목록
        public IReadOnlyList<string> DuplicateNames { get; } // 중복 관리자 목록
        public bool IsHealthy => RequiredCount > 0 && MissingNames.Count == 0 && DuplicateNames.Count == 0; // 전체 정상 여부

        public static BattleRuntimeDiagnostics Create(string sceneName, float elapsedMilliseconds, IReadOnlyList<RuntimeComponentStatus> statuses) // 관리자 상태 집계
        { // 메서드 범위
            int requiredCount = statuses != null ? statuses.Count : 0; // 필수 관리자 수 계산
            int readyCount = 0; // 준비 관리자 수 초기화
            var missingNames = new List<string>(); // 누락 관리자 목록 생성
            var duplicateNames = new List<string>(); // 중복 관리자 목록 생성

            for (int index = 0; index < requiredCount; index++) // 관리자 상태 전체 순회
            { // 반복 범위
                RuntimeComponentStatus status = statuses[index]; // 현재 관리자 상태 읽기
                if (status.Count > 0) readyCount++; // 존재 관리자 수 증가
                if (status.Count == 0) missingNames.Add(status.Name); // 누락 관리자 기록
                if (status.Count > 1) duplicateNames.Add($"{status.Name} ×{status.Count}"); // 중복 관리자 기록
            } // 반복 종료

            return new BattleRuntimeDiagnostics(sceneName, elapsedMilliseconds, requiredCount, readyCount, missingNames.ToArray(), duplicateNames.ToArray()); // 집계 결과 반환
        } // 메서드 종료
    } // 클래스 종료
} // 네임스페이스 종료
