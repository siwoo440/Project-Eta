using System; // 안정적인 카드 ID 정렬
using System.Collections.Generic; // 카드·레시피 목록
using ProjectEta.Pieces; // 실제 카드 정의
using ProjectEta.Fusion; // 알려진 합성 경로
using ProjectEta.Meta; // 런 고정 해금

namespace ProjectEta.Run // 이벤트 카드 제약
{ // 영역 시작
    public static class RunEventCardRules // 1성 지급·선택 대상 검사
    { // 타입 시작
        public static List<PieceDefinition> Targets(RunState run, RunEventChoice choice) // 선택 가능한 보유 카드
        { // 메서드 시작
            var result = new List<PieceDefinition>(); // 유효 대상 목록
            if (run == null || choice == null || !choice.NeedsTarget) return result; // 대상 없는 효과 제외
            foreach (PieceDefinition card in run.Deck.OwnedCardPool) // 실제 정상 보유 카드
            { // 반복 시작
                if (!ShopService.IsManageableCard(card)) continue; // 왕·적·보스 제외
                if ((choice.cardOperation == RunEventCardOperation.Remove || choice.cardOperation == RunEventCardOperation.Exchange) && card.Grade != PieceGrade.OneStar) continue; // 제거·교환은 1성만
                if ((long)card.BaseHp + choice.hpUpgrade > int.MaxValue || (long)card.BaseAtk + choice.atkUpgrade > int.MaxValue) continue; // 강화 수치 오버플로 차단
                result.Add(card); // 실제 선택 대상
            } // 반복 종료
            return result; // 원본 순서와 참조 유지
        } // 메서드 종료
        public static List<PieceDefinition> Rewards(RunState run, IReadOnlyList<PieceDefinition> catalog, RunContentUnlockSnapshot unlocks, IReadOnlyList<FusionRecipe> recipes, bool linked, string excludeId = null) // 해금된 1성 후보
        { // 메서드 시작
            var result = new List<PieceDefinition>(); // 지급 후보
            if (run == null || catalog == null || unlocks == null || !string.Equals(unlocks.RunId, run.RunId, StringComparison.Ordinal)) return result; // 다른 런 해금으로 지급 금지
            var seen = new HashSet<string>(StringComparer.Ordinal); // 같은 ID 후보 중복 방지
            foreach (PieceDefinition card in catalog) // 실제 플레이어 카드 풀
            { // 반복 시작
                if (card == null || card.Grade != PieceGrade.OneStar || !ShopService.IsManageableCard(card)) continue; // 플레이어 1성만 지급
                if (string.Equals(card.PieceId, excludeId, StringComparison.Ordinal) || !seen.Add(card.PieceId)) continue; // 자기 자신 교환·중복 ID 제외
                if (!MetaContentAvailabilityService.IsPieceAvailable(card, unlocks) || !CardRewardRules.CanOffer(card, run.Deck.OwnedCardPool, run.Deck.DeadCardPile)) continue; // 해금·정상과 사망 합산 상한
                if (CardRewardRules.CountOwnedCopies(run.Deck.OwnedCardPool, card.PieceId) + CardRewardRules.CountOwnedCopies(run.Deck.DeadCardPile, card.PieceId) >= 3) continue; // 이벤트 기획의 명시적 세 장 상한
                if (linked && !IsKnownMaterial(run, recipes, card.PieceId)) continue; // 발견한 경로의 1성 재료만
                result.Add(card); // 유효 카드 후보
            } // 반복 종료
            result.Sort((left, right) => string.CompareOrdinal(left.PieceId, right.PieceId)); // 카탈로그 순서와 독립된 Seed 결과
            return result; // 최종 지급 후보
        } // 메서드 종료
        private static bool IsKnownMaterial(RunState run, IReadOnlyList<FusionRecipe> recipes, string pieceId) // 공개·발견 레시피 재료 검사
        { // 메서드 시작
            if (recipes == null) return false; // 레시피 누락 시 다른 카드로 대체 금지
            foreach (FusionRecipe recipe in recipes) // 연결된 실제 레시피
            { // 반복 시작
                if (recipe == null || !run.FusionDiscovery.IsDiscovered(recipe)) continue; // 미발견 숨김 경로 제외
                if (recipe.MaterialA != null && recipe.MaterialA.PieceId == pieceId) return true; // 첫 재료 연결
                if (recipe.MaterialB != null && recipe.MaterialB.PieceId == pieceId) return true; // 두 번째 재료 연결
            } // 반복 종료
            return false; // 알려진 연결 없음
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
