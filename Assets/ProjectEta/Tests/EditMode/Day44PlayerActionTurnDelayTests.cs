using NUnit.Framework; // EditMode 테스트·Assert 사용
using ProjectEta.Battle; // TurnManager·TurnState 사용

namespace ProjectEta.Tests.EditMode // EditMode 테스트 네임스페이스
{
    public class Day44PlayerActionTurnDelayTests // 플레이어 연출 완료 전 적 턴 진입 차단 회귀 테스트
    {
        [Test] // 기존 비지연 동작 호환 검증
        public void PlayerAction_WithoutDeferredMode_ImmediatelyStartsEnemyTurn() // 기본 TurnManager 동작 유지 확인
        {
            var turnManager = CreatePlayerTurn(); // 플레이어 일반 턴 생성

            Assert.IsTrue(turnManager.TryCompletePlayerAction()); // 일반 행동 완료 처리
            Assert.AreEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 기존 즉시 적 턴 전환 검증
            Assert.IsFalse(turnManager.IsPlayerActionTransitionPending); // 지연 대기 없음 검증
        }

        [Test] // 런타임 지연 모드에서 적 턴 전환 보류 검증
        public void PlayerAction_WithDeferredMode_RemainsPlayerTurnUntilReleased() // 공격 복귀 중 적 행동 차단 확인
        {
            var turnManager = CreatePlayerTurn(); // 플레이어 일반 턴 생성
            turnManager.SetPlayerActionTransitionDeferred(true); // 연출 완료 대기 모드 활성화

            Assert.IsTrue(turnManager.TryCompletePlayerAction()); // 플레이어 행동 완료 예약
            Assert.AreEqual(TurnState.PlayerTurn, turnManager.CurrentState); // 연출 중 PlayerTurn 상태 유지 검증
            Assert.IsTrue(turnManager.HasPlayerActed); // 추가 입력 차단용 행동 완료 상태 검증
            Assert.IsFalse(turnManager.CanPlayerAct); // 연출 중 중복 행동 차단 검증
            Assert.IsTrue(turnManager.IsPlayerActionTransitionPending); // 적 턴 전환 대기 상태 검증
        }

        [Test] // 연출 종료 후 실제 적 턴 진입 검증
        public void ReleaseDeferredPlayerAction_AfterVisual_StartsEnemyTurn() // 복귀 완료 뒤 적 턴 전환 확인
        {
            var turnManager = CreatePlayerTurn(); // 플레이어 일반 턴 생성
            turnManager.SetPlayerActionTransitionDeferred(true); // 연출 완료 대기 모드 활성화
            turnManager.TryCompletePlayerAction(); // 플레이어 행동 완료 예약

            Assert.IsTrue(turnManager.ReleaseDeferredPlayerActionTransition()); // 연출 완료 신호 전달
            Assert.AreEqual(TurnState.EnemyTurn, turnManager.CurrentState); // 적 턴 진입 검증
            Assert.IsFalse(turnManager.IsPlayerActionTransitionPending); // 대기 상태 해제 검증
        }

        [Test] // 연출 중 전투 종료 시 늦은 적 턴 진입 차단 검증
        public void EndBattle_WhileDeferred_CancelsPendingEnemyTurn() // 승패 처리 후 지연 콜백 안전성 확인
        {
            var turnManager = CreatePlayerTurn(); // 플레이어 일반 턴 생성
            turnManager.SetPlayerActionTransitionDeferred(true); // 연출 완료 대기 모드 활성화
            turnManager.TryCompletePlayerAction(); // 플레이어 행동 완료 예약

            turnManager.EndBattle(BattleOutcome.Victory); // 연출 중 전투 승리 처리

            Assert.AreEqual(TurnState.BattleEnded, turnManager.CurrentState); // 전투 종료 상태 검증
            Assert.IsFalse(turnManager.IsPlayerActionTransitionPending); // 적 턴 대기 취소 검증
            Assert.IsFalse(turnManager.ReleaseDeferredPlayerActionTransition()); // 늦은 연출 완료 신호 무시 검증
        }

        private static TurnManager CreatePlayerTurn() // 시작 배치에서 일반 PlayerTurn까지 이동하는 테스트 도우미
        {
            var turnManager = new TurnManager(); // 새 전투 턴 상태 생성
            turnManager.MarkInitialKingPlaced(); // 시작 킹 배치 조건 충족
            turnManager.TryEndDeploymentTurn(); // 첫 일반 플레이어 턴 진입
            return turnManager; // 준비된 턴 매니저 반환
        }
    }
}
