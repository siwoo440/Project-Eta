using System.Reflection; // private 직렬화 필드 설정
using NUnit.Framework; // EditMode 테스트 사용
using UnityEngine; // ScriptableObject·Vector2Int 사용
using ProjectEta.Battle; // DamageContext·TurnManager 사용
using ProjectEta.Cards; // DeckState·HandState 사용
using ProjectEta.King; // 방어형·전략형 킹 능력 사용
using ProjectEta.Meta; // 영구 해금 상태 사용
using ProjectEta.Pieces; // PieceDefinition·PieceRuntimeState 사용

namespace ProjectEta.Tests.EditMode
{
    public class Day50KingAbilityTests
    {
        [Test]
        public void DefenseKing_StayingStill_GainsBarrierBeforeEnemyTurn()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 킹 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 추적 시작

            bool gained = DefenseKingAbility.HandleEnemyTurnStarting(state); // 적 턴 직전 방벽 획득 처리

            Assert.IsTrue(gained); // 방벽 신규 획득 검증
            Assert.IsTrue(state.BarrierActive); // 방벽 활성 상태 검증
        }

        [Test]
        public void DefenseKing_MovingThisTurn_PreventsBarrierGain()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 킹 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 추적 시작
            PieceRuntimeState king = CreatePiece(PieceMovementType.King, true); // 플레이어 킹 생성

            DefenseKingAbility.HandleAfterMove(state, king, Vector2Int.zero, Vector2Int.right); // 킹 이동 기록
            bool gained = DefenseKingAbility.HandleEnemyTurnStarting(state); // 적 턴 직전 방벽 획득 시도

