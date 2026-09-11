using System; // Random 사용
using System.Collections.Generic; // List<T>·HashSet<T>·IReadOnlyList<T> 사용
using ProjectEta.Meta; // Meta Progress 기반 런 콘텐츠 가용성 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run
{
    public static class CardRewardGenerator
    {
        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed)
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(MetaProgressService.Current); // 현재 런 해금 Snapshot 조회
            return Generate(sourcePool, ownedCards, candidateCount, seed, snapshot); // 기존 균등 후보 생성 유지
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot)
        {
            List<PieceDefinition> eligible = BuildEligible(sourcePool, ownedCards, snapshot); // 기존 획득 가능 후보 생성
            var random = new Random(seed); // 현재 보상 독립 난수 생성기

            for (int i = eligible.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1); // 교환 위치 결정
                PieceDefinition temporary = eligible[i]; // 현재 카드 임시 저장
                eligible[i] = eligible[swapIndex]; // 랜덤 카드 현재 위치 이동
                eligible[swapIndex] = temporary; // 현재 카드 랜덤 위치 이동
            }

            int safeCount = Math.Max(0, Math.Min(candidateCount, eligible.Count)); // 실제 생성 가능한 후보 수 보정
            return eligible.GetRange(0, safeCount); // 기존 균등 셔플 결과 반환
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, CardRewardProfile profile)
        {
            RunContentUnlockSnapshot snapshot = RunContentUnlockSnapshotService.GetOrCreateForActiveRun(MetaProgressService.Current); // 현재 런 해금 Snapshot 조회
            return Generate(sourcePool, ownedCards, candidateCount, seed, snapshot, profile); // 품질 기반 후보 생성 위임
        }

        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed, RunContentUnlockSnapshot snapshot, CardRewardProfile profile)
        {
            if (profile == null) return Generate(sourcePool, ownedCards, candidateCount, seed, snapshot); // 품질 누락 시 기존 생성 방식 유지

            List<PieceDefinition> remaining = BuildEligible(sourcePool, ownedCards, snapshot); // 현재 획득 가능한 전체 후보 생성
            int safeCount = Math.Max(0, Math.Min(candidateCount, remaining.Count)); // 실제 생성 가능한 후보 수 계산
            var result = new List<PieceDefinition>(safeCount); // 최종 가중 선택 결과
            var random = new Random(seed); // 동일 Seed 재현용 독립 난수 생성기

            while (result.Count < safeCount && remaining.Count > 0)
            {
                int selectedIndex = SelectWeightedIndex(remaining, profile, random); // Stage·Source 품질 기준 후보 위치 선택
                result.Add(remaining[selectedIndex]); // 선택 후보 결과 추가
                remaining.RemoveAt(selectedIndex); // 동일 PieceId 후보 재선택 방지
            }

            return result; // 품질 가중 후보 반환
        }

        private static List<PieceDefinition> BuildEligible(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, RunContentUnlockSnapshot snapshot)
        {
            var eligible = new List<PieceDefinition>(); // 획득 가능 후보 임시 목록
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal); // 동일 PieceId 후보 중복 차단 집합

            if (sourcePool == null) return eligible; // 빈 원본 풀 즉시 반환

            for (int i = 0; i < sourcePool.Count; i++)
            {
                PieceDefinition definition = sourcePool[i]; // 현재 카드 정의 조회
                if (!MetaContentAvailabilityService.IsPieceAvailable(definition, snapshot)) continue; // 잠긴 영구 해금 기물 제외
                if (!CardRewardRules.CanOffer(definition, ownedCards)) continue; // 일반 Reward 획득 불가 카드 제외
                if (!uniqueIds.Add(definition.PieceId)) continue; // 동일 카드 중복 후보 제외
                eligible.Add(definition); // 정상 후보 등록
            }

            return eligible; // 최종 획득 가능 후보 반환
        }

        private static int SelectWeightedIndex(IReadOnlyList<PieceDefinition> candidates, CardRewardProfile profile, Random random)
        {
            int totalWeight = 0; // 전체 후보 가중치 초기화

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i]; // 현재 후보 조회
                if (definition == null) continue; // 빈 후보 제외
                totalWeight += profile.GetGradeWeight(definition.Grade); // 현재 등급 가중치 누적
            }

            if (totalWeight <= 0) return random.Next(candidates.Count); // 품질 대상이 부족하면 전체 유효 풀에서 안전 fallback

            int roll = random.Next(totalWeight); // 전체 가중치 범위 난수 생성
            int cumulative = 0; // 누적 가중치 초기화

            for (int i = 0; i < candidates.Count; i++)
            {
                PieceDefinition definition = candidates[i]; // 현재 후보 조회
                if (definition == null) continue; // 빈 후보 제외
                cumulative += profile.GetGradeWeight(definition.Grade); // 현재 후보 누적 가중치 반영
                if (roll < cumulative) return i; // 난수 구간에 해당하는 후보 반환
            }

            return candidates.Count - 1; // 부동 조건 예외 시 마지막 후보 안전 반환
        }
    }
}
