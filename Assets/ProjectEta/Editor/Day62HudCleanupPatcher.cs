using System; // 문자열 위치 탐색 사용
using System.IO; // 소스 파일 읽기·쓰기 사용
using System.Text; // UTF-8 인코딩 사용
using UnityEditor; // 에디터 로드·에셋 재임포트 사용
using UnityEngine; // 패치 결과 로그 사용

namespace ProjectEta.EditorTools
{
    [InitializeOnLoad]
    public static class Day62HudCleanupPatcher
    {
        private const string TargetPath = "Assets/ProjectEta/Scripts/Board/BoardInputController.cs"; // 전투 디버그 IMGUI 원본 경로
        private const string MethodStartToken = "        private void OnGUI()"; // 디버그 IMGUI 메서드 시작 토큰
        private const string NextMethodToken = "        private string BuildTurnInputLabel()"; // 다음 메서드 시작 토큰
        private const string PatchedMarker = "개발용 전투 디버그 오버레이 숨김"; // 중복 패치 확인 문구

        static Day62HudCleanupPatcher()
        {
            EditorApplication.delayCall += ApplyCleanup; // 에디터 초기화 뒤 소스 패치 예약
        }

        private static void ApplyCleanup()
        {
            if (!File.Exists(TargetPath)) return; // 대상 소스가 없으면 패치 생략

            string source = File.ReadAllText(TargetPath); // 현재 BoardInputController 소스 읽기
            if (source.IndexOf(PatchedMarker, StringComparison.Ordinal) >= 0) return; // 이미 적용된 소스 재패치 차단

            int methodStart = source.IndexOf(MethodStartToken, StringComparison.Ordinal); // OnGUI 시작 위치 탐색
            if (methodStart < 0) return; // OnGUI가 없으면 패치 생략

            int nextMethodStart = source.IndexOf(NextMethodToken, methodStart, StringComparison.Ordinal); // 다음 메서드 시작 위치 탐색
            if (nextMethodStart < 0) return; // 예상 소스 구조가 아니면 안전하게 중단

            const string replacement =
                "        private void OnGUI() // 개발용 전투 디버그 IMGUI 표시를 차단하는 메서드\n" +
                "        {\n" +
                "            return; // 개발용 전투 디버그 오버레이 숨김\n" +
                "        }\n\n"; // 비표시 OnGUI 교체 코드 구성

            string patchedSource = source.Substring(0, methodStart) + replacement + source.Substring(nextMethodStart); // 디버그 OnGUI 본문 교체
            File.WriteAllText(TargetPath, patchedSource, new UTF8Encoding(false)); // BOM 없는 UTF-8로 변경 소스 저장
            AssetDatabase.ImportAsset(TargetPath, ImportAssetOptions.ForceUpdate); // 수정된 런타임 스크립트 즉시 재임포트
            Debug.Log("Day62 HUD 정리: 좌측 상단 BoardInputController 디버그 오버레이를 비활성화했습니다."); // 자동 패치 완료 기록
        }
    }
}