            Assert.IsFalse(gained); // 이동한 턴 방벽 획득 차단 검증
            Assert.IsFalse(state.BarrierActive); // 방벽 비활성 검증
        }

        [Test]
        public void DefenseKing_Barrier_ReducesNextDamageAndIsConsumed()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 킹 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 추적 시작
            DefenseKingAbility.HandleEnemyTurnStarting(state); // 방벽 획득
            PieceRuntimeState king = CreatePiece(PieceMovementType.King, true); // 플레이어 킹 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 공격원 생성
            var context = new DamageContext(king, enemy, 3); // 피해 3 컨텍스트 생성

            bool consumed = DefenseKingAbility.ApplyBeforeDamage(state, context, out int reducedAmount); // 방벽 피해 경감 적용

            Assert.IsTrue(consumed); // 방벽 소비 검증
            Assert.AreEqual(1, reducedAmount); // 피해 1 감소 검증
            Assert.AreEqual(2, context.Amount); // 최종 피해 2 검증
            Assert.IsFalse(state.BarrierActive); // 방벽 1회 소비 검증
        }

        [Test]
        public void DefenseKing_Barrier_KeepsMinimumDamageAtOne()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 킹 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 추적 시작
            DefenseKingAbility.HandleEnemyTurnStarting(state); // 방벽 획득
            PieceRuntimeState king = CreatePiece(PieceMovementType.King, true); // 플레이어 킹 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 공격원 생성
            var context = new DamageContext(king, enemy, 1); // 피해 1 컨텍스트 생성

            bool consumed = DefenseKingAbility.ApplyBeforeDamage(state, context, out int reducedAmount); // 최소 피해 방벽 처리

            Assert.IsTrue(consumed); // 피해 1에서도 방벽 소비 검증
            Assert.AreEqual(0, reducedAmount); // 최소 피해 규칙으로 실제 감소 0 검증
            Assert.AreEqual(1, context.Amount); // 최종 피해 최소 1 검증
        }

        [Test]
        public void DefenseKing_NonKingDamage_DoesNotConsumeBarrier()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Defense); // 방어형 킹 선택
            state.BeginPlayerTurn(); // 플레이어 턴 이동 추적 시작
            DefenseKingAbility.HandleEnemyTurnStarting(state); // 방벽 획득
            PieceRuntimeState pawn = CreatePiece(PieceMovementType.Pawn, true); // 아군 일반 기물 생성
            PieceRuntimeState enemy = CreatePiece(PieceMovementType.Pawn, false); // 적 공격원 생성
            var context = new DamageContext(pawn, enemy, 3); // 일반 기물 피해 컨텍스트 생성

            bool consumed = DefenseKingAbility.ApplyBeforeDamage(state, context, out int reducedAmount); // 방벽 오발동 여부 확인

            Assert.IsFalse(consumed); // 일반 기물 피해에서 방벽 미소비 검증
            Assert.AreEqual(0, reducedAmount); // 피해 감소 없음 검증
            Assert.AreEqual(3, context.Amount); // 원래 피해 유지 검증
            Assert.IsTrue(state.BarrierActive); // 킹 방벽 유지 검증
        }

        [Test]
        public void StrategyKing_PeeksTopThreeCardsInDrawOrder()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Strategy); // 전략형 킹 선택
            var deck = new DeckState(); // 테스트 드로우 덱 생성
            var hand = new HandState(); // 테스트 손패 생성
            PieceDefinition a = CreateCard("a"); // 맨 아래 카드 생성
            PieceDefinition b = CreateCard("b"); // 세 번째 후보 카드 생성
            PieceDefinition c = CreateCard("c"); // 두 번째 후보 카드 생성
            PieceDefinition d = CreateCard("d"); // 맨 위 후보 카드 생성
            deck.AddToDrawPile(a); // 드로우 덱 아래 카드 추가
            deck.AddToDrawPile(b); // 드로우 덱 후보 추가
            deck.AddToDrawPile(c); // 드로우 덱 후보 추가
            deck.AddToDrawPile(d); // 드로우 덱 맨 위 카드 추가

            var candidates = StrategyKingAbility.BuildCandidates(state, deck, hand); // 전략형 후보 생성

            Assert.AreEqual(3, candidates.Count); // 최대 3장 후보 검증
            Assert.AreSame(d, candidates[0]); // 덱 맨 위가 첫 후보인지 검증
            Assert.AreSame(c, candidates[1]); // 두 번째 카드 순서 검증
            Assert.AreSame(b, candidates[2]); // 세 번째 카드 순서 검증
        }

        [Test]
        public void StrategyKing_ChosenCardMovesToHand_AndOthersMoveToBottom()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Strategy); // 전략형 킹 선택
            var deck = new DeckState(); // 테스트 드로우 덱 생성
            var hand = new HandState(); // 테스트 손패 생성
            PieceDefinition a = CreateCard("a"); // 후보 밖 카드 생성
            PieceDefinition b = CreateCard("b"); // 세 번째 후보 생성
            PieceDefinition c = CreateCard("c"); // 선택할 두 번째 후보 생성
            PieceDefinition d = CreateCard("d"); // 첫 번째 후보 생성
            deck.AddToDrawPile(a); // 후보 밖 카드 추가
            deck.AddToDrawPile(b); // 후보 카드 추가
            deck.AddToDrawPile(c); // 후보 카드 추가
            deck.AddToDrawPile(d); // 맨 위 후보 카드 추가
            var candidates = StrategyKingAbility.BuildCandidates(state, deck, hand); // 후보 3장 생성
            Assert.IsTrue(state.TryBeginStrategyPreparation(5)); // 5턴 배치 능력 시작

            bool success = StrategyKingAbility.TryChoose(state, deck, hand, candidates, 1, out PieceDefinition selected); // 가운데 후보 선택

            Assert.IsTrue(success); // 카드 선택 성공 검증
            Assert.AreSame(c, selected); // 선택 카드 검증
            Assert.AreEqual(1, hand.Hand.Count); // 손패 1장 증가 검증
            Assert.AreSame(c, hand.Hand[0]); // 선택 카드 손패 이동 검증
            Assert.AreEqual(3, deck.DrawPile.Count); // 선택 카드 제외 덱 장수 검증
            Assert.AreSame(a, deck.PeekTopCards(1)[0]); // 선택하지 않은 후보가 덱 아래로 이동했는지 검증
            CollectionAssert.Contains(deck.DrawPile, b); // 미선택 후보 유지 검증
            CollectionAssert.Contains(deck.DrawPile, d); // 미선택 후보 유지 검증
            Assert.IsFalse(state.StrategyPreparationPending); // 선택 완료 상태 검증
        }

        [Test]
        public void StrategyKing_FullHand_DoesNotOfferCandidates()
        {
            var state = new KingRunState(); // 킹 런 상태 생성
            state.Select(KingArchetype.Strategy); // 전략형 킹 선택
            var deck = new DeckState(); // 테스트 드로우 덱 생성
            var hand = new HandState(); // 테스트 손패 생성
            deck.AddToDrawPile(CreateCard("candidate")); // 후보 카드 추가

            for (int i = 0; i < HandState.MaxHandSize; i++)
            {
                hand.TryAddCard(CreateCard($"hand_{i}")); // 손패 최대치까지 채움
            }

            var candidates = StrategyKingAbility.BuildCandidates(state, deck, hand); // 손패 가득 찬 상태 후보 생성

            Assert.AreEqual(0, candidates.Count); // 손패 Full 시 패시브 발동 차단 검증
            Assert.AreEqual(1, deck.DrawPile.Count); // 드로우 덱 미변경 검증
        }

        [Test]
        public void DeploymentChoicePending_BlocksDeploymentEndUntilResolved()
        {
            var manager = new TurnManager(); // 초기 배치 턴 매니저 생성
            manager.MarkInitialKingPlaced(); // 필수 킹 배치 완료 처리
            manager.SetDeploymentChoicePending(true); // 전략형 카드 선택 대기 설정

            bool blocked = manager.TryEndDeploymentTurn(); // 선택 전 배치 종료 시도
            manager.SetDeploymentChoicePending(false); // 전략형 카드 선택 완료 처리
            bool completed = manager.TryEndDeploymentTurn(); // 선택 후 배치 종료 시도

            Assert.IsFalse(blocked); // 선택 중 배치 종료 차단 검증
            Assert.IsTrue(completed); // 선택 완료 후 배치 종료 허용 검증
            Assert.AreEqual(TurnState.PlayerTurn, manager.CurrentState); // 정상 첫 플레이어 턴 진입 검증
        }

        [Test]
        public void DefenseAndStrategyKing_RequirePermanentUnlocks()
        {
            var progress = new MetaProgressState(); // 빈 영구 진행 상태 생성

            Assert.IsFalse(KingUnlockRules.CanSelect(KingArchetype.Defense, progress)); // 방어형 해금 전 선택 차단 검증
            Assert.IsFalse(KingUnlockRules.CanSelect(KingArchetype.Strategy, progress)); // 전략형 해금 전 선택 차단 검증

            progress.Unlock(MetaUnlockType.King, KingUnlockIds.Defense); // 방어형 킹 영구 해금
            progress.Unlock(MetaUnlockType.King, KingUnlockIds.Strategy); // 전략형 킹 영구 해금

            Assert.IsTrue(KingUnlockRules.CanSelect(KingArchetype.Defense, progress)); // 방어형 해금 후 선택 허용 검증
            Assert.IsTrue(KingUnlockRules.CanSelect(KingArchetype.Strategy, progress)); // 전략형 해금 후 선택 허용 검증
        }

        private static PieceRuntimeState CreatePiece(PieceMovementType movementType, bool isPlayerPiece)
        {
            PieceDefinition definition = CreateCard(movementType.ToString()); // 테스트 기물 정의 생성
            SetPrivateField(definition, "_movementType", movementType); // 테스트 이동 타입 지정
            return new PieceRuntimeState(definition, Vector2Int.zero, isPlayerPiece); // 테스트 런타임 기물 반환
        }

        private static PieceDefinition CreateCard(string displayName)
        {
            PieceDefinition definition = ScriptableObject.CreateInstance<PieceDefinition>(); // 테스트 카드 정의 생성
            SetPrivateField(definition, "_displayName", displayName); // 테스트 표시 이름 지정
            SetPrivateField(definition, "_baseHp", 3); // 테스트 기본 HP 지정
            SetPrivateField(definition, "_baseAtk", 1); // 테스트 기본 ATK 지정
            return definition; // 테스트 카드 정의 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance); // private 필드 조회
            Assert.IsNotNull(field); // 테스트 대상 필드 존재 검증
            field.SetValue(target, value); // 테스트 값 대입
        }
    }
}
