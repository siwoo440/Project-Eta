using System.IO; // 파일 읽기/쓰기를 사용하기 위한 네임스페이스
using UnityEngine; // Application, JsonUtility를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDatabase를 사용하기 위한 네임스페이스

namespace ProjectEta.Run // 런(플레이 세션) 관련 타입을 모아두는 네임스페이스
{
    public static class RunSaveSystem // 런 상태를 파일로 저장/복원하는 정적 클래스
    {
        private const string SaveFileName = "run_save.json"; // 저장 파일 이름

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName); // 실제 저장 경로 계산

        public static void Save(RunState runState) // 런 상태를 파일에 저장하는 메서드
        {
            var data = runState.ToSaveData(); // 런 상태를 저장용 데이터로 변환
            var json = JsonUtility.ToJson(data, true); // 저장용 데이터를 JSON 문자열로 변환
            File.WriteAllText(SavePath, json); // JSON 문자열을 파일에 기록
        }

        public static bool TryLoad(PieceDatabase database, out RunState runState) // 파일에서 런 상태를 불러오는 메서드
        {
            if (!File.Exists(SavePath)) // 저장 파일이 없으면
            {
                runState = null; // 결과를 null로 설정
                return false; // 불러오기 실패 반환
            }

            var json = File.ReadAllText(SavePath); // 저장 파일 내용을 문자열로 읽기
            var data = JsonUtility.FromJson<RunSaveData>(json); // JSON 문자열을 저장용 데이터로 변환
            runState = RunState.FromSaveData(data, database); // 저장용 데이터로 런 상태 복원
            return true; // 불러오기 성공 반환
        }
    }
}
