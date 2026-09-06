using System.Collections.Generic; // IEnumerable·List·HashSet 사용

namespace ProjectEta.Settings
{
    public readonly struct SettingsResolutionOption
    {
        public int Width { get; } // 해상도 너비
        public int Height { get; } // 해상도 높이

        public SettingsResolutionOption(int width, int height)
        {
            Width = width; // 해상도 너비 저장
            Height = height; // 해상도 높이 저장
        }

        public override string ToString()
        {
            return $"{Width} × {Height}"; // 설정 UI 표시 문자열 반환
        }
    }

    public static class GameSettingsResolutionCatalog
    {
        public static SettingsResolutionOption[] DeduplicateAndSort(IEnumerable<SettingsResolutionOption> candidates)
        {
            var result = new List<SettingsResolutionOption>(); // 고유 해상도 결과 목록
            var keys = new HashSet<string>(); // 중복 판정 키 목록

            if (candidates != null)
            {
                foreach (SettingsResolutionOption candidate in candidates)
                {
                    if (candidate.Width < GameSettingsData.MinimumWidth || candidate.Height < GameSettingsData.MinimumHeight) continue; // 최소 해상도 미만 제외
                    string key = $"{candidate.Width}x{candidate.Height}"; // 너비·높이 고유 키 생성
                    if (!keys.Add(key)) continue; // 동일 크기 중복 제외
                    result.Add(candidate); // 고유 해상도 후보 등록
                }
            }

            result.Sort((left, right) =>
            {
                long leftPixels = (long)left.Width * left.Height; // 왼쪽 총 픽셀 수 계산
                long rightPixels = (long)right.Width * right.Height; // 오른쪽 총 픽셀 수 계산
                int pixelCompare = rightPixels.CompareTo(leftPixels); // 높은 픽셀 수 우선 비교
                if (pixelCompare != 0) return pixelCompare; // 픽셀 수 차이 정렬 반환
                int widthCompare = right.Width.CompareTo(left.Width); // 같은 픽셀 수 너비 비교
                if (widthCompare != 0) return widthCompare; // 너비 차이 정렬 반환
                return right.Height.CompareTo(left.Height); // 마지막 높이 비교 반환
            });

            return result.ToArray(); // 정렬 완료 배열 반환
        }
    }
}
