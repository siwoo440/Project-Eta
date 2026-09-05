using System.Collections.Generic; // 테스트 목록 사용
using System.Reflection; // 테스트 PieceDefinition 설정
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // ScriptableObject 제거 사용
using ProjectEta.Cards; // DeckState 사용
using ProjectEta.Pieces; // PieceDefinition 사용
using ProjectEta.Run; // 47일차 런 규칙 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day47ShopEventTests
    {
        private readonly List<PieceDefinition> _createdDefinitions = new List<PieceDefinition>(); // 테스트 객체 정리 목록

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _createdDefinitions.Count; i++)
            {
                if (_createdDefinitions[i] != null) Object.DestroyImmediate(_createdDefinitions[i]); // 테스트 정의 제거
            }

            _createdDefinitions.Clear(); // 정리 목록 초기화
            RunEconomyService.ResetForTests(); // 런 경제 상태 초기화
        }

        [Test]
        public void Economy_NewRun_StartsWithPrototypeCurrency()
        {
            var runState = new RunState(3); // 테스트 런 생성
            RunEconomyState economy = RunEconomyService.GetOrCreate(runState); // 런 경제 상태 생성

            Assert.AreEqual(RunEconomyRules.StartingCurrency, economy.Currency); // 시작 재화 검증
        }

        [Test]
        public void Economy_TrySpend_RejectsInsufficientCurrency()
        {
            var runState = new RunState(3); // 테스트 런 생성
            RunEconomyState economy = RunEconomyService.GetOrCreate(runState); // 런 경제 상태 생성

            bool spent = economy.TrySpend(RunEconomyRules.StartingCurrency + 1); // 보유량 초과 지불 시도

            Assert.IsFalse(spent); // 초과 지불 차단 검증
            Assert.AreEqual(RunEconomyRules.StartingCurrency, economy.Currency); // 재화 보존 검증
        }

        [Test]
        public void Upgrade_ReplacesOwnedCard_WithRuntimeStatCopy()
        {
            var deck = new DeckState(); // 테스트 덱 생성
            PieceDefinition pawn = CreateDefinition("pawn", "Pawn", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Pawn, 3, 1); // 원본 카드 생성
            deck.AddToOwnedPool(pawn); // 원본 카드 보유 풀 등록

            bool upgraded = RuntimeCardUpgradeService.TryUpgradeOwnedCard(deck, pawn, out PieceDefinition upgradedCard); // 런타임 업그레이드 실행

            Assert.IsTrue(upgraded); // 업그레이드 성공 검증
            Assert.IsNotNull(upgradedCard); // 업그레이드 카드 생성 검증
            Assert.AreEqual(4, upgradedCard.BaseHp); // HP +1 검증
            Assert.AreEqual(2, upgradedCard.BaseAtk); // ATK +1 검증
            Assert.AreEqual(3, pawn.BaseHp); // 원본 에셋 비변경 검증
            Assert.AreEqual(1, deck.OwnedCardPool.Count); // 보유 카드 장수 유지 검증
            Assert.AreSame(upgradedCard, deck.OwnedCardPool[0]); // 업그레이드 카드 교체 검증

            _createdDefinitions.Add(upgradedCard); // 런타임 복제 객체 정리 등록
        }

        [Test]
        public void Upgrade_King_IsRejected()
        {
            var deck = new DeckState(); // 테스트 덱 생성
            PieceDefinition king = CreateDefinition("king", "King", PieceCategory.Special, PieceGrade.OneStar, PieceMovementType.King, 3, 1); // 킹 카드 생성
            deck.AddToOwnedPool(king); // 킹 보유 풀 등록

            bool upgraded = RuntimeCardUpgradeService.TryUpgradeOwnedCard(deck, king, out PieceDefinition upgradedCard); // 킹 업그레이드 시도

            Assert.IsFalse(upgraded); // 킹 업그레이드 차단 검증
            Assert.IsNull(upgradedCard); // 새 카드 미생성 검증
            Assert.AreSame(king, deck.OwnedCardPool[0]); // 원본 킹 유지 검증
        }

        [TestCase(1, StageEventType.CardFind)]
        [TestCase(2, StageEventType.Rest)]
        [TestCase(3, StageEventType.RiskReward)]
        public void EventGenerator_DepthCycle_ReturnsExpectedPrototypeType(int depth, StageEventType expected)
        {
            StageEventScenario scenario = StageEventGenerator.Create(depth); // 깊이 기반 이벤트 생성

            Assert.IsNotNull(scenario); // 이벤트 생성 검증
            Assert.AreEqual(expected, scenario.EventType); // 이벤트 타입 순환 검증
        }

        [Test]
        public void ChoiceResult_StoresSharedOutcomeFields()
        {
            PieceDefinition bishop = CreateDefinition("bishop", "Bishop", PieceCategory.Basic, PieceGrade.OneStar, PieceMovementType.Bishop, 3, 2); // 결과 카드 생성
            var result = new StageChoiceResult(StageChoiceEffectType.CardAdded, "카드 획득", 0, 0, bishop); // 공통 선택 결과 생성

            Assert.AreEqual(StageChoiceEffectType.CardAdded, result.EffectType); // 결과 타입 검증
            Assert.AreEqual("카드 획득", result.Summary); // 결과 설명 검증
            Assert.AreSame(bishop, result.Card); // 결과 카드 검증
        }

        private PieceDefinition CreateDefinition(string pieceId, string displayName, PieceCategory category, PieceGrade grade, PieceMovementType movementType, int hp, int atk)
        {
            var definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 기물 정의 생성
            SetField(definition, "_pieceId", pieceId); // 기물 ID 설정
            SetField(definition, "_displayName", displayName); // 표시 이름 설정
            SetField(definition, "_category", category); // 기물 분류 설정
            SetField(definition, "_grade", grade); // 기물 등급 설정
            SetField(definition, "_movementType", movementType); // 이동 타입 설정
            SetField(definition, "_baseHp", hp); // 기본 HP 설정
            SetField(definition, "_baseAtk", atk); // 기본 ATK 설정
            _createdDefinitions.Add(definition); // 정리 목록 등록
            return definition; // 테스트 정의 반환
        }

        private static void SetField<T>(PieceDefinition definition, string fieldName, T value)
        {
            FieldInfo field = typeof(PieceDefinition).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // private 필드 조회
            Assert.IsNotNull(field, $"PieceDefinition 필드 누락: {fieldName}"); // 필드 존재 검증
            field.SetValue(definition, value); // 테스트 값 적용
        }
    }
}
