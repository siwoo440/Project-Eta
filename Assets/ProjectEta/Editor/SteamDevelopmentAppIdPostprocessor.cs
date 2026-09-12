using System.IO; // File·Path 사용
using UnityEditor; // BuildTarget·EditorUserBuildSettings 사용
using UnityEditor.Callbacks; // PostProcessBuild 사용

namespace ProjectEta.Editor
{
    public static class SteamDevelopmentAppIdPostprocessor
    {
        private const string SteamAppIdFileName = "steam_appid.txt"; // Steam 로컬 개발 AppID 파일명

        [PostProcessBuild(100)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (!EditorUserBuildSettings.development) return; // Release Build에는 steam_appid.txt를 복사하지 않음

            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), SteamAppIdFileName); // 프로젝트 루트 개발 AppID 파일 조회
            if (!File.Exists(sourcePath)) return; // 로컬 AppID 파일 없으면 복사 생략

            string destinationDirectory = ResolveDestinationDirectory(target, pathToBuiltProject); // 플랫폼별 실행 파일 위치 계산
            if (string.IsNullOrWhiteSpace(destinationDirectory)) return; // 잘못된 빌드 경로 차단

            Directory.CreateDirectory(destinationDirectory); // 대상 폴더 보장
            File.Copy(sourcePath, Path.Combine(destinationDirectory, SteamAppIdFileName), true); // Development Build 옆에 AppID 파일 복사
        }

        private static string ResolveDestinationDirectory(BuildTarget target, string pathToBuiltProject)
        {
            if (target == BuildTarget.StandaloneOSX)
            {
                return Path.Combine(pathToBuiltProject, "Contents", "MacOS"); // macOS 실행 바이너리 폴더 반환
            }

            return Path.GetDirectoryName(pathToBuiltProject); // Windows·Linux 실행 파일 폴더 반환
        }
    }
}
