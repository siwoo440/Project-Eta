using System.Collections.Generic; // HashSet<T>·IReadOnlyCollection<T> 사용

namespace ProjectEta.Meta
{
    public sealed class MetaProgressState
    {
        private readonly HashSet<string> _unlockedPieceIds = new HashSet<string>(); // 영구 해금 기물 ID 집합
        private readonly HashSet<string> _unlockedKingIds = new HashSet<string>(); // 영구 해금 킹 ID 집합
        private readonly HashSet<string> _unlockedPassiveIds = new HashSet<string>(); // 영구 해금 패시브 ID 집합
        private readonly HashSet<string> _claimedRunRewardIds = new HashSet<string>(); // 메타 보상 지급 완료 런 ID 집합

        public int MetaTokens { get; private set; } // 런 밖에 유지되는 메타 토큰
        public IReadOnlyCollection<string> UnlockedPieceIds => _unlockedPieceIds; // 영구 해금 기물 목록
        public IReadOnlyCollection<string> UnlockedKingIds => _unlockedKingIds; // 영구 해금 킹 목록
        public IReadOnlyCollection<string> UnlockedPassiveIds => _unlockedPassiveIds; // 영구 해금 패시브 목록
        public IReadOnlyCollection<string> ClaimedRunRewardIds => _claimedRunRewardIds; // 지급 완료 런 ID 목록

        public void AddTokens(int amount)
        {
            if (amount <= 0) return; // 0 이하 지급 차단
            MetaTokens += amount; // 메타 토큰 영구 잔액 증가
        }

        public bool TrySpendTokens(int amount)
        {
            if (amount < 0 || MetaTokens < amount) return false; // 잘못된 비용·잔액 부족 차단
            MetaTokens -= amount; // 메타 토큰 비용 차감
            return true; // 비용 지불 성공 반환
        }

        public bool IsUnlocked(MetaUnlockType type, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return false; // 빈 해금 ID 차단
            return GetUnlockSet(type).Contains(unlockId); // 타입별 영구 해금 여부 반환
        }

        public bool Unlock(MetaUnlockType type, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return false; // 빈 해금 ID 차단
            return GetUnlockSet(type).Add(unlockId); // 신규 영구 해금 등록
        }

        public bool IsRunRewardClaimed(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false; // 빈 런 ID 미지급 취급
            return _claimedRunRewardIds.Contains(runId); // 런 메타 보상 지급 여부 반환
        }

        public bool TryClaimRunReward(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false; // 빈 런 ID 보상 Claim 차단
            return _claimedRunRewardIds.Add(runId); // 최초 런만 Claim 성공
        }

        public MetaProgressSaveData ToSaveData()
        {
            var data = new MetaProgressSaveData
            {
                version = MetaProgressSaveData.CurrentVersion, // 최신 메타 저장 버전 기록
                metaTokens = MetaTokens, // 영구 토큰 기록
                unlockedPieceIds = new List<string>(_unlockedPieceIds), // 기물 해금 기록
                unlockedKingIds = new List<string>(_unlockedKingIds), // 킹 해금 기록
                unlockedPassiveIds = new List<string>(_unlockedPassiveIds), // 패시브 해금 기록
                claimedRunRewardIds = new List<string>(_claimedRunRewardIds) // 런 보상 Claim 이력 기록
            };

            data.unlockedPieceIds.Sort(); // 기물 ID 정렬
            data.unlockedKingIds.Sort(); // 킹 ID 정렬
            data.unlockedPassiveIds.Sort(); // 패시브 ID 정렬
            data.claimedRunRewardIds.Sort(); // 런 보상 ID 정렬
            return data; // 저장 객체 반환
        }

        public static MetaProgressState FromSaveData(MetaProgressSaveData data)
        {
            var state = new MetaProgressState(); // 빈 영구 진행 상태 생성
            if (data == null) return state; // 저장 데이터 누락 시 기본 상태 반환

            state.MetaTokens = data.metaTokens < 0 ? 0 : data.metaTokens; // 음수 메타 토큰 보정
            RestoreSet(state._unlockedPieceIds, data.unlockedPieceIds); // 기물 해금 목록 복원
            RestoreSet(state._unlockedKingIds, data.unlockedKingIds); // 킹 해금 목록 복원
            RestoreSet(state._unlockedPassiveIds, data.unlockedPassiveIds); // 패시브 해금 목록 복원
            RestoreSet(state._claimedRunRewardIds, data.claimedRunRewardIds); // 런 보상 Claim 이력 복원
            return state; // 복원 상태 반환
        }

        private HashSet<string> GetUnlockSet(MetaUnlockType type)
        {
            if (type == MetaUnlockType.King) return _unlockedKingIds; // 킹 해금 집합 반환
            if (type == MetaUnlockType.Passive) return _unlockedPassiveIds; // 패시브 해금 집합 반환
            return _unlockedPieceIds; // 기본 기물 해금 집합 반환
        }

        private static void RestoreSet(HashSet<string> target, List<string> source)
        {
            if (target == null || source == null) return; // 복원 대상·원본 누락 방어

            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i]; // 현재 저장 ID 조회
                if (!string.IsNullOrWhiteSpace(value)) target.Add(value); // 정상 ID 복원
            }
        }
    }
}
