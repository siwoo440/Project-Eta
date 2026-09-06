using System; // Serializable 사용
using System.Collections.Generic; // HashSet<T>·List<T>·IReadOnlyCollection<T> 사용
using ProjectEta.Meta; // MetaProgressState·MetaUnlockType 사용

namespace ProjectEta.Run
{
    [Serializable]
    public sealed class RunContentUnlockSnapshotSaveData
    {
        public string runId; // Snapshot 대상 런 ID
        public List<string> unlockedPieceIds = new List<string>(); // 런 시작 시 해금 기물 ID 목록
        public List<string> unlockedKingIds = new List<string>(); // 런 시작 시 해금 킹 ID 목록
        public List<string> unlockedPassiveIds = new List<string>(); // 런 시작 시 해금 패시브 ID 목록
    }

    public sealed class RunContentUnlockSnapshot
    {
        private readonly HashSet<string> _unlockedPieceIds = new HashSet<string>(StringComparer.Ordinal); // 런 고정 기물 해금 집합
        private readonly HashSet<string> _unlockedKingIds = new HashSet<string>(StringComparer.Ordinal); // 런 고정 킹 해금 집합
        private readonly HashSet<string> _unlockedPassiveIds = new HashSet<string>(StringComparer.Ordinal); // 런 고정 패시브 해금 집합

        public string RunId { get; private set; } = string.Empty; // Snapshot 대상 런 ID
        public IReadOnlyCollection<string> UnlockedPieceIds => _unlockedPieceIds; // 런 고정 기물 해금 목록
        public IReadOnlyCollection<string> UnlockedKingIds => _unlockedKingIds; // 런 고정 킹 해금 목록
        public IReadOnlyCollection<string> UnlockedPassiveIds => _unlockedPassiveIds; // 런 고정 패시브 해금 목록

        public static RunContentUnlockSnapshot Capture(string runId, MetaProgressState progress)
        {
            var snapshot = new RunContentUnlockSnapshot
            {
                RunId = runId ?? string.Empty // 현재 런 ID 저장
            };

            if (progress == null) return snapshot; // 영구 진행 누락 시 기본 콘텐츠 전용 Snapshot 반환

            CopyToSet(snapshot._unlockedPieceIds, progress.UnlockedPieceIds); // 현재 기물 해금 상태 고정
            CopyToSet(snapshot._unlockedKingIds, progress.UnlockedKingIds); // 현재 킹 해금 상태 고정
            CopyToSet(snapshot._unlockedPassiveIds, progress.UnlockedPassiveIds); // 현재 패시브 해금 상태 고정
            return snapshot; // 새 런 해금 Snapshot 반환
        }

        public bool IsUnlocked(MetaUnlockType type, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return false; // 빈 영구 해금 ID 차단
            return GetUnlockSet(type).Contains(unlockId); // 런 시작 시점 해금 여부 반환
        }

        public RunContentUnlockSnapshotSaveData ToSaveData()
        {
            var data = new RunContentUnlockSnapshotSaveData
            {
                runId = RunId, // 런 ID 저장
                unlockedPieceIds = new List<string>(_unlockedPieceIds), // 기물 Snapshot 저장
                unlockedKingIds = new List<string>(_unlockedKingIds), // 킹 Snapshot 저장
                unlockedPassiveIds = new List<string>(_unlockedPassiveIds) // 패시브 Snapshot 저장
            };

            data.unlockedPieceIds.Sort(StringComparer.Ordinal); // 기물 ID 정렬
            data.unlockedKingIds.Sort(StringComparer.Ordinal); // 킹 ID 정렬
            data.unlockedPassiveIds.Sort(StringComparer.Ordinal); // 패시브 ID 정렬
            return data; // Snapshot 저장 DTO 반환
        }

        public static RunContentUnlockSnapshot FromSaveData(RunContentUnlockSnapshotSaveData data)
        {
            var snapshot = new RunContentUnlockSnapshot(); // 빈 런 Snapshot 생성
            if (data == null) return snapshot; // 저장 데이터 누락 시 빈 Snapshot 반환

            snapshot.RunId = data.runId ?? string.Empty; // 저장 런 ID 복원
            RestoreSet(snapshot._unlockedPieceIds, data.unlockedPieceIds); // 기물 해금 Snapshot 복원
            RestoreSet(snapshot._unlockedKingIds, data.unlockedKingIds); // 킹 해금 Snapshot 복원
            RestoreSet(snapshot._unlockedPassiveIds, data.unlockedPassiveIds); // 패시브 해금 Snapshot 복원
            return snapshot; // 복원 완료 Snapshot 반환
        }

        private HashSet<string> GetUnlockSet(MetaUnlockType type)
        {
            if (type == MetaUnlockType.King) return _unlockedKingIds; // 킹 해금 집합 반환
            if (type == MetaUnlockType.Passive) return _unlockedPassiveIds; // 패시브 해금 집합 반환
            return _unlockedPieceIds; // 기본 기물 해금 집합 반환
        }

        private static void CopyToSet(HashSet<string> target, IReadOnlyCollection<string> source)
        {
            if (target == null || source == null) return; // 복사 대상·원본 누락 방어

            foreach (string value in source)
            {
                if (!string.IsNullOrWhiteSpace(value)) target.Add(value); // 정상 영구 해금 ID 복사
            }
        }

        private static void RestoreSet(HashSet<string> target, List<string> source)
        {
            if (target == null || source == null) return; // 복원 대상·원본 누락 방어

            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i]; // 현재 저장 해금 ID 조회
                if (!string.IsNullOrWhiteSpace(value)) target.Add(value); // 정상 영구 해금 ID 복원
            }
        }
    }
}
