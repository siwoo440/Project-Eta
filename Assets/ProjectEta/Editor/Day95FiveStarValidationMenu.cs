#if UNITY_EDITOR // 에디터 전용 도구
using System; // 오류 보고
using System.Linq; // 능력 수 집계
using UnityEditor; // 에디터 메뉴
using UnityEditor.Build.Reporting; // 빌드 결과
using UnityEngine; // 로그 기록
using ProjectEta.Pieces; // 기물 데이터

namespace ProjectEta.EditorTools // 95일차 에디터 검증 영역
{ // 범위 시작
    public static class Day95FiveStarValidationMenu // 콘텐츠 확인과 개발 빌드
    { // 범위 시작
        [MenuItem("Project Eta/Day 95/Validate Five Star Content")] // 수동 확인 메뉴
        public static void ValidateContent() // 5성 능력 누락 확인
        { // 범위 시작
            var database = AssetDatabase.LoadAssetAtPath<PieceDatabase>("Assets/ProjectEta/Data/PieceDatabase.asset"); // 실제 등록 데이터 조회
            if (database == null) // 데이터베이스 누락 확인
            { // 범위 시작
                throw new InvalidOperationException("PieceDatabase 누락"); // 명확한 검증 오류
            } // 범위 종료
            var pieces = database.Definitions.Where(p => p != null && p.Grade == PieceGrade.FiveStar).ToArray(); // 플레이어 5성 목록
            if (pieces.Length != 8 || pieces.Any(p => p.Abilities.Length == 0 || p.Abilities.Any(a => a == null))) // 개수와 연결 검증
            { // 범위 시작
                throw new InvalidOperationException("5성 8종 능력 연결 오류"); // 누락 능력 보고
            } // 범위 종료
            Debug.Log("[Day95] 5성 8종 능력 " + pieces.Sum(p => p.Abilities.Length) + "개 연결 확인"); // 확인 결과 기록
        } // 범위 종료

        public static void BuildWindowsDevelopment() // 배치 실행용 개발 빌드
        { // 범위 시작
            ValidateContent(); // 빌드 전 데이터 검증
            string path = System.Environment.GetEnvironmentVariable("PROJECT_ETA_DAY95_BUILD"); // 임시 빌드 출력 경로
            if (string.IsNullOrEmpty(path)) // 잘못된 출력 경로 확인
            { // 범위 시작
                throw new InvalidOperationException("PROJECT_ETA_DAY95_BUILD 경로 누락"); // 출력 경로 오류
            } // 범위 종료
            var options = new BuildPlayerOptions // 기존 출시 설정을 바꾸지 않는 빌드 옵션
            { // 범위 시작
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), // 현재 등록된 씬 사용
                locationPathName = path, // 지정한 임시 실행 파일 경로
                target = BuildTarget.StandaloneWindows64, // Windows 64비트 대상
                options = BuildOptions.Development // 개발용 빌드
            }; // 범위 종료
            var report = BuildPipeline.BuildPlayer(options); // 전체 플레이어 빌드
            if (report.summary.result != BuildResult.Succeeded) // 빌드 실패 확인
            { // 범위 시작
                throw new InvalidOperationException("Day95 개발 빌드 실패: " + report.summary.result); // 실패 결과 보고
            } // 범위 종료
            Debug.Log("[Day95] Windows 개발 빌드 성공: " + path); // 빌드 결과 기록
        } // 범위 종료
    } // 범위 종료
} // 범위 종료
#endif // 에디터 전용 도구 종료
