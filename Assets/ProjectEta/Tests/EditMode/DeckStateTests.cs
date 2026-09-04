using NUnit.Framework; // [Test], Assert 등을 사용하기 위한 네임스페이스
using UnityEngine; // ScriptableObject를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // DeckState, HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class DeckStateTests // 카드 생명주기와 23일차 죽은 카드 중복 회귀를 검증하는 테스트 모음
    {
        [Test] // DiscardToBottom이 손패에서 카드를 빼서 드로우 더미 맨 아래에 넣는지 확인하는 테스트
        public void DiscardToBottom_MovesCardFromHandToDrawPileBottom()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var hand = new HandState(); // 테스트용 손패 상태 생성
            var remaining = ScriptableObject.CreateInstance<PieceDefinition>(); // 드로우 더미에 이미 있던 카드
            var discarded = ScriptableObject.CreateInstance<PieceDefinition>(); // 손패에서 정리될 카드

            deck.AddToDrawPile(remaining); // 드로우 더미에 기존 카드 추가
            hand.TryAddCard(discarded); // 손패에 정리 대상 카드 추가

            bool result = deck.DiscardToBottom(discarded, hand); // 실제 손패 정리 실행

            Assert.IsTrue(result); // 정리가 성공해야 함
            Assert.AreEqual(0, hand.Hand.Count); // 손패에서 카드가 제거돼야 함
            Assert.AreEqual(2, deck.DrawPile.Count); // 드로우 더미 장수가 1장 늘어야 함

            deck.TryDraw(out var firstDraw); // 정리 직후 첫 드로우 실행
            Assert.AreSame(remaining, firstDraw); // 원래 있던 카드가 먼저 뽑혀야 함

            deck.TryDraw(out var secondDraw); // 두 번째 드로우 실행
            Assert.AreSame(discarded, secondDraw); // 정리됐던 카드는 맨 마지막에 뽑혀야 함
        }

        [Test] // 손패에 없는 카드를 정리하려 하면 아무 상태도 바뀌지 않는지 확인하는 테스트
        public void DiscardToBottom_Fails_WhenCardNotInHand()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var hand = new HandState(); // 테스트용 손패 상태 생성
            var card = ScriptableObject.CreateInstance<PieceDefinition>(); // 손패에 없는 카드

            bool result = deck.DiscardToBottom(card, hand); // 손패에 없는 카드로 정리 시도

            Assert.IsFalse(result); // 정리가 실패해야 함
            Assert.AreEqual(0, deck.DrawPile.Count); // 드로우 더미가 변하지 않아야 함
        }

        [Test] // 죽은 카드 더미가 보유 카드 풀로 정확히 복귀하고 비워지는지 확인하는 테스트
        public void ReturnDeadPileToOwnedPool_MovesCardsAndClearsDeadPile()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var cardA = ScriptableObject.CreateInstance<PieceDefinition>(); // 죽은 카드 더미에 들어갈 카드 A
            var cardB = ScriptableObject.CreateInstance<PieceDefinition>(); // 죽은 카드 더미에 들어갈 카드 B
            deck.MoveToDeadPile(cardA); // 카드 A를 죽은 카드 더미로 이동
            deck.MoveToDeadPile(cardB); // 카드 B를 죽은 카드 더미로 이동

            deck.ReturnDeadPileToOwnedPool(); // 라운드 클리어 시 호출되는 복귀 실행

            Assert.AreEqual(0, deck.DeadCardPile.Count); // 죽은 카드 더미가 비워져야 함
            CollectionAssert.Contains(deck.OwnedCardPool, cardA); // 보유 풀에 카드 A가 포함돼야 함
            CollectionAssert.Contains(deck.OwnedCardPool, cardB); // 보유 풀에 카드 B가 포함돼야 함
        }

        [Test] // 23일차: 보유 카드가 사망 후 복귀할 때 같은 카드가 2장으로 복제되지 않는지 검증
        public void MoveToDeadPile_ThenReturn_KeepsOwnedCopyCountStable()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var card = ScriptableObject.CreateInstance<PieceDefinition>(); // 실제 한 장뿐인 카드 정의 생성
            deck.AddToOwnedPool(card); // 런이 보유한 카드 1장으로 등록

            deck.MoveToDeadPile(card); // 전투 중 카드가 사망했다고 가정

            Assert.AreEqual(0, deck.OwnedCardPool.Count); // 사망 중에는 보유 풀에서 해당 한 장이 빠져 있어야 함
            Assert.AreEqual(1, deck.DeadCardPile.Count); // 죽은 카드 더미에는 정확히 1장 존재해야 함

            deck.ReturnDeadPileToOwnedPool(); // 라운드 종료 시 죽은 카드를 다시 보유 풀로 복귀

            Assert.AreEqual(1, deck.OwnedCardPool.Count); // 복귀 후에도 원래 수량 1장만 존재해야 함
            Assert.AreEqual(0, deck.DeadCardPile.Count); // 죽은 카드 더미는 비워져야 함
            Assert.AreSame(card, deck.OwnedCardPool[0]); // 복귀한 카드 참조도 원래 카드와 동일해야 함
        }
    }
}
