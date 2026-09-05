using System.Collections.Generic; // List<T> 사용
using System.Reflection; // 테스트용 PieceDefinition 필드 설정
using NUnit.Framework; // EditMode 테스트·Assert 사용
using UnityEngine; // ScriptableObject·Object 사용
using ProjectEta.Pieces; // PieceDefinition·등급·분류·이동 타입 사용
using ProjectEta.Cards; // DeckState 사용
using ProjectEta.Run; // 카드 보상 상태·생성·규칙 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day46CardRewardTests // 46일차 카드 보상 후보·선택·보유 상한 회귀 테스트
    {
        private readonly List<PieceDefinition> _createdDefinitions = new List<PieceDefinition>(); // 테스트 생성 ScriptableObject 정리 목록

        [TearDown] // 각 테스트 후 생성 객체 정리
        public void TearDown() // ScriptableObject 누적 방지
        {
            for (int i = 0; i < _createdDefinitions.Count; i++) // 생성 정의 순회
            {
                if (_createdDefinitions[i] != null) Object.DestroyImmediate(_createdDefinitions[i]); // 테스트 객체 즉시 제거
            }

            _createdDefinitions.Clear(); // 정리 목록 초기화
        }

        [Test] // 정상 후보 3장 생성 검증
        public void Generate_EligiblePool_ReturnsThreeUniqueCards() // 중복 없는 3개 보상 후보 확인
        {
            var pool = new List<PieceDefinition> // 보상 원본 풀 구성
            {
                CreateDefinition("pawn", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn), // 후보 1 생성
                CreateDefinition("knight", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Knight), // 후보 2 생성
                CreateDefinition("bishop", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Bishop), // 후보 3 생성
                CreateDefinition("rook", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Rook) // 후보 4 생성
            };

            var candidates = CardRewardGenerator.Generate(pool, new List<PieceDefinition>(), 3, 46); // 보상 후보 생성

            Assert.AreEqual(3, candidates.Count); // 정확히 3장 생성 검증
            Assert.AreNotEqual(candidates[0].PieceId, candidates[1].PieceId); // 첫·둘째 후보 중복 차단 검증
            Assert.AreNotEqual(candidates[0].PieceId, candidates[2].PieceId); // 첫·셋째 후보 중복 차단 검증
            Assert.AreNotEqual(candidates[1].PieceId, candidates[2].PieceId); // 둘째·셋째 후보 중복 차단 검증
        }

        [Test] // 획득 경로·등급 예외 검증
        public void Generate_ExcludedCards_SkipsKingFusionBossAndHighGrade() // 킹·합성·보스·4성 이상 후보 제외 확인
        {
            var allowed = CreateDefinition("pawn", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 정상 후보 생성
            var pool = new List<PieceDefinition> // 예외 후보 포함 풀 구성
            {
                allowed, // 정상 카드 등록
                CreateDefinition("king", PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.King), // 킹 후보 등록
                CreateDefinition("amazon", PieceCategory.Fusion, PieceGrade.ThreeStar, PieceMovementType.Amazon), // 합성 전용 후보 등록
                CreateDefinition("boss", PieceCategory.Boss, PieceGrade.ThreeStar, PieceMovementType.Custom), // 보스 후보 등록
                CreateDefinition("legend", PieceCategory.Special, PieceGrade.FourStar, PieceMovementType.Custom) // 4성 후보 등록
            };

            var candidates = CardRewardGenerator.Generate(pool, new List<PieceDefinition>(), 3, 46); // 보상 후보 생성

            Assert.AreEqual(1, candidates.Count); // 정상 카드만 남는지 검증
            Assert.AreEqual(allowed.PieceId, candidates[0].PieceId); // 정상 후보 ID 검증
        }

        [Test] // 동일 카드 보유 상한 검증
        public void Generate_OwnedCopyLimitReached_SkipsCard() // 동일 카드 3장 보유 시 후보 제외 확인
        {
            var pawn = CreateDefinition("pawn", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 테스트 카드 생성
            var owned = new List<PieceDefinition> { pawn, pawn, pawn }; // 동일 카드 3장 보유 상태 구성

            var candidates = CardRewardGenerator.Generate(new[] { pawn }, owned, 3, 46); // 보상 후보 생성

            Assert.AreEqual(0, candidates.Count); // 보유 상한 카드 후보 제외 검증
        }

        [Test] // 선택 보상 OwnedCardPool 반영 규칙 검증
        public void TryAddOwnedCard_BelowLimit_AddsCard() // 동일 카드 상한 전 획득 성공 확인
        {
            var knight = CreateDefinition("knight", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Knight); // 테스트 카드 생성
            var deck = new DeckState(); // 실제 런 덱 상태 생성

            bool added = CardRewardRules.TryAddOwnedCard(deck, knight); // DeckState 공개 API를 통한 보상 카드 추가 시도

            Assert.IsTrue(added); // 추가 성공 검증
            Assert.AreEqual(1, deck.OwnedCardPool.Count); // OwnedCardPool 장수 증가 검증
            Assert.AreSame(knight, deck.OwnedCardPool[0]); // 선택한 PieceDefinition 직접 추가 검증
        }

        [Test] // 카드 보상 선택 1회 제한 검증
        public void RewardState_AfterSelection_RejectsSecondSelection() // 3개 중 1개만 선택 가능 확인
        {
            var pawn = CreateDefinition("pawn", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn); // 첫 후보 생성
            var knight = CreateDefinition("knight", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Knight); // 둘째 후보 생성
            var state = new CardRewardState(); // 보상 상태 생성
            state.Begin(new[] { pawn, knight }, CardRewardSource.BattleVictory); // 전투 승리 보상 시작

            Assert.IsTrue(state.TrySelect(pawn)); // 첫 선택 성공 검증
            Assert.IsFalse(state.TrySelect(knight)); // 두 번째 선택 차단 검증
            Assert.AreSame(pawn, state.SelectedCard); // 최초 선택 유지 검증
        }

        private PieceDefinition CreateDefinition(string pieceId, PieceCategory category, PieceGrade grade, PieceMovementType movementType) // 테스트용 기물 정의 생성
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 런타임 PieceDefinition 생성
            SetField(definition, "_pieceId", pieceId); // 기물 ID 설정
            SetField(definition, "_displayName", pieceId); // 표시 이름 설정
            SetField(definition, "_category", category); // 획득 분류 설정
            SetField(definition, "_grade", grade); // 등급 설정
            SetField(definition, "_movementType", movementType); // 이동 타입 설정
            SetField(definition, "_baseHp", 3); // 테스트 HP 설정
            SetField(definition, "_baseAtk", 2); // 테스트 ATK 설정
            _createdDefinitions.Add(definition); // 종료 시 정리 목록 등록
            return definition; // 완성 정의 반환
        }

        private static void SetField<T>(PieceDefinition definition, string fieldName, T value) // private SerializeField 테스트 설정
        {
            FieldInfo field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 대상 private 필드 조회
            Assert.IsNotNull(field, $"PieceDefinition 필드 누락: {fieldName}"); // 필드 존재 검증
            field.SetValue(definition, value); // 테스트 값 적용
        }
    }
}
