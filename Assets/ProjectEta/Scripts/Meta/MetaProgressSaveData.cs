using System; // Serializable 사용
using System.Collections.Generic; // List<T> 사용

namespace ProjectEta.Meta
{
    [Serializable]
    public sealed class MetaProgressSaveData
    {
        public const int CurrentVersion = 1; // 48일차 메타 저장 포맷 버전

        public int version = CurrentVersion; // 저장 포맷 버전
        public int metaTokens; // 영구 메타 토큰
        public List<string> unlockedPieceIds = new List<string>(); // 영구 해금 기물 ID
        public List<string> unlockedKingIds = new List<string>(); // 영구 해금 킹 ID
        public List<string> unlockedPassiveIds = new List<string>(); // 영구 해금 패시브 ID
    }
}
