using System.Collections.Generic; // HashSet<T>를 사용하기 위한 네임스페이스
using NUnit.Framework; // Test와 Assert를 사용하기 위한 네임스페이스
using UnityEditor; // 실제 PlayerStartingDeckCatalog 에셋을 로드하기 위한 네임스페이스
using ProjectEta.Cards; // DeckState, PlayerStartingDeckCatalog, PrototypePlayerDeck26Bootstrap을 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class Day26PlayerDeckRosterTests // 플레이어 시작 덱에 26종을 정확히 한 장씩 넣는 기능을 검증하는 테스트 모음
    {
        private const string CatalogPath = "Assets/ProjectEta/Resources/PlayerStartingDeck26.asset"; // 26종 시작 덱 카탈로그 에셋 경로

        [Test] // 카탈로그 자체가 26종을 중복 없이 한 장씩 보유하는지 검증
        public void StartingDeckCatalog_HasTwentySixUniqueCards()
        {
            var catalog = LoadCatalog(); // 실제 카탈로그 에셋 로드
            var ids = new HashSet<string>(); // PieceId 중복 검사용 집합 생성

            Assert.AreEqual(26, catalog.Cards.Count); // 총 카드 수가 정확히 26장인지 확인

            foreach (var card in catalog.Cards) // 카탈로그의 모든 카드 순회
            {
                Assert.IsNotNull(card, "시작 덱 카탈로그에는 null 카드가 들어가면 안 됩니다."); // null 금지
                Assert.IsTrue(ids.Add(card.PieceId), $"중복 PieceId: {card.PieceId}"); // 같은 기물이 두 장 이상 들어가지 않도록 검증
            }
        }

        [Test] // 기존 6종 프로토타입 덱을 26종 한 장씩으로 확장하는지 검증
        public void Expander_UpgradesOldPrototypeDeckToTwentySixCards()
        {
            var catalog = LoadCatalog(); // 실제 26종 카탈로그 로드
            var run = new RunState(3); // 새 테스트 런 생성

            for (int i = 0; i < 6; i++) // 기존 프로토타입 시작 덱 6종을 재현
            {
                run.Deck.AddToOwnedPool(catalog.Cards[i]); // OwnedCardPool에 기본 6종 등록
            }

            for (int i = 0; i < 5; i++) // 기존 시작 손패 5장을 재현
            {
                run.Hand.TryAddCard(catalog.Cards[i]); // 손패에 킹 포함 기본 카드 5장 배치
            }

            run.Deck.AddToDrawPile(catalog.Cards[5]); // 기본 6종 중 손패에 없는 마지막 1장을 DrawPile에 남김

            bool changed = PrototypePlayerDeck26Bootstrap.TryExpandPrototypeDeck(run, catalog.Cards, shuffleSeed: 26); // 26종 확장 실행

            Assert.IsTrue(changed); // 실제 확장이 수행되어야 함
            Assert.AreEqual(26, run.Deck.OwnedCardPool.Count); // 영구 보유 풀은 26장이어야 함
            Assert.AreEqual(5, run.Hand.Hand.Count); // 기존 시작 손패 5장은 그대로 유지
            Assert.AreEqual(21, run.Deck.DrawPile.Count); // 손패 5장을 제외한 나머지 21장이 드로우 더미에 있어야 함

            var ownedIds = new HashSet<string>(); // Owned 중복 검사 집합 생성
            foreach (var card in run.Deck.OwnedCardPool) // 보유 카드 26장을 순회
            {
                Assert.IsTrue(ownedIds.Add(card.PieceId), $"OwnedCardPool 중복: {card.PieceId}"); // 각 기물이 정확히 한 장인지 검증
            }

            Assert.AreEqual(26, ownedIds.Count); // 최종 고유 기물 수 확인
        }

        [Test] // 같은 확장 함수를 두 번 실행해도 카드가 중복되지 않는지 검증
        public void Expander_IsIdempotent()
        {
            var catalog = LoadCatalog(); // 실제 카탈로그 로드
            var run = new RunState(3); // 새 테스트 런 생성

            for (int i = 0; i < 6; i++) run.Deck.AddToOwnedPool(catalog.Cards[i]); // 기존 기본 6종 보유
            for (int i = 0; i < 5; i++) run.Hand.TryAddCard(catalog.Cards[i]); // 기존 손패 5장
            run.Deck.AddToDrawPile(catalog.Cards[5]); // 기존 드로우 1장

            Assert.IsTrue(PrototypePlayerDeck26Bootstrap.TryExpandPrototypeDeck(run, catalog.Cards, shuffleSeed: 26)); // 첫 실행은 확장
            Assert.IsFalse(PrototypePlayerDeck26Bootstrap.TryExpandPrototypeDeck(run, catalog.Cards, shuffleSeed: 26)); // 두 번째 실행은 변경 없음

            Assert.AreEqual(26, run.Deck.OwnedCardPool.Count); // 보유 카드 수는 그대로 26장
            Assert.AreEqual(21, run.Deck.DrawPile.Count); // 드로우 더미도 그대로 21장
        }

        [Test] // 사용자 커스텀 덱처럼 기본 6종 구조가 아닌 덱은 강제로 덮어쓰지 않는지 검증
        public void Expander_DoesNotOverwriteCustomDeck()
        {
            var catalog = LoadCatalog(); // 실제 카탈로그 로드
            var run = new RunState(3); // 새 테스트 런 생성
            var amazon = FindCard(catalog, "amazon"); // 기본 6종이 아닌 합성 기물 조회

            run.Deck.AddToOwnedPool(amazon); // 커스텀 덱에 Amazon 1장만 존재하는 상황 구성
            run.Deck.AddToDrawPile(amazon); // DrawPile에도 해당 카드 배치

            bool changed = PrototypePlayerDeck26Bootstrap.TryExpandPrototypeDeck(run, catalog.Cards, shuffleSeed: 26); // 확장 시도

            Assert.IsFalse(changed); // 커스텀 덱은 자동 확장하면 안 됨
            Assert.AreEqual(1, run.Deck.OwnedCardPool.Count); // 기존 보유 카드 유지
            Assert.AreEqual("amazon", run.Deck.OwnedCardPool[0].PieceId); // 카드 종류도 그대로 유지
        }

        private static PlayerStartingDeckCatalog LoadCatalog() // 실제 카탈로그 로드 공통 도우미
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PlayerStartingDeckCatalog>(CatalogPath); // 지정 경로에서 시작 덱 에셋 로드
            Assert.IsNotNull(catalog, "PlayerStartingDeck26.asset이 존재해야 합니다."); // 에셋 누락 시 명확한 실패
            return catalog; // 정상 카탈로그 반환
        }

        private static PieceDefinition FindCard(PlayerStartingDeckCatalog catalog, string pieceId) // 카탈로그 안에서 id로 카드를 찾는 테스트 도우미
        {
            foreach (var card in catalog.Cards) // 카탈로그 카드 순회
            {
                if (card != null && card.PieceId == pieceId) return card; // id가 일치하면 반환
            }

            Assert.Fail($"카탈로그에서 {pieceId} 카드를 찾지 못했습니다."); // 누락이면 테스트 실패
            return null; // 컴파일러 흐름 만족용 반환
        }
    }
}
