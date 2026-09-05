using System; // Random 사용
using System.Collections.Generic; // List<T>·HashSet<T>·IReadOnlyList<T> 사용
using ProjectEta.Pieces; // PieceDefinition 사용

namespace ProjectEta.Run // 카드 보상 후보 생성 네임스페이스
{
    public static class CardRewardGenerator // 획득 가능한 카드 풀에서 중복 없는 후보를 생성하는 46일차 생성기
    {
        public static IReadOnlyList<PieceDefinition> Generate(IReadOnlyList<PieceDefinition> sourcePool, IReadOnlyList<PieceDefinition> ownedCards, int candidateCount, int seed) // 카드 보상 후보 생성
        {
            var eligible = new List<PieceDefinition>(); // 획득 가능 후보 임시 목록
            var uniqueIds = new HashSet<string>(StringComparer.Ordinal); // 동일 PieceId 후보 중복 차단 집합

            if (sourcePool != null) // 원본 카드 풀 존재 확인
            {
                for (int i = 0; i < sourcePool.Count; i++) // 원본 카드 순회
                {
                    PieceDefinition definition = sourcePool[i]; // 현재 카드 정의 조회
                    if (!CardRewardRules.CanOffer(definition, ownedCards)) continue; // 획득 불가 카드 제외
                    if (!uniqueIds.Add(definition.PieceId)) continue; // 동일 카드 중복 후보 제외
                    eligible.Add(definition); // 정상 후보 등록
                }
            }

            var random = new Random(seed); // 현재 보상용 독립 난수 생성기
            for (int i = eligible.Count - 1; i > 0; i--) // Fisher-Yates 셔플 순회
            {
                int swapIndex = random.Next(i + 1); // 교환 위치 결정
                PieceDefinition temporary = eligible[i]; // 현재 카드 임시 저장
                eligible[i] = eligible[swapIndex]; // 랜덤 카드 현재 위치 이동
                eligible[swapIndex] = temporary; // 현재 카드 랜덤 위치 이동
            }

            int safeCount = Math.Max(0, Math.Min(candidateCount, eligible.Count)); // 실제 생성 가능한 후보 수 보정
            var result = new List<PieceDefinition>(safeCount); // 최종 후보 목록 생성

            for (int i = 0; i < safeCount; i++) // 필요한 후보 수만 순회
            {
                result.Add(eligible[i]); // 셔플된 상위 후보 추가
            }

            return result; // 중복 없는 카드 후보 반환
        }
    }
}
