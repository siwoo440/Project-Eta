using System; // Random 사용
using System.Collections.Generic; // List·HashSet·IReadOnlyList 사용
using UnityEngine; // Resources 사용
using Random = System.Random; // UnityEngine.Random과 충돌하지 않도록 시스템 난수 별칭 지정
using ProjectEta.Cards; // PlayerStartingDeckCatalog 사용
using ProjectEta.Meta; // Meta Progress 기반 가용성 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class CardRewardGenerator
    {
        private const string UnlockablePieceCatalogResourceName = "PlayerUnlockablePiecePool86"; // 86일차 신규 1성 보충 풀
        private static PlayerStartingDeckCatalog _unlockablePieceCatalog; // 신규 해금 기물 리소스 캐시

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed)
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(MetaProgressService.Current); // 현재 런 Snapshot 조회
            return Generate(sourcePool, ownedCards, candidateCount, seed, snapshot);
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot)
        {
            return GenerateUniform(sourcePool, ownedCards, null, candidateCount, seed, snapshot);
        }

        private static IReadOnlyList<PieceDefinition> GenerateUniform(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot)
        {
            List<PieceDefinition> eligible = BuildEligible(sourcePool, ownedCards, deadCards, snapshot); // 전체 후보 생성
            var random = new Random(seed); // 재현 가능한 난수 생성

            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1); // 교환 위치 결정
                PieceDefinition temporary = eligible[i];
                eligible[i] = eligible[swapIndex];
                eligible[swapIndex] = temporary;
            }

            int safeCount = Math.Max(0, Math.Min(candidateCount, eligible.Count)); // 후보 수 보정
            return eligible.GetRange(0, safeCount);
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, CardRewardProfile profile)
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(MetaProgressService.Current); // 현재 런 Snapshot 조회
            return Generate(sourcePool, ownedCards, candidateCount, seed, snapshot, profile);
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, int candidateCount, int seed, CardRewardProfile profile)
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(MetaProgressService.Current); // 현재 런 Snapshot 조회
            return Generate(sourcePool, ownedCards, deadCards, candidateCount, seed, snapshot, profile);
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot, CardRewardProfile profile)
        {
            return Generate(sourcePool, ownedCards, null, candidateCount, seed, snapshot, profile);
        }

        private static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot, CardRewardProfile profile)
        {
            if (profile == null) return GenerateUniform(sourcePool, ownedCards, deadCards, candidateCount, seed, snapshot);

            List<PieceDefinition> remaining = BuildEligible(sourcePool, ownedCards, deadCards, snapshot); // 획득 가능 후보 생성
            int safeCount = Math.Max(0, Math.Min(candidateCount, remaining.Count));
            var result = new List<PieceDefinition>(safeCount);
            var random = new Random(seed);

            while (result.Count < safeCount && remaining.Count > 0)
            {
                int selectedIndex = SelectWeightedIndex(remaining, profile, random);
                result.Add(remaining[selectedIndex]);
                remaining.RemoveAt(selectedIndex);
            }

            return result;
        }

        private static List<PieceDefinition> BuildEligible(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, RunContentUnlockSnapshot snapshot)
        {
            return BuildEligible(sourcePool, ownedCards, null, snapshot);
        }

        private static List<PieceDefinition> BuildEligible(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, RunContentUnlockSnapshot snapshot)
        {
            var eligible = new List<PieceDefinition>(); // 최종 후보 목록
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal); // PieceId 중복 차단

            if (sourcePool == null) return eligible; // 기존 null 입력 동작 유지

            AddEligibleFromPool(sourcePool, ownedCards, deadCards, snapshot, uniqueIds, eligible); // 기존 26종 소스 검사

            PlayerStartingDeckCatalog unlockableCatalog = GetUnlockablePieceCatalog(); // 86일차 신규 6종 로드
            if (unlockableCatalog != null)
            {
                AddEligibleFromPool(unlockableCatalog.Cards, ownedCards, deadCards, snapshot, uniqueIds, eligible); // 해금된 신규 1성만 병합
            }

            return eligible;
        }

        private static void AddEligibleFromPool(
            IReadOnlyList<PieceDefinition> sourcePool,
            IReadOnlyList<PieceDefinition> ownedCards,
            IReadOnlyList<PieceDefinition> deadCards,
            RunContentUnlockSnapshot snapshot,
            HashSet<string> uniqueIds,
            List<PieceDefinition> eligible)
        {
            if (sourcePool == null) return;

            for (int i = 0; i < sourcePool.Count; i++)
            {
                PieceDefinition definition = sourcePool[i]; // 현재 카드 정의
                if (!MetaContentAvailabilityService.IsPieceAvailable(definition, snapshot)) continue; // 미해금 신규 기물 제외
                if (!CardRewardRules.CanOffer(definition, ownedCards, deadCards)) continue; // 1성·보유 제한 검사
                if (!uniqueIds.Add(definition.PieceId)) continue; // 같은 PieceId 중복 제외
                eligible.Add(definition); // 정상 후보 등록
            }
        }

        private static PlayerStartingDeckCatalog GetUnlockablePieceCatalog()
        {
            if (_unlockablePieceCatalog != null) return _unlockablePieceCatalog; // 기존 캐시 재사용
            _unlockablePieceCatalog = Resources.Load<PlayerStartingDeckCatalog>(UnlockablePieceCatalogResourceName); // 신규 6종 리소스 로드
            return _unlockablePieceCatalog;
        }

        private static int SelectWeightedIndex(IReadOnlyList<PieceDefinition> candidates, CardRewardProfile profile, Random random)
        {
            int totalWeight = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i];
                if (definition == null) continue;
                totalWeight += profile.GetGradeWeight(definition.Grade);
            }

            if (totalWeight <= 0) return random.Next(candidates.Count);

            int roll = random.Next(totalWeight);
            int cumulative = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i];
                if (definition == null) continue;
                cumulative += profile.GetGradeWeight(definition.Grade);
                if (roll < cumulative) return i;
            }

            return candidates.Count - 1;
        }
    }
}
