using System.Reflection; // 테스트에서 직렬화된 기물 정의 필드에 값을 주입하기 위한 네임스페이스
using NUnit.Framework; // EditMode 테스트 어트리뷰트와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Object를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // DeckState, HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition을 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class CardFlowTests // 16일차 실제 덱·손패·숫자키 슬롯 선택 흐름을 검증하는 테스트 모음
    {
        [Test] // 보유 카드 풀을 기반으로 드로우 덱을 다시 만들 수 있는지 검증
        public void RebuildDrawPileFromOwnedPool_CopiesEveryOwnedCard()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var first = ScriptableObject.CreateInstance<PieceDefinition>(); // 첫 번째 테스트 카드 생성
            var second = ScriptableObject.CreateInstance<PieceDefinition>(); // 두 번째 테스트 카드 생성

            try // 테스트 중 예외가 나도 카드 에셋을 정리하도록 보장
            {
                deck.AddToOwnedPool(first); // 첫 카드를 보유 풀에 추가
                deck.AddToOwnedPool(second); // 두 번째 카드를 보유 풀에 추가
                deck.RebuildDrawPileFromOwnedPool(new System.Random(1)); // 고정 시드로 드로우 덱 재구성

                Assert.AreEqual(2, deck.DrawPile.Count); // 보유 카드 2장이 모두 드로우 덱에 있어야 함
                CollectionAssert.AreEquivalent(deck.OwnedCardPool, deck.DrawPile); // 순서는 달라도 구성 카드는 같아야 함
            }
            finally // 성공/실패와 무관하게 ScriptableObject 정리
            {
                Object.DestroyImmediate(first); // 첫 카드 정리
                Object.DestroyImmediate(second); // 둘째 카드 정리
            }
        }

        [Test] // 실제 드로우가 드로우 덱에서 카드를 하나 제거하는지 검증
        public void TryDraw_RemovesOneCardFromDrawPile()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var card = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 카드 생성

            try // 카드 정리를 보장하는 블록
            {
                deck.AddToOwnedPool(card); // 보유 풀에 카드 추가
                deck.RebuildDrawPileFromOwnedPool(new System.Random(1)); // 드로우 덱 구성

                bool result = deck.TryDraw(out var drawnCard); // 카드 1장 드로우 시도

                Assert.IsTrue(result); // 드로우가 성공해야 함
                Assert.AreSame(card, drawnCard); // 뽑힌 카드가 보유 카드와 같은 인스턴스여야 함
                Assert.AreEqual(0, deck.DrawPile.Count); // 드로우 후 덱에서 카드가 제거돼야 함
            }
            finally // 성공/실패와 무관하게 카드 정리
            {
                Object.DestroyImmediate(card); // 테스트 카드 정리
            }
        }

        [Test] // 손패가 10장이면 드로우 덱의 카드가 사라지지 않는지 검증
        public void TryDrawToHand_WhenHandIsFull_DoesNotConsumeDrawPile()
        {
            var deck = new DeckState(); // 테스트용 덱 상태 생성
            var hand = new HandState(); // 테스트용 손패 상태 생성
            var cards = new PieceDefinition[11]; // 손패 10장과 덱 1장에 사용할 카드 배열 생성

            try // 생성한 ScriptableObject 정리를 보장하는 블록
            {
                for (int i = 0; i < cards.Length; i++) // 카드 11장을 생성하며
                {
                    cards[i] = ScriptableObject.CreateInstance<PieceDefinition>(); // 개별 테스트 카드 생성
                }

                for (int i = 0; i < HandState.MaxHandSize; i++) // 손패 최대 장수만큼 반복하며
                {
                    Assert.IsTrue(hand.TryAddCard(cards[i])); // 손패를 정확히 10장까지 채움
                }

                deck.AddToOwnedPool(cards[10]); // 남은 한 장을 덱 보유 풀에 추가
                deck.RebuildDrawPileFromOwnedPool(new System.Random(1)); // 드로우 덱에 카드 1장 구성

                bool result = deck.TryDrawToHand(hand); // 가득 찬 손패로 드로우 시도

                Assert.IsFalse(result); // 손패가 가득 찼으므로 드로우 실패여야 함
                Assert.AreEqual(10, hand.Hand.Count); // 손패 수는 그대로 유지돼야 함
                Assert.AreEqual(1, deck.DrawPile.Count); // 드로우 덱 카드도 소비되면 안 됨
            }
            finally // 성공/실패와 무관하게 카드 정리
            {
                foreach (var card in cards) // 생성한 모든 카드를 순회하며
                {
                    if (card != null) Object.DestroyImmediate(card); // null이 아닌 카드만 정리
                }
            }
        }

        [Test] // 새 테스트 런이 보유 풀 6장→초기 손패 5장→드로우 덱 1장 흐름으로 시작하는지 검증
        public void EnsurePrototypeStartingHand_BuildsRealDeckAndDrawsFiveCards()
        {
            var context = CreateBoundContext(); // 공통 전투 테스트 컨텍스트 생성

            try // 테스트 오브젝트 정리를 보장하는 블록
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 기존 호환 진입점으로 실제 시작 덱 초기화

                Assert.AreEqual(6, context.RunState.Deck.OwnedCardPool.Count); // 기본 6종이 보유 풀에 등록돼야 함
                Assert.AreEqual(5, context.RunState.Hand.Hand.Count); // 초기 손패는 5장이어야 함
                Assert.AreEqual(1, context.RunState.Deck.DrawPile.Count); // 한 장은 드로우 덱에 남아 있어야 함
            }
            finally // 성공/실패와 무관하게 테스트 컨텍스트 정리
            {
                context.Dispose(); // 오브젝트와 ScriptableObject를 모두 정리
            }
        }

        [Test] // 2턴 플레이어 턴 시작 시 자동으로 한 장을 드로우하는지 검증
        public void PlayerTurnAfterFirst_AutomaticallyDrawsOneCard()
        {
            var context = CreateBoundContext(); // 공통 전투 테스트 컨텍스트 생성

            try // 테스트 컨텍스트 정리를 보장하는 블록
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 초기 손패 5장과 드로우 덱 1장 구성
                Assert.IsTrue(context.TurnManager.TryCompletePlayerAction()); // 1턴 플레이어 행동을 끝내 적 턴으로 전환
                Assert.IsTrue(context.TurnManager.CompleteEnemyTurn()); // 적 턴을 끝내 2턴 플레이어 턴으로 전환

                Assert.AreEqual(6, context.RunState.Hand.Hand.Count); // 2턴 시작 자동 드로우로 손패가 6장이 되어야 함
                Assert.AreEqual(0, context.RunState.Deck.DrawPile.Count); // 마지막 카드가 드로우되어 덱은 비어야 함
            }
            finally // 성공/실패와 무관하게 테스트 컨텍스트 정리
            {
                context.Dispose(); // 테스트 자원 정리
            }
        }

        [Test] // 숫자키가 고정 기물이 아니라 현재 손패 인덱스를 선택하는 기반 API인지 검증
        public void TrySelectHandSlot_SelectsCardByCurrentHandIndex()
        {
            var context = CreateBoundContext(); // 공통 전투 테스트 컨텍스트 생성

            try // 테스트 컨텍스트 정리를 보장하는 블록
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 실제 손패 구성
                var expected = context.RunState.Hand.Hand[2]; // 현재 손패의 세 번째 카드 참조

                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(2)); // 손패 인덱스 2를 선택
                Assert.AreSame(expected, context.BoardInput.SelectedCard); // 고정 Knight 등이 아니라 실제 손패 3번째 카드가 선택돼야 함
            }
            finally // 성공/실패와 무관하게 테스트 컨텍스트 정리
            {
                context.Dispose(); // 테스트 자원 정리
            }
        }

        private static TestContext CreateBoundContext() // 카드 흐름 테스트에 필요한 공통 객체를 만드는 메서드
        {
            var root = new GameObject("CardFlowTest"); // 테스트 루트 오브젝트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 컴포넌트 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 보드 입력 컴포넌트 추가
            var runState = new RunState(3); // 실제 전투와 동일한 런 상태 생성
            var turnManager = new TurnManager(); // 1턴 플레이어 턴으로 시작하는 턴 매니저 생성
            var definitions = new PieceDefinition[6]; // 기본 6종 역할을 대신할 테스트 카드 배열 생성

            for (int i = 0; i < definitions.Length; i++) // 기본 6종 수만큼 반복하며
            {
                definitions[i] = ScriptableObject.CreateInstance<PieceDefinition>(); // 서로 다른 카드 인스턴스 생성
            }

            SetPrivateField(boardInput, "_kingDefinition", definitions[0]); // 테스트 킹 필드 주입
            SetPrivateField(boardInput, "_pawnDefinition", definitions[1]); // 테스트 폰 필드 주입
            SetPrivateField(boardInput, "_knightDefinition", definitions[2]); // 테스트 나이트 필드 주입
            SetPrivateField(boardInput, "_bishopDefinition", definitions[3]); // 테스트 비숍 필드 주입
            SetPrivateField(boardInput, "_rookDefinition", definitions[4]); // 테스트 룩 필드 주입
            SetPrivateField(boardInput, "_queenDefinition", definitions[5]); // 테스트 퀸 필드 주입

            boardView.Bind(runState.Board); // 보드 뷰에 실제 런 보드 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력에 런 상태와 턴 매니저 연결

            return new TestContext(root, boardInput, runState, turnManager, definitions); // 완성된 테스트 컨텍스트 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // 직렬화 private 필드에 테스트 값을 주입하는 보조 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 지정 이름의 private 인스턴스 필드 탐색
            Assert.IsNotNull(field, $"필드 {fieldName}을 찾을 수 없습니다."); // 필드 이름 변경 시 테스트가 명확히 실패하도록 검증
            field.SetValue(target, value); // 찾은 필드에 테스트 값 주입
        }

        private sealed class TestContext // 테스트 객체와 정리 책임을 묶는 내부 컨텍스트 클래스
        {
            public GameObject Root { get; } // 테스트 루트 오브젝트
            public BoardInputController BoardInput { get; } // 테스트 대상 입력 컨트롤러
            public RunState RunState { get; } // 테스트 런 상태
            public TurnManager TurnManager { get; } // 테스트 턴 매니저
            private PieceDefinition[] Definitions { get; } // 정리할 테스트 카드 배열

            public TestContext(GameObject root, BoardInputController boardInput, RunState runState, TurnManager turnManager, PieceDefinition[] definitions) // 컨텍스트 생성자
            {
                Root = root; // 루트 저장
                BoardInput = boardInput; // 입력 컨트롤러 저장
                RunState = runState; // 런 상태 저장
                TurnManager = turnManager; // 턴 매니저 저장
                Definitions = definitions; // 카드 배열 저장
            }

            public void Dispose() // 테스트에서 생성한 Unity 객체를 정리하는 메서드
            {
                Object.DestroyImmediate(Root); // 테스트 GameObject 정리
                foreach (var definition in Definitions) // 카드 정의를 순회하며
                {
                    if (definition != null) Object.DestroyImmediate(definition); // 생성된 ScriptableObject 정리
                }
            }
        }
    }
}
