using System; // Serializable 사용
using System.Collections.Generic; // List<T> 사용

namespace ProjectEta.Meta
{
    [Serializable]
    public sealed class MetaProgressSaveData
    {
        public const int CurrentVersion = 3; // 89일차: 영구 Fusion Atlas 발견 기록 포함

        public int version = CurrentVersion; // 저장 포맷 버전
        public int metaTokens; // 영구 메타 토큰
        public List<string> unlockedPieceIds = new List<string>(); // 영구 해금 기물 ID
        public List<string> unlockedKingIds = new List<string>(); // 영구 해금 킹 ID
        public List<string> unlockedPassiveIds = new List<string>(); // 영구 해금 패시브 ID
        public List<string> claimedRunRewardIds = new List<string>(); // 이미 메타 보상을 받은 런 고유 ID
        public List<string> discoveredFusionRecipeIds = new List<string>(); // 89일차: 런을 넘어 유지되는 숨김 합성식 발견 ID
    }
}
