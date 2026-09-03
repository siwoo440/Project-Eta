using System.Linq; // IReadOnlyList<T>에 대한 Contains 확장 메서드를 사용하기 위한 네임스페이스
using System.Reflection; // 테스트에서 직렬화된 기물 정의 필드에 값을 주입하기 위한 네임스페이스
using NUnit.Framework; // EditMode 테스트 어트리뷰트와 Assert를 사용하기 위한 네임스페이스
using UnityEngine; // GameObject, ScriptableObject, Object, Vector2Int를 사용하기 위한 네임스페이스
using ProjectEta.Battle; // TurnManager, TurnState를 사용하기 위한 네임스페이스
using ProjectEta.Board; // BoardView, BoardInputController를 사용하기 위한 네임스페이스
using ProjectEta.Cards; // HandState를 사용하기 위한 네임스페이스
using ProjectEta.Pieces; // PieceDefinition, PieceMovementType를 사용하기 위한 네임스페이스
using ProjectEta.Run; // RunState를 사용하기 위한 네임스페이스

namespace ProjectEta.Tests.EditMode // 프로젝트 η EditMode 테스트 네임스페이스
{
    public class CardFlowTests // 시작 킹 필수·자유 배치·명시적 배치 턴 종료를 검증하는 테스트 모음
    {
        [Test] // 시작 손패에는 반드시 킹이 포함되는지 검증
        public void EnsurePrototypeStartingHand_AlwaysContainsKing()
        {
            var context = CreateBoundContext(); // 공통 테스트 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 실제 시작 덱·손패 구성

                Assert.That(FindCardIndex(context.RunState.Hand, context.Definitions[0]), Is.GreaterThanOrEqualTo(0)); // 킹이 손패에 존재
                Assert.AreEqual(5, context.RunState.Hand.Hand.Count); // 초기 손패 5장 유지
                Assert.AreEqual(1, context.RunState.Deck.DrawPile.Count); // 나머지 카드 1장은 드로우 더미
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 시작 배치에서 킹을 놓기 전에는 비킹 카드를 선택할 수 없는지 검증
        public void InitialDeployment_BeforeKing_RejectsNonKingSelection()
        {
            var context = CreateBoundContext(); // 시작 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 킹 포함 초기 손패 구성
                int nonKingIndex = FindFirstNonKingIndex(context.RunState.Hand); // 비킹 카드 위치 탐색

                Assert.IsFalse(context.BoardInput.TrySelectHandSlot(nonKingIndex)); // 비킹 카드 선택 실패
                Assert.IsNull(context.BoardInput.SelectedCard); // 선택 상태 없음
                Assert.IsFalse(context.TurnManager.TryEndDeploymentTurn()); // 킹 없이 배치 턴 종료도 실패
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 킹 배치 후에도 시작 배치 턴이 유지되고 다른 카드를 추가로 자유 배치할 수 있는지 검증
        public void InitialDeployment_AfterKing_AllowsAdditionalFreePlacementUntilEnd()
        {
            var context = CreateBoundContext(); // 시작 배치 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 구성
                int kingIndex = FindCardIndex(context.RunState.Hand, context.Definitions[0]); // 킹 손패 위치 탐색
                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(kingIndex)); // 킹 선택
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(0, 0))); // 킹 배치

                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 킹 배치 후에도 배치 턴 유지
                Assert.IsTrue(context.TurnManager.IsInitialKingPlaced); // 킹 조건 충족
                Assert.AreEqual(1, context.TurnManager.DeployedCardCount); // 배치 수 1

