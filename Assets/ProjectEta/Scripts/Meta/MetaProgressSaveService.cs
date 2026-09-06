using System.IO; // JSON 파일 읽기·쓰기 사용
using UnityEngine; // Application·JsonUtility·Debug 사용

namespace ProjectEta.Meta
{
    public static class MetaProgressSaveService
    {
        private const string SaveDirectoryName = "ProjectEta"; // 영구 진행 저장 폴더 이름
        private const string SaveFileName = "meta_progress.json"; // 영구 진행 저장 파일 이름

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveDirectoryName, SaveFileName); // 플랫폼별 영구 저장 경로

        public static string Serialize(MetaProgressState state)
        {
            MetaProgressState safeState = state ?? new MetaProgressState(); // null 상태 기본 객체 보정
            return JsonUtility.ToJson(safeState.ToSaveData(), true); // 읽기 쉬운 JSON 문자열 반환
        }

        public static MetaProgressState Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new MetaProgressState(); // 빈 JSON 기본 상태 반환

            try
            {
                MetaProgressSaveData data = JsonUtility.FromJson<MetaProgressSaveData>(json); // JSON 저장 DTO 복원
                return MetaProgressState.FromSaveData(data); // 영구 진행 상태 복원
            }
            catch
            {
                return new MetaProgressState(); // 손상 JSON 기본 상태 복구
            }
        }

        public static MetaProgressState LoadFromDisk()
        {
            if (!File.Exists(SavePath)) return new MetaProgressState(); // 최초 실행 기본 상태 반환

            try
            {
                return Deserialize(File.ReadAllText(SavePath)); // 영구 진행 JSON 읽기·복원
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"48일차 메타 진행 불러오기 실패: {exception.Message}"); // 저장 파일 읽기 실패 기록
                return new MetaProgressState(); // 실패 시 기본 상태 반환
            }
        }

        public static bool SaveToDisk(MetaProgressState state)
        {
            string directory = Path.GetDirectoryName(SavePath); // 저장 폴더 경로 조회
            string temporaryPath = SavePath + ".tmp"; // 임시 저장 파일 경로 생성

            try
            {
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory); // 영구 저장 폴더 보장
                File.WriteAllText(temporaryPath, Serialize(state)); // 임시 JSON 파일 기록
                File.Copy(temporaryPath, SavePath, true); // 실제 저장 파일 교체
                File.Delete(temporaryPath); // 임시 파일 정리
                return true; // 저장 성공 반환
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"48일차 메타 진행 저장 실패: {exception.Message}"); // 저장 실패 원인 기록
                if (File.Exists(temporaryPath))
                {
                    try { File.Delete(temporaryPath); } catch { } // 실패 후 임시 파일 정리
                }
                return false; // 저장 실패 반환
            }
        }
    }
}
