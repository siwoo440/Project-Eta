using System; // Exception·StringComparison 사용
using System.IO; // Snapshot 파일 읽기·쓰기 사용
using UnityEngine; // Application·JsonUtility·RuntimeInitializeOnLoadMethod 사용
using ProjectEta.Battle; // BattleController 사용
using ProjectEta.Meta; // MetaProgressState 사용

namespace ProjectEta.Run
{
    public static class RunContentUnlockSnapshotService
    {
        private const string SnapshotFileName = "run_content_unlock_snapshot.json"; // 현재 런 영구 해금 Snapshot 파일 이름
        private static RunContentUnlockSnapshot _cachedSnapshot; // 현재 런 메모리 Snapshot

        private static string SnapshotPath => Path.Combine(Application.persistentDataPath, SnapshotFileName); // Snapshot 실제 저장 경로

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _cachedSnapshot = null; // 플레이 세션 시작 시 Snapshot 메모리 캐시 초기화
        }

        public static RunContentUnlockSnapshot GetOrCreateForActiveRun(MetaProgressState progress)
        {
            BattleController battleController = UnityEngine.Object.FindFirstObjectByType<BattleController>(); // 현재 BattleController 조회
            RunState runState = battleController != null ? battleController.RunState : null; // 현재 활성 RunState 조회
            return GetOrCreate(runState, progress); // 활성 런 또는 임시 Snapshot 반환
        }

        public static RunContentUnlockSnapshot GetOrCreate(RunState runState, MetaProgressState progress)
        {
            if (runState == null || string.IsNullOrWhiteSpace(runState.RunId))
            {
                return RunContentUnlockSnapshot.Capture(string.Empty, progress); // 활성 런 없음 시 저장하지 않는 임시 Snapshot 반환
            }

            string runId = runState.RunId; // 현재 런 고유 ID 조회

            if (_cachedSnapshot != null && string.Equals(_cachedSnapshot.RunId, runId, StringComparison.Ordinal))
            {
                return _cachedSnapshot; // 같은 런 메모리 Snapshot 재사용
            }

            if (TryLoad(runId, out RunContentUnlockSnapshot loaded))
            {
                _cachedSnapshot = loaded; // 이어하기 런 저장 Snapshot 캐시
                return _cachedSnapshot; // 기존 런 고정 해금 상태 반환
            }

            _cachedSnapshot = RunContentUnlockSnapshot.Capture(runId, progress); // 새 런 시작 시 현재 Meta Progress Snapshot 생성
            TrySave(_cachedSnapshot); // 이어하기에서도 같은 해금 상태를 유지하도록 별도 저장
            return _cachedSnapshot; // 새 런 Snapshot 반환
        }

        public static bool TrySave(RunContentUnlockSnapshot snapshot)
        {
            if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.RunId)) return false; // 저장 불가능 Snapshot 차단

            try
            {
                RunContentUnlockSnapshotSaveData data = snapshot.ToSaveData(); // Snapshot 저장 DTO 생성
                string json = JsonUtility.ToJson(data, true); // Snapshot JSON 생성
                string directory = Path.GetDirectoryName(SnapshotPath); // 저장 폴더 경로 조회
                if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory); // Snapshot 저장 폴더 생성 보장
                File.WriteAllText(SnapshotPath, json); // 현재 런 Snapshot 파일 저장
                return true; // Snapshot 저장 성공 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"59일차 런 콘텐츠 Snapshot 저장 실패: {exception.Message}"); // Snapshot 저장 오류 기록
                return false; // Snapshot 저장 실패 반환
            }
        }

        public static bool TryLoad(string runId, out RunContentUnlockSnapshot snapshot)
        {
            snapshot = null; // 기본 Snapshot 로드 결과 초기화
            if (string.IsNullOrWhiteSpace(runId) || !File.Exists(SnapshotPath)) return false; // 런 ID·파일 누락 차단

            try
            {
                string json = File.ReadAllText(SnapshotPath); // Snapshot JSON 읽기
                if (string.IsNullOrWhiteSpace(json)) return false; // 빈 Snapshot 파일 차단

                RunContentUnlockSnapshotSaveData data = JsonUtility.FromJson<RunContentUnlockSnapshotSaveData>(json); // Snapshot 저장 DTO 역직렬화
                if (data == null || !string.Equals(data.runId, runId, StringComparison.Ordinal)) return false; // 다른 런 Snapshot 재사용 차단

                snapshot = RunContentUnlockSnapshot.FromSaveData(data); // 현재 런 Snapshot 객체 복원
                return snapshot != null; // Snapshot 복원 성공 여부 반환
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"59일차 런 콘텐츠 Snapshot 읽기 실패: {exception.Message}"); // Snapshot 읽기 오류 기록
                snapshot = null; // 손상 Snapshot 노출 차단
                return false; // Snapshot 읽기 실패 반환
            }
        }

        public static void ClearRuntimeCache()
        {
            _cachedSnapshot = null; // 테스트·런 교체용 메모리 Snapshot 캐시 초기화
        }
    }
}
