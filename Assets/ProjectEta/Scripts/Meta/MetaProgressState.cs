using System.Collections.Generic; // HashSet<T>·IReadOnlyCollection<T>·List<T> 사용

namespace ProjectEta.Meta
{
    public sealed class MetaProgressState
    {
        private readonly HashSet<string> _unlockedPieceIds = new HashSet<string>(); // 영구 해금 기물 ID 집합
        private readonly HashSet<string> _unlockedKingIds = new HashSet<string>(); // 영구 해금 킹 ID 집합
        private readonly HashSet<string> _unlockedPassiveIds = new HashSet<string>(); // 영구 해금 패시브 ID 집합
        private readonly HashSet<string> _claimedRunRewardIds = new HashSet<string>(); // 메타 보상 지급 완료 런 ID 집합
        private readonly HashSet<string> _discoveredFusionRecipeIds = new HashSet<string>(); // 89일차: 영구 Fusion Atlas 발견 ID 집합

        public int MetaTokens { get; private set; } // 런 밖에 유지되는 메타 토큰
        public IReadOnlyCollection<string> UnlockedPieceIds => _unlockedPieceIds; // 영구 해금 기물 목록
        public IReadOnlyCollection<string> UnlockedKingIds => _unlockedKingIds; // 영구 해금 킹 목록
        public IReadOnlyCollection<string> UnlockedPassiveIds => _unlockedPassiveIds; // 영구 해금 패시브 목록
        public IReadOnlyCollection<string> ClaimedRunRewardIds => _claimedRunRewardIds; // 지급 완료 런 ID 목록
        public IReadOnlyCollection<string> DiscoveredFusionRecipeIds => _discoveredFusionRecipeIds; // 영구 발견 숨김 Recipe 목록

        public void AddTokens(int amount)
        {
            if (amount <= 0) return;
            MetaTokens += amount;
        }

        public bool TrySpendTokens(int amount)
        {
            if (amount < 0 || MetaTokens < amount) return false;
            MetaTokens -= amount;
            return true;
        }

        public bool IsUnlocked(MetaUnlockType type, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return false;
            return GetUnlockSet(type).Contains(unlockId);
        }

        public bool Unlock(MetaUnlockType type, string unlockId)
        {
            if (string.IsNullOrWhiteSpace(unlockId)) return false;
            return GetUnlockSet(type).Add(unlockId);
        }

        public bool IsRunRewardClaimed(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false;
            return _claimedRunRewardIds.Contains(runId);
        }

        public bool TryClaimRunReward(string runId)
        {
            if (string.IsNullOrWhiteSpace(runId)) return false;
            return _claimedRunRewardIds.Add(runId);
        }

        public bool IsFusionRecipeDiscovered(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            return _discoveredFusionRecipeIds.Contains(recipeId);
        }

        public bool TryDiscoverFusionRecipe(string recipeId)
        {
            if (string.IsNullOrWhiteSpace(recipeId)) return false;
            return _discoveredFusionRecipeIds.Add(recipeId);
        }

        public int MergeDiscoveredFusionRecipes(IEnumerable<string> recipeIds)
        {
            if (recipeIds == null) return 0;
            int added = 0;

            foreach (string recipeId in recipeIds)
            {
                if (TryDiscoverFusionRecipe(recipeId)) added++;
            }

            return added;
        }

        public MetaProgressSaveData ToSaveData()
        {
            var data = new MetaProgressSaveData
            {
                version = MetaProgressSaveData.CurrentVersion,
                metaTokens = MetaTokens,
                unlockedPieceIds = new List<string>(_unlockedPieceIds),
                unlockedKingIds = new List<string>(_unlockedKingIds),
                unlockedPassiveIds = new List<string>(_unlockedPassiveIds),
                claimedRunRewardIds = new List<string>(_claimedRunRewardIds),
                discoveredFusionRecipeIds = new List<string>(_discoveredFusionRecipeIds)
            };

            data.unlockedPieceIds.Sort();
            data.unlockedKingIds.Sort();
            data.unlockedPassiveIds.Sort();
            data.claimedRunRewardIds.Sort();
            data.discoveredFusionRecipeIds.Sort();
            return data;
        }

        public static MetaProgressState FromSaveData(MetaProgressSaveData data)
        {
            var state = new MetaProgressState();
            if (data == null) return state;

            state.MetaTokens = data.metaTokens < 0 ? 0 : data.metaTokens;
            RestoreSet(state._unlockedPieceIds, data.unlockedPieceIds);
            RestoreSet(state._unlockedKingIds, data.unlockedKingIds);
            RestoreSet(state._unlockedPassiveIds, data.unlockedPassiveIds);
            RestoreSet(state._claimedRunRewardIds, data.claimedRunRewardIds);
            RestoreSet(state._discoveredFusionRecipeIds, data.discoveredFusionRecipeIds); // v1·v2에서는 null이므로 빈 도감으로 호환 복원
            return state;
        }

        private HashSet<string> GetUnlockSet(MetaUnlockType type)
        {
            if (type == MetaUnlockType.King) return _unlockedKingIds;
            if (type == MetaUnlockType.Passive) return _unlockedPassiveIds;
            return _unlockedPieceIds;
        }

        private static void RestoreSet(HashSet<string> target, List<string> source)
        {
            if (target == null || source == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                string value = source[i];
                if (!string.IsNullOrWhiteSpace(value)) target.Add(value);
            }
        }
    }
}
