using System; // ID 중복 비교
using System.Collections.Generic; // 후보 목록
using ProjectEta.Cards; // 카탈로그·보유 상한
using ProjectEta.Meta; // 고정 해금 판정
using ProjectEta.Pieces; // 기물 정의
using UnityEngine; // 후보 리소스 조회

namespace ProjectEta.Run // 콘텐츠 획득 안내 영역
{ // 영역 시작
    public static class RunContentEligibility // 후보 제외 사유 공통 판정
    { // 타입 시작
        public static string GetExclusionReason(PieceDefinition definition, IReadOnlyList<PieceDefinition> ownedCards, IReadOnlyList<PieceDefinition> deadCards, RunContentUnlockSnapshot snapshot) // 실제 획득 제한 안내
        { // 메서드 시작
            if (!RunContentPoolRules.IsValidPiece(definition)) // 유효 기물 확인
            { // 조건 시작
                return "기물 데이터 누락"; // 누락 사유 반환
            } // 조건 종료
            if (!RunContentPoolRules.CanUseAsReward(definition)) // 보상·상점 공통 정책 확인
            { // 조건 시작
                if (definition.MovementType == PieceMovementType.King) // 왕 전용 획득 확인
                { // 조건 시작
                    return "왕 선택 전용"; // 왕 제외 사유 반환
                } // 조건 종료
                if (definition.Category == PieceCategory.Monster || definition.Category == PieceCategory.Boss) // 적 전용 분류 확인
                { // 조건 시작
                    return "적 전용 기물"; // 적 제외 사유 반환
                } // 조건 종료
                return "합성으로 획득"; // 직접 획득 제외 사유 반환
            } // 조건 종료
            if (!MetaContentAvailabilityService.IsPieceAvailable(definition, snapshot)) // 런 시작 해금 상태 확인
            { // 조건 시작
                return "미해금 · 다음 런부터 해금 반영"; // 잠금 사유 반환
            } // 조건 종료
            if (!CardRewardRules.CanOffer(definition, ownedCards, deadCards)) // 정상·사망 포함 보유 상한 확인
            { // 조건 시작
                int limit = CardOwnershipRules.GetOwnedLimit(definition.Grade); // 등급별 상한 조회
                return $"보유 상한 {limit}장 도달 · 사망 포함"; // 상한 사유 반환
            } // 조건 종료
            return string.Empty; // 획득 가능 반환
        } // 메서드 종료
        public static IReadOnlyList<PieceDefinition> GetCandidatePool() // 실제 보상·상점의 전체 원본 후보 조회
        { // 메서드 시작
            var result = new List<PieceDefinition>(); // 후보 목록 생성
            var ids = new HashSet<string>(StringComparer.Ordinal); // 기물 ID 중복 차단
            AddCatalog("PlayerStartingDeck26", ids, result); // 기존 후보 추가
            AddCatalog("PlayerUnlockablePiecePool86", ids, result); // 신규 해금 후보 추가
            return result; // 실제 후보 원본 반환
        } // 메서드 종료
        public static int CountLocked(RunContentUnlockSnapshot snapshot) // 직접 획득 가능 기물의 미해금 수 조회
        { // 메서드 시작
            int count = 0; // 잠긴 기물 수 초기화
            foreach (PieceDefinition definition in GetCandidatePool()) // 실제 후보 순회
            { // 반복 시작
                if (RunContentPoolRules.CanUseAsReward(definition) && !MetaContentAvailabilityService.IsPieceAvailable(definition, snapshot)) // 정책 대상의 잠금 확인
                { // 조건 시작
                    count++; // 잠긴 후보 수 증가
                } // 조건 종료
            } // 반복 종료
            return count; // 미해금 후보 수 반환
        } // 메서드 종료
        private static void AddCatalog(string resourceName, HashSet<string> ids, List<PieceDefinition> result) // 실제 카탈로그 후보 병합
        { // 메서드 시작
            var catalog = Resources.Load<PlayerStartingDeckCatalog>(resourceName); // 카탈로그 조회
            if (catalog == null) // 카탈로그 누락 확인
            { // 조건 시작
                return; // 빈 원본 제외
            } // 조건 종료
            foreach (PieceDefinition definition in catalog.Cards) // 기물 목록 순회
            { // 반복 시작
                if (RunContentPoolRules.IsValidPiece(definition) && ids.Add(definition.PieceId)) // 유효한 고유 후보 확인
                { // 조건 시작
                    result.Add(definition); // 전체 후보 추가
                } // 조건 종료
            } // 반복 종료
        } // 메서드 종료
    } // 타입 종료
} // 영역 종료
