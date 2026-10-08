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
        { // 기존 호출 연결
            return GenerateBalanced(sourcePool, ownedCards, deadCards, candidateCount, seed, snapshot, profile, RunBalanceProfile.Current); // 실제 재료 설정 적용
        } // 기존 호출 종료

        public static IReadOnlyList<PieceDefinition> GenerateBalanced(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot, CardRewardProfile profile, RunBalanceProfile balanceProfile) // 재현 가능한 재료별 후보 생성
        { // 범위 시작
            if (profile == null) // 품질 가중치 누락 확인
            { // 범위 시작
                return GenerateUniform(sourcePool, ownedCards, deadCards, candidateCount, seed, snapshot); // 기존 균등 후보 생성 호환
            } // 범위 종료

            List<PieceDefinition> remaining = BuildEligible(sourcePool, ownedCards, deadCards, snapshot); // 획득 가능 후보 생성
            int safeCount = Math.Max(0, Math.Min(candidateCount, remaining.Count)); // 요청 수와 실제 후보 수 제한
            var result = new List<PieceDefinition>(safeCount); // 추출 결과 목록 준비
            var random = new Random(seed); // 동일 Seed 재현 난수 준비

            while (result.Count < safeCount && remaining.Count > 0) // 요청 수까지 중복 없는 후보 선택
            { // 범위 시작
                int selectedIndex = SelectWeightedIndex(remaining, profile, random, balanceProfile ?? RunBalanceProfile.Current); // 등급과 재료 가중치 적용 선택
                result.Add(remaining[selectedIndex]); // 선택한 기물 후보 추가
                remaining.RemoveAt(selectedIndex); // 같은 후보 재선택 제외
            } // 범위 종료

            return result; // 확정된 후보 목록 반환
        } // 범위 종료


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

        private static int SelectWeightedIndex(IReadOnlyList<PieceDefinition> candidates, CardRewardProfile profile, Random random, RunBalanceProfile balanceProfile)
        {
            long totalWeight = 0; // 가중치 합계 오버플로 방지

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i];
                if (definition == null) continue;
                totalWeight += (long)Mathf.Clamp(profile.GetGradeWeight(definition.Grade), 0, 10000) * balanceProfile.GetMaterialWeight(definition); // 등급과 재료별 가중치 결합
            }

            if (totalWeight <= 0) return random.Next(candidates.Count);

            long roll = (long)(random.NextDouble() * totalWeight); // 동일 Seed의 안전한 선택 난수
            long cumulative = 0; // 안전 누적 가중치

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i];
                if (definition == null) continue;
                cumulative += (long)Mathf.Clamp(profile.GetGradeWeight(definition.Grade), 0, 10000) * balanceProfile.GetMaterialWeight(definition); // 동일한 재료 가중치 누적
                if (roll < cumulative) return i;
            }

            return candidates.Count - 1;
        }
    }
}