                int nonKingIndex = FindFirstNonKingIndex(context.RunState.Hand); // 남은 비킹 카드 위치 탐색
                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(nonKingIndex)); // 킹 이후 비킹 카드 선택 가능
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(1, 0))); // 두 번째 카드 추가 배치

                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 두 장 배치 후에도 계속 배치 턴
                Assert.AreEqual(2, context.TurnManager.DeployedCardCount); // 배치 수 2
                Assert.IsTrue(context.TurnManager.TryEndDeploymentTurn()); // 사용자가 명시적으로 배치 턴 종료
                Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 이제 1턴 시작
                Assert.AreEqual(1, context.TurnManager.TurnNumber); // 초기 배치는 턴 미소비
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 5턴 주기 배치에서도 여러 카드를 놓은 뒤 수동 종료할 수 있는지 검증
        public void PeriodicDeployment_AllowsMultipleCardsBeforeExplicitEnd()
        {
            var context = CreateStartedBattleContext(); // 초기 배치를 마친 1턴 상태 생성

            try // 테스트 자원 정리를 보장
            {
                AdvanceToPeriodicDeploymentTurn(context.TurnManager); // 5턴 종료 후 배치 턴 진입

                int firstIndex = FindFirstNonKingIndex(context.RunState.Hand); // 첫 카드 위치
                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(firstIndex)); // 첫 카드 선택
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(2, 1))); // 첫 카드 배치
                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 계속 배치 턴

                int secondIndex = FindFirstNonKingIndex(context.RunState.Hand); // 제거 후 다시 첫 비킹 카드 탐색
                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(secondIndex)); // 두 번째 카드 선택
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(3, 1))); // 두 번째 카드 배치
                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 여전히 배치 턴
                Assert.That(context.TurnManager.DeployedCardCount, Is.EqualTo(2)); // 두 장 누적

                Assert.IsTrue(context.TurnManager.TryEndDeploymentTurn()); // 사용자가 배치 턴 종료
                Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 6턴 시작
                Assert.AreEqual(6, context.TurnManager.TurnNumber); // 다음 일반 턴 번호 확인
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }


        [Test] // 일반 플레이어 턴에 카드 1장을 소환하면 그 행동으로 턴이 즉시 종료되는지 검증
        public void PlayerTurn_SummonOneCard_ImmediatelyChangesToEnemyTurn()
        {
            var context = CreateStartedBattleContext(); // 시작 배치를 마친 1턴 PlayerTurn 상태 생성

            try // 테스트 자원 정리를 보장
            {
                int cardIndex = FindFirstNonKingIndex(context.RunState.Hand); // 일반 턴에 소환할 손패 카드 탐색
                Assert.That(cardIndex, Is.GreaterThanOrEqualTo(0)); // 소환 가능한 카드가 있어야 함
                int handBefore = context.RunState.Hand.Hand.Count; // 소환 전 손패 장수 저장

                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(cardIndex)); // 일반 PlayerTurn에서도 손패 카드 선택 가능
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(2, 2))); // 아군 영역 빈 칸에 카드 소환

                Assert.AreEqual(handBefore - 1, context.RunState.Hand.Hand.Count); // 카드 1장이 손패에서 소비돼야 함
                Assert.AreEqual(TurnState.EnemyTurn, context.TurnManager.CurrentState); // 소환 성공 즉시 적 턴으로 넘어가야 함
                Assert.IsNotNull(context.RunState.Board.GetTile(new Vector2Int(2, 2)).OccupyingPiece); // 소환한 기물이 보드에 존재해야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 적 턴에 적 손패 카드 1장을 소환하면 즉시 다음 턴으로 넘어가는지 검증
        public void EnemyTurn_SummonOneCard_ImmediatelyCompletesEnemyTurn()
        {
            var context = CreateStartedBattleContext(); // 1턴 PlayerTurn 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeEnemyStartingHand(); // 적 전용 프로토타입 손패 구성
                int enemyHandBefore = context.BoardInput.EnemyHandState.Hand.Count; // 적 소환 전 손패 장수 저장
                Assert.That(enemyHandBefore, Is.GreaterThan(0)); // 적 카드가 있어야 함

                Assert.IsTrue(context.TurnManager.TryCompletePlayerAction()); // 플레이어 행동을 끝내 EnemyTurn 진입
                Assert.IsTrue(context.BoardInput.TryEnemySummonOneCard()); // 적 카드 1장 자동 소환

                Assert.AreEqual(enemyHandBefore - 1, context.BoardInput.EnemyHandState.Hand.Count); // 적 카드 1장 소비
                Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 적 소환 성공 즉시 다음 PlayerTurn
                Assert.AreEqual(2, context.TurnManager.TurnNumber); // 2턴으로 증가
                Assert.That(context.RunState.Board.CountPieces(false), Is.GreaterThan(0)); // 적 진영 기물이 실제 보드에 존재
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 자유 배치 턴에서는 카드 소환이 일반 행동처럼 턴을 자동 종료시키지 않는지 검증
        public void DeploymentTurn_SummonCard_DoesNotAutoEndTurn()
        {
            var context = CreateBoundContext(); // 시작 배치 턴 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 플레이어 시작 손패 구성
                int kingIndex = FindCardIndex(context.RunState.Hand, context.Definitions[0]); // 필수 킹 위치 탐색

                Assert.IsTrue(context.BoardInput.TrySelectHandSlot(kingIndex)); // 킹 선택
                Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(0, 0))); // 킹 소환

                Assert.AreEqual(TurnState.DeploymentTurn, context.TurnManager.CurrentState); // 배치 성공만으로 턴 종료 금지
                Assert.IsTrue(context.TurnManager.IsInitialKingPlaced); // 킹 조건만 충족
                Assert.IsTrue(context.TurnManager.CanDeploy); // 계속 자유 배치 가능
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 19일차: 배치 턴에 손패 카드를 정리하면 손패에서 빠지고 드로우 더미 맨 아래로 이동하는지 검증
        public void DeploymentTurn_DiscardCard_RemovesFromHandAndAddsToDrawPileBottom()
        {
            var context = CreateBoundContext(); // 시작 배치 턴 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 5장 구성
                int nonKingIndex = FindFirstNonKingIndex(context.RunState.Hand); // 정리할 비킹 카드 위치 탐색
                var cardToDiscard = context.RunState.Hand.Hand[nonKingIndex]; // 정리 대상 카드 참조 저장
                int handCountBefore = context.RunState.Hand.Hand.Count; // 정리 전 손패 수 저장
                int drawCountBefore = context.RunState.Deck.DrawPile.Count; // 정리 전 드로우 더미 수 저장

                bool result = context.BoardInput.TryDiscardHandCardToBottom(cardToDiscard); // 실제 손패 정리 실행

                Assert.IsTrue(result); // 배치 턴에는 정리가 성공해야 함
                Assert.AreEqual(handCountBefore - 1, context.RunState.Hand.Hand.Count); // 손패 수가 1 줄어야 함
                Assert.AreEqual(drawCountBefore + 1, context.RunState.Deck.DrawPile.Count); // 드로우 더미 수가 1 늘어야 함
                Assert.IsFalse(context.RunState.Hand.Hand.Contains(cardToDiscard)); // 정리한 카드가 손패에 남아 있지 않아야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 19일차: 일반 PlayerTurn에는 손패 정리를 할 수 없는지 검증(배치 턴 전용 기능)
        public void PlayerTurn_DiscardCard_IsRejected()
        {
            var context = CreateStartedBattleContext(); // 킹 배치 후 1턴 PlayerTurn까지 진행한 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                int nonKingIndex = FindFirstNonKingIndex(context.RunState.Hand); // 손패의 비킹 카드 위치 탐색
                var card = context.RunState.Hand.Hand[nonKingIndex]; // 정리를 시도할 카드 참조 저장

                bool result = context.BoardInput.TryDiscardHandCardToBottom(card); // 일반 턴에 정리 시도

                Assert.IsFalse(result); // 배치 턴이 아니므로 거부돼야 함
                Assert.IsTrue(context.RunState.Hand.Hand.Contains(card)); // 손패에 카드가 그대로 남아 있어야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 19일차: 라운드 클리어 시 죽은 카드 더미가 보유 풀로 복귀하는지 BoardInputController 경로로 검증
        public void ReturnDeadPileToOwnedPool_ReturnsDeadCardsToOwnedPool()
        {
            var context = CreateBoundContext(); // 시작 배치 턴 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.RunState.Deck.MoveToDeadPile(context.Definitions[1]); // 전투 중 사망했다고 가정한 카드를 죽은 카드 더미에 직접 추가
                int ownedCountBefore = context.RunState.Deck.OwnedCardPool.Count; // 복귀 전 보유 풀 수 저장

                context.BoardInput.ReturnDeadPileToOwnedPool(); // 실제 라운드 클리어 복귀 진입점 실행

                Assert.AreEqual(0, context.RunState.Deck.DeadCardPile.Count); // 죽은 카드 더미가 비워져야 함
                Assert.AreEqual(ownedCountBefore + 1, context.RunState.Deck.OwnedCardPool.Count); // 보유 풀 수가 1 늘어야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 20일차: 적도 플레이어와 동일한 보유 풀→드로우 더미→손패 구조로 시작 덱이 구성되는지 검증
        public void EnsurePrototypeEnemyStartingHand_BuildsDeckAndInitialHand()
        {
            var context = CreateBoundContext(); // 시작 배치 턴 상태 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeEnemyStartingHand(); // 적 시작 덱·손패 구성

                Assert.AreEqual(5, context.BoardInput.EnemyDeck.OwnedCardPool.Count); // 폰/나이트/비숍/룩/퀸 5종이 보유 풀에 등록돼야 함
                Assert.AreEqual(3, context.BoardInput.EnemyHandState.Hand.Count); // 초기 손패는 3장만 뽑아야 함
                Assert.AreEqual(2, context.BoardInput.EnemyDeck.DrawPile.Count); // 나머지 2장은 드로우 더미에 남아야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 20일차: 적 손패가 비면 드로우 더미에서 자동으로 다시 채워 계속 소환할 수 있는지 검증
        public void TryEnemySummonOneCard_RefillsHandFromDrawPile_WhenHandEmpty()
        {
            var context = CreateStartedBattleContext(); // 킹 배치 후 1턴 PlayerTurn까지 진행한 컨텍스트 생성

            try // 테스트 자원 정리를 보장
            {
                context.BoardInput.EnsurePrototypeEnemyStartingHand(); // 적 시작 덱·손패 구성(손패 3장, 드로우 더미 2장)

                for (int i = 0; i < 3; i++) // 초기 손패 3장을 모두 소환으로 소비
                {
                    Assert.IsTrue(context.TurnManager.TryCompletePlayerAction()); // 플레이어 행동 완료 -> 적 턴
                    Assert.IsTrue(context.BoardInput.TryEnemySummonOneCard()); // 적이 손패 카드로 소환
                }

                Assert.AreEqual(0, context.BoardInput.EnemyHandState.Hand.Count); // 초기 손패 3장을 모두 사용해 손패가 비어야 함
                Assert.AreEqual(2, context.BoardInput.EnemyDeck.DrawPile.Count); // 아직 드로우 더미는 그대로 남아 있어야 함

                Assert.IsTrue(context.TurnManager.TryCompletePlayerAction()); // 다음 적 턴 진입
                bool summonedAfterRefill = context.BoardInput.TryEnemySummonOneCard(); // 손패가 비어도 자동으로 드로우 더미에서 채워 소환 시도

                Assert.IsTrue(summonedAfterRefill); // 드로우 더미가 남아 있으므로 소환이 성공해야 함
                Assert.AreEqual(1, context.BoardInput.EnemyDeck.DrawPile.Count); // 드로우 더미에서 1장을 손패로 옮겨 바로 소환에 사용해야 함
                Assert.AreEqual(4, context.RunState.Board.CountPieces(isPlayerPiece: false)); // 지금까지 적이 총 4기를 소환했어야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
            }
        }

        [Test] // 20일차: 적 기물이 죽으면 카드가 적의 죽은 카드 더미로 이동하는지 검증
        public void EnemyPieceDeath_MovesCardToEnemyDeadPile()
        {
            var context = CreateStartedBattleContext(); // 킹 배치 후 1턴 PlayerTurn까지 진행한 컨텍스트 생성
            PieceDefinition enemyDefinition = null; // finally에서 정리할 별도 생성 정의 참조

            try // 테스트 자원 정리를 보장
            {
                var attackerDefinition = context.Definitions[1]; // 테스트용 폰 정의를 공격자로 사용
                SetPrivateField(attackerDefinition, "_baseAtk", 5); // 한 방에 처치 가능한 공격력 부여
                enemyDefinition = ScriptableObject.CreateInstance<PieceDefinition>(); // 별도의 적 전용 기물 정의 생성
                SetPrivateField(enemyDefinition, "_baseHp", 1); // 공격 한 번에 사망하도록 HP 1 부여
                SetPrivateField(enemyDefinition, "_movementType", PieceMovementType.Pawn); // 폰의 전방 공격 범위로 검증

                var attackerPosition = new Vector2Int(4, 1); // 공격자 좌표(아군 영역)
                var enemyPosition = new Vector2Int(4, 2); // 대상 좌표(공격자 바로 앞)
                var attacker = new PieceRuntimeState(attackerDefinition, attackerPosition, isPlayerPiece: true); // 아군 공격자 생성
                var enemy = new PieceRuntimeState(enemyDefinition, enemyPosition, isPlayerPiece: false); // 적 대상 생성
                context.RunState.Board.GetTile(attackerPosition).OccupyingPiece = attacker; // 보드에 공격자 배치
                context.RunState.Board.GetTile(enemyPosition).OccupyingPiece = enemy; // 보드에 적 배치

                Assert.IsTrue(context.BoardInput.TrySelectPieceAt(attackerPosition)); // 공격자 선택
                Assert.IsTrue(context.BoardInput.TryAttackSelectedPieceTarget(enemyPosition)); // 적 처치

                Assert.AreEqual(1, context.BoardInput.EnemyDeck.DeadCardPile.Count); // 적 죽은 카드 더미에 1장이 들어가야 함
                Assert.AreSame(enemyDefinition, context.BoardInput.EnemyDeck.DeadCardPile[0]); // 처치된 카드와 정확히 같은 정의여야 함
                Assert.AreEqual(0, context.RunState.Deck.DeadCardPile.Count); // 아군 죽은 카드 더미는 영향받지 않아야 함
            }
            finally // 성공/실패와 무관하게 정리
            {
                context.Dispose(); // 테스트 객체 제거
                if (enemyDefinition != null) Object.DestroyImmediate(enemyDefinition); // 별도로 생성한 적 정의도 정리
            }
        }

        private static TestContext CreateStartedBattleContext() // 킹을 놓고 시작 배치 턴까지 종료한 컨텍스트 생성
        {
            var context = CreateBoundContext(); // 시작 배치 상태 생성
            context.BoardInput.EnsurePrototypeStartingHand(); // 시작 손패 구성
            int kingIndex = FindCardIndex(context.RunState.Hand, context.Definitions[0]); // 킹 위치 탐색
            Assert.IsTrue(context.BoardInput.TrySelectHandSlot(kingIndex)); // 킹 선택
            Assert.IsTrue(context.BoardInput.TryDeploySelectedCardTo(new Vector2Int(0, 0))); // 킹 배치
            Assert.IsTrue(context.TurnManager.TryEndDeploymentTurn()); // 배치 턴 명시적 종료
            Assert.AreEqual(TurnState.PlayerTurn, context.TurnManager.CurrentState); // 1턴 시작 확인
            return context; // 준비된 컨텍스트 반환
        }

        private static void AdvanceToPeriodicDeploymentTurn(TurnManager turnManager) // 1턴부터 첫 주기 배치 턴까지 진행하는 보조 메서드
        {
            while (turnManager.TurnNumber < 5) // 5턴까지 반복
            {
                Assert.IsTrue(turnManager.TryCompletePlayerAction()); // 플레이어 행동 완료
                Assert.IsTrue(turnManager.CompleteEnemyTurn()); // 적 행동 완료
            }

            Assert.IsTrue(turnManager.TryCompletePlayerAction()); // 5턴 플레이어 행동 완료
            Assert.IsTrue(turnManager.CompleteEnemyTurn()); // 주기 배치 턴 진입
            Assert.AreEqual(TurnState.DeploymentTurn, turnManager.CurrentState); // 배치 턴 확인
            Assert.IsFalse(turnManager.IsInitialDeployment); // 시작 배치가 아님을 확인
        }

        private static int FindCardIndex(HandState hand, PieceDefinition card) // 손패에서 특정 카드 위치를 찾는 보조 메서드
        {
            for (int i = 0; i < hand.Hand.Count; i++) // 손패 순회
            {
                if (hand.Hand[i] == card) return i; // 같은 카드면 인덱스 반환
            }

            return -1; // 없으면 실패값
        }

        private static int FindFirstNonKingIndex(HandState hand) // 손패에서 첫 비킹 카드 위치를 찾는 보조 메서드
        {
            for (int i = 0; i < hand.Hand.Count; i++) // 손패 순회
            {
                if (hand.Hand[i].MovementType != PieceMovementType.King) return i; // 비킹 카드 위치 반환
            }

            return -1; // 없으면 실패값
        }

        private static TestContext CreateBoundContext() // 공통 테스트 객체를 만드는 메서드
        {
            var root = new GameObject("CardFlowTest"); // 테스트 루트 생성
            var boardView = root.AddComponent<BoardView>(); // 보드 뷰 추가
            var boardInput = root.AddComponent<BoardInputController>(); // 보드 입력 추가
            var runState = new RunState(3); // 실제 런 상태 생성
            var turnManager = new TurnManager(); // 시작 배치 턴으로 생성
            var definitions = new PieceDefinition[6]; // 기본 6종 테스트 카드 배열
            var movementTypes = new[] // 각 카드 이동 타입
            {
                PieceMovementType.King, // 킹
                PieceMovementType.Pawn, // 폰
                PieceMovementType.Knight, // 나이트
                PieceMovementType.Bishop, // 비숍
                PieceMovementType.Rook, // 룩
                PieceMovementType.Queen // 퀸
            };

            for (int i = 0; i < definitions.Length; i++) // 테스트 카드 생성
            {
                definitions[i] = ScriptableObject.CreateInstance<PieceDefinition>(); // 카드 인스턴스 생성
                SetPrivateField(definitions[i], "_movementType", movementTypes[i]); // 이동 타입 주입
                SetPrivateField(definitions[i], "_displayName", movementTypes[i].ToString()); // 표시 이름 주입
            }

            SetPrivateField(boardInput, "_kingDefinition", definitions[0]); // 킹 정의 연결
            SetPrivateField(boardInput, "_pawnDefinition", definitions[1]); // 폰 정의 연결
            SetPrivateField(boardInput, "_knightDefinition", definitions[2]); // 나이트 정의 연결
            SetPrivateField(boardInput, "_bishopDefinition", definitions[3]); // 비숍 정의 연결
            SetPrivateField(boardInput, "_rookDefinition", definitions[4]); // 룩 정의 연결
            SetPrivateField(boardInput, "_queenDefinition", definitions[5]); // 퀸 정의 연결

            boardView.Bind(runState.Board); // 보드 상태 연결
            boardInput.Bind(runState, boardView, turnManager); // 입력 상태 연결

            return new TestContext(root, boardInput, runState, turnManager, definitions); // 컨텍스트 반환
        }

        private static void SetPrivateField(object target, string fieldName, object value) // private 필드 테스트 주입 보조 메서드
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic); // 필드 탐색
            Assert.IsNotNull(field, $"필드 {fieldName}을 찾을 수 없습니다."); // 필드 존재 검증
            field.SetValue(target, value); // 값 주입
        }

        private sealed class TestContext // 테스트 객체와 정리 책임을 묶는 내부 클래스
        {
            public GameObject Root { get; } // 테스트 루트
            public BoardInputController BoardInput { get; } // 입력 컨트롤러
            public RunState RunState { get; } // 런 상태
            public TurnManager TurnManager { get; } // 턴 매니저
            public PieceDefinition[] Definitions { get; } // 테스트 카드 배열

            public TestContext(GameObject root, BoardInputController boardInput, RunState runState, TurnManager turnManager, PieceDefinition[] definitions) // 생성자
            {
                Root = root; // 루트 저장
                BoardInput = boardInput; // 입력 저장
                RunState = runState; // 런 저장
                TurnManager = turnManager; // 턴 저장
                Definitions = definitions; // 카드 저장
            }

            public void Dispose() // 테스트 객체 정리
            {
                Object.DestroyImmediate(Root); // GameObject 제거
                foreach (var definition in Definitions) // 카드 순회
                {
                    if (definition != null) Object.DestroyImmediate(definition); // ScriptableObject 제거
                }
            }
        }
    }
}
